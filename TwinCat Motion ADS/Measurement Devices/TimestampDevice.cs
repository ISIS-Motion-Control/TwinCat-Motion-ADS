using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
}
