using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


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
        public string Value { get; set; }
        public string SetName { get; set; }
        public int BoxWidth { get; set; } = 200;

        public bool Disabled
        {
            get { return (bool)GetValue(DisabledProperty); }
            set { SetValue(DisabledProperty, value); }
        }

        //custom property so that I can disable the input from the user
        public static DependencyProperty DisabledProperty = DependencyProperty.Register("Disabled", typeof(bool), typeof(SettingControlMain));
    
    }

    public class InvertedBoolenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}