using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwinCat_Motion_ADS.MeasurementDevice
{    
    public class MD_TimestampDevice : BaseMeasurementDevice, I_MeasurementDevice
    {
        public MD_TimestampDevice()
        {
            DeviceType = DeviceTypes.Timestamp;
        }

        public bool Connect()
        {
            Connected = true;
            
            UpdateChannelList();
            return true;
        }

        public bool Disconnect()
        {
            if (AllowDisconnect())
            {
                Connected = false;
                UpdateChannelList();
                return true;
            }
            return false;
            
        }

        public Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            DateTime systemTime = DateTime.Now;
            return Task.FromResult(systemTime.ToString("dd/MM/yyyy HH:mm:ss:fff"));

        }

        public  Task<string> GetMeasurement()
        {
            DateTime systemTime = DateTime.Now;
            return Task.FromResult(systemTime.ToString("dd/MM/yyyy HH:mm:ss:fff"));
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
