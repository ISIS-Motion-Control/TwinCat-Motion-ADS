﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TwinCAT.Ads;
using System.Collections.ObjectModel;

namespace TwinCat_Motion_ADS.MeasurementDevice
{
    public class MD_MotionControllerChannel : BaseMeasurementDevice, I_MeasurementDevice
    {
        public MD_MotionControllerChannel(PLC plc)
        {
            DeviceType = DeviceTypes.MotionChannel;
            Plc = plc;
        }
        public ObservableCollection<string> VariableTypeList = new ObservableCollection<string>()
        {
            "string","double","short","bool"
        };

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

        public bool Connect()
        {
            if (Plc == null)
            {
                Console.WriteLine("No controller initialised!");
                return false;
            }
            if (!Plc.checkConnection())
            {
                Console.WriteLine("Not connected to controller");
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
            UpdateChannelList();
            return true;
        }

        public bool Disconnect()
        {
            //not really anything we need to do to disconnect, just set flag so that device doesn't read
            Connected = false;
            return true;
        }

        public void UpdateChannelList()
        {
            ChannelList.Clear();
            Tuple<string, int> t2 = (Name, 1).ToTuple();
            ChannelList.Add(t2);
            NumberOfChannels = ChannelList.Count;
        }

        public bool CreateVariableHandle()
        {
            try
            {
                channelHandle = Plc.TcAds.CreateVariableHandle(VariableString);
                return true;
            }
            catch
            {
                Console.WriteLine("Failed to find access path and compatible type");
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

        public async Task<string> GetMeasurement()
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

        public async Task<string> GetChannelMeasurement(int channelNumber = 0)
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
    }
}
