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

namespace TwinCat_Motion_ADS
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PLC Plc;
        public string selectedFolder = string.Empty;
        public MeasurementDevice MeasurementDevice1;
        public MeasurementDevice MeasurementDevice2;
        public MeasurementDevice MeasurementDevice3;
        public MeasurementDevice MeasurementDevice4;
        public ObservableCollection<string> DeviceTypeList = new ObservableCollection<string>()
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
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            ConsoleAllocator.ShowConsoleWindow();
            AmsNetID = Properties.Settings.Default.amsNetID;
            setupBinds();
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
            
            //testAxis = new Axis(1, Plc);  //Uncomment for no DTI
            var vm = (MainViewModel)this.DataContext;
            setupMeasurementCombos();
            
        }

        private void setupBinds()
        {
            Binding amsNetBinding = new();
            amsNetBinding.Source = this;
            amsNetBinding.Path = new PropertyPath("AmsNetID"); ;
            amsNetBinding.Mode = BindingMode.TwoWay;
            amsNetBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(amsNetIdTb, TextBox.TextProperty, amsNetBinding);
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

        private void setupMeasurementCombos()
        {
            Measurement1Combo.ItemsSource = DeviceTypeList;
            Measurement2Combo.ItemsSource = DeviceTypeList;
            Measurement3Combo.ItemsSource = DeviceTypeList;
            Measurement4Combo.ItemsSource = DeviceTypeList;
        }


        /// ////////////////////////////////////
        private void Measurement1Combo_DropDownClosed(object sender, EventArgs e)
        {
            if (MeasurementDevice1 == null)
            {
                if ((string)Measurement1Combo.SelectedItem != "")
                {
                    MeasurementDevice1 = new MeasurementDevice((string)Measurement1Combo.SelectedItem);
                }
                return;
            }
            if (!MeasurementDevice1.Connected && (string)Measurement1Combo.SelectedItem != "")
            {
                MeasurementDevice1.changeDeviceType((string)Measurement1Combo.SelectedItem);
            }
            else if (!MeasurementDevice1.Connected && (string)Measurement1Combo.SelectedItem == "")
            {
                MeasurementDevice1 = null;
            }
            else if (MeasurementDevice1.Connected)
            {
                Measurement1Combo.SelectedItem = MeasurementDevice1.DeviceTypeString;
            }
        }

        private void updatePorts1_Click(object sender, RoutedEventArgs e)
        {
            if(MeasurementDevice1!= null)
            {
                MeasurementDevice1.UpdatePortList();
                Measurement1Serial.ItemsSource = MeasurementDevice1.SerialPortList;
            }
        }

        private void initDevice1_Click(object sender, RoutedEventArgs e)
        {
            if(MeasurementDevice1!=null)
            {
                if((bool)useMeasurement1.IsChecked) //if selected to use
                {
                    MeasurementDevice1.PortName = (string)Measurement1Serial.SelectedItem;
                    if (MeasurementDevice1.ConnectToDevice())
                    {
                        Console.WriteLine("Connected to device 1");
                    }
                    else
                    {
                        Console.WriteLine("Failed to connect to device 1");
                    }
                }
                else
                {
                    if (MeasurementDevice1.DisconnectFromDevice())
                    {
                        Console.WriteLine("Disconnected from device 1");
                        MeasurementDevice1 = null;
                        Measurement1Combo.SelectedItem = "";
                    }
                    else
                    {
                        Console.WriteLine("Failed to disconnect from device 1");
                    }
                }
            }
        }

        private async void testDevice1_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice1 != null)
            {
                string measurement = await MeasurementDevice1.GetMeasurement();
                Console.WriteLine(measurement);
            }
        }
        /// ////////////////////////////////////
        private void Measurement2Combo_DropDownClosed(object sender, EventArgs e)
        {
            if (MeasurementDevice2 == null)
            {
                if ((string)Measurement2Combo.SelectedItem != "")
                {
                    MeasurementDevice2 = new MeasurementDevice((string)Measurement2Combo.SelectedItem);
                }
                return;
            }
            if (!MeasurementDevice2.Connected && (string)Measurement2Combo.SelectedItem != "")
            {
                MeasurementDevice2.changeDeviceType((string)Measurement2Combo.SelectedItem);
            }
            else if (!MeasurementDevice2.Connected && (string)Measurement2Combo.SelectedItem == "")
            {
                MeasurementDevice2 = null;
            }
            else if (MeasurementDevice2.Connected)
            {
                Measurement2Combo.SelectedItem = MeasurementDevice2.DeviceTypeString;
            }
        }

        private void updatePorts2_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice2 != null)
            {
                MeasurementDevice2.UpdatePortList();
                Measurement2Serial.ItemsSource = MeasurementDevice2.SerialPortList;
            }
        }

        private void initDevice2_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice2 != null)
            {
                if ((bool)useMeasurement2.IsChecked) //if selected to use
                {
                    MeasurementDevice2.PortName = (string)Measurement2Serial.SelectedItem;
                    if (MeasurementDevice2.ConnectToDevice())
                    {
                        Console.WriteLine("Connected to device 2");
                    }
                    else
                    {
                        Console.WriteLine("Failed to connect to device 2");
                    }
                }
                else
                {
                    if (MeasurementDevice2.DisconnectFromDevice())
                    {
                        Console.WriteLine("Disconnected from device 2");
                        MeasurementDevice2 = null;
                        Measurement2Combo.SelectedItem = "";
                    }
                    else
                    {
                        Console.WriteLine("Failed to disconnect from device 2");
                    }
                }
            }
        }

        private async void testDevice2_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice2 != null)
            {
                string measurement = await MeasurementDevice2.GetMeasurement();
                Console.WriteLine(measurement);
            }
        }
        /// ////////////////////////////////////
        private void Measurement3Combo_DropDownClosed(object sender, EventArgs e)
        {
            if (MeasurementDevice3 == null)
            {
                if ((string)Measurement3Combo.SelectedItem != "")
                {
                    MeasurementDevice3 = new MeasurementDevice((string)Measurement3Combo.SelectedItem);
                }
                return;
            }
            if(!MeasurementDevice3.Connected && (string)Measurement3Combo.SelectedItem != "")
            {
                MeasurementDevice3.changeDeviceType((string)Measurement3Combo.SelectedItem);
            }
            else if (!MeasurementDevice3.Connected && (string)Measurement3Combo.SelectedItem == "")
            {
                MeasurementDevice3 = null;
            }
            else if (MeasurementDevice3.Connected)
            {
                Measurement3Combo.SelectedItem = MeasurementDevice3.DeviceTypeString;
            }
        }

        private void updatePorts3_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice3 != null)
            {
                MeasurementDevice3.UpdatePortList();
                Measurement3Serial.ItemsSource = MeasurementDevice3.SerialPortList;
            }
        }

        private void initDevice3_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice3 != null)
            {
                if ((bool)useMeasurement3.IsChecked) //if selected to use
                {
                    MeasurementDevice3.PortName = (string)Measurement3Serial.SelectedItem;
                    if (MeasurementDevice3.ConnectToDevice())
                    {
                        Console.WriteLine("Connected to device 3");
                    }
                    else
                    {
                        Console.WriteLine("Failed to connect to device 3");
                    }
                }
                else
                {
                    if (MeasurementDevice3.DisconnectFromDevice())
                    {
                        Console.WriteLine("Disconnected from device 3");
                        MeasurementDevice3 = null;
                        Measurement3Combo.SelectedItem = "";
                    }
                    else
                    {
                        Console.WriteLine("Failed to disconnect from device 3");
                    }
                }
            }
        }

        private async void testDevice3_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice3 != null)
            {
                string measurement = await MeasurementDevice3.GetMeasurement();
                Console.WriteLine(measurement);
            }
        }
        /// ////////////////////////////////////
        private void Measurement4Combo_DropDownClosed(object sender, EventArgs e)
        {
            if (MeasurementDevice4 == null)
            {
                if ((string)Measurement4Combo.SelectedItem != "")
                {
                    MeasurementDevice4 = new MeasurementDevice((string)Measurement4Combo.SelectedItem);
                }
                return;
            }
            if (!MeasurementDevice4.Connected && (string)Measurement4Combo.SelectedItem != "")
            {
                MeasurementDevice4.changeDeviceType((string)Measurement4Combo.SelectedItem);
            }
            else if (!MeasurementDevice4.Connected && (string)Measurement4Combo.SelectedItem == "")
            {
                MeasurementDevice4 = null;
            }
            else if (MeasurementDevice4.Connected)
            {
                Measurement4Combo.SelectedItem = MeasurementDevice4.DeviceTypeString;
            }
        }

        private void updatePorts4_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice4 != null)
            {
                MeasurementDevice4.UpdatePortList();
                Measurement4Serial.ItemsSource = MeasurementDevice4.SerialPortList;
            }
        }

        private void initDevice4_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice4 != null)
            {
                if ((bool)useMeasurement4.IsChecked) //if selected to use
                {
                    MeasurementDevice4.PortName = (string)Measurement4Serial.SelectedItem;
                    if (MeasurementDevice4.ConnectToDevice())
                    {
                        Console.WriteLine("Connected to device 4");
                    }
                    else
                    {
                        Console.WriteLine("Failed to connect to device 4");
                    }
                }
                else
                {
                    if (MeasurementDevice4.DisconnectFromDevice())
                    {
                        Console.WriteLine("Disconnected from device 4");
                        MeasurementDevice4 = null;
                        Measurement4Combo.SelectedItem = "";
                    }
                    else
                    {
                        Console.WriteLine("Failed to disconnect from device 4");
                    }
                }
            }
        }

        private async void testDevice4_Click(object sender, RoutedEventArgs e)
        {
            if (MeasurementDevice4 != null)
            {
                string measurement = await MeasurementDevice4.GetMeasurement();
                Console.WriteLine(measurement);
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
