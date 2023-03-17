using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCatSysManagerLib;
using EnvDTE;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using TwinCat_Motion_ADS.APPLICATION_WINDOWS;
using System.IO;
using System.Windows;
using System.Xml;
using System.Diagnostics.Eventing.Reader;

namespace TwinCat_Motion_ADS
{
    public class TwinCatAutomation
    {
        #region PROPERTIES

        private DTE _ActiveDTE;
        public DTE ActiveDTE
        {
            get { return _ActiveDTE; }
            set { _ActiveDTE = value; }
        }

        private string _configFolder;
        public string ConfigFolder
        {
            get { return _configFolder ?? (_configFolder = SolutionFolderPath + @"\Config"); }
            set
            {
                _configFolder = value;
                Directory.CreateDirectory(_configFolder);
            }
        }
        private string _solutionFolderPath;
        public string SolutionFolderPath
        {
            get { return _solutionFolderPath; }
            set { _solutionFolderPath = value; }
        }

        private ITcSysManager15 SystemManager;
        private ITcConfigManager ConfigManager;
        private Solution solution;
        private Project solutionProject;
        private ITcSmTreeItem9 NcItem;
        private ITcSmTreeItem9 AxesItem;
        private ITcSmTreeItem9 IoItem;
        private ITcSmTreeItem9 PlcItem;
        private ITcSmTreeItem PlcProjectItem;
        private ITcSmTreeItem PlcProjectProjectItem;

        private int COMMAND_TIMEOUT = 30000;

        private const string LOGGEDIN_XML_NODE = "LoggedIn";
        private const string PLCAPPSTATE_XML_NODE = "PlcAppState";
        private const string PLCPROJECT_PROJECT_ITEM_STRING = "TIPC^tc_project_app^tc_project_app Project";
        private const string PLCPROJECT_ITEM_STRING = "TIPC^tc_project_app";

        //Config constants
        private const string PLC_DIRECTORY_SUFFIX = @"\plc";
        private const string DECLARATION_DIRECTORY_SUFFIX = @"\declarations";
        private const string IMPLEMENTATION_DIRECTORY_SUFFIX = @"\implementations";
        private const string AXES_DIRECTORY_SUFFIX = @"\axes";
        private const string APPLICATION_DIRECTORY_SUFFIX = @"\applications";
        private const string AXES_XML_DIRECTORY_SUFFIX = @"\axisXmls";
        private const string MAPPINGS_FILE = @"\mappings.xml";


        #endregion

        #region DTE METHODS
        public DTE AttachToExistingDte(string solutionPath, string progId)
        {
            if (!MessageFilter.IsRegistered)
                MessageFilter.Register();
            DTE dte = null;
            Hashtable dteInstances = GetIDEInstances(false, VSVersion.ReturnVersion(progId));
            IDictionaryEnumerator hashtableEnumerator = dteInstances.GetEnumerator();
            while (hashtableEnumerator.MoveNext())
            {
                DTE dteTemp = hashtableEnumerator.Value as EnvDTE.DTE;
                //Console.WriteLine(dteTemp.FullName);
                //Console.WriteLine(dteTemp.Solution.FullName);
                if (dteTemp.Solution.FullName == solutionPath)
                {
                    Console.WriteLine("Found solution in list of all open DTE objects. " + dteTemp.Name); 
                    dte = dteTemp;
                }
            }
            SolutionFolderPath = new FileInfo(solutionPath).Directory.FullName;
            ActiveDTE = dte;
            return dte;
        }

        public DTE CreateNewDTE(string solutionPath, string appID, bool ideVisible, bool suppressUI, bool userControl)
        {
            if (!MessageFilter.IsRegistered)
                MessageFilter.Register();

            Type tp = Type.GetTypeFromProgID(VSVersion.ReturnVersion(appID));
            if (tp == null)
                throw new ApplicationException($"AppID '{appID}' not found!");
            dynamic dteDyn = System.Activator.CreateInstance(tp, true);
            dteDyn.MainWindow.WindowState = 0;
            dteDyn.MainWindow.Visible = ideVisible;
            dteDyn.SuppressUI = suppressUI;
            dteDyn.UserControl = userControl;
            DTE dte = dteDyn;
            dte.Solution.Open(solutionPath);
            SolutionFolderPath = new FileInfo(solutionPath).Directory.FullName;
            ActiveDTE = dte;
            return dte;
        }

        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        public static Hashtable GetRunningObjectTable()
        {
            Hashtable result = new Hashtable();
            IntPtr numFetched = new IntPtr();
            IRunningObjectTable runningObjectTable;
            IEnumMoniker monikerEnumerator;
            IMoniker[] monikers = new IMoniker[1];
            GetRunningObjectTable(0, out runningObjectTable);
            runningObjectTable.EnumRunning(out monikerEnumerator);
            monikerEnumerator.Reset();
            while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
            {
                IBindCtx ctx;
                CreateBindCtx(0, out ctx);
                string runningObjectName;
                monikers[0].GetDisplayName(ctx, null, out runningObjectName);
                object runningObjectVal;
                runningObjectTable.GetObject(monikers[0], out runningObjectVal);
                result[runningObjectName] = runningObjectVal;
            }
            return result;
        }
        public static Hashtable GetIDEInstances(bool openSolutionsOnly, string progId)
        {
            Hashtable runningIDEInstances = new Hashtable();
            Hashtable runningObjects = GetRunningObjectTable();
            IDictionaryEnumerator rotEnumerator = runningObjects.GetEnumerator();
            while (rotEnumerator.MoveNext())
            {
                string candidateName = (string)rotEnumerator.Key;
                if (!candidateName.StartsWith("!" + progId))
                    continue;

                DTE ide = rotEnumerator.Value as DTE;
                if (ide == null)
                    continue;
                if (openSolutionsOnly)
                {
                    try
                    {
                        string solutionFile = ide.Solution.FullName;
                        if (solutionFile != String.Empty)
                            runningIDEInstances[candidateName] = ide;
                    }
                    catch { }
                }
                else
                    runningIDEInstances[candidateName] = ide;
            }
            return runningIDEInstances;
        }


        #endregion

        #region PLC Command
        private string CreatePlcCommandXml(PLCAction action)
        {
            List<bool> options = Enumerable.Repeat(false, 6).ToList();
            options[(int)action] = true;
            List<String> strOptions = options.ConvertAll(o => o.ToString().ToLowerInvariant());
            return String.Format(xmlTemplate, strOptions.ToArray());
        }

        private static string xmlTemplate = @"<TreeItem>
                                    <IECProjectDef>
                                        <OnlineSettings>
                                                <Commands>
                                                        <LoginCmd>{0}</LoginCmd>
                                                        <LogoutCmd>{1}</LogoutCmd>
                                                        <StartCmd>{2}</StartCmd>
                                                        <StopCmd>{3}</StopCmd>
                                                        <ResetColdCmd>{4}</ResetColdCmd>
                                                        <ResetOriginCmd>{5}</ResetOriginCmd>
                                                </Commands>
                                        </OnlineSettings>
                                    </IECProjectDef>
                                </TreeItem>";

        enum PLCAction
        {
            LOGIN = 0, LOGOUT = 1, START = 2, STOP = 3, RESET_COLD = 4, RESET_ORIGIN = 5
        }

        //Produces xml for the plc object, selects a specific node based on "tag", and returns if "expected" matches value
        private bool CheckStringMatchesXml(ITcSmTreeItem plcProjectItem, String tag, String expected)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(plcProjectItem.ProduceXml(true));
            XmlNodeList loggedIn = doc.SelectNodes(String.Format("//{0}", tag));
            return loggedIn.Item(0).InnerText.Equals(expected);
        }

        #endregion

        #region PLC Actions

        public bool LoginToPlc()
        {
            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.LOGIN));

            //Confirm action was successful
            if (!CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, LOGGEDIN_XML_NODE, "true")))
            {
                Console.WriteLine("Failed to login");
                return false;
            }
            return true;
        }

        public bool LogoutOfPlc()
        {
            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.LOGOUT));

            //Confirm action was successful
            if (!CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, LOGGEDIN_XML_NODE, "false")))
            {
                Console.WriteLine("Failed to logout");
                return false;
            }
            return true;
        }

        public bool StartPlc()
        {

            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.START));

            //Confirm action was successful
            if (!CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, PLCAPPSTATE_XML_NODE, "Run")))
            {
                Console.WriteLine("Failed to start");
                return false;
            }
            return true;
        }

        public bool StopPlc()
        {

            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.STOP));

            //Confirm action was successful
            if (!CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, PLCAPPSTATE_XML_NODE, "Stop")))
            {
                Console.WriteLine("Failed to stop");
                return false;
            }
            return true;
        }

        private void SetProjectToBoot()
        {
            ITcPlcProject plcProject = (ITcPlcProject)PlcProjectItem;
            plcProject.BootProjectAutostart = true;
            plcProject.GenerateBootProject(true);
        }

        #endregion

        public void SetupSytem()
        {
            Console.WriteLine("Current Config folder: " + ConfigFolder);            
            if (ActiveDTE == null) return;
            SetupSolutionObjects();
            ConfigManager.ActiveTargetPlatform = "TwinCAT RT (x64)";

            //NC Setup
            try
            {
                //Look for existing NC Task
                SystemManager.LookupTreeItem("TINC").Child[1].LookupChild("Axes");                
            }
            catch
            {
                //Add NC Task
                NcItem.CreateChild("NC-Task 1", 1);
            }
            //Create axes item
            AxesItem = (ITcSmTreeItem9)NcItem.Child[1].LookupChild("Axes");

            if (AxesItem.ChildCount != 0)
            {
                MessageBoxResult dialogResult = MessageBox.Show("Axes already exist in this solution. Do you want to remove them?", "NC Axes detected", MessageBoxButton.YesNoCancel);


                if (dialogResult == MessageBoxResult.Cancel)
                {
                    MessageFilter.Revoke();
                    return;
                }
                if (dialogResult == MessageBoxResult.Yes)
                {
                    DeleteNcAxes();
                }
            }

            if (!addNamedNcAxes())
            {
                Console.WriteLine("Issue adding axes");
                return;
            }

            //IO Handling
            if (IoItem.ChildCount != 0)
            {

                MessageBoxResult dialogResult = MessageBox.Show("Hardware already exists in this solution. Do you want to remove this?", "Time for a break", MessageBoxButton.YesNoCancel);

                if (dialogResult == MessageBoxResult.Cancel)
                {
                    MessageFilter.Revoke();
                    return;
                }
                if (dialogResult == MessageBoxResult.Yes)
                {
                    DeleteIoItems();
                }
            }

            Console.WriteLine("All axes and io removed");

            //Add the IO from a CSV file
            importIoList();
            Console.WriteLine("IO devices imported");
            //Run through all device xmls and import
            importAllIoXmls();
            Console.WriteLine("IO import complete");
            //Setup axis parameters from available axis xmls
            ncConsumeAllMaps();
            Console.WriteLine("NC Parameter import complete");
            //Add the plc "stuff"
            plcImportDeclarations();
            Console.WriteLine("PLC declarations updated");
            //New PLC "stuff" to add
            importApplications();
            Console.WriteLine("Application Specific PROGs imported");
            importAxes();
            Console.WriteLine("Axis PROGs imported");
            setupProgAction();
            Console.WriteLine("PROG action updated");

            buildPlcProject();
            Console.WriteLine("PLC compiled");

            importXmlMap();
            Console.WriteLine("Import mappings complete");

            saveAs();

            SetProjectToBoot();
            Console.WriteLine("Project autostart set");


            try
            {
                SystemManager.ActivateConfiguration();
                System.Threading.Thread.Sleep(1000);
            }
            catch
            {
                throw new ApplicationException("Unable to activate configuration");
            }
            try
            {
                SystemManager.StartRestartTwinCAT();
                System.Threading.Thread.Sleep(1000);
            }
            catch
            {
                throw new ApplicationException("Issue starting controller");
            }
            Console.WriteLine(SystemManager.IsTwinCATStarted());
            if (SystemManager.IsTwinCATStarted())
            {
                MessageBox.Show("TwinCAT is running");
                //plcLogin();
                //System.Threading.Thread.Sleep(1000);
                //plcStart();
                //System.Threading.Thread.Sleep(1000);
            }

            Console.WriteLine("Success!");
            Console.WriteLine(SystemManager.IsTwinCATStarted());
        }

        
        
        
        
        
        //Should probably change to ASYNC
        private bool CheckWithTimeout(int timeout, Func<Boolean> checkMethod)
        {
            const int TIME_BETWEEN_CHECKS = 500;
            for (int i = 0; i < timeout / TIME_BETWEEN_CHECKS; i++)
            {
                if (checkMethod())
                {
                    return true;
                }
                //this.utils.printErrors();
                System.Threading.Thread.Sleep(TIME_BETWEEN_CHECKS);
            }
            return checkMethod();
        }
        
       

        

        public void RevokeFilter()
        {
            MessageFilter.Revoke();
        }

        public bool saveAs()
        {
            string solutionName = Path.GetFileNameWithoutExtension(SolutionFolderPath);
            try
            {
                solution.SaveAs(SolutionFolderPath + @"\solution.sln");
                //solution.SaveAs(SolutionFolderPath + @"\" + solutionName + @"\" + solutionName + ".sln");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool importXmlMap()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFolder + MAPPINGS_FILE);
                SystemManager.ConsumeMappingInfo(xmlDoc.OuterXml);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void buildPlcProject()
        {
            string plcPath;
            string solutionName;
            solutionName = Path.GetFileNameWithoutExtension(SolutionFolderPath);
            //solutionName = new FileInfo(SlnPath).Name;
            plcPath = SolutionFolderPath +  @"\solution\" + PlcItem.Child[1].Name + @"\" + PlcItem.Child[1].Name + @".plcproj";
            solution.SolutionBuild.BuildProject("Release|TwinCAT RT (x64)", plcPath, true);
        }

        public void setupProgAction()
        {
            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager.LookupTreeItem("TIPC^tc_project_app^tc_project_app Project^POUs^MAIN^PROG");
            }
            catch
            {
                throw new ApplicationException($"Unable to find PROG action in solution");
            }
            ITcPlcImplementation plcImp;
            plcImp = (ITcPlcImplementation)plcItem;

            //Need to build up the string
            string impText = "";
            //Axes first
            ITcSmTreeItem axesPlcItem;
            try
            {
                axesPlcItem = SystemManager.LookupTreeItem("TIPC^tc_project_app^tc_project_app Project^POUs^Application_Specific^Axes");
            }
            catch
            {
                throw new ApplicationException($"Couldn't find axes folder");
            }
            for (int i = 1; i < axesPlcItem.ChildCount + 1; i++)
            {
                impText += axesPlcItem.Child[i].Name + @"();" + Environment.NewLine;
            }
            //Now Applications
            ITcSmTreeItem appsPlcItem;
            try
            {
                appsPlcItem = SystemManager.LookupTreeItem("TIPC^tc_project_app^tc_project_app Project^POUs^Application_Specific^Applications");
            }
            catch
            {
                throw new ApplicationException($"Couldn't find applications folder");
            }
            for (int i = 1; i < appsPlcItem.ChildCount + 1; i++)
            {
                impText += appsPlcItem.Child[i].Name + @"();" + Environment.NewLine;
            }
            plcImp.ImplementationText = impText;

        }

        public void importAxes()
        {
            String path = ConfigFolder + PLC_DIRECTORY_SUFFIX + AXES_DIRECTORY_SUFFIX;
            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager.LookupTreeItem("TIPC^tc_project_app^tc_project_app Project^POUs^Application_Specific^Axes");
            }
            catch
            {
                throw new ApplicationException($"Unable to find Axes Application Specific directory in solution");
            }
            //Clear the folder in the solution explorer
            if (plcItem.ChildCount > 0)
            {
                while (plcItem.ChildCount > 0)
                {
                    try
                    {
                        plcItem.DeleteChild(plcItem.Child[1].Name);
                    }
                    catch
                    {
                        throw new ApplicationException("Unable to delete Axis PLC Item");
                    }
                }
            }
            //Import all the items
            foreach (string filePath in Directory.GetFiles(path))
            {

                plcItem.CreateChild(Path.GetFileNameWithoutExtension(filePath), 58, null, filePath);
            }
        }

        public void importApplications()
        {
            string path = ConfigFolder + PLC_DIRECTORY_SUFFIX + APPLICATION_DIRECTORY_SUFFIX;
            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager.LookupTreeItem("TIPC^tc_project_app^tc_project_app Project^POUs^Application_Specific^Applications");
            }
            catch
            {
                throw new ApplicationException($"ERROR HEREUnable to find Axes Application Specific directory in solution");
            }
            //Clear the folder in the solution explorer
            if (plcItem.ChildCount > 0)
            {
                while (plcItem.ChildCount > 0)
                {
                    try
                    {
                        plcItem.DeleteChild(plcItem.Child[1].Name);
                    }
                    catch
                    {
                        throw new ApplicationException("Error, Unable to delete Application_Specific PLC Item");
                    }
                }
            }
            //Import all the items
            if (Directory.Exists(path))
            {
                foreach (string filePath in Directory.GetFiles(path))
                {
                    plcItem.CreateChild(Path.GetFileNameWithoutExtension(filePath), 58, null, filePath);
                }
            }
            else
            {
                Console.WriteLine($"WARNING, application folder {path} does not exist");
            }
        }

        public void plcImportDeclarations()
        {
            string directoryPath = ConfigFolder + PLC_DIRECTORY_SUFFIX + DECLARATION_DIRECTORY_SUFFIX;
            if (!Directory.Exists(directoryPath))
            {
                throw new ApplicationException($"Folder not found {directoryPath}");
            }
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                modifyDeclaration(file);
            }
        }

        public void modifyDeclaration(string decFile)
        {
            //check file exists
            if (!File.Exists(decFile))
            {
                throw new ApplicationException($"PLC file {decFile} could not be found.");
            }
            string plcItemName = File.ReadLines(decFile).First();
            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager.LookupTreeItem("TIPC^" + plcItemName);
            }
            catch
            {
                throw new ApplicationException($"Unable to find item {plcItemName}");
            }
            ITcPlcDeclaration plcItemDec;
            try
            {
                plcItemDec = (ITcPlcDeclaration)plcItem;
            }
            catch
            {
                throw new ApplicationException($"Unable to create declaration field for item {plcItemName}");
            }

            string declarationText = "";
            int lineCount = File.ReadLines(decFile).Count();
            for (int i = 2; i < lineCount; i++)
            {
                declarationText += Environment.NewLine + File.ReadLines(decFile).ElementAt(i);
            }

            if (File.ReadLines(decFile).ElementAt(1) == "add")
            {
                string existingText = plcItemDec.DeclarationText;
                plcItemDec.DeclarationText = existingText + declarationText;
            }
            else if (File.ReadLines(decFile).ElementAt(1) == "replace")
            {
                plcItemDec.DeclarationText = declarationText;
            }
            else
            {
                throw new ApplicationException("No valid add/replace method found in text file");
            }
        }

        

        public void ncConsumeAllMaps()
        {
            string axisFolder = ConfigFolder + AXES_XML_DIRECTORY_SUFFIX;
            if (!Directory.Exists(axisFolder))
            {
                throw new ApplicationException($"Folder not found: {axisFolder}");
            }

            foreach (string file in Directory.GetFiles(axisFolder))
            {
                ncAxisMapSearchConsume(file);
            }
        }


        public void ncAxisMapSearchConsume(string xmlFile)
        {
            ITcSmTreeItem axis;
            XmlDocument axisXml = new XmlDocument();
            if (!File.Exists(xmlFile))
            {
                throw new ApplicationException($"IO Xml file {xmlFile} could not be found.");
            }
            string axisName = Path.GetFileNameWithoutExtension(xmlFile);
            try
            {
                axis = AxesItem.LookupChild(axisName);
            }
            catch
            {
                throw new ApplicationException($"Not able to find {axisName}.");
            }
            axisXml.Load(xmlFile);
            try
            {
                axis.ConsumeXml(axisXml.OuterXml);
            }
            catch
            {
                throw new ApplicationException($"Unable to consume xml for {axisName}");
            }
        }

        public void importAllIoXmls()
        {
            string deviceFolder = ConfigFolder + @"\deviceXmls\";
            if (!Directory.Exists(deviceFolder))
            {
                Console.WriteLine($"WARNING: Folder not found: {deviceFolder}, No devices will be loaded");
            }
            else
            {
                foreach (string file in Directory.GetFiles(deviceFolder))
                {
                    importIoXmls(file);
                }
            }
        }
        
        public void importIoXmls(string xmlFile)
        {
            if (!File.Exists(xmlFile))
            {
                throw new ApplicationException($"IO Xml file {xmlFile} could not be found.");
            }

            string deviceName = Path.GetFileNameWithoutExtension(xmlFile);
            ITcSmTreeItem currentIo = null;

            //Need to look for a match "somewhere"
            //Top level first
            try
            {
                currentIo = IoItem.LookupChild(deviceName);
            }
            catch
            {
                //do nothing
            }
            //If not found, search next level
            if (currentIo == null)
            {
                for (int i = 1; i <= IoItem.ChildCount; i++)
                {
                    try
                    {
                        currentIo = IoItem.Child[i].LookupChild(deviceName);
                        if (currentIo != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        //do nothing, go for next run
                    }
                }
            }
            //If still not found, go for next level
            if (currentIo == null)
            {
                for (int i = 1; i <= IoItem.ChildCount; i++)
                {
                    for (int j = 1; j <= IoItem.Child[i].ChildCount; i++)
                    {
                        try
                        {
                            currentIo = IoItem.Child[i].Child[j].LookupChild(deviceName);
                            if (currentIo != null)
                            {
                                break;
                            }
                        }
                        catch
                        {
                            //do nothing, go for next run
                        }
                    }
                }
            }

            //we've looked everywhere dude
            if (currentIo == null)
            {
                throw new ApplicationException($"Could not find device {deviceName}");
            }

            XmlDocument ioXml = new XmlDocument();
            ioXml.Load(xmlFile);
            try
            {
                currentIo.ConsumeXml(ioXml.OuterXml);
            }
            catch
            {
                throw new ApplicationException($"Failed to load xml for {deviceName}");
            }

        }

        private string IoListFile = @"\ioList.csv";
        public void importIoList()
        {
            //Should I check for existing IO first?

            //Check that the file exists first!
            if (!File.Exists(ConfigFolder + IoListFile))
            {
                throw new ApplicationException("IO CSV file not found in selected config directory");
            }

            //Parse IO CSV file to create 2d array of data
            List<string[]> ioDataList = new List<string[]>();
            using (Microsoft.VisualBasic.FileIO.TextFieldParser parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(ConfigFolder + IoListFile))
            {
                parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    ioDataList.Add(parser.ReadFields());
                }
            }
            string[][] ioDataListArray = ioDataList.ToArray();

            //To create children we need to keep references to the parents to use later
            List<ITcSmTreeItem> termLevel = new List<ITcSmTreeItem>();
            //For each "row/device" in array
            for (int i = 0; i < ioDataListArray.GetLength(0); i++)
            {
                //Convert "null" strings in sub-array to null 
                for (int j = 0; j < ioDataListArray[i].GetLength(0); j++)
                {
                    if (ioDataListArray[i][j] == "null")
                    {
                        ioDataListArray[i][j] = null;
                    }
                }

                //Find our indent/Index level
                int terminalLevelIndex = Int32.Parse(ioDataListArray[i][0]);
                //If 0 index, new EtherCAT device
                if (terminalLevelIndex == 0)
                {
                    //Clear the list
                    termLevel.Clear();
                    //Create a new EtherCAT device and add to empty list
                    termLevel.Add(IoItem.CreateChild(ioDataListArray[i][1], Int32.Parse(ioDataListArray[i][2]), ioDataListArray[i][3], ioDataListArray[i][4]));
                }
                else
                {
                    //If we try to specify an N level device without an N-1 level device, throw exception
                    if (termLevel.Count < terminalLevelIndex)
                    {
                        throw new ApplicationException($"No Level {terminalLevelIndex - 1} device found for entry {ioDataListArray[i][1]}.");
                    }
                    //If more list entries than our index level we need to remove list items as have lowered our index (sub level no longer needed)
                    if (termLevel.Count > terminalLevelIndex)
                    {
                        while (termLevel.Count > terminalLevelIndex)
                        {
                            try
                            {
                                termLevel.RemoveAt(terminalLevelIndex);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }
                    //If our list length is equal to index level we have a new index level so need to add this potential parent to the list
                    if (termLevel.Count == terminalLevelIndex)
                    {
                        termLevel.Add(termLevel[terminalLevelIndex - 1].CreateChild(ioDataListArray[i][1], Int32.Parse(ioDataListArray[i][2]), ioDataListArray[i][3], ioDataListArray[i][4]));
                    }
                    else //just add the new terminal
                    {
                        termLevel.RemoveAt(terminalLevelIndex);
                        termLevel.Add(termLevel[terminalLevelIndex - 1].CreateChild(ioDataListArray[i][1], Int32.Parse(ioDataListArray[i][2]), ioDataListArray[i][3], ioDataListArray[i][4]));
                    }
                }
            }
        }

        private void DeleteIoItems()
        {
            if (IoItem.ChildCount == 0)
            {
                Console.WriteLine("Already no IO items");
                return;
            }
            while (IoItem.ChildCount != 0)
            {
                try
                {
                    IoItem.DeleteChild(IoItem.Child[1].Name);
                }
                catch
                {
                    Console.WriteLine("Error deleting " + IoItem.Child[1].Name);
                }
            }
        }
        
        private string axisDirectory = @"\axisXmls";
        public bool addNamedNcAxes()
        {
            string axisFolder = ConfigFolder + axisDirectory;
            if(!Directory.Exists(axisFolder))
            {
                Console.WriteLine("Axis folder not found");
                return false;
            }
            foreach (var file in Directory.GetFiles(axisFolder))
            {
                if (!addNamedNcAxis(Path.GetFileNameWithoutExtension(file))) return false;
            }
            return true;
        }
        public bool addNamedNcAxis(string axisName)
        {
            try
            {
                AxesItem.CreateChild(axisName, 1);
                return true;
            }
            catch { return false; }
        }


        public void DeleteNcAxes()
        {
            if (AxesItem.ChildCount == 0)
            {
                throw new ApplicationException("Already no axes in tree");
            }
            while (AxesItem.ChildCount != 0)
            {
                try
                {
                    AxesItem.DeleteChild(AxesItem.Child[1].Name);
                }
                catch
                {
                    throw new ApplicationException($"Unable to delete {AxesItem.Child[1].Name}.");
                }

            }
        }

        public void SetupSolutionObjects()
        {
            solution = ActiveDTE.Solution;
            solutionProject = solution.Projects.Item(1);
            SystemManager = (ITcSysManager15)solutionProject.Object;
            ConfigManager = (ITcConfigManager)SystemManager.ConfigurationManager;
            NcItem = (ITcSmTreeItem9)SystemManager.LookupTreeItem("TINC");
            IoItem = (ITcSmTreeItem9)SystemManager.LookupTreeItem("TIID");
            PlcItem = (ITcSmTreeItem9)SystemManager.LookupTreeItem("TIPC");
            PlcProjectProjectItem = SystemManager.LookupTreeItem(PLCPROJECT_PROJECT_ITEM_STRING);
            PlcProjectItem = SystemManager.LookupTreeItem(PLCPROJECT_ITEM_STRING);

        }



        

    }
}
