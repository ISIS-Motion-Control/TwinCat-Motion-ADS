using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

        const int _KEYENCE_MAX_CHANNELS = 16;
        public int KEYENCE_MAX_CHANNELS { get { return _KEYENCE_MAX_CHANNELS; } }
        public bool[] ChConnected { get; set; }
        public string[] ChName { get; set; }
        //internal readonly int KEYENCE_MAX_CHANNELS = 16;
        public ObservableCollection<string> SerialPortList = new();

        public KeyenceTM3000(string portName = "", int baudRate = 115200)
        {
            ChConnected = new bool[KEYENCE_MAX_CHANNELS];
            ChName = new string[KEYENCE_MAX_CHANNELS];

            Portname = portName;
            BaudRate = baudRate;
            SerialPort = new SerialPort();
        }

        public List<Tuple<string, int>> ChannelList = new();


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
            rxBuf = await ReadAsyncBuff(txBuf, 29, ct, timeoutMilliSeconds);
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
                if (str[1] != 'X')  //Check the 2nd char of returned string, F is when no data present and X is unused channel
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
        public async Task<byte[]> ReadAsyncBuff(byte[] cmd, int rb, CancellationTokenSource ct, int timeout = defaultTimeout)
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

            for (int i = 1; i <= KEYENCE_MAX_CHANNELS; i++)
            {
                if (ChConnected[i - 1])
                {
                    tmp = (ChName[i - 1], i).ToTuple();
                    ChannelList.Add(tmp);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
