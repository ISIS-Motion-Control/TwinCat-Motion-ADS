using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using TwinCat_Motion_ADS;

namespace TwinCat_Motion_ADS.MVVM.View
{
    /// <summary>
    /// Interaction logic for subWindow.xaml
    /// </summary>
    public partial class measurementDeviceWindow : Window
    {
        int DeviceIndex;
        public ObservableCollection<string> DeviceTypeList = new ObservableCollection<string>()
        {
            "",
        "DigimaticIndicator",
        "KeyenceTM3000",
        "Beckhoff"
        };
        public ObservableCollection<string> BaudRateList = new ObservableCollection<string>()
        {
            "9600", "19200","38400","57600","115200"
        };
        MeasurementDevice MDevice;
        public measurementDeviceWindow(int deviceIndex, MeasurementDevice mDevice)
        {
            
            InitializeComponent();
            
            DeviceIndex = deviceIndex;
            MDevice = mDevice;
            //Setup device name bind
            Binding deviceNameBind = new();
            deviceNameBind.Mode = BindingMode.TwoWay;
            deviceNameBind.Source = MDevice;
            deviceNameBind.Path = new PropertyPath("Name");
            deviceNameBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(deviceName, TextBox.TextProperty, deviceNameBind);

            DeviceType.ItemsSource = DeviceTypeList;

            Binding deviceTypeBind = new();
            deviceTypeBind.Mode = BindingMode.OneWay;
            deviceTypeBind.Source = MDevice;
            deviceTypeBind.Path = new PropertyPath("DeviceTypeString");
            deviceTypeBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(DeviceType, ComboBox.SelectedValueProperty, deviceTypeBind);

            updateWindow();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((MainWindow)(Application.Current.MainWindow)).measurementMenuItems[DeviceIndex].Header = MDevice.Name;
        }

        private void DeviceType_DropDownClosed(object sender, EventArgs e)
        {
            if(!MDevice.Connected)
            {
                MDevice.changeDeviceType((string)DeviceType.SelectedItem);
            }
            DeviceType.SelectedItem = MDevice.DeviceTypeString;
            updateWindow();
        }
        private void updateWindow()
        {
            deviceSettings.Children.Clear();
            if(MDevice.DeviceTypeString== "DigimaticIndicator" || MDevice.DeviceTypeString == "KeyenceTM3000")
            {
                //Create stack panel for 1st setting
                StackPanel setting1 = new();
                setting1.Orientation = Orientation.Horizontal;
                setting1.Margin = new Thickness(5,5,0,0);
                deviceSettings.Children.Add(setting1);

                //Setting text
                TextBlock setting1Text = new();
                setupTextBlock(ref setting1Text, "Com Port:");

                ComboBox comPort = new();
                MDevice.UpdatePortList();
                setupComboBox(ref comPort, "comPort", MDevice.SerialPortList);
                comPort.DropDownClosed += new EventHandler(portSelect_DropDownClosed);

                Binding comPortBind = new();
                comPortBind.Mode = BindingMode.OneWay;
                comPortBind.Source = MDevice;
                comPortBind.Path = new PropertyPath("PortName");
                comPortBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(comPort, ComboBox.SelectedValueProperty, comPortBind);

                Button updatePortsButton = new();
                updatePortsButton.Content = "Refresh";
                updatePortsButton.Margin = new Thickness(15, 0, 0, 0);
                updatePortsButton.Width = 100;
                updatePortsButton.Click += new RoutedEventHandler(refreshPorts_Click);

                setting1.Children.Add(setting1Text);
                setting1.Children.Add(comPort);
                setting1.Children.Add(updatePortsButton);

                //Create stack panel for 2nd setting
                StackPanel setting2 = new();
                setting2.Orientation = Orientation.Horizontal;
                setting2.Margin = new Thickness(5, 5, 0, 0);
                deviceSettings.Children.Add(setting2);

                TextBlock setting2Text = new();
                setupTextBlock(ref setting2Text, "Baud Rate:");
                ComboBox baudRate = new();
                setupComboBox(ref baudRate, "baudRate", BaudRateList);
                baudRate.DropDownClosed += new EventHandler(baudSelect_DropDownClosed);
                Binding baudRateBind = new();
                baudRateBind.Mode = BindingMode.OneWay;
                baudRateBind.Source = MDevice;
                baudRateBind.Path = new PropertyPath("BaudRate");
                baudRateBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(baudRate, ComboBox.SelectedValueProperty, baudRateBind);
                baudRate.SelectedItem = MDevice.BaudRate;
                setting2.Children.Add(setting2Text);
                setting2.Children.Add(baudRate);


                //Create button stack panel
                StackPanel buttons = new();
                buttons.Orientation = Orientation.Horizontal;
                buttons.HorizontalAlignment = HorizontalAlignment.Center;
                deviceSettings.Children.Add(buttons);

                Button connectButton = new();
                setupButton(ref connectButton, "Connect");
                connectButton.Click += new RoutedEventHandler(ConnectToDevice);

                Button disconnectButton = new();
                setupButton(ref disconnectButton, "Disconnect");
                disconnectButton.Click += new RoutedEventHandler(DisconnectFromDevice);

                Button testReadButton = new();
                setupButton(ref testReadButton, "Test read");
                testReadButton.Click += new RoutedEventHandler(TestRead);

                buttons.Children.Add(connectButton);
                buttons.Children.Add(disconnectButton);
                buttons.Children.Add(testReadButton);

                StackPanel status = new();
                status.Orientation = Orientation.Horizontal;
                status.HorizontalAlignment = HorizontalAlignment.Right;
                deviceSettings.Children.Add(status);

                CheckBox connected = new();
                connected.IsEnabled = false;
                connected.Content = "Connection status";
                Binding connectBind = new();
                connectBind.Mode = BindingMode.OneWay;
                connectBind.Source = MDevice;
                connectBind.Path = new PropertyPath("Connected");
                connectBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(connected, CheckBox.IsCheckedProperty, connectBind);

                //NEED DELETE DEVICE BUTTON
                    
                status.Children.Add(connected);

            }
            else if (MDevice.DeviceTypeString=="Beckhoff")
            {
                //Create stack panel for 1st setting
                StackPanel setting1 = new();
                setting1.Orientation = Orientation.Horizontal;
                setting1.Margin = new Thickness(5, 5, 0, 0);
                deviceSettings.Children.Add(setting1);

                //Setting text
                TextBlock setting1Text = new();
                setupTextBlock(ref setting1Text, "AMS NET ID:");

                TextBox netID = new();
                setupTextBox(ref netID, "x.x.x.x");
                //comPort.DropDownClosed += new EventHandler(portSelect_DropDownClosed);    //NEED TO ADD AN EVENT HANDLER TO UPDATE AMSNETID

                Binding amsBind = new();
                amsBind.Mode = BindingMode.TwoWay;
                amsBind.Source = MDevice;
                amsBind.Path = new PropertyPath("AmsNetId");
                amsBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(netID, TextBox.TextProperty, amsBind);


                setting1.Children.Add(setting1Text);
                setting1.Children.Add(netID);

                StackPanel channels = new();
                channels.Margin = new Thickness(5, 5, 0, 0);
                channels.Orientation = Orientation.Horizontal;               
                deviceSettings.Children.Add(channels);

                StackPanel col1 = new();
                col1.Orientation = Orientation.Vertical;

                //Digital input 1
                CheckBox dig1CB = new();
                dig1CB.Content = "DInput Ch1";
                Binding dig1Bind = new();
                dig1Bind.Mode = BindingMode.TwoWay;
                dig1Bind.Source = MDevice.beckhoff;
                dig1Bind.Path = new PropertyPath("Dig1Connection");
                dig1Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig1CB, CheckBox.IsCheckedProperty, dig1Bind);
                dig1CB.Checked += new RoutedEventHandler(SetupHandle);
                dig1CB.IsEnabled = false;

                //Digital input 2
                CheckBox dig2CB = new();
                dig2CB.Content = "DInput Ch2";
                Binding dig2Bind = new();
                dig2Bind.Mode = BindingMode.TwoWay;
                dig2Bind.Source = MDevice.beckhoff;
                dig2Bind.Path = new PropertyPath("Dig2Connection");
                dig2Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig2CB, CheckBox.IsCheckedProperty, dig2Bind);
                dig2CB.Checked += new RoutedEventHandler(SetupHandle);
                dig2CB.IsEnabled = false;

                //Digital input 3
                CheckBox dig3CB = new();
                dig3CB.Content = "DInput Ch3";
                Binding dig3Bind = new();
                dig3Bind.Mode = BindingMode.TwoWay;
                dig3Bind.Source = MDevice.beckhoff;
                dig3Bind.Path = new PropertyPath("Dig3Connection");
                dig3Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig3CB, CheckBox.IsCheckedProperty, dig3Bind);
                dig3CB.Checked += new RoutedEventHandler(SetupHandle);
                dig3CB.IsEnabled = false;

                //Digital input 4
                CheckBox dig4CB = new();
                dig4CB.Content = "DInput Ch4";
                Binding dig4Bind = new();
                dig4Bind.Mode = BindingMode.TwoWay;
                dig4Bind.Source = MDevice.beckhoff;
                dig4Bind.Path = new PropertyPath("Dig4Connection");
                dig4Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig4CB, CheckBox.IsCheckedProperty, dig4Bind);
                dig4CB.Checked += new RoutedEventHandler(SetupHandle);
                dig4CB.IsEnabled = false;

                //Digital input 5
                CheckBox dig5CB = new();
                dig5CB.Content = "DInput Ch5";
                Binding dig5Bind = new();
                dig5Bind.Mode = BindingMode.TwoWay;
                dig5Bind.Source = MDevice.beckhoff;
                dig5Bind.Path = new PropertyPath("Dig5Connection");
                dig5Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig5CB, CheckBox.IsCheckedProperty, dig5Bind);
                dig5CB.Checked += new RoutedEventHandler(SetupHandle);
                dig5CB.IsEnabled = false;

                //Digital input 6
                CheckBox dig6CB = new();
                dig6CB.Content = "DInput Ch6";
                Binding dig6Bind = new();
                dig6Bind.Mode = BindingMode.TwoWay;
                dig6Bind.Source = MDevice.beckhoff;
                dig6Bind.Path = new PropertyPath("Dig6Connection");
                dig6Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig6CB, CheckBox.IsCheckedProperty, dig6Bind);
                dig6CB.Checked += new RoutedEventHandler(SetupHandle);
                dig6CB.IsEnabled = false;

                //Digital input 7
                CheckBox dig7CB = new();
                dig7CB.Content = "DInput Ch7";
                Binding dig7Bind = new();
                dig7Bind.Mode = BindingMode.TwoWay;
                dig7Bind.Source = MDevice.beckhoff;
                dig7Bind.Path = new PropertyPath("Dig7Connection");
                dig7Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig7CB, CheckBox.IsCheckedProperty, dig7Bind);
                dig7CB.Checked += new RoutedEventHandler(SetupHandle);
                dig7CB.IsEnabled = false;

                //Digital input 8
                CheckBox dig8CB = new();
                dig8CB.Content = "DInput Ch8";
                Binding dig8Bind = new();
                dig8Bind.Mode = BindingMode.TwoWay;
                dig8Bind.Source = MDevice.beckhoff;
                dig8Bind.Path = new PropertyPath("Dig8Connection");
                dig8Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig8CB, CheckBox.IsCheckedProperty, dig8Bind);
                dig8CB.Checked += new RoutedEventHandler(SetupHandle);
                dig8CB.IsEnabled = false;

                col1.Children.Add(dig1CB);
                col1.Children.Add(dig2CB);
                col1.Children.Add(dig3CB);
                col1.Children.Add(dig4CB);
                col1.Children.Add(dig5CB);
                col1.Children.Add(dig6CB);
                col1.Children.Add(dig7CB);
                col1.Children.Add(dig8CB);
                channels.Children.Add(col1);


                StackPanel col2 = new();
                col2.Orientation = Orientation.Vertical;

                //PT100 - 1
                CheckBox pt1CB = new();
                pt1CB.Content = "PT100 Ch1";
                Binding pt1Bind = new();
                pt1Bind.Mode = BindingMode.TwoWay;
                pt1Bind.Source = MDevice.beckhoff;
                pt1Bind.Path = new PropertyPath("Pt1Connection");
                pt1Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(pt1CB, CheckBox.IsCheckedProperty, pt1Bind);
                pt1CB.Checked += new RoutedEventHandler(SetupHandle);
                pt1CB.IsEnabled = false;

                //PT100 - 2
                CheckBox pt2CB = new();
                pt2CB.Content = "PT100 Ch2";
                Binding pt2Bind = new();
                pt2Bind.Mode = BindingMode.TwoWay;
                pt2Bind.Source = MDevice.beckhoff;
                pt2Bind.Path = new PropertyPath("Pt2Connection");
                pt2Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(pt2CB, CheckBox.IsCheckedProperty, pt2Bind);
                pt2CB.Checked += new RoutedEventHandler(SetupHandle);
                pt2CB.IsEnabled = false;

                //PT100 - 3
                CheckBox pt3CB = new();
                pt3CB.Content = "PT100 Ch3";
                Binding pt3Bind = new();
                pt3Bind.Mode = BindingMode.TwoWay;
                pt3Bind.Source = MDevice.beckhoff;
                pt3Bind.Path = new PropertyPath("Pt3Connection");
                pt3Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(pt3CB, CheckBox.IsCheckedProperty, pt3Bind);
                pt3CB.Checked += new RoutedEventHandler(SetupHandle);
                pt3CB.IsEnabled = false;

                //PT100 - 4
                CheckBox pt4CB = new();
                pt4CB.Content = "PT100 Ch4";
                Binding pt4Bind = new();
                pt4Bind.Mode = BindingMode.TwoWay;
                pt4Bind.Source = MDevice.beckhoff;
                pt4Bind.Path = new PropertyPath("Pt4Connection");
                pt4Bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(pt4CB, CheckBox.IsCheckedProperty, pt4Bind);
                pt4CB.Checked += new RoutedEventHandler(SetupHandle);
                pt4CB.IsEnabled = false;


                col2.Children.Add(pt1CB);
                col2.Children.Add(pt2CB);
                col2.Children.Add(pt3CB);
                col2.Children.Add(pt4CB);
                channels.Children.Add(col2);

                //BUTTTTTTTONS
                //Create button stack panel
                StackPanel buttons = new();
                buttons.Orientation = Orientation.Horizontal;
                buttons.HorizontalAlignment = HorizontalAlignment.Center;
                deviceSettings.Children.Add(buttons);

                Button connectButton = new();
                setupButton(ref connectButton, "Connect");
                connectButton.Click += new RoutedEventHandler(ConnectToDevice);

                Button disconnectButton = new();
                setupButton(ref disconnectButton, "Disconnect");
                disconnectButton.Click += new RoutedEventHandler(DisconnectFromDevice);

                Button testReadButton = new();
                setupButton(ref testReadButton, "Test read");
                testReadButton.Click += new RoutedEventHandler(TestRead);

                buttons.Children.Add(connectButton);
                buttons.Children.Add(disconnectButton);
                buttons.Children.Add(testReadButton);

                StackPanel status = new();
                status.Orientation = Orientation.Horizontal;
                status.HorizontalAlignment = HorizontalAlignment.Right;
                deviceSettings.Children.Add(status);

                CheckBox connected = new();
                connected.IsEnabled = false;
                connected.Content = "Connection status";
                Binding connectBind = new();
                connectBind.Mode = BindingMode.OneWay;
                connectBind.Source = MDevice;
                connectBind.Path = new PropertyPath("Connected");
                connectBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(connected, CheckBox.IsCheckedProperty, connectBind);

                //NEED DELETE DEVICE BUTTON
                Binding cbEnableBind = new();
                cbEnableBind.Mode = BindingMode.OneWay;
                cbEnableBind.Source = connected;
                cbEnableBind.Path = new PropertyPath("IsChecked");
                cbEnableBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(dig1CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(dig2CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(dig3CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(dig4CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(dig5CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(dig6CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(dig7CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(dig8CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(pt1CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(pt2CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(pt3CB, CheckBox.IsEnabledProperty, cbEnableBind);
                BindingOperations.SetBinding(pt4CB, CheckBox.IsEnabledProperty, cbEnableBind);

                status.Children.Add(connected);


            }
        }
        private async void SetupHandle(object sender, EventArgs e)
        {
            var item = sender as CheckBox;
            if ((bool)item.IsChecked)
            {
                if ((string)item.Content == "DInput Ch1")
                {
                    await MDevice.beckhoff.CreateHandleDig1();
                }
                else if ((string)item.Content == "DInput Ch2")
                {
                    await MDevice.beckhoff.CreateHandleDig2();
                }
                else if ((string)item.Content == "DInput Ch3")
                {
                    await MDevice.beckhoff.CreateHandleDig3();
                }
                else if ((string)item.Content == "DInput Ch4")
                {
                    await MDevice.beckhoff.CreateHandleDig4();
                }
                else if ((string)item.Content == "DInput Ch5")
                {
                    await MDevice.beckhoff.CreateHandleDig5();
                }
                else if ((string)item.Content == "DInput Ch6")
                {
                    await MDevice.beckhoff.CreateHandleDig6();
                }
                else if ((string)item.Content == "DInput Ch7")
                {
                    await MDevice.beckhoff.CreateHandleDig7();
                }
                else if ((string)item.Content == "DInput Ch8")
                {
                    await MDevice.beckhoff.CreateHandleDig8();
                }
                else if ((string)item.Content == "PT100 Ch1")
                {
                    await MDevice.beckhoff.CreateHandlePt1();
                }
                else if ((string)item.Content == "PT100 Ch2")
                {
                    await MDevice.beckhoff.CreateHandlePt2();
                }
                else if ((string)item.Content == "PT100 Ch3")
                {
                    await MDevice.beckhoff.CreateHandlePt3();
                }
                else if ((string)item.Content == "PT100 Ch4")
                {
                    await MDevice.beckhoff.CreateHandlePt4();
                }


            }
        }



        public async void TestRead(object sender, EventArgs e)
        {
            if(!MDevice.Connected)
            {
                Console.WriteLine("Not connected to a device");
                return;
            }
            else
            {
                Console.WriteLine( await MDevice.GetMeasurement());                
            }

        }
        
        public void ConnectToDevice(object sender, EventArgs e)
        {
            if (MDevice.Connected)
            {
                Console.WriteLine("Already connected to device");
            }
            if (MDevice.ConnectToDevice())
            {
                Console.WriteLine("Connection made");
            }
            else
            {
                Console.WriteLine("Failed to connect");
            }
        }
        public void DisconnectFromDevice(object sender, EventArgs e)
        {
            if (!MDevice.Connected)
            {
                Console.WriteLine("Not connected to a device");
            }
            if (MDevice.DisconnectFromDevice())
            {
                Console.WriteLine("Disconnected");
            }
            else
            {
                Console.WriteLine("Failed to disconnect");
            }
        }

        public void setupButton(ref Button but, string butText)
        {
            but.Content = butText;
            but.Width = 120;
            but.Margin = new Thickness(5);
            but.Height = 20;
        }


        public void setupTextBlock(ref TextBlock tb, string tbText)
        {
            tb.HorizontalAlignment = HorizontalAlignment.Right;
            tb.TextAlignment = TextAlignment.Right;
            tb.Width = 100;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Text = tbText;
        }

        public void setupTextBox(ref TextBox tb, string tbText)
        {
            tb.HorizontalAlignment = HorizontalAlignment.Right;
            tb.TextAlignment = TextAlignment.Center;
            tb.Width = 150;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Text = tbText;
            tb.Margin = new Thickness(10, 0, 0, 0);
        }

        public void setupComboBox(ref ComboBox cb, string name, ObservableCollection<string> itemSource)
        {
            cb.Name = name;
            cb.ItemsSource = itemSource;
            cb.Margin = new Thickness(10, 0, 0, 0);
            cb.HorizontalAlignment = HorizontalAlignment.Left;
            cb.VerticalAlignment = VerticalAlignment.Center;
            cb.Width = 150;
        }

        private void portSelect_DropDownClosed(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
            MDevice.PortName = (string)combo.SelectedItem;
            combo.SelectedItem = MDevice.PortName;
        }
        private void baudSelect_DropDownClosed(object sender, EventArgs e)
        {
            var baud = sender as ComboBox;
            MDevice.UpdateBaudRate((string)baud.SelectedItem);
            baud.SelectedItem = MDevice.BaudRate;
        }

        

        private void refreshPorts_Click(object sender, EventArgs e)
        {
            MDevice.UpdatePortList();
        }

    }
}
