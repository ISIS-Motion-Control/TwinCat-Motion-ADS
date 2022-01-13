using System;
using System.Windows.Controls;


namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for SettingControl.xaml
    /// </summary>
    public partial class SettingControlMain : UserControl
    {
        public SettingControlMain()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public string SetValue { get; set; }
        public string SetName { get; set; }
        public int BoxWidth { get; set; } = 200;
    }
}
