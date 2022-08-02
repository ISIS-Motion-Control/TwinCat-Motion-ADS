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

namespace TwinCat_Motion_ADS
{
    public class TwinCatAutomation
    {
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

        public void SetupSytem()
        {
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

            //Add Named Axes

            //Add IO

            // Add PLC






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

        private void SetupSolutionObjects()
        {
            solution = ActiveDTE.Solution;
            solutionProject = solution.Projects.Item(1);
            SystemManager = (ITcSysManager15)solutionProject.Object;
            ConfigManager = (ITcConfigManager)SystemManager.ConfigurationManager;
            NcItem = (ITcSmTreeItem9)SystemManager.LookupTreeItem("TINC");
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

    }
}
