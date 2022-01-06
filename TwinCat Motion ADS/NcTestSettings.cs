using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TwinCat_Motion_ADS
{
    public class NcTestSettings : INotifyPropertyChanged
    {
        /*
         * Class for handling test settings
         * Will handle string/var conversion for tests
         * Will handle "bad" data inputs - string value won't update unless input can be correctly casted
         */
        public NcTestSettings()
        {

            TestTitle.UiVal = Properties.Settings.Default.testTitle;

            Velocity.UiVal = Properties.Settings.Default.velocity;

            Timeout.UiVal = Properties.Settings.Default.timeout;
            StrCycles = Properties.Settings.Default.cycles;
            StrCycleDelaySeconds = Properties.Settings.Default.cycleDelaySeconds;
            StrReversalVelocity = Properties.Settings.Default.reversalVelocity;
            StrReversalExtraTimeSeconds = Properties.Settings.Default.reversalExtraTimeSeconds;
            StrReversalSettleTimeSeconds = Properties.Settings.Default.reversalSettleTimeSeconds;
            StrInitialSetpoint = Properties.Settings.Default.initialSetpoint;
            StrNumberOfSteps = Properties.Settings.Default.numberOfSteps;
            StrStepSize = Properties.Settings.Default.stepSize;
            StrSettleTimeSeconds = Properties.Settings.Default.settleTimeSeconds;
            StrReversalDistance = Properties.Settings.Default.reversalDistance;

            //Test code
            OvershootDistance.UiVal = Properties.Settings.Default.overshootDistance;
        }

        public void ResetSettings()
        {
            TestTitle.UiVal = "New Test";
            Velocity.UiVal = "0";
            Timeout.UiVal = "0";
            StrCycles = "1";
            StrCycleDelaySeconds = "0";
            StrReversalVelocity = "0";
            StrReversalExtraTimeSeconds = "0";
            StrReversalSettleTimeSeconds = "0";
            StrInitialSetpoint = "0";
            StrNumberOfSteps = "1";
            StrStepSize = "0";
            StrSettleTimeSeconds = "0";
            StrReversalDistance = "0";
            OvershootDistance.UiVal = "0";
        }

        //Method to import and export test settings
        public SettingString TestTitle { get; set; } = new("testTitle");
        public SettingDouble Velocity { get; set; } = new("velocity");
        public SettingUint Timeout { get; set; } = new("timeout");



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
                    Properties.Settings.Default.cycles = value;
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

        private string _strCycleDelaySeconds;
        public string StrCycleDelaySeconds
        {
            get { return _strCycleDelaySeconds; }
            set
            {
                if (uint.TryParse(value, out _))
                {
                    _cycleDelaySeconds = Convert.ToUInt32(value);
                    _strCycleDelaySeconds = value;
                    Properties.Settings.Default.cycleDelaySeconds = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _cycleDelaySeconds;
        public uint CycleDelaySeconds
        {
            get { return _cycleDelaySeconds; }
            set
            {
                _cycleDelaySeconds = value;
                StrCycleDelaySeconds = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strReversalVelocity;
        public string StrReversalVelocity
        {
            get { return _strReversalVelocity; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    _reversalVelocity = Convert.ToDouble(value);
                    _strReversalVelocity = value;
                    Properties.Settings.Default.reversalVelocity = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _reversalVelocity;
        public double ReversalVelocity
        {
            get { return _reversalVelocity; }
            set
            {
                _reversalVelocity = value;
                StrReversalVelocity = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strReversalExtraTimeSeconds;
        public string StrReversalExtraTimeSeconds
        {
            get { return _strReversalExtraTimeSeconds; }
            set
            {
                if (uint.TryParse(value, out _))
                {
                    _reversalExtraTimeSeconds = Convert.ToUInt32(value);
                    _strReversalExtraTimeSeconds = value;
                    Properties.Settings.Default.reversalExtraTimeSeconds = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _reversalExtraTimeSeconds;
        public uint ReversalExtraTimeSeconds
        {
            get { return _reversalExtraTimeSeconds; }
            set
            {
                _reversalExtraTimeSeconds = value;
                StrReversalExtraTimeSeconds = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strReversalSettleTimeSeconds;
        public string StrReversalSettleTimeSeconds
        {
            get { return _strReversalSettleTimeSeconds; }
            set
            {
                if (uint.TryParse(value, out _))
                {
                    _reversalSettleTimeSeconds = Convert.ToUInt32(value);
                    _strReversalSettleTimeSeconds = value;
                    Properties.Settings.Default.reversalSettleTimeSeconds = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _reversalSettleTimeSeconds;
        public uint ReversalSettleTimeSeconds
        {
            get { return _reversalSettleTimeSeconds; }
            set
            {
                _reversalSettleTimeSeconds = value;
                StrReversalSettleTimeSeconds = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strInitialSetpoint;
        public string StrInitialSetpoint
        {
            get { return _strInitialSetpoint; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    _initialSetpoint = Convert.ToDouble(value);
                    _strInitialSetpoint = value;
                    Properties.Settings.Default.initialSetpoint = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _initialSetpoint;
        public double InitialSetpoint
        {
            get { return _initialSetpoint; }
            set
            {
                _initialSetpoint = value;
                StrInitialSetpoint = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strNumberOfSteps;
        public string StrNumberOfSteps
        {
            get { return _strNumberOfSteps; }
            set
            {
                if (uint.TryParse(value, out _))
                {
                    _numberOfSteps = Convert.ToUInt32(value);
                    _strNumberOfSteps = value;
                    Properties.Settings.Default.numberOfSteps = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _numberOfSteps;
        public uint NumberOfSteps
        {
            get { return _numberOfSteps; }
            set
            {
                _numberOfSteps = value;
                StrNumberOfSteps = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strStepSize;
        public string StrStepSize
        {
            get { return _strStepSize; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    _stepSize = Convert.ToDouble(value);
                    _strStepSize = value;
                    Properties.Settings.Default.stepSize = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _stepSize;
        public double StepSize
        {
            get { return _stepSize; }
            set
            {
                _stepSize = value;
                StrStepSize = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strSettleTimeSeconds;
        public string StrSettleTimeSeconds
        {
            get { return _strSettleTimeSeconds; }
            set
            {
                if (uint.TryParse(value, out _))
                {
                    _settleTimeSeconds = Convert.ToUInt32(value);
                    _strSettleTimeSeconds = value;
                    Properties.Settings.Default.settleTimeSeconds = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _settleTimeSeconds;
        public uint SettleTimeSeconds
        {
            get { return _settleTimeSeconds; }
            set
            {
                _settleTimeSeconds = value;
                StrSettleTimeSeconds = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strReversalDistance;
        public string StrReversalDistance
        {
            get { return _strReversalDistance; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    _reversalDistance = Convert.ToDouble(value);
                    _strReversalDistance = value;
                    Properties.Settings.Default.reversalDistance = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _reversalDistance;
        public double ReversalDistance
        {
            get { return _reversalDistance; }
            set
            {
                _reversalDistance = value;
                StrReversalDistance = value.ToString();
                OnPropertyChanged();
            }
        }


        public SettingDouble OvershootDistance = new("overshootDistance");


        //modify this to XML
        public void ImportSettings(string ImportSettingsFile)
        {
            if (!File.Exists(ImportSettingsFile)) { return; }   //Check the selected file exists
            Console.WriteLine(ImportSettingsFile);              //Print to console path of selected file

            //Import velocity string
            string line = File.ReadLines(ImportSettingsFile).Skip(2).Take(1).First();
            int charStartIndex = line.IndexOf(": ") + 2;
            string valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            Velocity.UiVal = valStr;

            //Import timeout string
            line = File.ReadLines(ImportSettingsFile).Skip(3).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            Timeout.UiVal = valStr;

            //Import number of cycles string
            line = File.ReadLines(ImportSettingsFile).Skip(4).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrCycles = valStr;

            //Import delay between cycles string
            line = File.ReadLines(ImportSettingsFile).Skip(5).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrCycleDelaySeconds = valStr;

            //Import limit switch reversing velocity string
            line = File.ReadLines(ImportSettingsFile).Skip(6).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrReversalVelocity = valStr;

            //Import extra time after reversing string
            line = File.ReadLines(ImportSettingsFile).Skip(7).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrReversalExtraTimeSeconds = valStr;

            //Import time to let position settle string
            line = File.ReadLines(ImportSettingsFile).Skip(8).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrReversalSettleTimeSeconds = valStr;

            //Import initial setpoint string
            line = File.ReadLines(ImportSettingsFile).Skip(9).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrInitialSetpoint = valStr;

            //Import number of steps in test string
            line = File.ReadLines(ImportSettingsFile).Skip(10).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrNumberOfSteps = valStr;

            //Import size of steps string
            line = File.ReadLines(ImportSettingsFile).Skip(11).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrStepSize = valStr;

            //Import time to let position settle before taking measurement string
            line = File.ReadLines(ImportSettingsFile).Skip(12).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrSettleTimeSeconds = valStr;

            //Import distance to undershoot for forward moves string
            line = File.ReadLines(ImportSettingsFile).Skip(13).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrReversalDistance = valStr;

            //Import distance to overshoot for backward moves string
            line = File.ReadLines(ImportSettingsFile).Skip(14).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            OvershootDistance.UiVal = valStr;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
                if (double.TryParse(value,out _))
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
