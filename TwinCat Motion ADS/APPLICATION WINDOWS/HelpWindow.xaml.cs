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
using TwinCat_Motion_ADS.Application_Windows;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpScreen_EndToEnd helpScreen_EndToEnd = new();
        public HelpScreen_Unidirectional helpScreen_Unidirectional = new();
        public HelpScreen_Bidirectional helpScreen_Bidirectional = new();
        public HelpScreen_Scaling helpScreen_Scaling = new();
        public HelpScreen_Backlash helpScreen_Backlash = new();

        public HelpWindow()
        {
            InitializeComponent();
        }

        private void MenuSelect_Click(object sender, RoutedEventArgs e)
        {
            if(((MenuItem)sender) == EndToEnd)
            {
                helpWindow.Content = helpScreen_EndToEnd;
            }
            if (((MenuItem)sender) == Unidirectional)
            {
                helpWindow.Content = helpScreen_Unidirectional;
            }
            if (((MenuItem)sender) == Bidirectional)
            {
                helpWindow.Content = helpScreen_Bidirectional;
            }
            if (((MenuItem)sender) == Scaling)
            {
                helpWindow.Content = helpScreen_Scaling;
            }
            if (((MenuItem)sender) == Backlash)
            {
                helpWindow.Content = helpScreen_Backlash;
            }
        }
    }
}
