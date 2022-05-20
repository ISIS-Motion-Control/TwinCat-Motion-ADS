using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
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

namespace Renishaw_XL80_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public partial class MainWindow : Window, ILaserSystemCallback
    {      
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private WeatherStationClient m_ws;
        private LaserSystemClient m_laser;
        private Edlen m_edlen;
        private MaterialExpansion m_expansion;

        LocalXCHost xcHost = new LocalXCHost();
        LocalEC10Host ecHost = new LocalEC10Host();
        LocalXLHost xlHost = null;
        

        private SynchronizationContext m_SynchronizationContext;

        NamedPipeClientStream client = new NamedPipeClientStream("RenishawXL80_Pipe");
        StreamReader reader;
        StreamWriter writer;
        bool SendInProgress = false;

        public MainWindow()
        {
            InitializeComponent();
            m_SynchronizationContext = SynchronizationContext.Current;
            //ConnectToServer();
            using (xcHost)
            {
                xcHost.Open();
                using (ecHost)
                {
                    ecHost.Open();
                    try
                    {
                        xlHost = new LocalXLHost();
                        xlHost.Open();
                    }
                    catch
                    {
                        MessageBox.Show("Unable to create XLHost");
                    }
                }
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
            //WriteLine(control, "ConnectionStatus: " + value.ToString());
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
            //WriteLine(textBox9, "Reading triggered: " + (1000D * value.ValueOf).ToString("F6"));
            //WriteLine(textBox9, "Velocity: " + (1000D * value.Velocity).ToString("F6"));
            //WriteLine(textBox9, "Acceleration: " + (1000D * value.Acceleration).ToString("F6"));
        }

        #endregion

        



        private async void SendMessage()
        {
            if (SendInProgress) return;
            SendInProgress = true;
            await writer.WriteLineAsync(m_laserReadingTextBox.Text);
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
            while (true)
            {
                cmd = await reader.ReadLineAsync();

                if (cmd == "1")
                {
                    SendMessage();
                }
                if (cmd == "0")
                    break;
            }

        }

        
        
        
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void button_LaserConnect_Click(object sender, RoutedEventArgs e)
        {
            DeviceInfo info = new DeviceInfo();
            try
            {
                if (Laser.Connect(ref info))
                {
                    //m_edlen = new Edlen(Laser.GetVacuumWavelength());
                    MessageBox.Show("Laser - " + info.SerialNumber);
                    //WriteLine(textBox9, "Vacuum wavelength = " + m_edlen.VacuumWavelength.ToString("F9"));
                }
            }
            catch
            {
                MessageBox.Show("Didn't work :(");
            }            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (xlHost != null)
            {
                xlHost.Dispose();
            }
        }
    }
}
