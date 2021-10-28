using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TwinCat_Motion_ADS
{
    class KeyenceTM3000
    {
        public string Measurement { get; set; }
        private SerialPort SerialPort { get; set; }
        public string Units { get; set; }
        public string Readtime { get; set; }
        public string Portname { get; set; }
        public int BaudRate { get; set; }
        private const int readDelay = 10;   //Give buffer time to fill
        private const int defaultTimeout = 1000;

        public ObservableCollection<string> SerialPortList = new();

        public KeyenceTM3000(string portName = "", int baudRate = 9600)
        {
            Portname = portName;
            BaudRate = baudRate;
            SerialPort = new SerialPort();
        }

        public void UpdatePortList()
        {
            SerialPortList.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                SerialPortList.Add(port);
            }
        }

        public bool OpenPort()
        {
            if (SerialPort.IsOpen)
            {
                return false;
            }
            if (string.IsNullOrEmpty(Portname))
            {
                return false;
            }
            try
            {
                SerialPort = new SerialPort(Portname, 9600);
                SerialPort.Open();
            }
            catch
            {
                return false;
            }
            return true;
        }
        
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
            return false;
        }

        public async Task<string> GetMeasureAsync(int measurementChannel, int timeoutMilliSeconds = defaultTimeout)
        {
            CancellationTokenSource ct = new CancellationTokenSource();
            var txBuf = new byte[] { 0x4D, 0x4D, 0x2C, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x0D };
            txBuf[measurementChannel + 2] = 0x31;
            var rxBuf = new byte[29];
            rxBuf = await ReadAsyncBuff(txBuf, 29, ct,timeoutMilliSeconds);
            if (rxBuf.Length != 29)
            {
                return "Measurement failed";
            }
            byte[] measureBuf = new byte[9];
            System.Array.Copy(rxBuf, 20, measureBuf, 0, 8);
            var str = System.Text.Encoding.Default.GetString(measureBuf);
            return str;
        }

        public async Task<List<string>> GetAllMeasures(int timeoutMilliSeconds = defaultTimeout)
        {
            List<string> measurements = new List<string>();
            for (int i = 1; i < 17; i++)
            {
                var str = await GetMeasureAsync(i);
                if(str[1]!='F' && str[1]!='X')
                {
                    measurements.Add(str);
                }
            }
            return measurements;
        }

        public async Task<byte[]> ReadAsyncBuff(byte[] cmd,int rb, CancellationTokenSource ct, int timeout = defaultTimeout)
        {
            byte[] measurement = new byte[rb];
            var readTask = Task<byte[]>.Run(async () =>
            {
                
                SerialPort.DiscardInBuffer();
                SerialPort.Write(cmd, 0, 20);   //Should be 20 bytes for a read
                while (SerialPort.BytesToRead < rb) 
                {
                    if (ct.Token.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }
                }
                SerialPort.Read(measurement, 0, rb);
                
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
                return System.Array.Empty<byte>();
            }

        }


    }
}
