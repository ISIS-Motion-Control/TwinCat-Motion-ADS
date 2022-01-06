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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections.Specialized;

namespace TwinCat_Motion_ADS
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PLC Plc;
        public string selectedFolder = string.Empty;
        public TestSuite TestSuiteWindow;
        public NcAxisView NcAxisView;
        public AirAxisView AirAxisView;
        public bool windowClosing = false;

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

        ListBoxWriter lbw;
        public ObservableCollection<ListBoxStatusItem> consoleStringList = new();

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
            foreach(var device in measurementMenuItems)
            {
                if(mI == device)
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
            int testInt = MeasurementDevices.NumberOfDevices - 1;


            newMenuItem.Click += new RoutedEventHandler(DeviceMenu_Click);
            newMenuItem.Template = (ControlTemplate)FindResource("VsMenuSub");
            MeasureDevicesMenu.Items.Add(newMenuItem);  //adding to the actual UI
            measurementMenuItems.Add(newMenuItem);      //Adding to internal list

            int deviceIndex = MeasurementDevices.NumberOfDevices - 1;

            if(!suppress)
            {
                measurementDeviceWindow newMeasureWindow = new(deviceIndex, MeasurementDevices.MeasurementDeviceList[deviceIndex]);
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //bit of a 'hacky' method. Due to the test suite window being hidden and not actually closed I need a way for that window to check if the whole application is closing
            windowClosing = true;
            //Because I hide the window
            if(TestSuiteWindow !=null)
            {
                TestSuiteWindow.Close();
            }
        }
    }

    public class ListBoxStatusItem
    {
        public string timestamp { get; set; }
        public string statusMessage { get; set; }
        public ListBoxStatusItem(string status)
        {
            statusMessage = status;
            timestamp = DateTime.Now.ToString();
        }
    }

    public class ListBoxWriter : TextWriter
    {
        private ListBox list;
        private StringBuilder content = new StringBuilder();

        public ListBoxWriter(ListBox list)
        {
            this.list = list;
        }

        public override void Write(char value)
        {
            base.Write(value);
            content.Append(value);
            if (value == '\n')
            {
                ListBoxStatusItem temp = new(content.ToString());
                //((ObservableCollection<string>)(list.ItemsSource)).Add(content.ToString());
                ((ObservableCollection<ListBoxStatusItem>)(list.ItemsSource)).Add(temp);
                //list.Items.Add(content.ToString());
                content = new StringBuilder();
            }
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }

    class ListBoxBehavior
    {
        static readonly Dictionary<ListBox, Capture> Associations =
               new Dictionary<ListBox, Capture>();

        public static bool GetScrollOnNewItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollOnNewItemProperty);
        }

        public static void SetScrollOnNewItem(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollOnNewItemProperty, value);
        }

        public static readonly DependencyProperty ScrollOnNewItemProperty =
            DependencyProperty.RegisterAttached(
                "ScrollOnNewItem",
                typeof(bool),
                typeof(ListBoxBehavior),
                new UIPropertyMetadata(false, OnScrollOnNewItemChanged));

        public static void OnScrollOnNewItemChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var listBox = d as ListBox;
            if (listBox == null) return;
            bool oldValue = (bool)e.OldValue, newValue = (bool)e.NewValue;
            if (newValue == oldValue) return;
            if (newValue)
            {
                listBox.Loaded += ListBox_Loaded;
                listBox.Unloaded += ListBox_Unloaded;
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.AddValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
            else
            {
                listBox.Loaded -= ListBox_Loaded;
                listBox.Unloaded -= ListBox_Unloaded;
                if (Associations.ContainsKey(listBox))
                    Associations[listBox].Dispose();
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.RemoveValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
        }

        private static void ListBox_ItemsSourceChanged(object sender, EventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            Associations[listBox] = new Capture(listBox);
        }

        static void ListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            listBox.Unloaded -= ListBox_Unloaded;
        }

        static void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var incc = listBox.Items as INotifyCollectionChanged;
            if (incc == null) return;
            listBox.Loaded -= ListBox_Loaded;
            Associations[listBox] = new Capture(listBox);
        }

        class Capture : IDisposable
        {
            private readonly ListBox listBox;
            private readonly INotifyCollectionChanged incc;

            public Capture(ListBox listBox)
            {
                this.listBox = listBox;
                incc = listBox.ItemsSource as INotifyCollectionChanged;
                if (incc != null)
                {
                    incc.CollectionChanged += incc_CollectionChanged;
                }
            }

            void incc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    listBox.ScrollIntoView(e.NewItems[0]);
                    listBox.SelectedItem = e.NewItems[0];
                }
            }

            public void Dispose()
            {
                if (incc != null)
                    incc.CollectionChanged -= incc_CollectionChanged;
            }
        }
    }
}
