using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TwinCat_Motion_ADS
{
    public interface I_MeasurementDevice
    {
        public bool Connect();
        public bool Disconnect();
        public Task<string> GetMeasurement();
        public Task<string> GetChannelMeasurement(int channelNumber = 0);
        public void UpdateChannelList();
        public void AddToChannelList(Tuple<string, int> ch);
        public void RemoveFromChannelList(Tuple<string, int> ch);

        public DeviceTypes DeviceType { get; set; }
        public int NumberOfChannels { get; set; }
        public List<Tuple<string, int>> ChannelList { get; set; }
        public bool ReadInProgress { get; set; }
        public string Name { get; set; }
        public bool Connected { get; set; }
    }

    public abstract class BaseMeasurementDevice : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _NumberOfChannels;
        public int NumberOfChannels
        {
            get { return _NumberOfChannels; }
            set
            {
                _NumberOfChannels = value;
                OnPropertyChanged();
            }
        }

        public DeviceTypes DeviceType { get; set; }
        
        public List<Tuple<string, int>> ChannelList { get; set; } = new();

        public bool ReadInProgress { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                OnPropertyChanged();
            }
        }
        public void RemoveFromChannelList(Tuple<string, int> ch)
        {
            ChannelList.Remove(ch);
            NumberOfChannels = ChannelList.Count;
        }

        public void AddToChannelList(Tuple<string, int> ch)
        {
            ChannelList.Add(ch);
            NumberOfChannels = ChannelList.Count;
        }

        protected bool AllowDisconnect()
        {
            if (!Connected)
            {
                Console.WriteLine("Nothing to disconnect from");
                return false; //Nothing to disconnect from
            }
            if (ReadInProgress)
            {
                Console.WriteLine("Cannot disconnect during read");
                return false;
            }
            return true;
        }
    }

    public abstract class BaseRs232MeasurementDevice : BaseMeasurementDevice
    {
        public ObservableCollection<string> BaudRateList = new ObservableCollection<string>()
        {
            "9600", "19200","38400","57600","115200"
        };

        protected SerialPort SerialPort { get; set; }
        protected string _PortName;
        public string PortName
        {
            get { return _PortName; }
            set
            {
                if (Connected) return;
                _PortName = value;
                OnPropertyChanged();
            }
        }
        protected string _BaudRate;
        public string BaudRate
        {
            get { return _BaudRate; }
            protected set
            {
                _BaudRate = value;
                OnPropertyChanged();
            }
        }

        public void UpdateBaudRate(string bRate)
        {
            if (!Connected) BaudRate = bRate;
        }

        public ObservableCollection<string> SerialPortList = new();

        public BaseRs232MeasurementDevice(string portName = "", string baudRate = "9600")
        {
            PortName = portName;
            BaudRate = baudRate;
            SerialPort = new SerialPort();
        }

        //Populate the SerialPortList
        public void UpdatePortList()
        {
            SerialPortList.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                SerialPortList.Add(port);
            }
        }

        //Open the local port
        public bool Connect()
        {
            if (SerialPort.IsOpen)  //If port already open and in use
            {
                return false;
            }
            if (string.IsNullOrEmpty(PortName)) //If no port name specified
            {
                return false;
            }
            try
            {
                SerialPort = new SerialPort(PortName, Int32.Parse(BaudRate));    //Create serial port instance
                SerialPort.Open();
            }
            catch
            {
                return false;
            }
            Connected = true;
            return true;
        }

        public bool CheckConnected()
        {
            return SerialPort.IsOpen;
        }

        public bool Disconnect()
        {
            ChannelList.Clear();
            if (SerialPort.IsOpen)
            {
                try
                {
                    SerialPort.Close();
                    SerialPort.Dispose();
                }
                catch
                {

                    return false;
                }
                Connected = false;
                return true;
            }
            return false;
        }
    }

    public enum DeviceTypes
    {
        [StringValue("NoneSelected")]
        NoneSelected,
        [StringValue("DigimaticIndicator")]
        DigimaticIndicator,
        [StringValue("KeyenceTM3000")]
        KeyenceTM3000,
        [StringValue("Beckhoff")]
        Beckhoff,
        [StringValue("MotionChannel")]
        MotionChannel,
        [StringValue("Timestamp")]
        Timestamp,
        [StringValue("RenishawXL80")]
        RenishawXL80
    }

    public class NoneSelectedMeasurementDevice : BaseMeasurementDevice, I_MeasurementDevice
    {
        public bool Connect()
        {
            Console.WriteLine("No device type selected"); return false;
        }
    
        public bool Disconnect()
        {
            Console.WriteLine("No device type selected"); return false;
        }

        public Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            return Task.FromResult("");
        }

        public  Task<string> GetMeasurement()
        {
            return Task.FromResult("");
        }

        public void UpdateChannelList()
        {
            ChannelList.Clear();
            NumberOfChannels = 0;
        }
    }


    

     
}
