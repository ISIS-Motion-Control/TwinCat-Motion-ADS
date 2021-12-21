using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
                if (uint.TryParse(value, out _))
                {
                    _cycles = Convert.ToUInt32(value);
                    _strCycles = value;
                    Properties.Settings.Default.airCycles = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _cycles;
        public uint Cycles
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
                if (uint.TryParse(value, out _))
                {
                    _settlingReads = Convert.ToUInt32(value);
                    _strSettlingReads = value;
                    Properties.Settings.Default.airSettlingReads = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _settlingReads;
        public uint SettlingReads
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
                if (uint.TryParse(value, out _))
                {
                    _ReadDelayMs = Convert.ToUInt32(value);
                    _strReadDelayMs = value;
                    Properties.Settings.Default.airReadDelayMs = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _ReadDelayMs;
        public uint ReadDelayMs
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
                if (uint.TryParse(value, out _))
                {
                    _DelayAfterExtend = Convert.ToUInt32(value);
                    _strDelayAfterExtend = value;
                    Properties.Settings.Default.airDelayAfterExtend = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _DelayAfterExtend;
        public uint DelayAfterExtend
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
                if (uint.TryParse(value, out _))
                {
                    _DelayAfterRetract = Convert.ToUInt32(value);
                    _strDelayAfterRetract = value;
                    Properties.Settings.Default.airDelayAfterRetract = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _DelayAfterRetract;
        public uint DelayAfterRetract
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
                if (uint.TryParse(value, out _))
                {
                    _ExtendTimeout = Convert.ToUInt32(value);
                    _strExtendTimeout = value;
                    Properties.Settings.Default.airExtendTimeout = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _ExtendTimeout;
        public uint ExtendTimeout
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
                if (uint.TryParse(value, out _))
                {
                    _RetractTimeout = Convert.ToUInt32(value);
                    _strRetractTimeout = value;
                    Properties.Settings.Default.airRetractTimeout = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _RetractTimeout;
        public uint RetractTimeout
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
