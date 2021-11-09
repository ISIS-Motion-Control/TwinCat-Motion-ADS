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
        "KeyenceTM3000"
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
            if(MDevice.DeviceTypeString== "DigimaticIndicator")
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
            but.Width = 150;
            but.Margin = new Thickness(20);
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
            Console.WriteLine(combo.SelectedValue);
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
