using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TwinCat_Motion_ADS
{
    public class AirTestSettings : INotifyPropertyChanged
    {
        public AirTestSettings()
        {
            StrCycles = Properties.Settings.Default.airCycles;
            StrSettlingReads = Properties.Settings.Default.airSettlingReads;
            StrReadDelayMs = Properties.Settings.Default.airReadDelayMs;
            StrDelayAfterExtend = Properties.Settings.Default.airDelayAfterExtend;
            StrDelayAfterRetract = Properties.Settings.Default.airDelayAfterRetract;
            StrExtendTimeout = Properties.Settings.Default.airExtendTimeout;
            StrRetractTimeout = Properties.Settings.Default.airRetractTimeout;
        }



        private string _strCycles;
        public string StrCycles
        {
            get { return _strCycles; }
            set
            {
                if(int.TryParse(value, out _cycles))
                {
                    _strCycles = value;
                    Properties.Settings.Default.airCycles = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _cycles;
        public int Cycles
        {
            get { return _cycles; }
            set
            {
                _cycles = value;
                StrCycles = value.ToString();
                OnPropertyChanged();
            }
        }


        private string _strSettlingReads;
        public string StrSettlingReads
        {
            get { return _strSettlingReads; }
            set
            {
                if (int.TryParse(value, out _settlingReads))
                {
                    _strSettlingReads = value;
                    Properties.Settings.Default.airSettlingReads = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _settlingReads;
        public int SettlingReads
        {
            get { return _settlingReads; }
            set
            {
                _settlingReads = value;
                StrSettlingReads = value.ToString();
                OnPropertyChanged();
            }
        }


        private string _strReadDelayMs;
        public string StrReadDelayMs
        {
            get { return _strReadDelayMs; }
            set
            {
                if (int.TryParse(value, out _ReadDelayMs))
                {
                    _strReadDelayMs = value;
                    Properties.Settings.Default.airReadDelayMs = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _ReadDelayMs;
        public int ReadDelayMs
        {
            get { return _ReadDelayMs; }
            set
            {
                _ReadDelayMs = value;
                StrReadDelayMs = value.ToString();
                OnPropertyChanged();
            }
        }


        private string _strDelayAfterExtend;
        public string StrDelayAfterExtend
        {
            get { return _strDelayAfterExtend; }
            set
            {
                if (int.TryParse(value, out _DelayAfterExtend))
                {
                    _strDelayAfterExtend = value;
                    Properties.Settings.Default.airDelayAfterExtend = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _DelayAfterExtend;
        public int DelayAfterExtend
        {
            get { return _DelayAfterExtend; }
            set
            {
                _DelayAfterExtend = value;
                StrDelayAfterExtend = value.ToString();
                OnPropertyChanged();
            }
        }


        private string _strDelayAfterRetract;
        public string StrDelayAfterRetract
        {
            get { return _strDelayAfterRetract; }
            set
            {
                if (int.TryParse(value, out _DelayAfterRetract))
                {
                    _strDelayAfterRetract = value;
                    Properties.Settings.Default.airDelayAfterRetract = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _DelayAfterRetract;
        public int DelayAfterRetract
        {
            get { return _DelayAfterRetract; }
            set
            {
                _DelayAfterRetract = value;
                StrDelayAfterRetract = value.ToString();
                OnPropertyChanged();
            }
        }


        private string _strExtendTimeout;
        public string StrExtendTimeout
        {
            get { return _strExtendTimeout; }
            set
            {
                if (int.TryParse(value, out _ExtendTimeout))
                {
                    _strExtendTimeout = value;
                    Properties.Settings.Default.airExtendTimeout = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _ExtendTimeout;
        public int ExtendTimeout
        {
            get { return _ExtendTimeout; }
            set
            {
                _ExtendTimeout = value;
                StrExtendTimeout = value.ToString();
                OnPropertyChanged();
            }
        }


        private string _strRetractTimeout;
        public string StrRetractTimeout
        {
            get { return _strRetractTimeout; }
            set
            {
                if (int.TryParse(value, out _RetractTimeout))
                {
                    _strRetractTimeout = value;
                    Properties.Settings.Default.airRetractTimeout = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _RetractTimeout;
        public int RetractTimeout
        {
            get { return _RetractTimeout; }
            set
            {
                _RetractTimeout = value;
                StrRetractTimeout = value.ToString();
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
