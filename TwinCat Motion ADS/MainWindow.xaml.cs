using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TwinCAT.Ads;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using TwinCat_Motion_ADS.MeasurementDevice;

namespace TwinCat_Motion_ADS
{
    public partial class MainWindow : Window
    {
        #region Properties
        public PLC Plc;
        public string selectedFolder = string.Empty;
        public TestSuite TestSuiteWindow;
        public HelpWindow HelpWindow;
        public NcAxisView NcAxisView;
        public AirAxisView AirAxisView;
        public bool windowClosing = false;

        public MeasurementDevices MeasurementDevices = new();
        public List<MenuItem> measurementMenuItems = new();
        
        ListBoxWriter lbw;
        public ObservableCollection<ListBoxStatusItem> consoleStringList = new();

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
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            lbw = new(consoleListBox);
            consoleListBox.ItemsSource = consoleStringList;
            Console.SetOut(lbw);

            AmsNetID = Properties.Settings.Default.amsNetID;
            SetupBinds();
            if (!string.IsNullOrEmpty(amsNetIdTb.Text))
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
            NcAxisView = new();
            AirAxisView = new();
            tabbedWindow.Content = NcAxisView;
           
        }
        #endregion

        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double)value);
            else return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue) { }

        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion

        #region Window methods
        private void MainGrid_SizeChanged(object sender, EventArgs e) => CalculateScale();

        private void CalculateScale()
        {
            double yScale = ActualHeight / 1000f;
            double xScale = ActualWidth / 1920f;
            double value = Math.Min(xScale, yScale);

            ScaleValue = (double)OnCoerceScaleValue(myMainWindow, value);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (NcAxisView.testAxis != null)
            {
                if (NcAxisView.testAxis.testRunning)
                {
                    MessageBoxResult dialogResult = MessageBox.Show("You have a test running do you want to exit?", "Please Don't Leave Me", MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            //bit of a 'hacky' method. Due to the test suite window being hidden and not actually closed I need a way for that window to check if the whole application is closing
            windowClosing = true;
            //Because I hide the window
            if (TestSuiteWindow != null)
            {
                TestSuiteWindow.Close();
            }
            //Disconnect and close any open measurement device windows
            foreach (var Window in App.Current.Windows)
            {
                if (Window is measurementDeviceWindow)
                {
                    ((measurementDeviceWindow)Window).MDevice.Disconnect();
                    ((measurementDeviceWindow)Window).Close();
                }
            }
            //Disconnect from any remaining connected devices (allows proper disposing)
            foreach (var md in this.MeasurementDevices.MeasurementDeviceList)
            {
                if (md.Connected)
                {
                    md.Disconnect();
                }
            }
        }


        #endregion

        #region Measurement Device Menu
        private void AddNewDevice(object sender, RoutedEventArgs e)
        {
            MeasurementDevices.AddDevice(DeviceTypes.NoneSelected);
            UpdateMeasurementDeviceMenu();
        }


        private void DeviceMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mI = sender as MenuItem;
            int i = 0;
            int deviceIndex = 0;
            foreach (var device in measurementMenuItems)
            {
                if (mI == device)
                {
                    deviceIndex = i;
                }
                i++;
            }

            measurementDeviceWindow newMeasureWindow = new(deviceIndex, MeasurementDevices.MeasurementDeviceList[deviceIndex]);
            newMeasureWindow.Show();
        }

        //update devices menu
        public void UpdateMeasurementDeviceMenu(bool suppress = false)
        {
            MenuItem newMenuItem = new();

            //This binding seems to be bugged, works fine but does not update. - I think due to the style used
            //int testInt = MeasurementDevices.NumberOfDevices - 1;


            newMenuItem.Click += new RoutedEventHandler(DeviceMenu_Click);
            newMenuItem.Template = (ControlTemplate)FindResource("VsMenuSub");
            MeasureDevicesMenu.Items.Add(newMenuItem);  //adding to the actual UI
            measurementMenuItems.Add(newMenuItem);      //Adding to internal list

            int deviceIndex = MeasurementDevices.NumberOfDevices - 1;

            if (!suppress)
            {
                measurementDeviceWindow newMeasureWindow = new(deviceIndex, MeasurementDevices.MeasurementDeviceList[deviceIndex]);
                newMeasureWindow.Show();
            }
        }
        public void RemoveMeasurementMenuItem(int index)
        {
            measurementMenuItems.RemoveAt(index); //interal list doesn't contain default items
            index += 4; //need to account for the 4 default items in the menu (Add, import, export, seperator)
            MeasureDevicesMenu.Items.RemoveAt(index);
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
            foreach (MenuItem mi in measurementMenuItems)
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
                MeasurementDevices.ExportDevicesXml(selectedFile);
            }
        }
        #endregion

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
            Console.WriteLine("Connecting to PLC...");
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
        
        private void clearScreenButton_Click(object sender, RoutedEventArgs e)
        {
            consoleStringList.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow = new();

            
            HelpWindow.Show();
        }
    }
}
