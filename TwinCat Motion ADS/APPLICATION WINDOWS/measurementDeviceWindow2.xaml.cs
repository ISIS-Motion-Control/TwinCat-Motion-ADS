using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for accessing measurement devices
    /// </summary>
    public partial class measurementDeviceWindow2 : Window
    {
        int DeviceIndex;
        public ObservableCollection<DeviceTypes> DeviceTypeList = new(Enum.GetValues(typeof(DeviceTypes)).Cast<DeviceTypes>());
        public ObservableCollection<string> BaudRateList = new ObservableCollection<string>()
        {
            "9600", "19200","38400","57600","115200"
        };
        public ObservableCollection<string> VariableTypeList = new ObservableCollection<string>()
        {
            "string","double","short","bool"
        };
        I_MeasurementDevice MDevice;
        const int KEYENCE_CHANNELS = 16;



        /// <summary>
        /// Constructor for the class
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="mDevice"></param>
        public measurementDeviceWindow2(int deviceIndex, I_MeasurementDevice mDevice)
        {
            InitializeComponent();
            DeviceIndex = deviceIndex;
            MDevice = mDevice;
            ConstructDeviceSettingsScreen();
        }

        private void ConstructDeviceSettingsScreen()
        {

            XamlUI.TextboxBinding(deviceName, MDevice, "Name");                                 //Bind the name field          
            DeviceTypeComboBox.ItemsSource = DeviceTypeList;
            DeviceTypeComboBox.SelectedItem = MDevice.DeviceType;
            deviceSettings.Children.Clear();                                                    //Clear the settings stackpanel

            //Create button stack panel
            StackPanel buttonsStackPanel = new();
            buttonsStackPanel.Orientation = Orientation.Horizontal;
            buttonsStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            //Create buttons
            Button connectButton = new();
            Button disconnectButton = new();
            Button testReadButton = new();
            //Setup buttons
            XamlUI.SetupButton(ref connectButton, "Connect");
            XamlUI.SetupButton(ref disconnectButton, "Disconnect");
            XamlUI.SetupButton(ref testReadButton, "Test read");
            //Setup event handlers
            connectButton.Click += new RoutedEventHandler(ConnectToDevice);
            disconnectButton.Click += new RoutedEventHandler(DisconnectFromDevice);
            testReadButton.Click += new RoutedEventHandler(TestRead);
            //Add buttons to stackpanel
            buttonsStackPanel.Children.Add(connectButton);
            buttonsStackPanel.Children.Add(disconnectButton);
            buttonsStackPanel.Children.Add(testReadButton);

            //Create extra buttons stack panel
            StackPanel extraButtonsStackPanel = new();
            extraButtonsStackPanel.Orientation = Orientation.Horizontal;
            extraButtonsStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            //Create buttons
            Button updateChannelButton = new();
            Button checkChannelsButton = new();
            //Setup buttons
            XamlUI.SetupButton(ref updateChannelButton, "Update Channel");
            XamlUI.SetupButton(ref checkChannelsButton, "Check Channels");
            //Setup event handlers
            updateChannelButton.Click += new RoutedEventHandler(UpdateChannels);
            checkChannelsButton.Click += new RoutedEventHandler(CheckChannels);
            //Add buttons to stack panel
            extraButtonsStackPanel.Children.Add(updateChannelButton);
            extraButtonsStackPanel.Children.Add(checkChannelsButton);

            //Create connection status stack panel
            StackPanel statusStackPanel = new();
            statusStackPanel.Orientation = Orientation.Horizontal;
            statusStackPanel.HorizontalAlignment = HorizontalAlignment.Right;
            //Connection status feedback checkbox
            CheckBox connected = new();
            connected.IsEnabled = false;
            connected.Margin = new Thickness(15, 0, 0, 5);
            XamlUI.CheckBoxBinding("Connection status", connected, MDevice, "Connected", BindingMode.OneWay);
            //Create feedback for number of channels currently connected
            TextBlock numChannels = new();
            TextBlock numberOfChannels = new();
            XamlUI.SetupTextBlock(ref numChannels, "Channels:");
            XamlUI.SetupTextBlock(ref numberOfChannels, "Channels:", 20);
            XamlUI.TextBlockBinding(numberOfChannels, MDevice, "NumberOfChannels", "D");
            //Add statuses to stack panel
            statusStackPanel.Children.Add(numChannels);
            statusStackPanel.Children.Add(numberOfChannels);
            statusStackPanel.Children.Add(connected);

            /*if (MDevice.DeviceType == DeviceTypes.DigimaticIndicator || MDevice.DeviceType == DeviceTypes.KeyenceTM3000)
            {
                CommonRs232Window();                                                    //Settings common to all RS232 devices
                if (MDevice.DeviceType == DeviceTypes.KeyenceTM3000)                           //Extra settings for keyence TM 3000
                {
                    //Create stack panels to show the channel settings
                    StackPanel allChannels = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
                    StackPanel col1Channels = new() { Orientation = Orientation.Vertical };
                    StackPanel col2Channels = new() { Orientation = Orientation.Vertical, Margin = new Thickness(10, 0, 0, 0) };

                    //Add stack panels to screen
                    deviceSettings.Children.Add(allChannels);
                    allChannels.Children.Add(col1Channels);
                    allChannels.Children.Add(col2Channels);

                    //Populate a list to contain all channel UI elements
                    List<KeyenceChannel> keyenceChannels = new();
                    for (int i = 0; i < MDevice.keyence.KEYENCE_MAX_CHANNELS; i++)
                    {
                        keyenceChannels.Add(new(i + 1, MDevice.keyence));
                    }
                    //Populate the UI columns based on channel numbers (8 channels per column)
                    foreach(KeyenceChannel kc in keyenceChannels)
                    {
                        if(kc.channelID<9)
                        {
                            col1Channels.Children.Add(kc.sp);
                        }
                        else if(kc.channelID>8 && kc.channelID<17)
                        {
                            col2Channels.Children.Add(kc.sp);
                        }
                    }
                }
            }
            else if (MDevice.DeviceType == DeviceTypes.Beckhoff)
            {
                //Create stack panel for 1st setting
                StackPanel setting1 = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
                deviceSettings.Children.Add(setting1);

                //AMS NET ID SETTING
                TextBlock setting1Text = new();
                TextBox netID = new();
                XamlUI.SetupTextBlock(ref setting1Text, "AMS NET ID:");
                XamlUI.SetupTextBox(ref netID, "x.x.x.x");
                XamlUI.TextboxBinding(netID, MDevice, "AmsNetId");
                setting1.Children.Add(setting1Text);
                setting1.Children.Add(netID);

                //Create stack panel elements to hold channels
                StackPanel channels = new() {Orientation=Orientation.Horizontal,Margin = new Thickness(5,5,0,0) };
                StackPanel col1 = new() { Orientation = Orientation.Vertical };
                StackPanel col2 = new() { Orientation = Orientation.Vertical, Margin = new Thickness(5, 0, 0, 0) };

                //Create digital input channels
                for (int i=1;i<=MDevice.beckhoff.DIGITAL_INPUT_CHANNELS;i++)
                {
                    CheckBox cb = new();
                    XamlUI.CheckBoxBinding("DInput Ch" + i, cb, MDevice.beckhoff, "DigitalInputConnected[" + (i-1) + "]");
                    col1.Children.Add(cb);
                }
                //Create PT100 input channels
                for (int i = 1; i <= MDevice.beckhoff.PT100_CHANNELS; i++)
                {
                    CheckBox cb = new();
                    XamlUI.CheckBoxBinding("PT100 Ch" + i, cb, MDevice.beckhoff, "PT100Connected[" + (i - 1) + "]");
                    col2.Children.Add(cb);
                }
                deviceSettings.Children.Add(channels);
                channels.Children.Add(col1);
                channels.Children.Add(col2);

            }
            else if (MDevice.DeviceType == DeviceTypes.MotionChannel)
            {
                //VARIABLE TYPE
                StackPanel setting1 = new();
                setting1.Orientation = Orientation.Horizontal;
                setting1.Margin = new Thickness(5, 5, 0, 0);
                deviceSettings.Children.Add(setting1);

                TextBlock setting1Text = new();
                XamlUI.SetupTextBlock(ref setting1Text, "Variable Type");
                ComboBox variableType = new();
                XamlUI.SetupComboBox(ref variableType, "variableType", VariableTypeList);
                variableType.DropDownClosed += new EventHandler(variableType_DropDownClosed);
                XamlUI.ComboBoxBinding(VariableTypeList, variableType, MDevice.motionChannel,"VariableType");
                setting1.Children.Add(setting1Text);
                setting1.Children.Add(variableType);


                //VARIABLE STRING
                StackPanel setting2 = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
                deviceSettings.Children.Add(setting2);

                TextBlock setting2Text = new();
                XamlUI.SetupTextBlock(ref setting2Text, "Access Path");
                TextBox accessPath = new();
                XamlUI.SetupTextBox(ref accessPath, "",250);
                XamlUI.TextboxBinding(accessPath, MDevice.motionChannel, "VariableString");
                setting2.Children.Add(setting2Text);
                setting2.Children.Add(accessPath);
            }
            else if (MDevice.DeviceType == DeviceTypes.Timestamp)
            {
                //Don't need anything for a timestamp!
            }*/

            deviceSettings.Children.Add(buttonsStackPanel);
            deviceSettings.Children.Add(extraButtonsStackPanel);
            deviceSettings.Children.Add(statusStackPanel);

        }

        public void CommonRs232Window()
        {
            /*
            //Create stack panel for 1st setting
            StackPanel setting1 = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
            deviceSettings.Children.Add(setting1);

            //Com Port Setting
            TextBlock setting1Text = new();
            ComboBox comPort = new();
            Button updatePortsButton = new();
            
            MDevice.UpdatePortList();   //Generate new serial port list

            XamlUI.SetupTextBlock(ref setting1Text, "Com Port:");
            XamlUI.SetupComboBox(ref comPort, "comPort", MDevice.SerialPortList);
            XamlUI.ComboBoxBinding(MDevice.SerialPortList, comPort, MDevice, "PortName");
            XamlUI.SetupButton(ref updatePortsButton, "Refresh");

            comPort.DropDownClosed += new EventHandler(portSelect_DropDownClosed);
            updatePortsButton.Click += new RoutedEventHandler(refreshPorts_Click);

            setting1.Children.Add(setting1Text);
            setting1.Children.Add(comPort);
            setting1.Children.Add(updatePortsButton);


            //Baud rate setting
            StackPanel setting2 = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
            deviceSettings.Children.Add(setting2);

            TextBlock setting2Text = new();
            ComboBox baudRate = new();
            
            XamlUI.SetupTextBlock(ref setting2Text, "Baud Rate:");
            XamlUI.SetupComboBox(ref baudRate, "baudRate", BaudRateList);
            XamlUI.ComboBoxBinding(BaudRateList, baudRate, MDevice, "BaudRate");

            baudRate.DropDownClosed += new EventHandler(baudSelect_DropDownClosed);           
            baudRate.SelectedItem = MDevice.BaudRate;

            setting2.Children.Add(setting2Text);
            setting2.Children.Add(baudRate);
            */
        }

        public void UpdateChannels(object sender, EventArgs e)
        {
            MDevice.UpdateChannelList();
        }

        public async void TestRead(object sender, EventArgs e)
        {
            if (!MDevice.Connected)
            {
                Console.WriteLine("Not connected to a device");
                return;
            }
            else
            {
                foreach (var channel in MDevice.ChannelList)
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
            if (MDevice.Connect())
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
            if (MDevice.Disconnect())
            {
                Console.WriteLine("Disconnected");
            }
            else
            {
                Console.WriteLine("Failed to disconnect");
            }
        }

        private void Window_Closing2(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((MainWindow)(Application.Current.MainWindow)).measurementMenuItems2[DeviceIndex].Header = MDevice.Name;
        }

        private void DeviceType_DropDownClosed(object sender, EventArgs e)
        {
            if (!MDevice.Connected)
            {
                //MDevice.changeDeviceType((DeviceTypes)DeviceTypeComboBox.SelectedItem);
            }
            DeviceTypeComboBox.SelectedItem = MDevice.DeviceType.GetStringValue();
            ConstructDeviceSettingsScreen();
        }

        private void portSelect_DropDownClosed(object sender, EventArgs e)
        {
            /*
            var combo = sender as ComboBox;
            MDevice.PortName = (string)combo.SelectedItem;
            combo.SelectedItem = MDevice.PortName;*/
        }

        private void baudSelect_DropDownClosed(object sender, EventArgs e)
        {
            /*
            var baud = sender as ComboBox;
            MDevice.UpdateBaudRate((string)baud.SelectedItem);
            baud.SelectedItem = MDevice.BaudRate;*/
        }

        private void variableType_DropDownClosed(object sender, EventArgs e)
        {
            /*
            var combo = sender as ComboBox;
            MDevice.motionChannel.VariableType = (string)combo.SelectedItem;
            combo.SelectedItem = MDevice.motionChannel.VariableType;
            */
        }

        private void refreshPorts_Click(object sender, EventArgs e)
        {
            /*
            MDevice.UpdatePortList();
            */
        }

    }

    
}