using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

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

        public bool EnableCosineCorrection { get; set; }

        private double cosineCorrectionValue;
        public double CosineCorrectionValue 
        { 
            get { return cosineCorrectionValue; } 
            set { cosineCorrectionValue = value; }
        }

        private string initialValue;
        public string InitialValue 
        { 
            get { return initialValue; } 
            set 
            {
                if (double.TryParse(value, out _))
                {
                    initialValue = value;
                }
            } 
        }

        private string distanceTraveled;
        public string DistanceTraveled 
        { 
            get { return distanceTraveled; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    distanceTraveled = value;
                }
            }
        }

        private string finalValue;
        public string FinalValue 
        { 
            get { return finalValue; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    finalValue = value;
                }
            }
        }

        public void CalculateCosineCorrection(object sender, EventArgs e)
        {
            if (EnableCosineCorrection)
            {
                COSINECalculation(InitialValue, DistanceTraveled, FinalValue);
                Console.WriteLine("Correction Value : " + Convert.ToString(CosineCorrectionValue));
            }
            else
            {
                Console.WriteLine("COSIGN Correction Not Enabled");
            }
            
        }

        public void ResetCosineCalculation(object sender, EventArgs e)
        {
            InitialValue = "0";
            DistanceTraveled = "1";
            FinalValue = "1";
            COSINECalculation(InitialValue, DistanceTraveled, FinalValue);
            Console.WriteLine("COSINE Calculaion Reset");
        }
        public async void InitialValue_ReadIn(object sender, EventArgs e)
        {
            if (!Connected)
            {
                Console.WriteLine("Not connected to a device");
                return;
            }
            else
            {
                string measurement = await GetMeasurement_Uncorrected();
                Console.WriteLine(Name + ": " + measurement);
                InitialValue = measurement;
            }
        }
        public async void FinalValue_ReadIn(object sender, EventArgs e)
        {
            if (!Connected)
            {
                Console.WriteLine("Not connected to a device");
                return;
            }
            else
            {
                string measurement = await GetMeasurement_Uncorrected();
                Console.WriteLine(Name + ": " + measurement);
                FinalValue = measurement;
            }
        }

        public void COSINECalculation(string adjacent_point1, string adjacent_point2, string hypotenuse)
        {

            double adjacent = Math.Abs(Convert.ToDouble(adjacent_point2) - Convert.ToDouble(adjacent_point1));
            CosineCorrectionValue = adjacent / Math.Abs(Convert.ToDouble(hypotenuse));
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
