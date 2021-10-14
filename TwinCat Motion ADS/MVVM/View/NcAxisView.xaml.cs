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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TwinCat_Motion_ADS.MVVM.View
{
    /// <summary>
    /// Interaction logic for NcAxisView.xaml
    /// </summary>
    public partial class NcAxisView : UserControl
    {
        MainWindow windowData;
        public NcAxisView()
        {
            InitializeComponent();
            windowData = (MainWindow)Application.Current.MainWindow;
        }

        private void moveAbsButton_Click(object sender, RoutedEventArgs e)
        {            
            windowData.moveAbsButton_Click(sender, e);
        }
        public void setupBinds()
        {
            Binding axisPositionBind = new Binding();
            axisPositionBind.Mode = BindingMode.TwoWay;
            axisPositionBind.Source = windowData.testAxis;
            axisPositionBind.Path = new PropertyPath("AxisPosition");
            axisPositionBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            axisPositionBind.StringFormat = "F3";
            BindingOperations.SetBinding(axisPositionRB, TextBlock.TextProperty, axisPositionBind);
        }
    }
}
