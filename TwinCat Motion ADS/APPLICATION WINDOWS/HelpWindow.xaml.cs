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

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpScreen_EndToEnd HelpScreen_EndToEnd = new();
        public HelpScreen_Unidirectional HelpScreen_Unidirectional = new();
        public HelpScreen_Bidirectional HelpScreen_Bidirectional = new();
        public HelpScreen_Scaling HelpScreen_Scaling = new();
        public HelpScreen_Backlash HelpScreen_Backlash = new();

        public HelpWindow()
        {
            InitializeComponent();
        }

        private void MenuSelect_Click(object sender, RoutedEventArgs e)
        {
            if(((MenuItem)sender) == EndToEnd)
            {
                Console.WriteLine("Hit : END2END");
                helpWindow.Content = HelpScreen_EndToEnd;
            }
            if (((MenuItem)sender) == Unidirectional)
            {
                Console.WriteLine("Hit : UNI");
                helpWindow.Content = HelpScreen_Unidirectional;
            }
            if (((MenuItem)sender) == Bidirectional)
            {
                Console.WriteLine("Hit : BiDi");
                helpWindow.Content = HelpScreen_Bidirectional;
            }
            if (((MenuItem)sender) == Scaling)
            {
                Console.WriteLine("Hit : Scaling");
                helpWindow.Content = HelpScreen_Scaling;
            }
            if (((MenuItem)sender) == Backlash)
            {
                Console.WriteLine("Hit : Backlash");
                helpWindow.Content = HelpScreen_Backlash;
            }
        }
    }
}
