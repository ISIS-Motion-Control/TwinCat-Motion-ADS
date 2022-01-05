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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace TwinCat_Motion_ADS
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PLC Plc;
        public string selectedFolder = string.Empty;
        public MVVM.View.TestSuite TestSuiteWindow;
        public MVVM.View.NcAxisView NcAxisView;
        public MVVM.View.AirAxisView AirAxisView;

        public MeasurementDevices MeasurementDevices = new();
        public List<MenuItem> measurementMenuItems = new();
        public ObservableCollection<string> DeviceTypeList = new()
        {
            "",
        "DigimaticIndicator",
        "KeyenceTM3000"
        };
        private string _amsNetID;
        public string AmsNetID
        {
            get { return _amsNetID; }
            set
            {
                _amsNetID = value;
                Properties.Settings.Default.amsNetID = value;
                OnPropertyChanged();
                Properties.Settings.Default.Save();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            
            ConsoleAllocator.ShowConsoleWindow();
            AmsNetID = Properties.Settings.Default.amsNetID;
            SetupBinds();
            if (!string.IsNullOrEmpty(amsNetIdTb.Text))
            {
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
            }
            NcAxisView = new();
            AirAxisView = new();
            tabbedWindow.Content = NcAxisView;
        }

        private void SetupBinds()
        {
            Binding amsNetBinding = new();
            amsNetBinding.Source = this;
            amsNetBinding.Path = new PropertyPath("AmsNetID"); ;
            amsNetBinding.Mode = BindingMode.TwoWay;
            amsNetBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(amsNetIdTb, TextBox.TextProperty, amsNetBinding);
        }

        private void ConnectToPlc_Click(object sender, RoutedEventArgs e)
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void AddNewDevice(object sender, RoutedEventArgs e)
        {
            MeasurementDevices.AddDevice("none");
            UpdateMeasurementDeviceMenu();
        }

        private void DeviceMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mI = sender as MenuItem;
            int i = 0;
            int deviceIndex = 0;
            foreach(var device in measurementMenuItems)
            {
                if(mI == device)
                {
                    deviceIndex = i;
                    //Console.WriteLine("That's a match on " + deviceIndex); //debugging line to check we can find item based on menuItemList
                }
                i++;
            }

            TwinCat_Motion_ADS.MVVM.View.measurementDeviceWindow newMeasureWindow = new(deviceIndex, MeasurementDevices.MeasurementDeviceList[deviceIndex]);
            newMeasureWindow.Show();
        }

        //update devices menu
        public void UpdateMeasurementDeviceMenu(bool suppress = false)
        {
            MenuItem newMenuItem = new();

            //This binding seems to be bugged, works fine but does not update. - I think due to the style used
            int testInt = MeasurementDevices.NumberOfDevices - 1;


            newMenuItem.Click += new RoutedEventHandler(DeviceMenu_Click);
            newMenuItem.Template = (ControlTemplate)FindResource("VsMenuSub");
            MeasureDevicesMenu.Items.Add(newMenuItem);  //adding to the actual UI
            measurementMenuItems.Add(newMenuItem);      //Adding to internal list

            int deviceIndex = MeasurementDevices.NumberOfDevices - 1;

            if(!suppress)
            {
                MVVM.View.measurementDeviceWindow newMeasureWindow = new(deviceIndex, MeasurementDevices.MeasurementDeviceList[deviceIndex]);
                newMeasureWindow.Show();
            }
            

        }

        private void ImportDevices_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fbd = new();
            fbd.Filter = "*.XML|*.xml";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                int temp = MeasurementDevices.ImportDevicesXML(selectedFile);
                if (temp > 0)
                {
                    for (int i = 0; i < temp; i++)
                    {
                        UpdateMeasurementDeviceMenu(true);
                    }
                }
            }
        }

        private void MeasureDevicesMenu_Click(object sender, RoutedEventArgs e)
        {
            MeasureDevicesMenu.Items.Refresh();
            int counter = 0;
            foreach(MenuItem mi in measurementMenuItems)
            {
                mi.Header = MeasurementDevices.MeasurementDeviceList[counter].Name;
                counter++;
            }
        }

        private void ExportDevices_Click(object sender, RoutedEventArgs e)
        {
            VistaSaveFileDialog fbd = new();
            fbd.AddExtension = true;
            fbd.DefaultExt = ".XML";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                MeasurementDevices.ExportDeviceesXml(selectedFile);
            }
        }

        private void TestSuiteMenu_Click(object sender, RoutedEventArgs e)
        {
            if (TestSuiteWindow == null)
            {
                TestSuiteWindow = new();
            }
            TestSuiteWindow.Show();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if(((RadioButton)sender) == NcAxis)
            {
                tabbedWindow.Content = NcAxisView;
            }
            else if(((RadioButton)sender)== AirAxis)
            {
                tabbedWindow.Content = AirAxisView;
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
