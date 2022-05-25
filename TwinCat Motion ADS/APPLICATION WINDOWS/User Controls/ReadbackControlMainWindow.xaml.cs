using System;
using System.Windows.Controls;


namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for SettingControl.xaml
    /// </summary>
    public partial class ReadbackControlMainWindow : UserControl
    {
        public ReadbackControlMainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public new string SetValue { get; set; }
        public string SetName { get; set; }
        public int BoxWidth { get; set; } = 200;
        public string strTests { get; set; }
        public int TextWidth { get; set; } = 220;
    }
}
