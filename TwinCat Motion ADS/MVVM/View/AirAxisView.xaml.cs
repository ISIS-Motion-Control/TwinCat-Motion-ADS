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
    /// Interaction logic for AirAxisView.xaml
    /// </summary>
    public partial class AirAxisView : UserControl
    {
        public AirAxisView()
        {
            InitializeComponent();
        }

        private void initPneumatic_Click(object sender, RoutedEventArgs e)
        {
            var window = (MainWindow)Application.Current.MainWindow;
            window.initPneumatic_Click(sender, e);
        }
    }
}
