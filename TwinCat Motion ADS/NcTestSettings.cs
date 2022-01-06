using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TwinCat_Motion_ADS.MVVM.View;

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
            //Settings are initialised with current saved defaults
            TestTitle.UiVal = Properties.Settings.Default.testTitle;
            Velocity.UiVal = Properties.Settings.Default.velocity;
            Timeout.UiVal = Properties.Settings.Default.timeout;
            Cycles.UiVal = Properties.Settings.Default.cycles;
            CycleDelaySeconds.UiVal = Properties.Settings.Default.cycleDelaySeconds;
            ReversalVelocity.UiVal = Properties.Settings.Default.reversalVelocity;
            ReversalExtraTimeSeconds.UiVal = Properties.Settings.Default.reversalExtraTimeSeconds;
            ReversalSettleTimeSeconds.UiVal = Properties.Settings.Default.reversalSettleTimeSeconds;
            InitialSetpoint.UiVal = Properties.Settings.Default.initialSetpoint;
            NumberOfSteps.UiVal = Properties.Settings.Default.numberOfSteps;
            StepSize.UiVal = Properties.Settings.Default.stepSize;
            SettleTimeSeconds.UiVal = Properties.Settings.Default.settleTimeSeconds;
            ReversalDistance.UiVal = Properties.Settings.Default.reversalDistance;
            OvershootDistance.UiVal = Properties.Settings.Default.overshootDistance;
        }

        public void ResetSettings()
        {
            TestTitle.UiVal = "New Test";
            Velocity.UiVal = "0";
            Timeout.UiVal = "0";
            Cycles.UiVal = "1";
            CycleDelaySeconds.UiVal = "0";
            ReversalVelocity.UiVal = "0";
            ReversalExtraTimeSeconds.UiVal = "0";
            ReversalSettleTimeSeconds.UiVal = "0";
            InitialSetpoint.UiVal = "0";
            NumberOfSteps.UiVal = "1";
            StepSize.UiVal = "0";
            SettleTimeSeconds.UiVal = "0";
            ReversalDistance.UiVal = "0";
            OvershootDistance.UiVal = "0";
        }

        //Method to import and export test settings
        public SettingString TestTitle { get; set; } = new("testTitle");
        public SettingDouble Velocity { get; set; } = new("velocity");
        public SettingUint Timeout { get; set; } = new("timeout");
        public SettingUint Cycles { get; set; } = new("cycles");
        public SettingUint CycleDelaySeconds { get; set; } = new("cycleDelaySeconds");
        public SettingDouble ReversalVelocity { get; set; } = new("reversalVelocity");
        public SettingUint ReversalExtraTimeSeconds { get; set; } = new("reversalExtraTimeSeconds");
        public SettingUint ReversalSettleTimeSeconds { get; set; } = new("reversalSettleTimeSeconds");
        public SettingDouble InitialSetpoint { get; set; } = new("initialSetpoint");
        public SettingUint NumberOfSteps { get; set; } = new("numberOfSteps");
        public SettingDouble StepSize { get; set; } = new("stepSize");
        public SettingUint SettleTimeSeconds { get; set; } = new("settleTimeSeconds");
        public SettingDouble ReversalDistance { get; set; } = new("reversalDistance");
        public SettingDouble OvershootDistance = new("overshootDistance");

        //modify this to XML
        public void ImportSettingsXML(string ImportSettingsFile)
        {
            XmlDocument doc = new();
            doc.Load(ImportSettingsFile);
            XmlNode testSettings = doc.SelectSingleNode("Settings");
            TestListItem tli = new("1"); //container
            TestSuite.ImportSingleTestSettings(tli, testSettings);
            TestTitle.UiVal = tli.TestSettings.TestTitle.UiVal;
            Velocity.UiVal = tli.TestSettings.Velocity.UiVal;

            Timeout.UiVal = tli.TestSettings.Timeout.UiVal; ;
            Cycles.UiVal = tli.TestSettings.Cycles.UiVal; ;
            CycleDelaySeconds.UiVal = tli.TestSettings.CycleDelaySeconds.UiVal; ;
            ReversalVelocity.UiVal = tli.TestSettings.ReversalVelocity.UiVal; ;
            ReversalExtraTimeSeconds.UiVal = tli.TestSettings.ReversalExtraTimeSeconds.UiVal; ;
            ReversalSettleTimeSeconds.UiVal = tli.TestSettings.ReversalSettleTimeSeconds.UiVal; ;
            InitialSetpoint.UiVal = tli.TestSettings.InitialSetpoint.UiVal; ;
            NumberOfSteps.UiVal = tli.TestSettings.NumberOfSteps.UiVal; ;
            StepSize.UiVal = tli.TestSettings.StepSize.UiVal; ;
            SettleTimeSeconds.UiVal = tli.TestSettings.SettleTimeSeconds.UiVal; ;
            ReversalDistance.UiVal = tli.TestSettings.ReversalDistance.UiVal; ;
            OvershootDistance.UiVal = tli.TestSettings.OvershootDistance.UiVal; ;
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
