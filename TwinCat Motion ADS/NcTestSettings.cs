using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

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
            StrTestTitle = Properties.Settings.Default.testTitle;
            StrVelocity = Properties.Settings.Default.velocity;
            StrTimeout = Properties.Settings.Default.timeout;
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
            StrOvershootDistance = Properties.Settings.Default.overshootDistance;
        }

        //Method to import and export test settings
        private string _strTestTitle;
        public string StrTestTitle
        {
            get { return _strTestTitle; }
            set
            {
                _strTestTitle = value;
                Properties.Settings.Default.testTitle = value;
                OnPropertyChanged();
            }
        }

        private string _strVelocity;
        public string StrVelocity
        {
            get { return _strVelocity; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    _velocity = Convert.ToDouble(value);
                    _strVelocity = value;
                    Properties.Settings.Default.velocity = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _velocity;
        public double Velocity
        {
            get { return _velocity; }
            set
            {
                _velocity = value;
                StrVelocity = value.ToString();
                OnPropertyChanged();
            }
        }

        private string _strTimeout;
        public string StrTimeout
        {
            get { return _strTimeout; }
            set
            {
                if (uint.TryParse(value, out _))
                {
                    _timeout = Convert.ToUInt32(value);
                    _strTimeout = value;
                    Properties.Settings.Default.timeout = value;
                    OnPropertyChanged();
                }
            }
        }
        private uint _timeout;
        public uint Timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                StrTimeout = value.ToString();
                OnPropertyChanged();
            }
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

        private string _strOvershootDistance;
        public string StrOvershootDistance
        {
            get { return _strOvershootDistance; }
            set
            {
                if (double.TryParse(value, out _))
                {
                    _overshootDistance = Convert.ToDouble(value);
                    _strOvershootDistance = value;
                    Properties.Settings.Default.overshootDistance = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _overshootDistance;
        public double OvershootDistance
        {
            get { return _overshootDistance; }
            set
            {
                _overshootDistance = value;
                StrOvershootDistance = value.ToString();
                OnPropertyChanged();
            }
        }



        public void ImportSettings(string ImportSettingsFile)
        {
            if (!File.Exists(ImportSettingsFile)) { return; }   //Check the selected file exists
            Console.WriteLine(ImportSettingsFile);              //Print to console path of selected file

            //Import velocity string
            string line = File.ReadLines(ImportSettingsFile).Skip(2).Take(1).First();
            int charStartIndex = line.IndexOf(": ") + 2;
            string valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrVelocity = valStr;

            //Import timeout string
            line = File.ReadLines(ImportSettingsFile).Skip(3).Take(1).First();
            charStartIndex = line.IndexOf(": ") + 2;
            valStr = line.Substring(charStartIndex, line.Length - charStartIndex);
            StrTimeout = valStr;

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
            StrOvershootDistance = valStr;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
