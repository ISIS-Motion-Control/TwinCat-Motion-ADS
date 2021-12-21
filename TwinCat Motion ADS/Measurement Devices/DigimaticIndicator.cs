using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace TwinCat_Motion_ADS
{
    class DigimaticIndicator
    {
        public string Measurement { get; set; }
        private SerialPort SerialPort { get; set; }
        public string Units { get; set; }
        public string Readtime { get; set; }
        public string Portname { get; set; }
        public int BaudRate { get; set; }
        private const int readDelay = 25;   //Give buffer time to fill
        private const int defaultTimeout = 1000;

        public ObservableCollection<string> SerialPortList = new();

        public DigimaticIndicator(string portName = "", int baudRate = 9600)
        {
            Portname = portName;
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
        public bool OpenPort()
        {
            if (SerialPort.IsOpen)  //If port already open and in use
            {
                return false;
            }
            if (string.IsNullOrEmpty(Portname)) //If no port name specified
            {
                return false;
            }
            try
            {
                SerialPort = new SerialPort(Portname, 9600);    //Create serial port instance
                SerialPort.Open();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool CheckConnected()
        {
            return SerialPort.IsOpen;
        }

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
            return false;
        }

        //Async method for getting DTI measurement
        public async Task<string> GetMeasurementAsync(int timeoutMilliSeconds = defaultTimeout)
        {
            CancellationTokenSource ct = new();
            return await ReadAsync("1", ct, readDelay, timeoutMilliSeconds);
        }

        //Generic write/read command to DTI. 1 is measurement, 2 is unit, 3 is readtime
        public async Task<string> ReadAsync(string cmd, CancellationTokenSource ct, int msReadDelay, int timeout = defaultTimeout)
        {
            string readValue = string.Empty;
            var readTask = Task<string>.Run(async () =>
            {
                SerialPort.DiscardInBuffer(); //clear unread data
                SerialPort.Write(cmd);
                while (SerialPort.BytesToRead == 0)
                {
                    if (ct.Token.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }
                }
                await Task.Delay(msReadDelay);
                readValue = SerialPort.ReadExisting();
                return readValue;
            });

            if (await Task.WhenAny(readTask, Task.Delay(timeout, ct.Token)) == readTask)
            {
                ct.Cancel();
                return readValue;
            }
            else
            {
                ct.Cancel();
                return "*Measurement timeout*";
            }

        }


        //OBSOLETE Read DTI data
        public string GetMeasurement()
        {
            if (SerialPort == null) return "No port initialised";
            if (!SerialPort.IsOpen) return "Port not open";
            SerialPort.DiscardInBuffer();   //Clear any unread data from the buffer
            SerialPort.Write("1");          //Command to request a measurement read
            while (SerialPort.BytesToRead == 0) { } //wait until we start reading data
            Thread.Sleep(readDelay);
            Measurement = SerialPort.ReadExisting();
            return Measurement;
        }

        //OBSOLETE Read DTI units
        public string GetUnits()
        {
            SerialPort.DiscardInBuffer(); //Clear any unread data
            SerialPort.Write("2");
            while (SerialPort.BytesToRead == 0) { }
            Thread.Sleep(readDelay);
            Units = SerialPort.ReadExisting();
            return Units;
        }

        //OBSOLETE Read DTI readtime
        public string GetReadTime()
        {
            SerialPort.DiscardInBuffer();
            SerialPort.Write("3");
            while (SerialPort.BytesToRead == 0) { }
            Thread.Sleep(readDelay);
            Readtime = SerialPort.ReadExisting();
            return Readtime;
        }
    }
}
