using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO.Pipes;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Renishaw.Calibration;
using Renishaw.Calibration.Compensation;
using Renishaw.Calibration.Laser;
using Renishaw.Calibration.Laser.Service;
using Renishaw.Calibration.Sensors;
using Renishaw.Calibration.WeatherStationService.Service;
using System.Threading;
using System.ServiceModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Renishaw.IO.Bluetooth;
using Renishaw.IO;
using System.Threading.Tasks;
using Renishaw.Calibration.Devices.XR;
using Renishaw.Calibration.Devices.Infrastructure;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace Renishaw_XL80_App
{
    public class MyDelay : IDelay
    {
        public async Task Delay(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        public async Task Delay(int milliseconds, CancellationToken token)
        {
            await Task.Delay(milliseconds, token);;
        }
    }

    [CallbackBehavior(UseSynchronizationContext = false)]
    public partial class MainWindow : Window, ILaserSystemCallback, IWeatherStationCallback, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        //Local hosts are required to establish and setup the USB connection
        LocalXCHost xcHost = null;
        LocalXLHost xlHost = null;
        
        

        //Measurement systems and custom renishaw classes
        private WeatherStationClient m_ws;
        private LaserSystemClient m_laser;


        private Edlen m_edlen;
        private MaterialExpansion m_expansion;


        private bool _Connected;
        public bool Connected
        {
            get { return _Connected; }
            private set { _Connected = value; }
        }

        private bool _WeatherConnected;
        public bool WeatherConnected
        {
            get { return _WeatherConnected; }
            private set { _WeatherConnected = value; }
        }

        private double _XrVelocity = 5.0;
        public double XrVelocity
        {
            get { return _XrVelocity; }
            private set { 
                if (double.TryParse(value.ToString(), out _))
                {
                    _XrVelocity = value;
                    OnPropertyChanged();
                    MessageBox.Show("Value changed to: " + value.ToString());
                }
                
            }
        }

        public string XrVelocityString
        {
            get { return _XrVelocity.ToString(); }
            set
            {
                
                if (double.TryParse(value.ToString(), out _))
                {
                    _XrVelocity = double.Parse(value);
                    OnPropertyChanged();
                }

            }
        }

        private SynchronizationContext m_SynchronizationContext;

        //Named pipe server elements required to connect to main commissioning application
        NamedPipeClientStream client = new NamedPipeClientStream("RenishawXL80_Pipe");
        StreamReader reader;
        StreamWriter writer;
        bool SendInProgress = false;

        //Custom "console" text writer class to allow writing timestamped data to a listbox
        ListBoxWriter lbw;
        ListBoxWriter lbw_Weather;
        ListBoxWriter lbw_XR;
        public ObservableCollection<ListBoxStatusItem> consoleStringList = new ObservableCollection<ListBoxStatusItem>();
        public ObservableCollection<ListBoxStatusItem> consoleStringList_Weather = new ObservableCollection<ListBoxStatusItem>();
        public ObservableCollection<ListBoxStatusItem> consoleStringList_XR = new ObservableCollection<ListBoxStatusItem>();

        //constructor
        public MainWindow()
        {
            
            InitializeComponent();
            m_SynchronizationContext = SynchronizationContext.Current;
            
            
            //Console for the laser
            lbw = new ListBoxWriter(consoleListBox);
            consoleListBox.ItemsSource = consoleStringList;

            //Console for the weather settings
            lbw_Weather = new ListBoxWriter(consoleListBoxWeather);
            consoleListBoxWeather.ItemsSource = consoleStringList_Weather;

            //Console for the XR
            lbw_XR = new ListBoxWriter(consoleListBoxXr);
            consoleListBoxXr.ItemsSource = consoleStringList_XR;

            //Set default console to the laser listbox
            Console.SetOut(lbw);

            ConnectToServer();

            try
            {
                xlHost = new LocalXLHost();
                xlHost.Open();
            }
            catch
            {
                MessageBox.Show("Unable to create XLHost");
            }

            Binding VelocityTextboxBind = new Binding();
            VelocityTextboxBind.Mode = BindingMode.TwoWay;
            VelocityTextboxBind.Source = this;
            VelocityTextboxBind.Path = new PropertyPath("XrVelocityString");
            VelocityTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
            BindingOperations.SetBinding(textbox_xrVelocity, TextBox.TextProperty, VelocityTextboxBind);

            
        }
        
        

        private async void SendMessage(string value)
        {
            if (SendInProgress) return;
            SendInProgress = true;
            await writer.WriteLineAsync(value);
            await writer.FlushAsync();
            SendInProgress = false;
        }

        private void ConnectToServer()
        {
            client.Connect();
            reader = new StreamReader(client);
            writer = new StreamWriter(client);
            ConstantReadMode();
        }

        private async void ConstantReadMode()
        {
            string cmd;
            LaserReading recordLaser = null;// = Laser.GetLatestReading();
            EnvironmentalRecord recordEnviron = null;;// = WeatherStation.GetEnvironmentalRecord();
            MaterialTemperatureRecord recordMaterial = null; // = WeatherStation.GetMaterialTemperatureRecord();
            while (true)
            {
                cmd = await reader.ReadLineAsync();
                string measure = string.Empty;
                switch (cmd)
                {
                    case "1":
                        recordLaser = Laser.GetLatestReading();
                        measure = Connected ? (recordLaser.ValueOf * 1000D).ToString("F6") : "Disconnected";
                        break;

                    case "2":
                        measure = Connected ? recordLaser.Valid.ToString() : "Disconnected";
                        break;

                    case "3":
                        measure = Connected ? recordLaser.SignalStrength.ToString() : "Disconnected";
                        break;

                    case "4":
                        recordEnviron = WeatherStation.GetEnvironmentalRecord();
                        measure = WeatherConnected ? recordEnviron.AirTemperature.ValueOf.ToString() : "Disconnected";
                        break;

                    case "5":
                        measure = WeatherConnected ? recordEnviron.AirTemperature.Valid.ToString() : "Disconnected";
                        break;

                    case "6":
                        measure = WeatherConnected ? recordEnviron.AirPressure.ValueOf.ToString() : "Disconnected";
                        break;

                    case "7":
                        measure = WeatherConnected ? recordEnviron.AirPressure.Valid.ToString() : "Disconnected";
                        break;

                    case "8":
                        measure = WeatherConnected ? recordEnviron.AirHumidity.ValueOf.ToString() : "Disconnected";
                        break;

                    case "9":
                        measure = WeatherConnected ? recordEnviron.AirHumidity.Valid.ToString() : "Disconnected";
                        break;

                    case "10":
                        recordMaterial = WeatherStation.GetMaterialTemperatureRecord();
                        measure = WeatherConnected ? recordMaterial.AverageMaterialTemperature.ValueOf.ToString() : "Disconnected";
                        break;

                    case "11":
                        measure = WeatherConnected ? recordMaterial.AverageMaterialTemperature.Valid.ToString() : "Disconnected";
                        break;

                    case "12":
                        measure = WeatherConnected ? recordMaterial[0].ValueOf.ToString() : "Disconnected";
                        break;

                    case "13":
                        measure = WeatherConnected ? recordMaterial[0].Valid.ToString() : "Disconnected";
                        break;

                    case "14":
                        measure = WeatherConnected ? recordMaterial[1].ValueOf.ToString() : "Disconnected";
                        break;

                    case "15":
                        measure = WeatherConnected ? recordMaterial[1].Valid.ToString() : "Disconnected";
                        break;

                    case "16":
                        measure = WeatherConnected ? recordMaterial[2].ValueOf.ToString() : "Disconnected";
                        break;

                    case "17":
                        measure = WeatherConnected ? recordMaterial[2].Valid.ToString() : "Disconnected";
                        break;


                    case "Weather":
                        measure = WeatherConnected.ToString();
                        break;

                    case "Laser":
                        measure = Connected.ToString();
                        break;
                }
                SendMessage(measure);
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (xlHost != null)
            {
                xlHost.Dispose();
            }
            if (xcHost != null)
            {
                xcHost.Dispose();
            }
        }

        #region LASER
        private LaserSystemClient Laser
        {
            get
            {
                if (m_laser == null)
                {
                    m_laser = LocalXLHost.CreateLaserProxy(this);
                    m_expansion = new MaterialExpansion();
                }

                return m_laser;
            }
        }

        public void ConnectionStatusChanged(DeviceInfo info, ConnectionStatus value)
        {
            //MessageBox.Show("Connection status changed");
            //TextBox control = info.Model.Contains("XL-80") || info.Model.Contains("ML10") ? textBox9 : textBox1;
            //Console.WriteLine("ConnectionStatus: " + value.ToString());
        }
        public void LatestReadingUpdated(DeviceInfo info, Renishaw.Calibration.Laser.LaserReading value)
        {
            //tmpMeasure = value;
            //MessageBox.Show(value.ValueOf.ToString());
            UpdateReading(value);
        }
        private void UpdateReading(Renishaw.Calibration.Laser.LaserReading reading)
        {
            
            SendOrPostCallback update = delegate (object value)
            {
                m_laserReadingTextBox.Text = (1000 * reading.ValueOf).ToString("F6");
                m_laserReadingTextBox.Background = (reading.Valid) ? Brushes.White : Brushes.Red;
                //m_laserStatusTextBox.Text = ((int)reading.Status).ToString("X8");
            };

            m_SynchronizationContext.Post(update, null);
            
        }
        public void ReadingTriggered(DeviceInfo info, Renishaw.Calibration.Laser.LaserReading value)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => {
                WriteTriggeredReading(value);
            }));         
        }

        #endregion

        #region WEATHER STATION
        private WeatherStationClient WeatherStation
        {
            get
            {
                if (m_ws == null)
                {
                    xcHost = new LocalXCHost();
                    xcHost.Open();
                    m_ws = LocalXCHost.CreateProxy(this);
                }

                return m_ws;
            }
        }
        #endregion

        #region IWeatherStationCallback Members
        public void EnvironmentalRecordUpdated(DeviceInfo info, Renishaw.Calibration.Sensors.EnvironmentalRecord value)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => {
                lbw_Weather.WriteLine("Air temperature: " + value.AirTemperature.ToString());
                lbw_Weather.WriteLine("Air pressure: " + value.AirPressure.ToString());
                lbw_Weather.WriteLine("Air humidity: " + value.AirHumidity.ToString());
            }));

            UpdateEnvironment(value);
        }

        private void UpdateEnvironment(EnvironmentalRecord record)
        {
            SendOrPostCallback update = delegate (object value)
            {
                UpdateLambda(record);

                textbox_xcAirHumidity.Text = record.AirHumidity.ValueOf.ToString("F2");
                textbox_xcAirHumidity.Background = (record.AirHumidity.Valid) ? Brushes.White : Brushes.Red;
                textbox_xcAirPressure.Text = record.AirPressure.ValueOf.ToString("F2");
                textbox_xcAirPressure.Background = (record.AirPressure.Valid) ? Brushes.White : Brushes.Red;
                textbox_xcAirTemp.Text = record.AirTemperature.ValueOf.ToString("F2");
                textbox_xcAirTemp.Background = (record.AirTemperature.Valid) ? Brushes.White : Brushes.Red;
            };

            m_SynchronizationContext.Post(update, null);
        }

        private void UpdateLambda(EnvironmentalRecord record)
        {
            if (m_laser != null)
            {
                if (Laser.GetConnectionStatus() != ConnectionStatus.None)
                {
                    double lambda = m_edlen.Calculate(
                        record.AirTemperature.ValueOf,
                        record.AirPressure.ValueOf,
                        record.AirHumidity.ValueOf
                        );

                    Laser.SetLambda(lambda);
                    Console.WriteLine("Lambda = " + lambda.ToString("F9"));
                }
            }
        }

        public void MaterialTemperatureRecordUpdated(DeviceInfo info, Renishaw.Calibration.Sensors.MaterialTemperatureRecord value)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => {
                lbw_Weather.WriteLine("Average material temperature: " + value.AverageMaterialTemperature.ToString());
                lbw_Weather.WriteLine("Material temperature[0]: " + value[0].ToString());
                lbw_Weather.WriteLine("Material temperature[1]: " + value[1].ToString());
                lbw_Weather.WriteLine("Material temperature[2]: " + value[2].ToString());

            }));
            
            UpdateMaterial(value);
        }

        private void UpdateMaterial(MaterialTemperatureRecord record)
        {
            SendOrPostCallback update = delegate (object value)
            {
                UpdateMaterialExpansionFactor(record);

                textbox_xcMatTemp1.Text = record[0].ValueOf.ToString("F2");
                textbox_xcMatTemp1.Background = (record[0].Valid) ? Brushes.White : Brushes.Red;
                textbox_xcMatTemp2.Text = record[1].ValueOf.ToString("F2");
                textbox_xcMatTemp2.Background = (record[1].Valid) ? Brushes.White : Brushes.Red;
                textbox_xcMatTemp3.Text = record[2].ValueOf.ToString("F2");
                textbox_xcMatTemp3.Background = (record[2].Valid) ? Brushes.White : Brushes.Red;
                textbox_xcMatTempAverage.Text = record.AverageMaterialTemperature.ValueOf.ToString("F2");
                textbox_xcMatTempAverage.Background = (record.AverageMaterialTemperature.Valid) ? Brushes.White : Brushes.Red;
            };

            m_SynchronizationContext.Post(update, null);
        }

        private void UpdateMaterialExpansionFactor(MaterialTemperatureRecord record)
        {
            //if (m_laser != null)
            //{
            //    if (Laser.GetConnectionStatus() != ConnectionStatus.None)
            //    {
            //        SensorReading average = record.AverageMaterialTemperature;
            //        if (average != SensorReading.Null)
            //        {
            //            m_expansion.MaterialTemperature = record.AverageMaterialTemperature;

            //            double factor = (m_expansion.Reading.ValueOf);
            //            Laser.SetMaterialExpansionFactor(factor);
            //            WriteLine(textBox9, "Material expansion factor = " + (1 - factor).ToString("F9"));
            //        }
            //    }
            //}
        }

        #endregion



        public void WriteTriggeredReading(LaserReading value)
        {
            Console.WriteLine("Reading triggered: " + (1000D * value.ValueOf).ToString("F6"));
            Console.WriteLine("Velocity: " + (1000D * value.Velocity).ToString("F6"));
            Console.WriteLine("Acceleration: " + (1000D * value.Acceleration).ToString("F6"));
        }


        #region LASER BUTTONS AND UI
        private void button_LaserConnect_Click(object sender, RoutedEventArgs e)
        {
            DeviceInfo info = new DeviceInfo();
            try
            {
                if (Laser.Connect(ref info))
                {
                    m_edlen = new Edlen(Laser.GetVacuumWavelength());
                    Console.WriteLine("Laser - " + info.SerialNumber + " connected");
                    Console.WriteLine("Vacuum wavelength = " + m_edlen.VacuumWavelength.ToString("F9"));
                    Connected = true;
                }
            }
            catch
            {
                Console.WriteLine("Didn't work :(");
            }
        }

        private void button_LaserDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Laser.Disconnect();
            Console.WriteLine("Laser disconnected");
            Connected = false;
        }

        private void button_LaserVersion_Click(object sender, RoutedEventArgs e)
        {
            VersionInfoDictionary info = Laser.GetVersionInfo();
            foreach (string key in info.Keys)
            {
                Console.WriteLine(key + ": " + info[key].ToString());
            }
        }

        private void button_LaserClearScreen_Click(object sender, RoutedEventArgs e)
        {
            consoleStringList.Clear();
        }

        private void button_LaserReadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected) return;
            double value = Laser.GetPreset() * 1000D;
            Console.WriteLine("Preset: " + value.ToString("F9"));
        }

        private void button_LaserDeviceInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected) return;
            DeviceInfo info = Laser.GetDeviceInfo();
            Console.WriteLine("Manufacturer: " + info.Manufacturer);
            Console.WriteLine("Model: " + info.Model);
            Console.WriteLine("Serial number: " + info.SerialNumber);
            Console.WriteLine("Name: " + info.Name);
        }

        private void button_downloadPreset_Click(object sender, RoutedEventArgs e)
        {
            double value = double.Parse(textbox_preset.Text) / 1000D;
            Laser.SetPreset(value);
            Console.WriteLine("Preset updated to: " + textbox_preset.Text);
        }

        private void button_downloadMatExpCoeff_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected) return;
            m_expansion.Coefficient = double.Parse(textbox_materialCoeff.Text);
            double factor = (m_expansion.Reading.ValueOf);
            Laser.SetMaterialExpansionFactor(factor);
        }

        private void textbox_materialCoeff_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateTextNumber(sender);
        }

        private void comboBox_laserAveraging_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Connected)
            {
                comboBox_laserAveraging.SelectedIndex = 0;
                return;
            }
            AveragingKind value = (AveragingKind)comboBox_laserAveraging.SelectedIndex;
            Laser.SetAveragingKind(value);
        }

        private void button_LaserCalibInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected) return;
            CalibrationInfo info = Laser.GetCalibrationInfo();
            Console.WriteLine("Organization: " + info.Organization);
            Console.WriteLine("CertificateNumber: " + info.CertificateNumber);
            Console.WriteLine("CalibrationDate: " + info.CalibrationDate.ToShortDateString());
        }

        private void button_LaserConnectionStatus_Click(object sender, RoutedEventArgs e)
        {
            ConnectionStatus value = Laser.GetConnectionStatus();
            Console.WriteLine("ConnectionStatus: " + value.ToString());
        }

        private void button_LaserMaterialExpCoeff_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected) return;
            double value = m_expansion.Coefficient;
            Console.WriteLine("Material expansion coefficient: " + value.ToString("F2"));
        }

        private void button_LaserGetSingleReading_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected)
            {
                Console.WriteLine("No laser connected");
                return;
            }

            LaserReading reading = Laser.GetLatestReading();
            double value = reading.ValueOf * 1000D;
            Console.WriteLine(value.ToString("F6"));
            Console.WriteLine("Valid: " + reading.Valid.ToString());
            Console.WriteLine("Velocity: " + (1000D * reading.Velocity).ToString("F6"));
            Console.WriteLine("Acceleration: " + (1000D * reading.Acceleration).ToString("F6"));
            Console.WriteLine("Status: " + reading.Status.ToString());
            Console.WriteLine("Signal strength: " + reading.SignalStrength.ToString());
        }

        private void button_LaserSetDatum_Click(object sender, RoutedEventArgs e)
        {
            Laser.Datum();
        }

        private void button_LaserToggleDirection_Click(object sender, RoutedEventArgs e)
        {
            Laser.ToggleDirectionSense();
        }

        private void button_LaserReset_Click(object sender, RoutedEventArgs e)
        {
            Laser.ResetDatalink();
        }

        private void button_LaserAveraging_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected) return;
            AveragingKind value = Laser.GetAveragingKind();
            Console.WriteLine("Averaging kind: " + value.ToString());
        }

        private void button_LaserTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (!Connected) return;
            Laser.Trigger();
        }
        #endregion

        private void textbox_preset_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateTextNumber(sender);
            
            
        }

        private bool ValidateTextNumber(object sender)
        {
            double value = 0;
            if (!double.TryParse(((TextBox)sender).Text, out value))
            {
                Console.WriteLine("Must be a number");
                ((TextBox)sender).Text = "0";
                return false;
            }
            return true;
        }

        #region Weather Station Buttons
        private void button_weatherConnect_Click(object sender, RoutedEventArgs e)
        {
            DeviceInfo info = new DeviceInfo();
            if (WeatherStation.Connect(ref info))
            {
                lbw_Weather.WriteLine("Connected");
                WeatherConnected = true;
            }
        }

        private void button_weatherDisconnect_Click(object sender, RoutedEventArgs e)
        {
            WeatherStation.Disconnect();
            lbw_Weather.WriteLine("Disconnected");

            WeatherConnected = false;
        }

        private void button_weatherVersion_Click(object sender, RoutedEventArgs e)
        {
            if(!WeatherConnected) return;
            VersionInfoDictionary info = WeatherStation.GetVersionInfo();
            foreach (string key in info.Keys)
            {
                lbw_Weather.WriteLine(key + ": " + info[key].ToString());
            }
        }

        private void button_weatherClearScreen_Click(object sender, RoutedEventArgs e)
        {
            consoleStringList_Weather.Clear();
        }

        private void button_weatherDeviceInfo_Click(object sender, RoutedEventArgs e)
        {
            if(!WeatherConnected) return ;
            DeviceInfo info = WeatherStation.GetDeviceInfo();
            lbw_Weather.WriteLine("Manufacturer: " + info.Manufacturer);
            lbw_Weather.WriteLine("Model: " + info.Model);
            lbw_Weather.WriteLine("Serial number: " + info.SerialNumber);
            lbw_Weather.WriteLine("Name: " + info.Name);
        }

        private void button_weatherCalibInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!WeatherConnected) return;

            CalibrationInfo info = WeatherStation.GetCalibrationInfo();
            lbw_Weather.WriteLine("Organization: " + info.Organization);
            lbw_Weather.WriteLine("CertificateNumber: " + info.CertificateNumber);
            lbw_Weather.WriteLine("CalibrationDate: " + info.CalibrationDate.ToShortDateString());
        }

        private void button_weatherConnectionStatus_Click(object sender, RoutedEventArgs e)
        {
            ConnectionStatus value = WeatherStation.GetConnectionStatus();
            lbw_Weather.WriteLine("ConnectionStatus: " + value.ToString());
        }

        private void button_weatherBrowse_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<DeviceInfo> devices = WeatherStation.BrowseAttachedDevices();
            foreach (DeviceInfo device in devices)
            {
                lbw_Weather.WriteLine(device.Model + ":" + device.SerialNumber);
            }
        }

        private void button_weatherEnviron_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentalRecord record = WeatherStation.GetEnvironmentalRecord();
            lbw_Weather.WriteLine("Air temperature: " + record.AirTemperature.ToString());
            lbw_Weather.WriteLine("Air pressure: " + record.AirPressure.ToString());
            lbw_Weather.WriteLine("Air humidity: " + record.AirHumidity.ToString());
        }

        private void button_weatherMaterialRead_Click(object sender, RoutedEventArgs e)
        {
            MaterialTemperatureRecord record = WeatherStation.GetMaterialTemperatureRecord();
            lbw_Weather.WriteLine("Average material temperature: " + record.AverageMaterialTemperature.ToString());
            lbw_Weather.WriteLine("Material temperature[0]: " + record[0].ToString());
            lbw_Weather.WriteLine("Material temperature[1]: " + record[1].ToString());
            lbw_Weather.WriteLine("Material temperature[2]: " + record[2].ToString());
        }





        #endregion

        #region XR20 logic
        public BluetoothEndPoint XR20_Endpoint;
        public XR20BluetoothConnector xrConnector;
        private List<BluetoothDeviceType> bluetoothDevices = new List<BluetoothDeviceType> { BluetoothDeviceType.XR20_W };
        private CancellationTokenSource bluetoothSearchCT;
        public loadingWindow _loadingWindow = new loadingWindow();
        BluetoothDeviceDiscoverer discoverer;
        private async void button_xr_search_Click(object sender, RoutedEventArgs e)
        {
            bluetoothSearchCT = new CancellationTokenSource();
            discoverer = new BluetoothDeviceDiscoverer();
            _loadingWindow.Show();

            _loadingWindow.CancelRequested += CancelSearch;
            discoverer.ScanForBluetoothDevicesAsync(bluetoothDevices, BluetoothDeviceFound, bluetoothSearchCT.Token);
        }

        public void CancelSearch(object sender, EventArgs e)
        {
            try
            {
                bluetoothSearchCT.Cancel();
                _loadingWindow.Hide();
                discoverer.Dispose();                
            }
            catch
            {
                return;
            }
        }

        private async void BluetoothDeviceFound(DiscoveredBluetoothDevice foundDevice)
        {
            await Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => {
                _loadingWindow.Hide();
            }));
            
            MessageBoxResult result = MessageBox.Show("Device found with serial: " + foundDevice.SerialNumber +"\nDo you want to connect?","Device found",MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                ConnectToBleDevice(foundDevice);
            }
        }

        XRDevice xrDevice;
        private async void ConnectToBleDevice(DiscoveredBluetoothDevice device)
        {
            BluetoothEndPoint bleEndpoint = device.EndPoint;
            BluetoothConnector bleConnector = new BluetoothConnector();
            RealDelay realDelay = new RealDelay();
            XR20BluetoothConnector xrBleConnector = new XR20BluetoothConnector(bleConnector, realDelay);
            RealTimer realTimer = new RealTimer();
            xrDevice = new XRDevice(bleEndpoint, xrBleConnector, realTimer);
            await xrDevice.Open();
            await Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => {
                lbw_XR.WriteLine("Connection status: " + xrDevice.ConnectionStatus);
            }));
     
            SubscribeToEvent(xrDevice);
            GetXrBatteryLevel(CancellationToken.None);
        }



        public async Task GetXrBatteryLevel(CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                if (xrDevice == null) return;
                WriteBatteryLevel(xrDevice.BatteryLevel);                    
                await Task.Delay(100);
            }
            return;
        }
        public void WriteBatteryLevel(int level)
        {
            Application.Current.Dispatcher.BeginInvoke(
               DispatcherPriority.Background,
               new Action(() => {
                   textbox_xrBatteryLevel.Text = level.ToString();
               }));
        }
            
        

        private void SubscribeToEvent(XRDevice xrDevice) => xrDevice.LatestReadingUpdated += this.Xr20ReadingUpdate;
        private void Xr20ReadingUpdate(object sender, EventArgs args)
        {
            XR20Reading reading = (sender as XRDevice).LatestReading;
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => {
                textbox_xrReadingValue.Text = reading.Position.ToString();
                textbox_xrReadingStatus.Text = reading.Status.ToString();
            }));
        }


        private async void button_xrReference_Click(object sender, RoutedEventArgs e)
        {
            if (xrDevice == null) return;
            try
            {
                Task refTask = xrDevice.ReferenceAsync(CancellationToken.None);

                await refTask;
                if (refTask.IsCompleted)
                {
                    lbw_XR.WriteLine("Reference move complete");
                }
                await xrDevice.DisableServosAsync(CancellationToken.None);
            }
            catch
            {
                lbw_XR.WriteLine("Reference Timeout");
                await xrDevice.DisableServosAsync(CancellationToken.None);
                return;
            }
        }

        private async void button_xrLock_Click(object sender, RoutedEventArgs e)
        {
            if (xrDevice == null) return;
            try
            {
                await xrDevice.LockAsync(CancellationToken.None);
            }
            catch
            {
                lbw_XR.WriteLine("Lock timeout");
                return;
            }
            lbw_XR.WriteLine("Rotation locked");
        }

        private async void button_xrUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (xrDevice == null) return;
            try
            {
                await xrDevice.UnlockAsync(CancellationToken.None);
            }
            catch
            {
                lbw_XR.WriteLine("Unlock timeout");
                return;
            }
            lbw_XR.WriteLine("Rotation unlocked");
        }

        private async void button_xrDisableServos_Click(object sender, RoutedEventArgs e)
        {
            if (xrDevice == null) return;
            try
            {
                await xrDevice.DisableServosAsync(CancellationToken.None);
            }
            catch
            {
                lbw_XR.WriteLine("Disable servos timeout");
                return;
            }
            lbw_XR.WriteLine("Servos disabled");
        }

        private void button_xrDatum_Click(object sender, RoutedEventArgs e)
        {
            if (xrDevice == null) return;
            try
            {
                xrDevice.Datum();
            }
            catch
            {
                lbw_XR.WriteLine("Unable to datum");
                return;
            }
        }




        #endregion

        private void button_xrClearScreen_Click(object sender, RoutedEventArgs e)
        {
            consoleStringList_XR.Clear();
        }

        private void button_xrConnectionStatus_Click(object sender, RoutedEventArgs e)
        {
            ConnectionStatus status = new ConnectionStatus();
            status = xrDevice.ConnectionStatus;
            lbw_XR.WriteLine(status.ToString());
        }

        private void button_xrSweepAntiClockwise_Click(object sender, RoutedEventArgs e)
        {
            if (xrDevice == null) return;
            xrDevice.StartSweepAsync(Renishaw.Calibration.Devices.Constants.RotaryDirectionKind.Anticlockwise, XrVelocity);
        }

        private void button_xrSweepClockwise_Click(object sender, RoutedEventArgs e)
        {
            if (xrDevice == null) return;
            xrDevice.StartSweepAsync(Renishaw.Calibration.Devices.Constants.RotaryDirectionKind.Clockwise, XrVelocity);

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
                ListBoxStatusItem temp = new ListBoxStatusItem(content.ToString());
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
