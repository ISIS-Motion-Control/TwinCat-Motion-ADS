using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TwinCat_Motion_ADS
{
    public class AirTestSettings : INotifyPropertyChanged
    {
        public AirTestSettings()
        {
            Cycles.UiVal = Properties.Settings.Default.airCycles;
            SettlingReads.UiVal = Properties.Settings.Default.airSettlingReads;
            ReadDelayMs.UiVal = Properties.Settings.Default.airReadDelayMs;
            DelayAfterExtend.UiVal = Properties.Settings.Default.airDelayAfterExtend;
            DelayAfterRetract.UiVal = Properties.Settings.Default.airDelayAfterRetract;
            ExtendTimeout.UiVal = Properties.Settings.Default.airExtendTimeout;
            RetractTimeout.UiVal = Properties.Settings.Default.airRetractTimeout;
        }

        public SettingUint Cycles { get; set; } = new("airCycles");
        public SettingUint SettlingReads { get; set; } = new("airSettlingReads");
        public SettingUint ReadDelayMs { get; set; } = new("airReadDelayMs");
        public SettingUint DelayAfterExtend { get; set; } = new("airDelayAfterExtend");
        public SettingUint DelayAfterRetract { get; set; } = new("airDelayAfterRetract");
        public SettingUint ExtendTimeout { get; set; } = new("airExtendTimeout");
        public SettingUint RetractTimeout { get; set; } = new("airRetractTimeout");


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
