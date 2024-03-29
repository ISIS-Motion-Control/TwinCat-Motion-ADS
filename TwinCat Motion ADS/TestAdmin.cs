﻿using CsvHelper;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TwinCat_Motion_ADS.MeasurementDevice;

namespace TwinCat_Motion_ADS
{
    public abstract class TestAdmin : INotifyPropertyChanged
    {
        //PLC object to which the test axis belongs
        public PLC Plc { get; set; }

        //Progress and completion
        private double _testProgress;
        public double TestProgress  //between 0 and 1
        {
            get { return _testProgress; }
            set
            {
                _testProgress = value;
                OnPropertyChanged();
            }
        }
        protected double progScaler;
        protected double stepScaler;
        public EstimatedTime EstimatedTimeRemaining = new();


        //Directory for saving test csv
        public string TestDirectory { get; set; } = string.Empty;
        
        
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
        
        public async Task<bool> PauseTask(CancellationToken ct)
        {
            bool firstCycle = true;
            while (PauseTest)
            {
                if (firstCycle) Console.WriteLine("Test Paused");
                firstCycle = false;
                await Task.Delay(10, ct);
                if(ct.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                if (CancelTest) break;
            }
            return true;
        }

        public async Task<bool> CancelTestTask(CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                while (true)
                {
                    if (IsTestCancelled())
                    {
                        return true;
                    }
                    if (ct.IsCancellationRequested)
                    {
                        return false;
                    }
                }
            });           
        }

        public bool IsTestCancelled()
        {
            if(CancelTest)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() => {Console.WriteLine("Test cancelled");}));
                TestProgress = 0;
                EstimatedTimeRemaining.TimeRemaining = "";
                EstimatedTimeRemaining.EstimatedEndTime = DateTime.Now;
                PauseTest = false;
                CancelTest = false;
                return true;
            }
            else
            {
                return false;
            }
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
        protected bool ValidCommand() //always going to check if PLC is valid or not
        {
            if (!Plc.IsStateRun())
            {
                Console.WriteLine("Incorrect PLC configuration");
                Valid = false;
                return false;
            }

            Valid = true;
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #region CSV STUFF FOR AIR TESTS


        public void StartCSVAIR(string fp, MeasurementDevices md)
        {
            using (FileStream stream = File.Open(fp, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(stream))
            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<PneumaticEnd2EndCSVv2>();
                if (md != null)
                {
                    foreach (var device in md.MeasurementDeviceList) //for every device in the list
                    {
                        if (device.Connected)   //if the device is connected
                        {
                            foreach (var channel in device.ChannelList)  //for every channel on the device
                            {
                                csv.WriteField(channel.Item1);  //add a header for it
                            }
                        }
                    }
                }
                csv.NextRecord();
            }
        }

        public async Task<bool> WriteToCSVAIR(string fp, PneumaticEnd2EndCSVv2 csvData, MeasurementDevices md)
        {
            int retryCounter = 0;
            while (true)
            {
                try
                {
                    using (FileStream stream = File.Open(fp, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (StreamWriter writer = new StreamWriter(stream))
                    using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecord(csvData);
                        if (md != null)
                        {

                            foreach (var device in md.MeasurementDeviceList)
                            {
                                if (device.Connected)
                                {
                                    foreach (var channel in device.ChannelList)
                                    {
                                        var measurement = await device.GetChannelMeasurement(channel.Item2);
                                        Console.WriteLine(channel.Item1 + ": " + measurement);
                                        csv.WriteField(measurement);
                                    }
                                }
                            }
                        }
                        csv.NextRecord();
                    }
                    return true;
                }
                catch
                {
                    if (retryCounter == 3)
                    {
                        return false;
                    }
                    retryCounter += 1;
                    MessageBox.Show("File not accesible. Press OK to retry.\n" + (4 - retryCounter) + " attempt(s) remaining.");

                }
            }


        }

        #endregion


    }

    //Class to handle UI elements of setting elements of type string - not strictly necessary, more for standardisation of rest of code.
    public class SettingTestType : INotifyPropertyChanged
    {
        PropertyDescriptor pd;
        public SettingTestType(string settingName)
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
                if(Enum.TryParse<TestTypes>(value, out _))
                {
                    _val = Enum.Parse<TestTypes>(value);
                    _uiVal = value;
                    pd.SetValue(Properties.Settings.Default, value);
                    OnPropertyChanged();
                }
            }
        }
        private TestTypes _val;
        public TestTypes Val
        {
            get { return _val; }
            set
            {
                _val = value;
                UiVal = value.GetStringValue();
                OnPropertyChanged();
            }
        }
    }
    
    
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

    public class EstimatedTime : INotifyPropertyChanged
    {
        public EstimatedTime()
        {           
            StartTime = DateTime.Now;
            EstimatedEndTime = DateTime.Now;
            TimeRemaining = "";
            GetTimeRemaining();
            lastUpdatedCycle = 0;
        }

        private DateTime _StartTime;
        public DateTime StartTime
        {
            get { return _StartTime; }
            set { _StartTime = value;
                OnPropertyChanged();
            }
        }
        private TimeSpan _CycleTime;
        public TimeSpan CycleTime
        {
            get { return _CycleTime; }
            set
            {
                _CycleTime = value;
                OnPropertyChanged();
            }
        }
        private DateTime _EstimatedEndTime;
        public DateTime EstimatedEndTime
        {
            get { return _EstimatedEndTime; }
            set
            {
                _EstimatedEndTime = value;
                StrEndTime = EstimatedEndTime.ToString();
                OnPropertyChanged();
            }
        }
        private string _StrEndTime;
        public string StrEndTime
        {
            get { return EstimatedEndTime.ToString(); }
            set { _StrEndTime = value;
                OnPropertyChanged();
            }
        }
        private string _TimeRemaining;
        public string TimeRemaining
        {
            get { return _TimeRemaining; }
            set
            {
                _TimeRemaining = value;
                OnPropertyChanged();
            }
        }

        private uint lastUpdatedCycle;

        private void UpdateStartTime()
        {
            StartTime = DateTime.Now;
        }
        private void UpdateCycleTime()
        {
            CycleTime = DateTime.Now - StartTime;
        }
        private void UpdateEstimatedEndTime(uint remainingCycles)
        {
            EstimatedEndTime = DateTime.Now + CycleTime * remainingCycles;
        }

        //Call this method at the start of each test cycle
        public void TimeEstimateUpdate(uint currentCycle, uint totalCycles)
        {
            if(currentCycle == 1)
            {
                UpdateStartTime();
                return;
            }
            UpdateCycleTime();
            UpdateStartTime();
            lastUpdatedCycle = currentCycle;
            UpdateEstimatedEndTime((uint)(totalCycles-currentCycle+1));

        }

        private void GetTimeRemaining()
        {
            Task.Run(() => TimeRemainingCalc(CancellationToken.None));
        }

        private  Task<string> TimeRemainingCalc(CancellationToken ct)
        { 
            while(!ct.IsCancellationRequested)
            {
                if (EstimatedEndTime > DateTime.Now)
                {
                    TimeSpan temp = EstimatedEndTime - DateTime.Now;
                    //TimeRemaining = String.Format("{d} days {hh} hours {mm} minutes {ss} seconds", temp);

                    string formatted = string.Format("{0}{1}{2}{3}",
                    temp.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", temp.Days, temp.Days == 1 ? string.Empty : "s") : string.Empty,
                    temp.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", temp.Hours, temp.Hours == 1 ? string.Empty : "s") : string.Empty,
                    temp.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", temp.Minutes, temp.Minutes == 1 ? string.Empty : "s") : string.Empty,
                    temp.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", temp.Seconds, temp.Seconds == 1 ? string.Empty : "s") : string.Empty);
                    TimeRemaining = formatted;
                }
                else 
                {
                    TimeRemaining = "";
                }
            }
            return Task.FromResult(TimeRemaining);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class StringValueAttribute : Attribute
    {

        #region Properties

        /// <summary>
        /// Holds the stringvalue for a value in an enum.
        /// </summary>
        public string StringValue { get; protected set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor used to init a StringValue Attribute
        /// </summary>
        /// <param name="value"></param>
        public StringValueAttribute(string value)
        {
            this.StringValue = value;
        }

        #endregion

    }
    public static class Extension
    {
        public static string GetStringValue(this Enum value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            FieldInfo fieldInfo = type.GetField(value.ToString());

            // Get the stringvalue attributes
            StringValueAttribute[] attribs = fieldInfo.GetCustomAttributes(
                typeof(StringValueAttribute), false) as StringValueAttribute[];

            // Return the first if there was a match.
            return attribs.Length > 0 ? attribs[0].StringValue : null;
        }
    }
}
