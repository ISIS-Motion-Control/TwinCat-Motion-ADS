using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace TwinCat_Motion_ADS
{
    public class KeyenceTM3000 : INotifyPropertyChanged
    {
        public string Measurement { get; set; }
        private SerialPort SerialPort { get; set; }
        public string Units { get; set; }
        public string Readtime { get; set; }
        public string Portname { get; set; }
        public int BaudRate { get; set; }

        private const int defaultTimeout = 1000;

        public ObservableCollection<string> SerialPortList = new();

        public KeyenceTM3000(string portName = "", int baudRate = 115200)
        {
            Portname = portName;
            BaudRate = baudRate;
            SerialPort = new SerialPort();
        }

        public List<Tuple<string, int>> ChannelList = new();

        private bool _Ch1Connected;
        public bool Ch1Connected
        {
            get { return _Ch1Connected; }
            set
            {
                _Ch1Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch2Connected;
        public bool Ch2Connected
        {
            get { return _Ch2Connected; }
            set
            {
                _Ch2Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch3Connected;
        public bool Ch3Connected
        {
            get { return _Ch3Connected; }
            set
            {
                _Ch3Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch4Connected;
        public bool Ch4Connected
        {
            get { return _Ch4Connected; }
            set
            {
                _Ch4Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch5Connected;
        public bool Ch5Connected
        {
            get { return _Ch5Connected; }
            set
            {
                _Ch5Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch6Connected;
        public bool Ch6Connected
        {
            get { return _Ch6Connected; }
            set
            {
                _Ch6Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch7Connected;
        public bool Ch7Connected
        {
            get { return _Ch7Connected; }
            set
            {
                _Ch7Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch8Connected;
        public bool Ch8Connected
        {
            get { return _Ch8Connected; }
            set
            {
                _Ch8Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch9Connected;
        public bool Ch9Connected
        {
            get { return _Ch9Connected; }
            set
            {
                _Ch9Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch10Connected;
        public bool Ch10Connected
        {
            get { return _Ch10Connected; }
            set
            {
                _Ch10Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch11Connected;
        public bool Ch11Connected
        {
            get { return _Ch11Connected; }
            set
            {
                _Ch11Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch12Connected;
        public bool Ch12Connected
        {
            get { return _Ch12Connected; }
            set
            {
                _Ch12Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch13Connected;
        public bool Ch13Connected
        {
            get { return _Ch13Connected; }
            set
            {
                _Ch13Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch14Connected;
        public bool Ch14Connected
        {
            get { return _Ch14Connected; }
            set
            {
                _Ch14Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch15Connected;
        public bool Ch15Connected
        {
            get { return _Ch15Connected; }
            set
            {
                _Ch15Connected = value;
                OnPropertyChanged();
            }
        }
        private bool _Ch16Connected;
        public bool Ch16Connected
        {
            get { return _Ch16Connected; }
            set
            {
                _Ch16Connected = value;
                OnPropertyChanged();
            }
        }

        private string _Ch1Name;
        public string Ch1Name
        {
            get { return _Ch1Name; }
            set
            {
                _Ch1Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch2Name;
        public string Ch2Name
        {
            get { return _Ch2Name; }
            set
            {
                _Ch2Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch3Name;
        public string Ch3Name
        {
            get { return _Ch3Name; }
            set
            {
                _Ch3Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch4Name;
        public string Ch4Name
        {
            get { return _Ch4Name; }
            set
            {
                _Ch4Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch5Name;
        public string Ch5Name
        {
            get { return _Ch5Name; }
            set
            {
                _Ch5Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch6Name;
        public string Ch6Name
        {
            get { return _Ch6Name; }
            set
            {
                _Ch6Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch7Name;
        public string Ch7Name
        {
            get { return _Ch7Name; }
            set
            {
                _Ch7Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch8Name;
        public string Ch8Name
        {
            get { return _Ch8Name; }
            set
            {
                _Ch8Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch9Name;
        public string Ch9Name
        {
            get { return _Ch9Name; }
            set
            {
                _Ch9Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch10Name;
        public string Ch10Name
        {
            get { return _Ch10Name; }
            set
            {
                _Ch10Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch11Name;
        public string Ch11Name
        {
            get { return _Ch11Name; }
            set
            {
                _Ch11Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch12Name;
        public string Ch12Name
        {
            get { return _Ch12Name; }
            set
            {
                _Ch12Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch13Name;
        public string Ch13Name
        {
            get { return _Ch13Name; }
            set
            {
                _Ch13Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch14Name;
        public string Ch14Name
        {
            get { return _Ch14Name; }
            set
            {
                _Ch14Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch15Name;
        public string Ch15Name
        {
            get { return _Ch15Name; }
            set
            {
                _Ch15Name = value;
                OnPropertyChanged();
            }
        }
        private string _Ch16Name;
        public string Ch16Name
        {
            get { return _Ch16Name; }
            set
            {
                _Ch16Name = value;
                OnPropertyChanged();
            }
        }



        /// <summary>
        /// Populate the serial port COM list, generally unused as MeasurementDevice class can do same
        /// </summary>
        public void UpdatePortList()
        {
            SerialPortList.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                SerialPortList.Add(port);
            }
        }

        /// <summary>
        /// Connect to serial port
        /// </summary>
        /// <returns></returns>
        public bool OpenPort()
        {
            if (SerialPort.IsOpen)  //reject command if already connected
            {
                return false;
            }
            if (string.IsNullOrEmpty(Portname)) //reject command if no port name for connecting
            {
                return false;
            }
            try
            {
                SerialPort = new SerialPort(Portname, BaudRate);
                SerialPort.Open();
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Return connection status of serial port
        /// </summary>
        /// <returns></returns>
        public bool CheckConnected()
        {
            return SerialPort.IsOpen;
        }
        
        /// <summary>
        /// Disconnect from the serial port
        /// </summary>
        /// <returns></returns>
        public bool ClosePort()
        {
            if(SerialPort.IsOpen)
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
                return true;
            }
            return false;   //port already closed
        }

        /// <summary>
        /// Get a measurement from the keyence device
        /// </summary>
        /// <param name="measurementChannel">Measurement channel to read</param>
        /// <param name="timeoutMilliSeconds"></param>
        /// <returns></returns>
        public async Task<string> GetMeasureAsync(int measurementChannel, int timeoutMilliSeconds = defaultTimeout)
        {
            CancellationTokenSource ct = new CancellationTokenSource();
            //Create a generic message buffer of the command MM,0000000000000000 (16 measurement channels)
            var txBuf = new byte[] { 0x4D, 0x4D, 0x2C, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x0D };
            //For the specified measurement channel, set the buffer value from 0 to 1
            txBuf[measurementChannel + 2] = 0x31;
            //create a buffer to hold the returned value
            var rxBuf = new byte[29];
            rxBuf = await ReadAsyncBuff(txBuf, 29, ct,timeoutMilliSeconds);
            if (rxBuf.Length != 29) //Device should always return 29 bytes for a single channel read
            {
                return "Measurement failed";
            }
            byte[] measureBuf = new byte[9]; //setup a buffer for extracting just the 8 bytes of measurement data
            System.Array.Copy(rxBuf, 20, measureBuf, 0, 8);
            var str = System.Text.Encoding.Default.GetString(measureBuf); //convert measurement buffer to a string
            return str;
        }

        /// <summary>
        /// Request all 16 measurement channels from keyence
        /// </summary>
        /// <param name="timeoutMilliSeconds"></param>
        /// <returns></returns>
        public async Task<List<string>> GetAllMeasures(int timeoutMilliSeconds = defaultTimeout)
        {
            List<string> measurements = new List<string>(); //setup a list to hold measurement data
            for (int i = 1; i < 17; i++)
            {
                var str = await GetMeasureAsync(i);
                if(str[1]!='X')  //Check the 2nd char of returned string, F is when no data present and X is unused channel
                {
                    measurements.Add(str);  //If valid data add to the list
                }
            }
            return measurements;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="rb"></param>
        /// <param name="ct"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<byte[]> ReadAsyncBuff(byte[] cmd,int rb, CancellationTokenSource ct, int timeout = defaultTimeout)
        {
            byte[] measurement = new byte[rb];
            var readTask = Task<byte[]>.Run(() =>
            {
                
                SerialPort.DiscardInBuffer();
                SerialPort.Write(cmd, 0, 20);   //Should be 20 bytes for a read
                while (SerialPort.BytesToRead < rb) 
                {
                    if (ct.Token.IsCancellationRequested)
                    {
                        return null;
                        //throw new TaskCanceledException();
                    }
                }
                _ = SerialPort.Read(measurement, 0, rb);
                return measurement;
            });
            if (await Task.WhenAny(readTask, Task.Delay(timeout, ct.Token)) == readTask)
            {
                ct.Cancel();
                return measurement;
            }
            else
            {
                ct.Cancel();
                return System.Array.Empty<byte>(); //if timeout return empty array
            }
        }

        public void UpdateChannelList()
        {
            ChannelList.Clear();
            Tuple<string, int> tmp;// = (Name, 1).ToTuple();
            if(Ch1Connected)
            {
                tmp = (Ch1Name, 1).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch2Connected)
            {
                tmp = (Ch2Name, 2).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch3Connected)
            {
                tmp = (Ch3Name, 3).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch4Connected)
            {
                tmp = (Ch4Name, 4).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch5Connected)
            {
                tmp = (Ch5Name, 5).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch6Connected)
            {
                tmp = (Ch6Name, 6).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch7Connected)
            {
                tmp = (Ch7Name, 7).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch8Connected)
            {
                tmp = (Ch8Name, 8).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch9Connected)
            {
                tmp = (Ch9Name, 9).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch10Connected)
            {
                tmp = (Ch10Name, 10).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch11Connected)
            {
                tmp = (Ch11Name, 11).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch12Connected)
            {
                tmp = (Ch12Name, 12).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch13Connected)
            {
                tmp = (Ch13Name, 13).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch14Connected)
            {
                tmp = (Ch14Name, 14).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch15Connected)
            {
                tmp = (Ch15Name, 15).ToTuple();
                ChannelList.Add(tmp);
            }
            if (Ch16Connected)
            {
                tmp = (Ch16Name, 16).ToTuple();
                ChannelList.Add(tmp);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
