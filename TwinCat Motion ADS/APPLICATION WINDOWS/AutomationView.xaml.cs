using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TwinCat_Motion_ADS.APPLICATION_WINDOWS
{
    /// <summary>
    /// Interaction logic for AutomationView.xaml
    /// </summary>
    public partial class AutomationView : UserControl
    {
        public TwinCatAutomation autoInterface = new();
        private string _SolutionPathName;
        public string SolutionPathName
        {
            get
            {
                return _SolutionPathName;
            }
            set
            {
                _SolutionPathName = value;
            }
        }
        public AutomationView()
        {
            InitializeComponent();
            VsVersionCombo.ItemsSource = VSVersion.VsVersionList;
            VsVersionCombo.SelectedItem = "TWINCAT_SHELL";
        }

        public void SelectSolution_Click (object sender, RoutedEventArgs e)
        {
            var fbd = new VistaOpenFileDialog();
            SolutionPathName = String.Empty;
            if (fbd.ShowDialog() == true)
            {
                SolutionPathName = fbd.FileName;
            }
            Console.WriteLine(SolutionPathName);
        }




        private void Button_ExistingInstance_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SolutionPathName))
            {
                Console.WriteLine("No solution selected");
                return;
            }
            autoInterface.AttachToExistingDte(SolutionPathName, VsVersionCombo.Text);
            autoInterface.SetupSolutionObjects();
        }

        private void Button_NewInstance_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(SolutionPathName))
            {
                Console.WriteLine("No solution selected");
                return;
            }
            autoInterface.CreateNewDTE(SolutionPathName, VsVersionCombo.Text, true, false, true);
            autoInterface.SetupSolutionObjects();
        }

        private void Button_SelectConfig_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new VistaFolderBrowserDialog();

            if (fbd.ShowDialog() == true)
            {
                autoInterface.ConfigFolder = fbd.SelectedPath;
            }
            Console.WriteLine(fbd.SelectedPath);
        }

        private void Button_SetupSolution_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.SetupSytem();
        }

        private void Button_RevokeFilter_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.RevokeFilter();
        }

        private void Button_PlcLogin_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.LoginToPlc();
        }

        private void Button_PlcLogout_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.LogoutOfPlc();
        }

        private void Button_PlcStart_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.StartPlc();
        }

        private void Button_PlcStop_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.StopPlc();
        }

        private void Button_CreateConfig_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.CreateSolutionConfiguration();
        }

        private void Button_CheckStatusesNC_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.PrintNcStatuses();
        }

        private void Button_StartLogging_Click(object sender, RoutedEventArgs e)
        {
            autoInterface.StartStatusLogging();
        }

        private void Button_StopLogging_Click(object sender, RoutedEventArgs e)
        {
            autoInterface?.StopStatusLogging();
        }

        private void Button_SetupConfig_Click(object sender, RoutedEventArgs e)
        {
            autoInterface?.SetupConfigurationFolder();
        }
    }



    public class VSVersion
    {
        public static readonly VSVersion VS_2010 = new VSVersion("VisualStudio.DTE.10.0");
        public static readonly VSVersion VS_2012 = new VSVersion("VisualStudio.DTE.11.0");
        public static readonly VSVersion VS_2013 = new VSVersion("VisualStudio.DTE.12.0");
        public static readonly VSVersion VS_2015 = new VSVersion("VisualStudio.DTE.14.0");
        public static readonly VSVersion VS_2017 = new VSVersion("VisualStudio.DTE.15.0");
        public static readonly VSVersion VS_2019 = new VSVersion("VisualStudio.DTE.16.0");
        public static readonly VSVersion VS_2022 = new VSVersion("VisualStudio.DTE.17.0");
        public static readonly VSVersion TWINCAT_SHELL = new VSVersion("TcXaeShell.DTE.15.0");

        public static List<string> VsVersionList = new List<string>()
        {"VS_2019", "VS_2022", "TWINCAT_SHELL"};

        public String DTEDesc;
        private VSVersion(String DTEDesc)
        {
            this.DTEDesc = DTEDesc;
        }
        public static String ReturnVersion(string appID)
        {
            switch (appID)
            {
                case "VS_2010":
                    return VSVersion.VS_2010.DTEDesc;
                case "VS_2012":
                    return VSVersion.VS_2012.DTEDesc;
                case "VS_2013":
                    return VSVersion.VS_2013.DTEDesc;
                case "VS_2015":
                    return VSVersion.VS_2015.DTEDesc;
                case "VS_2017":
                    return VSVersion.VS_2017.DTEDesc;
                case "VS_2019":
                    return VSVersion.VS_2019.DTEDesc;
                case "VS_2022":
                    return VSVersion.VS_2022.DTEDesc;
                case "TWINCAT_SHELL":
                    return VSVersion.TWINCAT_SHELL.DTEDesc;
                default:
                    return VSVersion.TWINCAT_SHELL.DTEDesc;
            }
        }

    }

}



