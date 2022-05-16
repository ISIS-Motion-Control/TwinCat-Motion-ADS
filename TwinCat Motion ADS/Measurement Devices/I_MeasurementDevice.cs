using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TwinCat_Motion_ADS
{
    public interface I_MeasurementDevice
    {
        public bool Connect();
        public bool Disconnect();
        public Task<string> GetMeasurement();
        public Task<string> GetChannelMeasurement(int channelNumber = 0);
        public void UpdateChannelList();
        public void AddToChannelList(Tuple<string, int> ch);
        public void RemoveFromChannelList(Tuple<string, int> ch);


        public DeviceTypes DeviceType { get; set; }
        public int NumberOfChannels { get; set; }
        public List<Tuple<string, int>> ChannelList { get; set; }
        public bool ReadInProgress { get; set; }
        public string Name { get; set; }
        public bool Connected { get; set; }

    }

     public abstract class BaseMeasurementDevice : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _NumberOfChannels;
        public int NumberOfChannels
        {
            get { return _NumberOfChannels; }
            set
            {
                _NumberOfChannels = value;
                OnPropertyChanged();
            }
        }
        public DeviceTypes DeviceType { get; set; }
        public List<Tuple<string, int>> ChannelList { get; set; } =  new();

        public bool ReadInProgress { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set 
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                OnPropertyChanged();
            }
        }
        public void RemoveFromChannelList(Tuple<string, int> ch)
        {
            ChannelList.Remove(ch);
            NumberOfChannels = ChannelList.Count;
        }
        public void AddToChannelList(Tuple<string, int> ch)
        {
            ChannelList.Add(ch);
            NumberOfChannels = ChannelList.Count;
        }

        private bool AllowDisconnect()
        {
            if (!Connected)
            {
                Console.WriteLine("Nothing to disconnect from");
                return false; //Nothing to disconnect from
            }
            if (ReadInProgress)
            {
                Console.WriteLine("Cannot disconnect during read");
                return false;
            }
            return true;
        }
    }
}
