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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;

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
        ListBoxWriter lbw;
        public ObservableCollection<ListBoxStatusItem> consoleStringList = new ObservableCollection<ListBoxStatusItem>();
        
        public MainWindow()
        {
            InitializeComponent();
            m_SynchronizationContext = SynchronizationContext.Current;
            lbw = new ListBoxWriter(consoleListBox);
            consoleListBox.ItemsSource = consoleStringList;
            Console.SetOut(lbw);
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

        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (xlHost != null)
            {
                xlHost.Dispose();
            }
        }

        private void button_LaserConnect_Click(object sender, RoutedEventArgs e)
        {
            DeviceInfo info = new DeviceInfo();
            try
            {
                if (Laser.Connect(ref info))
                {
                    //m_edlen = new Edlen(Laser.GetVacuumWavelength());
                    Console.WriteLine("Laser - " + info.SerialNumber + " connected");
                    //WriteLine(textBox9, "Vacuum wavelength = " + m_edlen.VacuumWavelength.ToString("F9"));
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
            double value = Laser.GetPreset() * 1000D;
            Console.WriteLine("Preset: " + value.ToString("F9"));
        }

        private void textbox_preset_LostFocus(object sender, RoutedEventArgs e)
        {
            if(ValidateTextNumber(sender))
            {
                double value = double.Parse(((TextBox)sender).Text) / 1000D;
                Laser.SetPreset(value);
            }
            
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
