using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace TwinCat_Motion_ADS.MeasurementDevice
{
    public class MD_DigimaticIndicator : BaseRs232MeasurementDevice, I_MeasurementDevice
    {
        public MD_DigimaticIndicator(string baudRate = "9600")
        {
            BaudRate = baudRate;
            DeviceType = DeviceTypes.DigimaticIndicator;
        }
        private const int defaultTimeout = 1000;
        private const int readDelay = 25;   //Give buffer time to fill

        public async Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            if (!Connected)
            {
                return "No device connected"; //Nothing to disconnect from
            }
            ReadInProgress = true;
            string measurement;
            CancellationTokenSource ct = new();
            measurement = await ReadAsync("1", ct, readDelay, defaultTimeout);
            measurement = Convert.ToString(cosineCorrectionValue * Convert.ToDouble(measurement));
            return measurement;
        }

        public async Task<string> GetMeasurement()
        {
            if (!Connected)
            {
                return "No device connected"; //Nothing to disconnect from
            }
            ReadInProgress = true;
            string measurement;
            CancellationTokenSource ct = new();
            measurement = await ReadAsync("1", ct, readDelay, defaultTimeout);
            measurement = Convert.ToString(cosineCorrectionValue * Convert.ToDouble(measurement));
            return measurement;
        }
        public async Task<string> GetMeasurement_Uncorrected()
        {
            if (!Connected)
            {
                return "No device connected"; //Nothing to disconnect from
            }
            ReadInProgress = true;
            string measurement;
            CancellationTokenSource ct = new();
            measurement = await ReadAsync("1", ct, readDelay, defaultTimeout);
            return measurement;
        }

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
        public double cosineCorrectionValue = 1;
        public void COSINECalculation(string adjacent_point1, string adjacent_point2, string hypotenuse)
        {

            double adjacent = Math.Abs(Convert.ToDouble(adjacent_point2) - Convert.ToDouble(adjacent_point1));
            cosineCorrectionValue = adjacent / Math.Abs(Convert.ToDouble(hypotenuse));
        }

        public new bool Disconnect()
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
                UpdateChannelList();
                return true;
            }
            return false;
        }

        public void UpdateChannelList()
        {
            ChannelList.Clear();
            NumberOfChannels = 0;
            if (!Connected) return;

            Tuple<string, int> t3 = (Name, 1).ToTuple();
            ChannelList.Add(t3);
            NumberOfChannels = ChannelList.Count;
        }
    }
}
