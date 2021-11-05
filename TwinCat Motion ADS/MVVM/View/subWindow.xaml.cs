using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TwinCat_Motion_ADS.MVVM.View
{
    /// <summary>
    /// Interaction logic for subWindow.xaml
    /// </summary>
    public partial class subWindow : Window
    {
        public subWindow()
        {
            
            InitializeComponent();

            baudRateTB.Text = ((MainWindow)(Application.Current.MainWindow)).MeasurementDevice1.BaudRate;
            /*Binding baudRateBind = new Binding();
            baudRateBind.Mode = BindingMode.TwoWay;
            //baudRateBind.Source = ;
            baudRateBind.Path = new PropertyPath("AxisPosition");
            baudRateBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            baudRateBind.StringFormat = "F3";
            BindingOperations.SetBinding(baudRateTB, TextBox.TextProperty, baudRateBind);*/
        }
    }
}
