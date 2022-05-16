using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TwinCat_Motion_ADS
{
    public class TimestampDevice : INotifyPropertyChanged
    {
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


        public bool Connect()
        {
            Connected = true;
            return true;
        }
        public bool Disconnect()
        {
            Connected = false;
            return true;
        }

        public string GetMeasurement()
        {
            DateTime systemTime = DateTime.Now;
            return systemTime.ToString("dd/MM/yyyy HH:mm:ss:fff");
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class TimestampDevice_V2 : BaseMeasurementDevice, I_MeasurementDevice
    {
        public bool Connect()
        {
            Connected = true;
            return true;
        }
        public bool Disconnect()
        {
            Connected = false;
            return true;
        }

        public async Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            DateTime systemTime = DateTime.Now;
            return systemTime.ToString("dd/MM/yyyy HH:mm:ss:fff");
        }

        public async Task<string> GetMeasurement()
        {
            DateTime systemTime = DateTime.Now;
            return systemTime.ToString("dd/MM/yyyy HH:mm:ss:fff");
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
