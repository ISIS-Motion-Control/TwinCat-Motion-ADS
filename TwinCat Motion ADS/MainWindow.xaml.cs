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
            //var vm = (MainViewModel)this.DataContext;     
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
            Console.WriteLine(MeasurementDevices.MeasurementDeviceList[deviceIndex].Name);
            TwinCat_Motion_ADS.MVVM.View.measurementDeviceWindow newMeasureWindow = new(deviceIndex, MeasurementDevices.MeasurementDeviceList[deviceIndex]);
            newMeasureWindow.Show();
        }

        //update devices menu
        private void UpdateMeasurementDeviceMenu()
        {
            MenuItem newMenuItem = new();

            //This binding seems to be bugged, works fine but does not update.
            Binding menuItemName = new();
            menuItemName.Mode = BindingMode.OneWay;
            menuItemName.Source = MeasurementDevices.MeasurementDeviceList[MeasurementDevices.NumberOfDevices - 1];
            menuItemName.Path = new PropertyPath("Name");
            menuItemName.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(newMenuItem, MenuItem.HeaderProperty, menuItemName);

           // newMenuItem.Header = MeasurementDevices.MeasurementDeviceList[MeasurementDevices.NumberOfDevices-1].Name;
            newMenuItem.Click += new RoutedEventHandler(DeviceMenu_Click);
            newMenuItem.Template = (ControlTemplate)FindResource("VsMenuSub");
            MeasureDevicesMenu.Items.Add(newMenuItem);
            measurementMenuItems.Add(newMenuItem);

            int deviceIndex = MeasurementDevices.NumberOfDevices - 1;

            TwinCat_Motion_ADS.MVVM.View.measurementDeviceWindow newMeasureWindow = new(deviceIndex, MeasurementDevices.MeasurementDeviceList[deviceIndex]);
            newMeasureWindow.Show();

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
