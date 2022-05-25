using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TwinCat_Motion_ADS
{
    public static class XamlUI
    {
        public static void ProgressBarBinding(DependencyObject item, object source, string pp)
        {
            Binding ProgBind = new();
            ProgBind.Mode = BindingMode.OneWay;
            ProgBind.Source = source;
            ProgBind.Path = new PropertyPath(pp);
            ProgBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(item, ProgressBar.ValueProperty, ProgBind);
        }

        public static void TextboxBinding(DependencyObject item, object source, string pp, UpdateSourceTrigger ust = UpdateSourceTrigger.PropertyChanged)
        {
            Binding TextboxBind = new();
            TextboxBind.Mode = BindingMode.TwoWay;
            TextboxBind.Source = source;
            TextboxBind.Path = new PropertyPath(pp);
            TextboxBind.UpdateSourceTrigger = ust;
            BindingOperations.SetBinding(item, TextBox.TextProperty, TextboxBind);
        }

        public static void TextBlockBinding(DependencyObject item, object source, string pp, string sFormat = "F3")
        {
            Binding TextBlockBind = new();
            TextBlockBind.Mode = BindingMode.OneWay;
            TextBlockBind.Source = source;
            TextBlockBind.Path = new PropertyPath(pp);
            TextBlockBind.StringFormat = sFormat;
            TextBlockBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(item, TextBlock.TextProperty, TextBlockBind);
        }

        public static void CheckBoxBinding(string content, DependencyObject item, object source, string pp, BindingMode bm = BindingMode.TwoWay)
        {
            //((CheckBox)item).Content = content;
            Binding checkBoxBind = new();
            checkBoxBind.Mode = bm;
            checkBoxBind.Source = source;
            checkBoxBind.Path = new PropertyPath(pp);
            checkBoxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(item, CheckBox.IsCheckedProperty, checkBoxBind);
        }

        public static void ComboBoxBinding(ObservableCollection<string> list, DependencyObject item, object source, string pp, BindingMode bm = BindingMode.OneWay)
        {
            ((ComboBox)item).ItemsSource = list;
            Binding comboBind = new();
            comboBind.Mode = bm;
            comboBind.Source = source;
            comboBind.Path = new PropertyPath(pp);
            comboBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(item, ComboBox.SelectedValueProperty, comboBind);
        }

        public static void SetupButton(ref Button but, string butText)
        {
            but.Content = butText;
            but.Width = 120;
            but.Margin = new Thickness(5);
            but.Height = 20;
        }

        public static void SetupTextBlock(ref TextBlock tb, string tbText, int wd = 100)
        {
            tb.HorizontalAlignment = HorizontalAlignment.Right;
            tb.TextAlignment = TextAlignment.Right;
            tb.Width = wd;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Text = tbText;
        }

        public static void SetupTextBox(ref TextBox tb, string tbText, double wd = 150)
        {
            tb.HorizontalAlignment = HorizontalAlignment.Right;
            tb.TextAlignment = TextAlignment.Center;
            tb.Width = wd;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Text = tbText;
            tb.Margin = new Thickness(10, 0, 0, 0);
        }

        public static void SetupComboBox(ref ComboBox cb, string name, ObservableCollection<string> itemSource)
        {
            cb.Name = name;
            cb.ItemsSource = itemSource;
            cb.Margin = new Thickness(10, 0, 0, 0);
            cb.HorizontalAlignment = HorizontalAlignment.Left;
            cb.VerticalAlignment = VerticalAlignment.Center;
            cb.Width = 150;
        }

    }
}
