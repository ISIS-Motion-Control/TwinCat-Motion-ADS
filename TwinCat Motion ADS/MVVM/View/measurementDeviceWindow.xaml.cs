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
        "Beckhoff",
        "MotionChannel",
        "Timestamp"
        };
        public ObservableCollection<string> BaudRateList = new ObservableCollection<string>()
        {
            "9600", "19200","38400","57600","115200"
        };
        public ObservableCollection<string> VariableTypeList = new ObservableCollection<string>()
        {
            "string","double","short","bool"
        };
        MeasurementDevice MDevice;
        public measurementDeviceWindow(int deviceIndex, MeasurementDevice mDevice)
        {
            
            InitializeComponent();          
            DeviceIndex = deviceIndex;
            MDevice = mDevice;


            //Create the Name field for the measurement device
            Binding deviceNameBind = new();
            deviceNameBind.Mode = BindingMode.TwoWay;
            deviceNameBind.Source = MDevice;
            deviceNameBind.Path = new PropertyPath("Name");
            deviceNameBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(deviceName, TextBox.TextProperty, deviceNameBind);

            //Create the device type drop down list
            DeviceType.ItemsSource = DeviceTypeList;
            Binding deviceTypeBind = new();
            deviceTypeBind.Mode = BindingMode.OneWay;
            deviceTypeBind.Source = MDevice;
            deviceTypeBind.Path = new PropertyPath("DeviceTypeString");
            deviceTypeBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(DeviceType, ComboBox.SelectedValueProperty, deviceTypeBind);

            //Setup the local copy of the channel list based on selected device (because we're creating a new instance each time we open the window which is bad but I need to implement a fix)            
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
            //Create button stack panel
            StackPanel buttons = new();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Center;
            

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

            StackPanel extraButtons = new();
            extraButtons.Orientation = Orientation.Horizontal;
            extraButtons.HorizontalAlignment = HorizontalAlignment.Center;
            

            Button updateChannelButton = new();
            setupButton(ref updateChannelButton, "Update Channel");
            updateChannelButton.Click += new RoutedEventHandler(UpdateChannels);
            Button checkChannelsButton = new();
            setupButton(ref checkChannelsButton, "Check Channels");
            checkChannelsButton.Click += new RoutedEventHandler(CheckChannels);


            extraButtons.Children.Add(updateChannelButton);
            extraButtons.Children.Add(checkChannelsButton);

            StackPanel status = new();
            status.Orientation = Orientation.Horizontal;
            status.HorizontalAlignment = HorizontalAlignment.Right;
            

            CheckBox connected = new();
            connected.IsEnabled = false;
            connected.Content = "Connection status";
            connected.Margin = new Thickness(15, 0, 0, 5);
            Binding connectBind = new();
            connectBind.Mode = BindingMode.OneWay;
            connectBind.Source = MDevice;
            connectBind.Path = new PropertyPath("Connected");
            connectBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(connected, CheckBox.IsCheckedProperty, connectBind);

            TextBlock numChannels = new();
            setupTextBlock(ref numChannels, "Channels:");
            TextBlock numberOfChannels = new();
            setupTextBlock(ref numberOfChannels, "0");
            numberOfChannels.Width = 20;
            Binding chBind = new();
            chBind.Mode = BindingMode.OneWay;
            chBind.Source = MDevice;
            chBind.Path = new PropertyPath("NumberOfChannels");
            chBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(numberOfChannels, TextBlock.TextProperty, chBind);
            
            status.Children.Add(numChannels);
            status.Children.Add(numberOfChannels);
            status.Children.Add(connected);

            if (MDevice.DeviceTypeString== "DigimaticIndicator" || MDevice.DeviceTypeString == "KeyenceTM3000")
            {
                CommonRs232Window();
                if(MDevice.DeviceTypeString=="KeyenceTM3000")
                {
                    StackPanel allChannels = new();
                    StackPanel col1Channels = new();
                    StackPanel col2Channels = new();
                    allChannels.Orientation = Orientation.Horizontal;
                    col1Channels.Orientation = Orientation.Vertical;
                    col2Channels.Orientation = Orientation.Vertical;
                    col2Channels.Margin = new Thickness(10, 0, 0, 0);
                    allChannels.Margin = new Thickness(5, 5, 0, 0);
                    deviceSettings.Children.Add(allChannels);
                    allChannels.Children.Add(col1Channels);
                    allChannels.Children.Add(col2Channels);


                    //Channel 1
                    StackPanel keyenceCh1 = new();
                    keyenceCh1.Orientation = Orientation.Horizontal;
                    TextBox ch1Name = new();
                    setupTextBox(ref ch1Name, "*Ch1*", 100);
                    TextboxBinding(ch1Name, MDevice.keyence, "Ch1Name");
                    CheckBox Ch1 = new();
                    CheckBoxBinding("Ch1",Ch1, MDevice.keyence, "Ch1Connected");
                    keyenceCh1.Children.Add(ch1Name);
                    keyenceCh1.Children.Add(Ch1);

                    //Channel 2
                    StackPanel keyenceCh2 = new();
                    keyenceCh2.Orientation = Orientation.Horizontal;
                    TextBox ch2Name = new();
                    setupTextBox(ref ch2Name, "*Ch2*",100);
                    TextboxBinding(ch2Name, MDevice.keyence, "Ch2Name");
                    CheckBox Ch2 = new();
                    CheckBoxBinding("Ch2", Ch2, MDevice.keyence, "Ch2Connected");
                    keyenceCh2.Children.Add(ch2Name);
                    keyenceCh2.Children.Add(Ch2);
                    
                    //Channel 3
                    StackPanel keyenceCh3 = new();
                    keyenceCh3.Orientation = Orientation.Horizontal;
                    TextBox ch3Name = new();
                    setupTextBox(ref ch3Name, "*Ch3*", 100);
                    TextboxBinding(ch3Name, MDevice.keyence, "Ch3Name");
                    CheckBox Ch3 = new();
                    CheckBoxBinding("Ch3", Ch3, MDevice.keyence, "Ch3Connected");
                    keyenceCh3.Children.Add(ch3Name);
                    keyenceCh3.Children.Add(Ch3);

                    //Channel 4
                    StackPanel keyenceCh4 = new();
                    keyenceCh4.Orientation = Orientation.Horizontal;
                    TextBox ch4Name = new();
                    setupTextBox(ref ch4Name, "*Ch4*", 100);
                    TextboxBinding(ch4Name, MDevice.keyence, "Ch4Name");
                    CheckBox Ch4 = new();
                    CheckBoxBinding("Ch4", Ch4, MDevice.keyence, "Ch4Connected");
                    keyenceCh4.Children.Add(ch4Name);
                    keyenceCh4.Children.Add(Ch4);

                    //Channel 5
                    StackPanel keyenceCh5 = new();
                    keyenceCh5.Orientation = Orientation.Horizontal;
                    TextBox ch5Name = new();
                    setupTextBox(ref ch5Name, "*Ch5*", 100);
                    TextboxBinding(ch5Name, MDevice.keyence, "Ch5Name");
                    CheckBox Ch5 = new();
                    CheckBoxBinding("Ch5", Ch5, MDevice.keyence, "Ch5Connected");
                    keyenceCh5.Children.Add(ch5Name);
                    keyenceCh5.Children.Add(Ch5);

                    //Channel 6
                    StackPanel keyenceCh6 = new();
                    keyenceCh6.Orientation = Orientation.Horizontal;
                    TextBox ch6Name = new();
                    setupTextBox(ref ch6Name, "*Ch6*", 100);
                    TextboxBinding(ch6Name, MDevice.keyence, "Ch6Name");
                    CheckBox Ch6 = new();
                    CheckBoxBinding("Ch6", Ch6, MDevice.keyence, "Ch6Connected");
                    keyenceCh6.Children.Add(ch6Name);
                    keyenceCh6.Children.Add(Ch6);

                    //Channel 7
                    StackPanel keyenceCh7 = new();
                    keyenceCh7.Orientation = Orientation.Horizontal;
                    TextBox ch7Name = new();
                    setupTextBox(ref ch7Name, "*Ch7*", 100);
                    TextboxBinding(ch7Name, MDevice.keyence, "Ch7Name");
                    CheckBox Ch7 = new();
                    CheckBoxBinding("Ch7", Ch7, MDevice.keyence, "Ch7Connected");
                    keyenceCh7.Children.Add(ch7Name);
                    keyenceCh7.Children.Add(Ch7);

                    //Channel 8
                    StackPanel keyenceCh8 = new();
                    keyenceCh8.Orientation = Orientation.Horizontal;
                    TextBox ch8Name = new();
                    setupTextBox(ref ch8Name, "*Ch8*", 100);
                    TextboxBinding(ch8Name, MDevice.keyence, "Ch8Name");
                    CheckBox Ch8 = new();
                    CheckBoxBinding("Ch8", Ch8, MDevice.keyence, "Ch8Connected");
                    keyenceCh8.Children.Add(ch8Name);
                    keyenceCh8.Children.Add(Ch8);

                    //Channel 9
                    StackPanel keyenceCh9 = new();
                    keyenceCh9.Orientation = Orientation.Horizontal;
                    TextBox ch9Name = new();
                    setupTextBox(ref ch9Name, "*Ch9*", 100);
                    TextboxBinding(ch9Name, MDevice.keyence, "Ch9Name");
                    CheckBox Ch9 = new();
                    CheckBoxBinding("Ch9", Ch9, MDevice.keyence, "Ch9Connected");
                    keyenceCh9.Children.Add(ch9Name);
                    keyenceCh9.Children.Add(Ch9);

                    //Channel 10
                    StackPanel keyenceCh10 = new();
                    keyenceCh10.Orientation = Orientation.Horizontal;
                    TextBox ch10Name = new();
                    setupTextBox(ref ch10Name, "*Ch10*", 100);
                    TextboxBinding(ch10Name, MDevice.keyence, "Ch10Name");
                    CheckBox Ch10 = new();
                    CheckBoxBinding("Ch10", Ch10, MDevice.keyence, "Ch10Connected");
                    keyenceCh10.Children.Add(ch10Name);
                    keyenceCh10.Children.Add(Ch10);

                    //Channel 11
                    StackPanel keyenceCh11 = new();
                    keyenceCh11.Orientation = Orientation.Horizontal;
                    TextBox ch11Name = new();
                    setupTextBox(ref ch11Name, "*Ch11*", 100);
                    TextboxBinding(ch11Name, MDevice.keyence, "Ch11Name");
                    CheckBox Ch11 = new();
                    CheckBoxBinding("Ch11", Ch11, MDevice.keyence, "Ch11Connected");
                    keyenceCh11.Children.Add(ch11Name);
                    keyenceCh11.Children.Add(Ch11);

                    //Channel 12
                    StackPanel keyenceCh12 = new();
                    keyenceCh12.Orientation = Orientation.Horizontal;
                    TextBox ch12Name = new();
                    setupTextBox(ref ch12Name, "*Ch12*", 100);
                    TextboxBinding(ch12Name, MDevice.keyence, "Ch12Name");
                    CheckBox Ch12 = new();
                    CheckBoxBinding("Ch12", Ch12, MDevice.keyence, "Ch12Connected");
                    keyenceCh12.Children.Add(ch12Name);
                    keyenceCh12.Children.Add(Ch12);

                    //Channel 13
                    StackPanel keyenceCh13 = new();
                    keyenceCh13.Orientation = Orientation.Horizontal;
                    TextBox ch13Name = new();
                    setupTextBox(ref ch13Name, "*Ch13*", 100);
                    TextboxBinding(ch13Name, MDevice.keyence, "Ch13Name");
                    CheckBox Ch13 = new();
                    CheckBoxBinding("Ch13", Ch13, MDevice.keyence, "Ch13Connected");
                    keyenceCh13.Children.Add(ch13Name);
                    keyenceCh13.Children.Add(Ch13);

                    //Channel 14
                    StackPanel keyenceCh14 = new();
                    keyenceCh14.Orientation = Orientation.Horizontal;
                    TextBox ch14Name = new();
                    setupTextBox(ref ch14Name, "*Ch14*", 100);
                    TextboxBinding(ch14Name, MDevice.keyence, "Ch14Name");
                    CheckBox Ch14 = new();
                    CheckBoxBinding("Ch14", Ch14, MDevice.keyence, "Ch14Connected");
                    keyenceCh14.Children.Add(ch14Name);
                    keyenceCh14.Children.Add(Ch14);

                    //Channel 15
                    StackPanel keyenceCh15 = new();
                    keyenceCh15.Orientation = Orientation.Horizontal;
                    TextBox ch15Name = new();
                    setupTextBox(ref ch15Name, "*Ch15*", 100);
                    TextboxBinding(ch15Name, MDevice.keyence, "Ch15Name");
                    CheckBox Ch15 = new();
                    CheckBoxBinding("Ch15", Ch15, MDevice.keyence, "Ch15Connected");
                    keyenceCh15.Children.Add(ch15Name);
                    keyenceCh15.Children.Add(Ch15);

                    //Channel 16
                    StackPanel keyenceCh16 = new();
                    keyenceCh16.Orientation = Orientation.Horizontal;
                    TextBox ch16Name = new();
                    setupTextBox(ref ch16Name, "*Ch16*", 100);
                    TextboxBinding(ch16Name, MDevice.keyence, "Ch16Name");
                    CheckBox Ch16 = new();
                    CheckBoxBinding("Ch16", Ch16, MDevice.keyence, "Ch16Connected");
                    keyenceCh16.Children.Add(ch16Name);
                    keyenceCh16.Children.Add(Ch16);

                    col1Channels.Children.Add(keyenceCh1);
                    col1Channels.Children.Add(keyenceCh2);
                    col1Channels.Children.Add(keyenceCh3);
                    col1Channels.Children.Add(keyenceCh4);
                    col1Channels.Children.Add(keyenceCh5);
                    col1Channels.Children.Add(keyenceCh6);
                    col1Channels.Children.Add(keyenceCh7);
                    col1Channels.Children.Add(keyenceCh8);
                    col2Channels.Children.Add(keyenceCh9);
                    col2Channels.Children.Add(keyenceCh10);
                    col2Channels.Children.Add(keyenceCh11);
                    col2Channels.Children.Add(keyenceCh12);
                    col2Channels.Children.Add(keyenceCh13);
                    col2Channels.Children.Add(keyenceCh14);
                    col2Channels.Children.Add(keyenceCh15);
                    col2Channels.Children.Add(keyenceCh16);

                }
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
                CheckBoxBinding("DInput Ch1", dig1CB, MDevice.beckhoff, "Dig1Connection");

                //Digital input 2
                CheckBox dig2CB = new();
                CheckBoxBinding("DInput Ch2", dig2CB, MDevice.beckhoff, "Dig2Connection");

                //Digital input 3
                CheckBox dig3CB = new();
                CheckBoxBinding("DInput Ch3", dig3CB, MDevice.beckhoff, "Dig3Connection");

                //Digital input 4
                CheckBox dig4CB = new();
                CheckBoxBinding("DInput Ch4", dig4CB, MDevice.beckhoff, "Dig4Connection");


                //Digital input 5
                CheckBox dig5CB = new();
                CheckBoxBinding("DInput Ch5", dig5CB, MDevice.beckhoff, "Dig5Connection");

                //Digital input 6
                CheckBox dig6CB = new();
                CheckBoxBinding("DInput Ch6", dig6CB, MDevice.beckhoff, "Dig6Connection");

                //Digital input 7
                CheckBox dig7CB = new();
                CheckBoxBinding("DInput Ch7", dig7CB, MDevice.beckhoff, "Dig7Connection");

                //Digital input 8
                CheckBox dig8CB = new();
                CheckBoxBinding("DInput Ch8", dig8CB, MDevice.beckhoff, "Dig8Connection");


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
                col2.Margin = new Thickness(5, 0, 0, 0);

                //PT100 - 1
                CheckBox pt1CB = new();
                CheckBoxBinding("PT100 Ch1", pt1CB, MDevice.beckhoff, "Pt1Connection");

                //PT100 - 2
                CheckBox pt2CB = new();
                CheckBoxBinding("PT100 Ch2", pt2CB, MDevice.beckhoff, "Pt2Connection");

                //PT100 - 3
                CheckBox pt3CB = new();
                CheckBoxBinding("PT100 Ch3", pt3CB, MDevice.beckhoff, "Pt3Connection");

                //PT100 - 4
                CheckBox pt4CB = new();
                CheckBoxBinding("PT100 Ch4", pt4CB, MDevice.beckhoff, "Pt4Connection");

                col2.Children.Add(pt1CB);
                col2.Children.Add(pt2CB);
                col2.Children.Add(pt3CB);
                col2.Children.Add(pt4CB);
                channels.Children.Add(col2);

            }
            else if(MDevice.DeviceTypeString=="MotionChannel")
            {
                //VARIABLE TYPE
                StackPanel setting1 = new();
                setting1.Orientation = Orientation.Horizontal;
                setting1.Margin = new Thickness(5, 5, 0, 0);
                deviceSettings.Children.Add(setting1);
               
                TextBlock setting1Text = new();
                setupTextBlock(ref setting1Text, "Variable Type");
                ComboBox variableType = new();
                setupComboBox(ref variableType, "variableType", VariableTypeList);
                variableType.DropDownClosed += new EventHandler(variableType_DropDownClosed);

                Binding varTypeBind = new();
                varTypeBind.Mode = BindingMode.OneWay;
                varTypeBind.Source = MDevice.motionChannel;
                varTypeBind.Path = new PropertyPath("VariableType");
                varTypeBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(variableType, ComboBox.SelectedItemProperty, varTypeBind);

                setting1.Children.Add(setting1Text);
                setting1.Children.Add(variableType);

                //VARIABLE STRING
                StackPanel setting2 = new();
                setting2.Orientation = Orientation.Horizontal;
                setting2.Margin = new Thickness(5, 5, 0, 0);
                deviceSettings.Children.Add(setting2);

                TextBlock setting2Text = new();
                setupTextBlock(ref setting2Text, "Access Path");
                TextBox accessPath = new();
                setupTextBox(ref accessPath, "");
                accessPath.Width = 250;

                Binding accessBind = new();
                accessBind.Mode = BindingMode.TwoWay;
                accessBind.Source = MDevice.motionChannel;
                accessBind.Path = new PropertyPath("VariableString");
                accessBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(accessPath, TextBox.TextProperty, accessBind);

                setting2.Children.Add(setting2Text);
                setting2.Children.Add(accessPath);
            }
            else if(MDevice.DeviceTypeString=="Timestamp")
            {
                //Don't need anything for a timestamp!
            }

            deviceSettings.Children.Add(buttons);
            deviceSettings.Children.Add(extraButtons);
            deviceSettings.Children.Add(status);

        }

        private void CheckBoxBinding(string content, DependencyObject item, object source, string pp)
        {
            ((CheckBox)item).Content = content;
            Binding checkBoxBind = new();
            checkBoxBind.Mode = BindingMode.TwoWay;
            checkBoxBind.Source = source;
            checkBoxBind.Path = new PropertyPath(pp);
            checkBoxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(item, CheckBox.IsCheckedProperty, checkBoxBind);  
        }
        private void TextboxBinding(DependencyObject item, object source, string pp)
        {
            Binding TextboxBind = new();
            TextboxBind.Mode = BindingMode.TwoWay;
            TextboxBind.Source = source;
            TextboxBind.Path = new PropertyPath(pp);
            TextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(item, TextBox.TextProperty, TextboxBind);
        }

        public void CommonRs232Window()
        {
            //Create stack panel for 1st setting
            StackPanel setting1 = new();
            setting1.Orientation = Orientation.Horizontal;
            setting1.Margin = new Thickness(5, 5, 0, 0);
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
        }

        public void UpdateChannels(object sender, EventArgs e)
        {
            MDevice.UpdateChannelList();
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
                foreach(var channel in MDevice.ChannelList)
                {
                    var measurement = await MDevice.GetChannelMeasurement(channel.Item2);
                    Console.WriteLine(channel.Item1 + ": " + measurement);
                }
            }
        }

        public void CheckChannels(object sender, EventArgs e)
        {
            MDevice.ChannelList.ForEach(i => Console.WriteLine(i.Item1 + ":" + i.Item2));
            Console.WriteLine("");//write a new line
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

        public void setupTextBox(ref TextBox tb, string tbText, double wd = 150)
        {
            tb.HorizontalAlignment = HorizontalAlignment.Right;
            tb.TextAlignment = TextAlignment.Center;
            tb.Width = wd;
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
        
        private void variableType_DropDownClosed(object sender,EventArgs e)
        {
            var combo = sender as ComboBox;
            MDevice.motionChannel.VariableType = (string)combo.SelectedItem;
            combo.SelectedItem = MDevice.motionChannel.VariableType;
        }

        private void refreshPorts_Click(object sender, EventArgs e)
        {
            MDevice.UpdatePortList();
        }

    }
}
