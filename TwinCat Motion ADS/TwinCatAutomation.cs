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

using System.Threading;

using TwinCAT.TypeSystem;
using Application = System.Windows.Application;
using System.CodeDom;

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
        readonly MainWindow _MainWindow;

        public NcAxis NcAxis { get; set; }

        private ITcSysManager15 SystemManager;
        private ITcConfigManager ConfigManager;
        private Solution TwinCatSolution;
        private Project solutionProject;
        private ITcSmTreeItem9 NcItem;
        private ITcSmTreeItem9 AxesItem;
        private ITcSmTreeItem9 IoItem;
        private ITcSmTreeItem9 PlcItem;
        private ITcSmTreeItem PlcProjectItem;
        private ITcSmTreeItem PlcProjectProjectItem;

        public XmlDocument xmlDoc;  //Generic holder for xmlDocument

        private int COMMAND_TIMEOUT = 30000;

        private const string LOGGEDIN_XML_NODE = "LoggedIn";
        private const string PLCAPPSTATE_XML_NODE = "PlcAppState";
        private const string PLCPROJECT_PROJECT_ITEM_STRING = "TIPC^tc_project_app^tc_project_app Project";
        private const string PLCPROJECT_ITEM_STRING = "TIPC^tc_project_app";
        private const string PLC_PREFIX = "TIPC^";

        //Config constants
        private const string PLC_DIRECTORY_SUFFIX = @"\plc";
        private const string DECLARATION_DIRECTORY_SUFFIX = @"\declarations";
        private const string IMPLEMENTATION_DIRECTORY_SUFFIX = @"\implementations";
        private const string AXES_DIRECTORY_SUFFIX = @"\axes";
        private const string APPLICATION_DIRECTORY_SUFFIX = @"\applications";
        private const string AXES_XML_DIRECTORY_SUFFIX = @"\axisXmls";
        private const string IO_XML_DIRECTORY_SUFFIX = @"\deviceXmls\";
        private const string MAPPINGS_FILE = @"\mappings.xml";
        private const string IO_LIST_FILE = @"\ioList.csv";

        //Solution constants

        private const string PLC_APPLICATIONS_FOLDER = @"\solution\tc_project_app\POUs\Application_Specific\Applications";
        private const string PLC_AXES_FOLDER = @"\solution\tc_project_app\POUs\Application_Specific\Axes";
        private const string PLC_MAIN_PROG_ITEM = "TIPC^tc_project_app^tc_project_app Project^POUs^MAIN^PROG";
        
        private const string PLC_PROJECT_AXES_FOLDER = "TIPC^tc_project_app^tc_project_app Project^POUs^Application_Specific^Axes";

        private const string PLC_PROJECT_APPLICATIONS_FOLDER = "TIPC^tc_project_app^tc_project_app Project^POUs^Application_Specific^Applications";

        //Used for declarations
        private const string PLC_MAIN_ITEM = "TIPC^tc_project_app^tc_project_app Project^POUs^MAIN";
        private const string PLC_GVL_APP_ITEM = "TIPC^tc_project_app^tc_project_app Project^GVLs^GVL_APP";
        private const string ADD_METHOD = "add";
        private const string REPLACE_METHOD = "replace";
        #endregion

        #region Constructor
        public TwinCatAutomation()
        {
            _MainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;
            NcAxis = _MainWindow.NcAxisView.testAxis;
        }
        #endregion


        public void SetupSolutionObjects()
        {
            TwinCatSolution = ActiveDTE.Solution;
            solutionProject = TwinCatSolution.Projects.Item(1);
            SystemManager = (ITcSysManager15)solutionProject.Object;
            ConfigManager = (ITcConfigManager)SystemManager.ConfigurationManager;
            NcItem = (ITcSmTreeItem9)SystemManager.LookupTreeItem("TINC");
            AxesItem = (ITcSmTreeItem9)NcItem.Child[1].LookupChild("Axes");
            IoItem = (ITcSmTreeItem9)SystemManager.LookupTreeItem("TIID");
            PlcItem = (ITcSmTreeItem9)SystemManager.LookupTreeItem("TIPC");
            PlcProjectProjectItem = SystemManager.LookupTreeItem(PLCPROJECT_PROJECT_ITEM_STRING);
            PlcProjectItem = SystemManager.LookupTreeItem(PLCPROJECT_ITEM_STRING);

        }


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

        public async Task<bool> LoginToPlc()
        {
            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.LOGIN));

            //Confirm action was successful
            if (await CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, LOGGEDIN_XML_NODE, "true")) == false)
            {
                Console.WriteLine("Failed to login");
                return false;
            }
            return true;
        }

        public async Task<bool> LogoutOfPlc()
        {
            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.LOGOUT));

            //Confirm action was successful
            if (await CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, LOGGEDIN_XML_NODE, "false"))==false)
            {
                Console.WriteLine("Failed to logout");
                return false;
            }
            return true;
        }

        public async Task<bool> StartPlc()
        {

            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.START));

            //Confirm action was successful
            if (await CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, PLCAPPSTATE_XML_NODE, "Run")) == false)
            {
                Console.WriteLine("Failed to start");
                return false;
            }
            return true;
        }

        public async Task<bool> StopPlc()
        {
            PlcProjectProjectItem.ConsumeXml(CreatePlcCommandXml(PLCAction.STOP));

            //Confirm action was successful
            if (await CheckWithTimeout(COMMAND_TIMEOUT, () => CheckStringMatchesXml(PlcProjectProjectItem, PLCAPPSTATE_XML_NODE, "Stop"))==false)
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

        public void BuildPlcProject()
        {
            string plcPath;
            string solutionName;
            solutionName = Path.GetFileNameWithoutExtension(SolutionFolderPath);
            //solutionName = new FileInfo(SlnPath).Name;
            plcPath = SolutionFolderPath + @"\solution\" + PlcItem.Child[1].Name + @"\" + PlcItem.Child[1].Name + @".plcproj";
            try
            {
                TwinCatSolution.SolutionBuild.BuildProject("Release|TwinCAT RT (x64)", plcPath, true);
            }
            catch
            {
                Console.WriteLine("Something went wrong");
            }
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
                MessageBoxResult dialogResult = System.Windows.MessageBox.Show("Axes already exist in this solution. Do you want to remove them?", "NC Axes detected", MessageBoxButton.YesNoCancel);


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

            if (!AddAllConfigNcAxes())
            {
                Console.WriteLine("Issue adding axes");
                return;
            }

            //IO Handling
            if (IoItem.ChildCount != 0)
            {

                MessageBoxResult dialogResult = System.Windows.MessageBox.Show("Hardware already exists in this solution. Do you want to remove this?", "Time for a break", MessageBoxButton.YesNoCancel);

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
            ImportIoList();
            Console.WriteLine("IO devices imported");
            //Run through all device xmls and import
            ImportAllIoXmls();
            Console.WriteLine("IO import complete");
            //Setup axis parameters from available axis xmls
            NcImportAllAxisXtis();
            Console.WriteLine("NC Parameter import complete");
            //Add the plc "stuff"
            ImportConfigPlcDeclarations();
            Console.WriteLine("PLC declarations updated");
            //New PLC "stuff" to add
            ImportConfigApplicationsProgs();
            Console.WriteLine("Application Specific PROGs imported");
            ImportConfigAxesProgs();
            Console.WriteLine("Axis PROGs imported");
            SetupMainProgAction();
            Console.WriteLine("PROG action updated");

            BuildPlcProject();
            Console.WriteLine("PLC compiled");

            ImportConfigMappingsXML();
            Console.WriteLine("Import mappings complete");

            SaveSolutionAs();

            SetProjectToBoot();
            Console.WriteLine("Project autostart set");


            try
            {
                SystemManager.ActivateConfiguration();
                System.Threading.Thread.Sleep(1000);
            }
            catch
            {
                Console.WriteLine("Unable to activate configuration");
            }
            try
            {
                SystemManager.StartRestartTwinCAT();
                System.Threading.Thread.Sleep(1000);
            }
            catch
            {
                Console.WriteLine("Issue starting controller");
            }
            Console.WriteLine(SystemManager.IsTwinCATStarted());
            if (SystemManager.IsTwinCATStarted())
            {
                System.Windows.MessageBox.Show("TwinCAT is running");
                //plcLogin();
                //System.Threading.Thread.Sleep(1000);
                //plcStart();
                //System.Threading.Thread.Sleep(1000);
            }

            Console.WriteLine("Success!");
            Console.WriteLine(SystemManager.IsTwinCATStarted());
        }


        #region HELPER METHODS
        private async Task<bool> CheckWithTimeout(int timeout, Func<bool> checkMethod)
        {
            const int TIME_BETWEEN_CHECKS = 500;
            for (int i = 0; i < timeout / TIME_BETWEEN_CHECKS; i++)
            {
                if (checkMethod())
                {
                    return true;
                }
                //this.utils.printErrors();
                await Task.Delay(TIME_BETWEEN_CHECKS);
            }
            return checkMethod();
        }

        public void RevokeFilter()
        {
            MessageFilter.Revoke();
        }
        #endregion


        public bool SaveSolutionAs()
        {
            string solutionName = Path.GetFileNameWithoutExtension(SolutionFolderPath);
            try
            {
                TwinCatSolution.SaveAs(SolutionFolderPath + @"\solution.sln");
                //solution.SaveAs(SolutionFolderPath + @"\" + solutionName + @"\" + solutionName + ".sln");
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Import the mappings files from the config
        /// </summary>
        /// <returns></returns>
        public bool ImportConfigMappingsXML()
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

        
        /// <summary>
        /// Populates the PLC Main.Prog action to call each of the progs in application specific axes and applications folder
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void SetupMainProgAction()
        {
            //Try to locate the PLC MAIN.PROG item in the solution
            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager.LookupTreeItem(PLC_MAIN_PROG_ITEM);
            }
            catch
            {
                throw new ApplicationException($"Unable to find PROG action in solution");
            }

            //Need to access the implementation section of MAIN.PROG
            ITcPlcImplementation ImplementationSection;
            ImplementationSection = (ITcPlcImplementation)plcItem;
            //Placeholder string for creating implementation
            string ImplementationText = "";

            //Locate solution application specific axes folder
            ITcSmTreeItem ApplicationSpecificAxesFolder;
            try
            {
                ApplicationSpecificAxesFolder = SystemManager.LookupTreeItem(PLC_PROJECT_AXES_FOLDER);
            }
            catch
            {
                throw new ApplicationException($"Couldn't find axes folder");
            }

            //Locate solution application specific applications folder
            ITcSmTreeItem ApplicationSpecificAppsFolder;
            try
            {
                ApplicationSpecificAppsFolder = SystemManager.LookupTreeItem(PLC_PROJECT_APPLICATIONS_FOLDER);
            }
            catch
            {
                throw new ApplicationException($"Couldn't find applications folder");
            }

            //Populate the implementation text by calling each of the prog actions in the folders

            //THIS NEEDS CHANGING. WE NEED TO KNOW WHAT SUBTYPE WE ARE LOOKING AT NOW THAT I'M INCLUDING FOLDERS
            for (int i = 1; i < ApplicationSpecificAxesFolder.ChildCount + 1; i++)
            {
                ITcSmTreeItem currentItem = ApplicationSpecificAxesFolder.Child[i];
                if (currentItem.ItemType == 602)
                {
                    ImplementationText += ApplicationSpecificAxesFolder.Child[i].Name + @"();" + Environment.NewLine;
                }

                if(currentItem.ItemType == 601)
                {
                    
                    for (int j = 1; j <currentItem.ChildCount+1; j++)
                    {                       
                        ITcSmTreeItem subItem = currentItem.Child[j];
                        if (subItem.ItemType == 602)
                        {
                            ImplementationText += subItem.Name + @"();" + Environment.NewLine;
                        }
                    }
                }
                
                
            }
            
            for (int i = 1; i < ApplicationSpecificAppsFolder.ChildCount + 1; i++)
            {
                ITcSmTreeItem currentItem = ApplicationSpecificAppsFolder.Child[i];

                if (currentItem.ItemType == 602)
                {
                    ImplementationText += ApplicationSpecificAppsFolder.Child[i].Name + @"();" + Environment.NewLine;
                }

                if (currentItem.ItemType == 601)
                {
                    for (int j = 1; j < currentItem.ChildCount + 1; j++)
                    {
                        ITcSmTreeItem subItem = currentItem.Child[j];
                        if (subItem.ItemType == 602)
                        {
                            ImplementationText += subItem.Name + @"();" + Environment.NewLine;
                        }
                    }
                }
            }

            //Set the implementation section text to the generated string
            ImplementationSection.ImplementationText = ImplementationText;

        }

        /// <summary>
        /// Import axis progs from config
        /// Monadic null check implemented and should be tested
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void ImportConfigAxesProgs()
        {
            string AxesConfigPath = ConfigFolder + PLC_DIRECTORY_SUFFIX + AXES_DIRECTORY_SUFFIX;

            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager?.LookupTreeItem(PLC_PROJECT_AXES_FOLDER); 
            }
            catch
            {
                throw new ApplicationException($"Unable to find Axes Application Specific directory in solution");
            }

            //Clear the folder in the solution explorer
            if (plcItem?.ChildCount > 0)
            {
                while (plcItem.ChildCount > 0)
                {
                    try
                    {
                        plcItem.DeleteChild(plcItem.Child[1].Name);
                    }
                    catch
                    {
                        Console.WriteLine("Unable to delete Axis PLC Item");
                    }
                }
            }
            //Import all the items
            if (Directory.Exists(AxesConfigPath))
            {
                foreach (string filePath in Directory.GetFiles(AxesConfigPath))
                {
                    plcItem?.CreateChild(Path.GetFileNameWithoutExtension(filePath), 58, null, filePath);
                }

                foreach (string folder in Directory.GetDirectories(AxesConfigPath))
                {
                    DirectoryInfo info = new(folder);

                    ITcSmTreeItem solutionFolderItem = plcItem?.CreateChild(info.Name, 601, null, null);
                    
                    foreach (string filePath in Directory.GetFiles(folder))
                    {
                        solutionFolderItem.CreateChild(Path.GetFileNameWithoutExtension(filePath), 58, null, filePath);
                    }
                }

            }
        }

        /// <summary>
        /// Import application progs from config
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void ImportConfigApplicationsProgs()
        {
            string path = ConfigFolder + PLC_DIRECTORY_SUFFIX + APPLICATION_DIRECTORY_SUFFIX;

            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager?.LookupTreeItem(PLC_PROJECT_APPLICATIONS_FOLDER);
            }
            catch
            {
                throw new ApplicationException($"ERROR HEREUnable to find Axes Application Specific directory in solution");
            }
            //Clear the folder in the solution explorer
            if (plcItem?.ChildCount > 0)
            {
                while (plcItem.ChildCount > 0)
                {
                    try
                    {
                        plcItem.DeleteChild(plcItem.Child[1].Name);
                    }
                    catch
                    {
                        Console.WriteLine("Error, Unable to delete Application_Specific PLC Item");
                    }
                }
            }
            //Import all the items
            if (Directory.Exists(path))
            {
                foreach (string filePath in Directory.GetFiles(path))
                {
                    plcItem?.CreateChild(Path.GetFileNameWithoutExtension(filePath), 58, null, filePath);
                }

                foreach (string folder in Directory.GetDirectories(path))
                {
                    DirectoryInfo info = new(folder);

                    ITcSmTreeItem solutionFolderItem = plcItem?.CreateChild(info.Name, 601, null, null);

                    foreach (string filePath in Directory.GetFiles(folder))
                    {
                        solutionFolderItem.CreateChild(Path.GetFileNameWithoutExtension(filePath), 58, null, filePath);
                    }
                }
            }
            else
            {
                Console.WriteLine($"WARNING, application folder {path} does not exist");
            }
        }

        /// <summary>
        /// Import config plc declaration files in to solution
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void ImportConfigPlcDeclarations()
        {
            string directoryPath = ConfigFolder + PLC_DIRECTORY_SUFFIX + DECLARATION_DIRECTORY_SUFFIX;

            if (!Directory.Exists(directoryPath))
            {
                throw new ApplicationException($"Folder not found {directoryPath}");
            }
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                ModifyPlcDeclaration(file);
            }
        }

        /// <summary>
        /// Helper method for ImportConfigPlcDeclarations for individual file import
        /// </summary>
        /// <param name="decFile"></param>
        /// <exception cref="ApplicationException"></exception>
        private void ModifyPlcDeclaration(string decFile)
        {
            //check file exists
            if (!File.Exists(decFile))
            {
                throw new ApplicationException($"PLC file {decFile} could not be found.");
            }

            //Locate the PLC item with matching name
            string plcItemName = File.ReadLines(decFile).First();
            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager.LookupTreeItem(plcItemName); //removed PLC prefix as should just be put in to file  - warning this will break all old config files!!
            }
            catch
            {
                throw new ApplicationException($"Unable to find item {plcItemName}");
            }

            //Cast to decleration
            ITcPlcDeclaration plcItemDec;
            try
            {
                plcItemDec = (ITcPlcDeclaration)plcItem;
            }
            catch
            {
                throw new ApplicationException($"Unable to create declaration field for item {plcItemName}");
            }

            //placeholder string
            string declarationText = "";

            //for each line in file (ignore first line) add it to the declarationText string
            int lineCount = File.ReadLines(decFile).Count();
            for (int i = 2; i < lineCount; i++)
            {
                declarationText += Environment.NewLine + File.ReadLines(decFile).ElementAt(i);
            }

            //Decide if adding to delcaration or replacing (first line of text file)
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
       
        /// <summary>
        /// Import and consume all NC xml files from config
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void NcImportAllAxisXtis()
        {
            string axisFolder = ConfigFolder + AXES_XML_DIRECTORY_SUFFIX;

            if (!Directory.Exists(axisFolder))
            {
                throw new ApplicationException($"Folder not found: {axisFolder}");
            }

            foreach (string file in Directory.GetFiles(axisFolder))
            {
                NcImportAxisXti(file);
            }
        }

        /// <summary>
        /// Locates named axes in the NC project from xmlFilePath and imports parameters
        /// </summary>
        /// <param name="xmlFilePath"></param>
        /// <exception cref="ApplicationException"></exception>
        private void NcImportAxisXti(string xmlFilePath)
        {
            //Create placeholder xmlDoc and validate xmlFile input
            XmlDocument axisXml = new XmlDocument();
            if (!File.Exists(xmlFilePath))
            {
                throw new ApplicationException($"IO Xml file {xmlFilePath} could not be found.");
            }

            //Get axis name from file and find in NC project
            string axisName = Path.GetFileNameWithoutExtension(xmlFilePath);
            ITcSmTreeItem axis;
            try
            {
                axis = AxesItem.LookupChild(axisName);
            }
            catch
            {
                throw new ApplicationException($"Not able to find {axisName}.");
            }

            //Load XmlDoc from xmlFilePath
            axisXml.Load(xmlFilePath);
            try
            {
                axis.ConsumeXml(axisXml.OuterXml);
            }
            catch
            {
                throw new ApplicationException($"Unable to consume xml for {axisName}");
            }
        }

        /// <summary>
        /// Import and consume all IO xml files from config
        /// </summary>
        public void ImportAllIoXmls()
        {
            string deviceFolder = ConfigFolder + IO_XML_DIRECTORY_SUFFIX;
            if (!Directory.Exists(deviceFolder))
            {
                Console.WriteLine($"WARNING: Folder not found: {deviceFolder}, No devices will be loaded");
            }
            else
            {
                foreach (string file in Directory.GetFiles(deviceFolder))
                {
                    ImportIoXmls(file);
                }
            }
        }
        
        /// <summary>
        /// Located named IO in the IO project from xmlFilePath and import setup
        /// </summary>
        /// <param name="xmlFilepath"></param>
        /// <exception cref="ApplicationException"></exception>
        public void ImportIoXmls(string xmlFilepath)
        {
            //Check valid file path
            if (!File.Exists(xmlFilepath))
            {
                throw new ApplicationException($"IO Xml file {xmlFilepath} could not be found.");
            }

            //Get device name from file name
            string deviceName = Path.GetFileNameWithoutExtension(xmlFilepath);
            //Placeholder io tree item
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
            //If still not found, go for next level lookup
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

            //If we're here, we've found a match for the device name in currentIo object
            XmlDocument ioXml = new XmlDocument();
            ioXml.Load(xmlFilepath);
            try
            {
                currentIo.ConsumeXml(ioXml.OuterXml);
            }
            catch
            {
                Console.WriteLine($"Failed to load xml for {deviceName}");
            }

        }

        /// <summary>
        /// Populate the IO section of the solution from the config IO list csv file
        /// A bit overly complicated as devices can nest under other devices
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void ImportIoList()
        {
            //Check that the file exists first!
            if (!File.Exists(ConfigFolder + IO_LIST_FILE))
            {
                throw new ApplicationException("IO CSV file not found in selected config directory");
            }

            //Parse IO CSV file to create 2d array of data
            List<string[]> ioDataList = new List<string[]>();
            using (Microsoft.VisualBasic.FileIO.TextFieldParser parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(ConfigFolder + IO_LIST_FILE))
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
            //Every time we load in an item, create a list for it to refer back to

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

        /// <summary>
        /// Clear all IO from the solution IO project
        /// </summary>
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
        
        /// <summary>
        /// Populate NC with Axes from config folder
        /// </summary>
        /// <returns></returns>
        public bool AddAllConfigNcAxes()
        {
            string axisFolder = ConfigFolder + AXES_XML_DIRECTORY_SUFFIX;
            //Check folder is valid
            if(!Directory.Exists(axisFolder))
            {
                Console.WriteLine("Axis folder not found");
                return false;
            }
            //For each axis file in folder add to the NC, return if fail
            foreach (var file in Directory.GetFiles(axisFolder))
            {
                if (!AddNamedNcAxis(Path.GetFileNameWithoutExtension(file))) return false;
            }
            return true;
        }

        /// <summary>
        /// Add single axis to NC with name
        /// </summary>
        /// <param name="axisName"></param>
        /// <returns></returns>
        public bool AddNamedNcAxis(string axisName)
        {
            try
            {
                AxesItem?.CreateChild(axisName, 1);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Clear all NC axes from solution
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void DeleteNcAxes()
        {
            if (AxesItem?.ChildCount == 0)
            {
                throw new ApplicationException("Already no axes in tree");
            }
            while (AxesItem?.ChildCount != 0)
            {
                try
                {
                    AxesItem?.DeleteChild(AxesItem.Child[1].Name);
                }
                catch
                {
                    throw new ApplicationException($"Unable to delete {AxesItem.Child[1].Name}.");
                }

            }
        }
       
        /// <summary>
        /// Start and restart TwinCAT
        /// </summary>
        public void StartRestartTwincat()
        {
            SystemManager?.StartRestartTwinCAT();
        }

        /// <summary>
        /// Setup the selected folder with the correct folder structure for configuration
        /// </summary>
        public void SetupConfigurationFolder()
        {
            //If still default string, user has not selected valid location
            if (ConfigFolder == @"\Config")
            {
                Console.WriteLine("Config folder path not set");
                return;
            }

            Directory.CreateDirectory(ConfigFolder + AXES_XML_DIRECTORY_SUFFIX);
            Directory.CreateDirectory(ConfigFolder + IO_XML_DIRECTORY_SUFFIX);
            Directory.CreateDirectory(ConfigFolder + PLC_DIRECTORY_SUFFIX + DECLARATION_DIRECTORY_SUFFIX);
            Directory.CreateDirectory(ConfigFolder + PLC_DIRECTORY_SUFFIX + IMPLEMENTATION_DIRECTORY_SUFFIX);
            Directory.CreateDirectory(ConfigFolder + PLC_DIRECTORY_SUFFIX + AXES_DIRECTORY_SUFFIX);
            Directory.CreateDirectory(ConfigFolder + PLC_DIRECTORY_SUFFIX + APPLICATION_DIRECTORY_SUFFIX);
        }


        public void CreateSolutionConfiguration()
        {
            //CHECK CONFIG NOT EMPTY FIRST!!!
            if (ConfigFolder == @"\Config")
            {
                Console.WriteLine("You have not selected a configuration folder location");
                return;
            }

            //Check solution is populated
            if (TwinCatSolution == null)
            {
                Console.WriteLine("No solution linked");
                return;
            }

            //If no message filter registered, then register one
            if (!MessageFilter.IsRegistered)
                MessageFilter.Register();

            SetupConfigurationFolder();
            ExportMappingsAsXml();
            ClearMappings();
            ExportAllAxisXmls();
            ExportAllIoXmls();
            ExportIoCsvList();
            CreateMainAndGvlDeclaration();
            ExportPlcAxes();
            ExportPlcApplications();
            //cleanUp();
            Console.WriteLine("Export complete.");
        }

        /// <summary>
        /// Export solution mappings (variable links)
        /// </summary>
        /// <returns></returns>
        public bool ExportMappingsAsXml()
        {
            try
            {
                xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(SystemManager.ProduceMappingInfo());
                xmlDoc.Save(ConfigFolder + MAPPINGS_FILE);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clear mappings in solution
        /// </summary>
        /// <returns></returns>
        public bool ClearMappings()
        {
            try
            {
                SystemManager.ClearMappingInfo();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Export all NC axes to config folder
        /// </summary>
        public void ExportAllAxisXmls()
        {
            for (int i = 0; i < GetAxisCount(); i++)
            {
                ExportAxisXml(i);
            }
        }

        /// <summary>
        /// Helper method for ExportAllAxisXmls
        /// Exports specific numbered axis
        /// </summary>
        /// <param name="axisNumber"></param>
        private void ExportAxisXml(int axisNumber)
        {
            ITcSmTreeItem axisName = AxesItem.Child[axisNumber + 1];
            string xmlDescription = axisName.ProduceXml();
            File.WriteAllText(ConfigFolder + AXES_XML_DIRECTORY_SUFFIX + @"\" + axisName.Name + @".xml", xmlDescription);    //might have too many slashes so I removed one between config and AXES_XML
        }

        /// <summary>
        /// Get the number of axes in the NC project of Twincat solution
        /// </summary>
        /// <returns></returns>
        public int GetAxisCount()
        {
            return AxesItem.ChildCount;
        }

        /// <summary>
        /// Export xml files for all IO devices in IO project of TwinCAT solution
        /// </summary>
        public void ExportAllIoXmls()
        {
            int tier1DeviceLayer;   //number of devices in IO tree
            int tier2CouplerLayer; //number of couplers and terminals under a device
            int tier3TerminalLayer; //number of terminals under a coupler
            ITcSmTreeItem ioName;

            tier1DeviceLayer = IoItem.ChildCount;
            for (int i = 1; i <= tier1DeviceLayer; i++)
            {
                ioName = IoItem.Child[i];
                ExportIoXml(ioName);
                tier2CouplerLayer = IoItem.Child[i].ChildCount;

                for (int j = 1; j <= tier2CouplerLayer; j++)
                {
                    ioName = IoItem.Child[i].Child[j];
                    ExportIoXml(ioName);
                    tier3TerminalLayer = IoItem.Child[i].Child[j].ChildCount;
                    //Iterate here

                    for (int k = 1; k <= tier3TerminalLayer; k++)
                    {
                        ioName = IoItem.Child[i].Child[j].Child[k];
                        ExportIoXml(ioName);
                    }
                    //Then add child count to tier2
                    j += tier3TerminalLayer;
                }
            }
        }

        /// <summary>
        /// Create an XML file in the configuration folder for an inputted IoDevice
        /// </summary>
        /// <param name="ioDevice"></param>
        public void ExportIoXml(ITcSmTreeItem ioDevice)
        {
            string xmlDescription = ioDevice.ProduceXml();
            File.WriteAllText(ConfigFolder + IO_XML_DIRECTORY_SUFFIX + ioDevice.Name + @".xml", xmlDescription);
        }

        /// <summary>
        /// Create a CSV list of IO devices in TwinCAT solution IO project
        /// </summary>
        public void ExportIoCsvList()
        {
            int tier1DeviceLayer;   //number of devices in IO tree
            int tier2CouplerLayer; //number of couplers and terminals under a device
            int tier3TerminalLayer; //number of terminals under a coupler
            ITcSmTreeItem ioName;
            List<string> ioList = new List<string>();

            tier1DeviceLayer = IoItem.ChildCount;
            for (int i = 1; i <= tier1DeviceLayer; i++)
            {
                ioName = IoItem.Child[i];
                ioList.Add(GetIoData("0", ioName));
                tier2CouplerLayer = IoItem.Child[i].ChildCount;

                for (int j = 1; j <= tier2CouplerLayer; j++)
                {
                    ioName = IoItem.Child[i].Child[j];
                    ioList.Add(GetIoData("1", ioName));
                    tier3TerminalLayer = IoItem.Child[i].Child[j].ChildCount;

                    for (int k = 1; k <= tier3TerminalLayer; k++)
                    {
                        ioName = IoItem.Child[i].Child[j].Child[k];
                        ioList.Add(GetIoData("2", ioName));
                    }
                    //Then add child count to tier2
                    j += tier3TerminalLayer;
                }
            }
            String path = ConfigFolder + @"\ioList.csv";
            File.WriteAllLines(path, ioList.ToArray());
        }

        /// <summary>
        /// Helper method for ExportIoCsvList
        /// Grabs all the data for an inputted device item and outputs as string used in CSV file
        /// </summary>
        /// <param name="tierLevel"></param>
        /// <param name="ioName"></param>
        /// <returns></returns>
        private string GetIoData(string tierLevel, ITcSmTreeItem ioName)
        {
            //Need to get product revision from XML
            XmlDocument ioXml = new XmlDocument();
            ioXml.LoadXml(ioName.ProduceXml());
            string productRevision;
            string name;
            string subType;
            string strB4; //not utilised but needs to be set. Can be used to state where in tree the device should appear when created.
            string ioListString;

            name = ioName.Name;
            subType = (ioName.ItemSubType).ToString();

            var node = ioXml.SelectSingleNode("TreeItem/EtherCAT/Slave/Info/ProductRevision");
            if (node != null)
            {
                strB4 = " ";
                productRevision = node.InnerText;
            }
            else
            {
                strB4 = "null";
                productRevision = "null";
            }
            ioListString = tierLevel + "," + name + "," + subType + "," + strB4 + "," + productRevision;
            return ioListString;
        }

        /// <summary>
        /// Creates declaration configuration files for MAIN and GVL
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void CreateMainAndGvlDeclaration()
        {
            //Create two files for GVL and MAIN. Probably don't need Main anymore with how we use application specific PROGS? look to delete that bit
            //GVL will be an actual copy of project GVL
            //Main will just be a template file
            string path = ConfigFolder + PLC_DIRECTORY_SUFFIX + DECLARATION_DIRECTORY_SUFFIX;
            string fileName = @"\mainDeclaration.txt";

            string declaration = PLC_MAIN_ITEM + Environment.NewLine + ADD_METHOD;

            File.WriteAllText(path + fileName, declaration);

            fileName = @"\gvlAppDeclaration.txt";
            ITcSmTreeItem plcItem;
            try
            {
                plcItem = SystemManager.LookupTreeItem(PLC_GVL_APP_ITEM);
            }
            catch
            {
                throw new ApplicationException($"Unable to find item GVL_APP");
            }
            ITcPlcDeclaration plcItemDec;
            try
            {
                plcItemDec = (ITcPlcDeclaration)plcItem;
            }
            catch
            {
                throw new ApplicationException($"Unable to create declaration field for item GVL_APP");
            }
            declaration = PLC_GVL_APP_ITEM + Environment.NewLine + REPLACE_METHOD + Environment.NewLine + plcItemDec.DeclarationText;
            File.WriteAllText(path + fileName, declaration);
        }

        

        #region NC Axis Status readout for testing
        public void PrintNcStatuses()
        {
            if (NcAxis == null)
            {
                Console.WriteLine("Axis is null");
                try
                {
                    NcAxis = _MainWindow.NcAxisView.testAxis;
                }
                catch
                {
                    return;
                }

                if (NcAxis == null)  return;
            }

            NcAxis.PrintAxisStatus();

        }

        private CancellationTokenSource loggingToken;
        private bool LoggingInProgress = false;

        public async void StartStatusLogging()
        {
            if (!LoggingInProgress)
            {
                LoggingInProgress = true;
                loggingToken = new CancellationTokenSource();

                await LogStatus(loggingToken.Token);
            }
            
            //LogStatus(loggingToken.Token);
        }

        public async Task LogStatus(CancellationToken ct)
        {
            while(true)
            {
                PrintNcStatuses();
                await Task.Delay(1000);
                if (ct.IsCancellationRequested)
                {
                    Console.WriteLine("Logging cancelled");
                    LoggingInProgress = false;
                    break;
                }
            }
        }

        public void StopStatusLogging()
        { 
            loggingToken?.Cancel();
        }

        #endregion

        #region CONFIG EXPORT METHODS
        public void ExportPlcApplications()
        {
            //setup config folder path for plc applications
            string path = ConfigFolder + PLC_DIRECTORY_SUFFIX + APPLICATION_DIRECTORY_SUFFIX;
            //if folder does not exist, create it
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
            //Setup folder path in twincat solution for PLC applications
            string appsFolder = SolutionFolderPath + PLC_APPLICATIONS_FOLDER;

            //for each file in the config folder, delete it
            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
            foreach (string folder in Directory.GetDirectories(path))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    File.Delete(file);
                }
                Directory.Delete(folder);
            }

            //for each file in applications folder, make a copy of it in config
            foreach (string filePath in Directory.GetFiles(appsFolder))
            { File.Copy(filePath, filePath.Replace(appsFolder, path));}
            
            //One folder deep
            foreach (string folder in Directory.GetDirectories(appsFolder))
            {
                //create the directory in the config folder
                DirectoryInfo info = new DirectoryInfo(folder);
                string tempPath = path + @"\" + info.Name;

                Directory.CreateDirectory(tempPath);

                foreach (string file in Directory.GetFiles(folder))
                {
                    File.Copy(file, file.Replace(folder,tempPath));
                }

            }           
        }

        /// <summary>
        /// Export PLC axes folder
        /// </summary>
        public void ExportPlcAxes()
        {
            string path = ConfigFolder + PLC_DIRECTORY_SUFFIX + AXES_DIRECTORY_SUFFIX;
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(ConfigFolder + PLC_DIRECTORY_SUFFIX + AXES_DIRECTORY_SUFFIX);
            }
            string axesFolder = SolutionFolderPath + PLC_AXES_FOLDER;

            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
            foreach (string folder in Directory.GetDirectories(path))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    File.Delete(file);
                }
                Directory.Delete(folder);
            }

            foreach (string filePath in Directory.GetFiles(axesFolder))
            { File.Copy(filePath, filePath.Replace(axesFolder, path)); }

            foreach (string folder in Directory.GetDirectories(axesFolder))
            {
                //create the directory in the config folder
                DirectoryInfo info = new DirectoryInfo(folder);
                string tempPath = path + @"\" + info.Name;

                Directory.CreateDirectory(tempPath);

                foreach (string file in Directory.GetFiles(folder))
                {
                    File.Copy(file, file.Replace(folder, tempPath));
                }
            }
        }



        #endregion



        #region METHOD IDEAS
        //Create progs for each NC axis
        //automatically populate ApplicationSpecific^Axes folder with named axes (with template)

        //Create Axis_Names_GVL
        //Create and populate a new GVL with all the NC axis names and INTS assigned

        //Call all axis progs
        //Method to automatically call all axis progs in the MAIN.Prog
        #endregion  
    }
}
