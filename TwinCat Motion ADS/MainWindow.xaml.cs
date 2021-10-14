using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TwinCAT.Ads;
using Ookii.Dialogs.Wpf;
using TwinCat_Motion_ADS.MVVM.ViewModel;

namespace TwinCat_Motion_ADS
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PLC Plc;
        //PLC Plc = new PLC("5.65.74.200.1.1", 852);
        public PneumaticAxis pneumaticAxis;
        public string selectedFolder = string.Empty;
        


        public MainWindow()
        {

            InitializeComponent();
            ConsoleAllocator.ShowConsoleWindow();
            Plc = new PLC(amsNetIdTb.Text, 852); //5.65.74.200.1.1
            //Plc = new PLC("5.65.74.200.1.1", 852);
            Plc.setupPLC();
            if (Plc.AdsState == AdsState.Invalid)
            {
                Console.WriteLine("Ads state is invalid");

            }
            else if (Plc.AdsState == AdsState.Stop)
            {
                Console.WriteLine("Device connected but PLC not running");

            }
            else if (Plc.AdsState == AdsState.Run)
            {
                Console.WriteLine("Device connected and running");

            }
            //testAxis = new Axis(1, Plc);  //Uncomment for no DTI
            var vm = (MainViewModel)this.DataContext;
        }

        


        private void connect2PlcButton_Click(object sender, RoutedEventArgs e)
        {
            Plc = new PLC(amsNetIdTb.Text, 852);
            Plc.setupPLC();
            if (Plc.AdsState == AdsState.Invalid)
            {
                Console.WriteLine("Ads state is invalid");
            }
            else if (Plc.AdsState == AdsState.Stop)
            {
                Console.WriteLine("Device connected but PLC not running");
            }
            else if (Plc.AdsState == AdsState.Run)
            {
                Console.WriteLine("Device connected and running");
            }
        }



        public void initPneumatic_Click(object sender, RoutedEventArgs e)
        {
            if (pneumaticAxis == null)
            {
                pneumaticAxis = new PneumaticAxis(Plc);
            }
            else
            {
                pneumaticAxis = null;
                pneumaticAxis = new PneumaticAxis(Plc);
            }
            pneumaticAxis.startLimitRead();
            

            

            
        }

        private async void shutterEnd2End_button_Click(object sender, RoutedEventArgs e)
        {
            await pneumaticAxis.End2EndTest(10, 12, 500, 2, 3, 0, 0, true, false);
        }

        private void shutterTestFolderDir_button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new VistaFolderBrowserDialog();
            selectedFolder = String.Empty;
            if (fbd.ShowDialog() == true)
            {
                selectedFolder = fbd.SelectedPath;
            }
            Console.WriteLine(selectedFolder);
            pneumaticAxis.TestDirectory = selectedFolder;
        }



        private async void extendCylinder_button_Click(object sender, RoutedEventArgs e)
        {
            if(await pneumaticAxis.extendCylinderAndWait()==false)
            { 
                Console.WriteLine("FAILED");
            }
            
        }

        private async void retractCylinder_button_Click(object sender, RoutedEventArgs e)
        {
            if (await pneumaticAxis.retractCylinderAndWait()==false)
            {
                Console.WriteLine("FAILED");
            }
        }
    }


    internal static class ConsoleAllocator
    {
        [DllImport(@"kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport(@"kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport(@"user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SwHide = 0;
        const int SwShow = 5;


        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                AllocConsole();
            }
            else
            {
                ShowWindow(handle, SwShow);
            }
        }

        public static void HideConsoleWindow()
        {
            var handle = GetConsoleWindow();

            ShowWindow(handle, SwHide);
        }
    }

}
