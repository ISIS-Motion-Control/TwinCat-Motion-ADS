using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TwinCat_Motion_ADS.MeasurementDevice;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for accessing measurement devices
    /// </summary>
    public partial class measurementDeviceWindow : Window
    {
        int DeviceIndex;     
        public I_MeasurementDevice MDevice;
        public ObservableCollection<DeviceTypes> DeviceTypeList = new(Enum.GetValues(typeof(DeviceTypes)).Cast<DeviceTypes>());

        public measurementDeviceWindow(int deviceIndex, I_MeasurementDevice mDevice)
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

            switch(MDevice.DeviceType)
            {
                case DeviceTypes.DigimaticIndicator:
                    CommonRs232Window();
                    //Create UI for COSINE error calculation
                    //Create UI elements
                    CheckBox enableCosineCalculationCheckBox = new CheckBox();
                    Button resetCosineCalculationButton = new Button();
                    TextBox initialValue = new TextBox() { Name = "initialValue"};
                    Button initialValue_ReadIn = new Button();
                    TextBox distanceTraveled = new TextBox() { Name = "distanceTraveled" };
                    TextBox finalValue = new TextBox() { Name = "finalValue" };
                    Button finalValue_ReadIn = new Button();

                    //setup UI elements
                    XamlUI.SetupButton(ref resetCosineCalculationButton, "RESET");
                    XamlUI.SetupButton(ref initialValue_ReadIn, "Read in");
                    XamlUI.SetupButton(ref finalValue_ReadIn, "Read in");
                    XamlUI.SetupTextBox(ref initialValue, "Initial Value");
                    XamlUI.SetupTextBox(ref distanceTraveled, "Distance Traveled");
                    XamlUI.SetupTextBox(ref finalValue, "Final Value");

                    //Setup event handlers
                    resetCosineCalculationButton.Click += new RoutedEventHandler(ResetCosineCalculation);
                    initialValue_ReadIn.Click += new RoutedEventHandler(InitialValue_ReadIn);
                    finalValue_ReadIn.Click += new RoutedEventHandler(FinalValue_ReadIn);

                    //Create grid to show the UI
                    Grid cosineCorrection = new() { Width = 300, Height = 150, HorizontalAlignment = HorizontalAlignment.Center};

                    //Define the Columns
                    ColumnDefinition colDef0 = new ColumnDefinition();
                    ColumnDefinition colDef1 = new ColumnDefinition();
                    cosineCorrection.ColumnDefinitions.Add(colDef0);
                    cosineCorrection.ColumnDefinitions.Add(colDef1);

                    //Define the Rows
                    RowDefinition rowDef0 = new RowDefinition();
                    RowDefinition rowDef1 = new RowDefinition();
                    RowDefinition rowDef2 = new RowDefinition();
                    RowDefinition rowDef3 = new RowDefinition();
                    cosineCorrection.RowDefinitions.Add(rowDef0);
                    cosineCorrection.RowDefinitions.Add(rowDef1);
                    cosineCorrection.RowDefinitions.Add(rowDef2);
                    cosineCorrection.RowDefinitions.Add(rowDef3);

                    //Add grid to screen
                    deviceSettings.Children.Add(enableCosineCalculationCheckBox);
                    deviceSettings.Children.Add(cosineCorrection);

                    //Add UI elements to grid
                    cosineCorrection.Children.Add(resetCosineCalculationButton);
                    cosineCorrection.Children.Add(initialValue);
                    cosineCorrection.Children.Add(initialValue_ReadIn);
                    cosineCorrection.Children.Add(distanceTraveled);
                    cosineCorrection.Children.Add(finalValue);
                    cosineCorrection.Children.Add(finalValue_ReadIn);

                    //Order UI elements in grid
                    Grid.SetRow(resetCosineCalculationButton, 0);
                    Grid.SetRow(initialValue, 1);
                    Grid.SetRow(initialValue_ReadIn, 1);
                    Grid.SetRow(distanceTraveled, 2);
                    Grid.SetRow(finalValue, 3);
                    Grid.SetRow(finalValue_ReadIn, 3);

                    Grid.SetColumn(initialValue, 0);
                    Grid.SetColumn(distanceTraveled, 0);
                    Grid.SetColumn(finalValue, 0);
                    Grid.SetColumn(resetCosineCalculationButton, 1);
                    Grid.SetColumn(initialValue_ReadIn, 1);
                    Grid.SetColumn(finalValue_ReadIn, 1);
                    
                    break;

                case DeviceTypes.KeyenceTM3000:
                    CommonRs232Window();
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
                    for (int i = 0; i < ((MD_KeyenceTM3000)MDevice).KEYENCE_MAX_CHANNELS; i++)
                    {
                        keyenceChannels.Add(new(i + 1, ((MD_KeyenceTM3000)MDevice)));
                    }
                    //Populate the UI columns based on channel numbers (8 channels per column)
                    foreach (KeyenceChannel kc in keyenceChannels)
                    {
                        if (kc.channelID < 9)
                        {
                            col1Channels.Children.Add(kc.sp);
                        }
                        else if (kc.channelID > 8 && kc.channelID < 17)
                        {
                            col2Channels.Children.Add(kc.sp);
                        }
                    }
                    break;

                case DeviceTypes.Beckhoff:
                    //Create stack panel for 1st setting
                    StackPanel setting1 = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
                    deviceSettings.Children.Add(setting1);

                    //AMS NET ID SETTING
                    TextBlock setting1Text = new();
                    TextBox netID = new();
                    XamlUI.SetupTextBlock(ref setting1Text, "AMS NET ID:");
                    XamlUI.SetupTextBox(ref netID, "x.x.x.x");
                    XamlUI.TextboxBinding(netID, ((MD_Beckhoff)MDevice).Plc, "ID");
                    setting1.Children.Add(setting1Text);
                    setting1.Children.Add(netID);

                    //Create stack panel elements to hold channels
                    StackPanel channels = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
                    StackPanel col1 = new() { Orientation = Orientation.Vertical };
                    StackPanel col2 = new() { Orientation = Orientation.Vertical, Margin = new Thickness(5, 0, 0, 0) };

                    //Create digital input channels
                    for (int i = 1; i <= ((MD_Beckhoff)MDevice).DIGITAL_INPUT_CHANNELS; i++)
                    {
                        CheckBox cb = new();
                        XamlUI.CheckBoxBinding("DInput Ch" + i, cb, ((MD_Beckhoff)MDevice), "DigitalInputConnected[" + (i - 1) + "]");
                        col1.Children.Add(cb);
                    }
                    //Create PT100 input channels
                    for (int i = 1; i <= ((MD_Beckhoff)MDevice).PT100_CHANNELS; i++)
                    {
                        CheckBox cb = new();
                        XamlUI.CheckBoxBinding("PT100 Ch" + i, cb, ((MD_Beckhoff)MDevice), "PT100Connected[" + (i - 1) + "]");
                        col2.Children.Add(cb);
                    }
                    deviceSettings.Children.Add(channels);
                    channels.Children.Add(col1);
                    channels.Children.Add(col2);
                    break;

                case DeviceTypes.MotionChannel:
                    //VARIABLE TYPE
                    StackPanel setting_motionCh = new();
                    setting_motionCh.Orientation = Orientation.Horizontal;
                    setting_motionCh.Margin = new Thickness(5, 5, 0, 0);
                    deviceSettings.Children.Add(setting_motionCh);

                    TextBlock settingText_motionCh = new();
                    XamlUI.SetupTextBlock(ref settingText_motionCh, "Variable Type");
                    ComboBox variableType = new();
                    XamlUI.SetupComboBox(ref variableType, "variableType", ((MD_MotionControllerChannel)MDevice).VariableTypeList);
                    variableType.DropDownClosed += new EventHandler(variableType_DropDownClosed);
                    XamlUI.ComboBoxBinding(((MD_MotionControllerChannel)MDevice).VariableTypeList, variableType, ((MD_MotionControllerChannel)MDevice), "VariableType");
                    setting_motionCh.Children.Add(settingText_motionCh);
                    setting_motionCh.Children.Add(variableType);


                    //VARIABLE STRING
                    StackPanel setting2 = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
                    deviceSettings.Children.Add(setting2);

                    TextBlock setting2Text = new();
                    XamlUI.SetupTextBlock(ref setting2Text, "Access Path");
                    TextBox accessPath = new();
                    XamlUI.SetupTextBox(ref accessPath, "", 250);
                    XamlUI.TextboxBinding(accessPath, ((MD_MotionControllerChannel)MDevice), "VariableString");
                    setting2.Children.Add(setting2Text);
                    setting2.Children.Add(accessPath);
                    break;

                case DeviceTypes.RenishawXL80:
                    break;

                case DeviceTypes.Timestamp:
                    //nothing needed
                    break;

                case DeviceTypes.NoneSelected:
                    //nothing needed
                    break;
            }
            deviceSettings.Children.Add(buttonsStackPanel);
            deviceSettings.Children.Add(extraButtonsStackPanel);
            deviceSettings.Children.Add(statusStackPanel);


            Button deleteDeviceButton = new();
            //Setup buttons
            XamlUI.SetupButton(ref deleteDeviceButton, "Delete Device");
            //Setup event handlers
            deleteDeviceButton.Click += new RoutedEventHandler(DeleteDevice);
            deviceSettings.Children.Add(deleteDeviceButton);
        }

        public void CommonRs232Window()
        {


            //Create stack panel for 1st setting
            StackPanel setting1 = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 0) };
            deviceSettings.Children.Add(setting1);

            //Com Port Setting
            TextBlock setting1Text = new();
            ComboBox comPort = new();
            Button updatePortsButton = new();

            ((BaseRs232MeasurementDevice)MDevice).UpdatePortList();   //Generate new serial port list

            XamlUI.SetupTextBlock(ref setting1Text, "Com Port:");
            XamlUI.SetupComboBox(ref comPort, "comPort", ((BaseRs232MeasurementDevice)MDevice).SerialPortList);
            XamlUI.ComboBoxBinding(((BaseRs232MeasurementDevice)MDevice).SerialPortList, comPort, ((BaseRs232MeasurementDevice)MDevice), "PortName");
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
            XamlUI.SetupComboBox(ref baudRate, "baudRate", ((BaseRs232MeasurementDevice)MDevice).BaudRateList);
            XamlUI.ComboBoxBinding(((BaseRs232MeasurementDevice)MDevice).BaudRateList, baudRate, ((BaseRs232MeasurementDevice)MDevice), "BaudRate");

            baudRate.DropDownClosed += new EventHandler(baudSelect_DropDownClosed);
            baudRate.SelectedItem = ((BaseRs232MeasurementDevice)MDevice).BaudRate.ToString();

            setting2.Children.Add(setting2Text);
            setting2.Children.Add(baudRate);
            
        }

        public void UpdateChannels(object sender, EventArgs e)
        {
            MDevice.UpdateChannelList();
        }

        public void DeleteDevice(object sender, EventArgs e)
        {
            if(MDevice.Connected)
            {
                Console.WriteLine("Cannot delete connected device");
                return;
            }
            MDevice = null;
            ((MainWindow)Application.Current.MainWindow).MeasurementDevices.RemoveDevice(DeviceIndex);
            ((MainWindow)Application.Current.MainWindow).RemoveMeasurementMenuItem(DeviceIndex);
            this.Close();
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
        public void ResetCosineCalculation(object sender, EventArgs e)
        {
            Console.WriteLine("COSINE Calculaion Reset");
            foreach (TextBox tb in FindVisualChilds<TextBox>(this))
            {
                if (tb.Name == "initialValue")
                {
                    tb.Text = String.Empty;
                }
                if (tb.Name == "distanceTraveled")
                {
                    tb.Text = String.Empty;
                }
                if (tb.Name == "finalValue")
                {
                    tb.Text = String.Empty;
                }
            }
        }
        public async void InitialValue_ReadIn(object sender, EventArgs e)
        {
            
            if (!MDevice.Connected)
            {
                Console.WriteLine("Not connected to a device");
                return;
            }
            else
            {
                string measurement = string.Empty;
                foreach (var channel in MDevice.ChannelList)
                {
                    measurement = await MDevice.GetChannelMeasurement(channel.Item2);
                    Console.WriteLine(channel.Item1 + ": " + measurement);
                }
                foreach (TextBox tb in FindVisualChilds<TextBox>(this))
                {
                    if (tb.Name == "initialValue")
                    {
                        tb.Text = measurement;
                    }
                }
            }
        }
        public async void FinalValue_ReadIn(object sender, EventArgs e)
        {
            if (!MDevice.Connected)
            {
                Console.WriteLine("Not connected to a device");
                return;
            }
            else
            {
                string measurement = string.Empty;
                foreach (var channel in MDevice.ChannelList)
                {
                    measurement = await MDevice.GetChannelMeasurement(channel.Item2);
                    Console.WriteLine(channel.Item1 + ": " + measurement);
                }
                foreach (TextBox tb in FindVisualChilds<TextBox>(this))
                {
                    if (tb.Name == "finalValue")
                    {
                        tb.Text = measurement;
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChilds<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChilds<T>(ithChild)) yield return childOfChild;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(MDevice != null)
            {
                ((MainWindow)(Application.Current.MainWindow)).measurementMenuItems[DeviceIndex].Header = MDevice.Name;
            }            
        }

        private void DeviceType_DropDownClosed(object sender, EventArgs e)
        {
            if (!MDevice.Connected)
            {
                ((MainWindow)(Application.Current.MainWindow)).MeasurementDevices.ChangeDeviceType(DeviceIndex, (DeviceTypes)DeviceTypeComboBox.SelectedItem);
                MDevice = ((MainWindow)(Application.Current.MainWindow)).MeasurementDevices.MeasurementDeviceList[DeviceIndex];
            }
            DeviceTypeComboBox.SelectedItem = MDevice.DeviceType.GetStringValue();
            ConstructDeviceSettingsScreen();
        }

        private void portSelect_DropDownClosed(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
            ((BaseRs232MeasurementDevice)MDevice).PortName = (string)combo.SelectedItem;
            combo.SelectedItem = ((BaseRs232MeasurementDevice)MDevice).PortName;
        }

        private void baudSelect_DropDownClosed(object sender, EventArgs e)
        {           
            var baud = sender as ComboBox;
            ((BaseRs232MeasurementDevice)MDevice).UpdateBaudRate((string)baud.SelectedItem);
            baud.SelectedItem = ((BaseRs232MeasurementDevice)MDevice).BaudRate.ToString();
        }

        private void variableType_DropDownClosed(object sender, EventArgs e)
        {         
            var combo = sender as ComboBox;
            ((MD_MotionControllerChannel)MDevice).VariableType = (string)combo.SelectedItem;
            combo.SelectedItem = ((MD_MotionControllerChannel)MDevice).VariableType;
        }

        private void refreshPorts_Click(object sender, EventArgs e)
        {           
            ((BaseRs232MeasurementDevice)MDevice).UpdatePortList();           
        }

    }

    class KeyenceChannel
    {
        public int channelID;
        public StackPanel sp = new() { Orientation = Orientation.Horizontal };
        private TextBox tb = new();
        private CheckBox cb = new();

        public KeyenceChannel(int channel, object source)
        {
            channelID = channel;
            //Setup strings for content and property paths
            //string tbpp = "Ch" + channel + "Name";
            string tbpp = "ChName[" + (channel - 1) + "]";
            string cbContent = "Ch" + channel;
            string cbpp = "ChConnected[" + (channel - 1) + "]";
            string tbContent;
            //Don't want to reset name field unless it's empty
            if (string.IsNullOrEmpty(((MD_KeyenceTM3000)source).ChName[channel - 1]))
            {
                tbContent = "*Ch" + channel + "*";
            }
            else
            {
                tbContent = ((MD_KeyenceTM3000)source).ChName[channel - 1];
            }

            //Bind and setup UI elements
            XamlUI.TextboxBinding(tb, source, tbpp);
            XamlUI.SetupTextBox(ref tb, tbContent, 100);
            XamlUI.CheckBoxBinding(cbContent, cb, source, cbpp);

            //Add elements to stackpanel
            sp.Children.Add(tb);
            sp.Children.Add(cb);
        }
    }
}