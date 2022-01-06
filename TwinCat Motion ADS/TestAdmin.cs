using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TwinCat_Motion_ADS
{
    //abstract defines this as an inheritance only class
    public abstract class TestAdmin : INotifyPropertyChanged
    {
        //PLC object to which the test axis belongs
        public PLC Plc { get; set; }

        //Directory for saving test csv
        public string TestDirectory { get; set; } = string.Empty;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        //Request test pause
        private bool _pauseTest = false;
        public bool PauseTest
        {
            get { return _pauseTest; }
            set { _pauseTest = value; OnPropertyChanged(); }
        }
        //Request test cancellation
        private bool _cancelTest = false;
        public bool CancelTest
        {
            get { return _cancelTest; }
            set { _cancelTest = value; OnPropertyChanged(); }
        }
        public async Task<bool> CheckCancellationRequestTask(CancellationToken wToken)
        {
            while (CancelTest == false)
            {
                await Task.Delay(10, wToken);
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            return true;
        }
        public async Task<bool> CheckPauseRequestTask(CancellationToken wToken)
        {
            if (PauseTest)
            {
                Console.WriteLine("Test Paused");
            }
            while (PauseTest)
            {
                await Task.Delay(10, wToken);
                if (CancelTest)
                {
                    return true;
                }
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            return true;
        }

        private bool _valid;
        public bool Valid
        {
            get { return _valid; }
            set
            {
                _valid = value;
                OnPropertyChanged();
            }
        }

    }

    //Class to handle UI elements of setting elements of type string - not strictly necessary, more for standardisation of rest of code.
    public class SettingString : INotifyPropertyChanged
    {
        PropertyDescriptor pd;
        public event PropertyChangedEventHandler PropertyChanged;
        private string _uiVal;
        private string _val;

        public SettingString(string settingName)
        {
            pd = TypeDescriptor.GetProperties(Properties.Settings.Default)[settingName];
        }

        public string UiVal
        {
            get { return _uiVal; }
            set
            {
                _uiVal = value;
                _val = value;
                pd.SetValue(Properties.Settings.Default, value);
                OnPropertyChanged();
            }
        }
        public string Val
        {
            get { return _val; }
            set
            {
                _val = value;
                UiVal = value;
                OnPropertyChanged();
            }
        }


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    //Class to handle UI elements of setting elements of type double
    public class SettingDouble : INotifyPropertyChanged
    {
        PropertyDescriptor pd;
        public SettingDouble(string settingName)
        {
            pd = TypeDescriptor.GetProperties(Properties.Settings.Default)[settingName];
        }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private string _uiVal; //textSetting
        public string UiVal
        {
            get { return _uiVal; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    _val = Convert.ToDouble(value);
                    _uiVal = value;

                    pd.SetValue(Properties.Settings.Default, value);
                    OnPropertyChanged();
                }
            }
        }
        private double _val;
        public double Val
        {
            get { return _val; }
            set
            {
                _val = value;
                UiVal = value.ToString();
                OnPropertyChanged();
            }
        }

    }
    //Class to handle UI elements of setting elements of type Uint
    public class SettingUint : INotifyPropertyChanged
    {
        PropertyDescriptor pd;
        private string _uiVal; //User interface element
        private uint _val;  //Logic element
        public event PropertyChangedEventHandler PropertyChanged;

        public SettingUint(string settingName)
        {
            pd = TypeDescriptor.GetProperties(Properties.Settings.Default)[settingName];
        }

        public string UiVal
        {
            get { return _uiVal; }
            set
            {
                if (uint.TryParse(value, out _))
                {
                    _val = Convert.ToUInt32(value);
                    _uiVal = value;

                    pd.SetValue(Properties.Settings.Default, value);
                    OnPropertyChanged();
                }
            }
        }
        public uint Val
        {
            get { return _val; }
            set
            {
                _val = value;
                UiVal = value.ToString();
                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
