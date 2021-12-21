using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TwinCat_Motion_ADS
{
    public class MotionControllerChannel : INotifyPropertyChanged
    {
        private uint channelHandle;
        public PLC Plc { get; set; }

        private string _VariableString;
        public string VariableString
        {
            get { return _VariableString; }
            set
            {
                if (!Connected)
                {
                    _VariableString = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _VariableType;
        public string VariableType
        {
            get { return _VariableType; }
            set
            {
                if (!Connected)
                {
                    _VariableType = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _Connected;
        public bool Connected
        {
            get { return _Connected; }
            set
            {
                _Connected = value;
                OnPropertyChanged();
            }
        }

        public MotionControllerChannel()
        {

        }

        public bool Connect()
        {
            //check plc is connected first

            if (!Plc.checkConnection())
            {
                Console.WriteLine("Not connected to PLC");
                Connected = false;
                return false;
            }
            //create the variable handle
            Console.WriteLine(VariableString);
            if (!CreateVariableHandle())
            {
                Console.WriteLine("Failed to create handle");
                Connected = false;
                return false;
            }
            //confirm able to read handle
            if (CanGetMeasurement() == null)
            {
                Connected = false;
                return false;
            }
            Console.WriteLine("Connected to channel");
            Connected = true;
            return true;
        }

        public bool Disconnect()
        {
            //not really anything we need to do to disconnect, just set flag so that device doesn't read
            Connected = false;
            return true;
        }

        //want to create handle and do a test read before confirming connected!

        public bool CreateVariableHandle()
        {
            try
            {
                channelHandle = Plc.TcAds.CreateVariableHandle(VariableString);
                return true;
            }
            catch
            {
                Console.WriteLine("Failed to find variable string");
                return false;
            }
        }

        public string CanGetMeasurement()   //need a non async method to not block
        {
            try
            {
                switch (VariableType)    //string, double, short, bool types to start with
                {
                    case "string":
                        var resultString = Plc.TcAds.ReadAny<string>(channelHandle);
                        return resultString.ToString();
                    case "double":
                        var resultDouble = Plc.TcAds.ReadAny<double>(channelHandle);
                        return resultDouble.ToString();
                    case "short":
                        var resultShort = Plc.TcAds.ReadAny<short>(channelHandle);
                        return resultShort.ToString();
                    case "bool":
                        var resultBool = Plc.TcAds.ReadAny<bool>(channelHandle);
                        return resultBool.ToString();
                    default:
                        return "No valid variable type selected";
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetMeasurementAsync()
        {
            try
            {
                switch (VariableType)    //string, double, short, bool types to start with
                {
                    case "string":
                        ResultValue<string> resultString = await Plc.TcAds.ReadAnyAsync<string>(channelHandle, CancellationToken.None);
                        return resultString.Value.ToString();
                    case "double":
                        ResultValue<double> resultDouble = await Plc.TcAds.ReadAnyAsync<double>(channelHandle, CancellationToken.None);
                        return resultDouble.Value.ToString();
                    case "short":
                        ResultValue<short> resultShort = await Plc.TcAds.ReadAnyAsync<short>(channelHandle, CancellationToken.None);
                        return resultShort.Value.ToString();
                    case "bool":
                        ResultValue<bool> resultBool = await Plc.TcAds.ReadAnyAsync<bool>(channelHandle, CancellationToken.None);
                        return resultBool.Value.ToString();
                    default:
                        return "No valid variable type selected";
                }
            }
            catch
            {
                return null;
            }
        }




        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
