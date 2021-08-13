using System;
using System.Runtime.InteropServices;
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
using TwinCAT.Ads;


namespace TwinCat_Motion_ADS
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TestClass testSystem = TestClass.Instance;
        public MainWindow()
        {
            InitializeComponent();


            ConsoleAllocator.ShowConsoleWindow();
            testSystem.setupPLC();
        }
        public void setupBinds()
        {
            Binding axisPositionBind = new Binding();
            axisPositionBind.Mode = BindingMode.TwoWay;
            axisPositionBind.Source = testSystem.testAxis;
            axisPositionBind.Path = new PropertyPath("AxisPosition");
            axisPositionBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            axisPositionBind.StringFormat= "F3";
            BindingOperations.SetBinding(axisPositionRB, TextBlock.TextProperty, axisPositionBind);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            double posCommanded = Convert.ToInt32(positionText.Text);
            await testSystem.testAxis.moveAbsoluteAndWait(posCommanded, 20);
        }

        private void initAxis_Click(object sender, RoutedEventArgs e)
        {
            testSystem.axisInstance(Convert.ToUInt32(axisSelection.Text));
            setupBinds(); //doesn't seem to be working right
            
        }

        private async void moveRelButton_Click(object sender, RoutedEventArgs e)
        {
            double posCommanded = Convert.ToInt32(positionText.Text);
            await testSystem.testAxis.moveRelativeAndWait(posCommanded, 20);
        }

        private async void moveVelButton_Click(object sender, RoutedEventArgs e)
        {
            await testSystem.testAxis.moveVelocity( 20);
        }

        private void stopPosRead_Click(object sender, RoutedEventArgs e)
        {
            testSystem.testAxis.StopPositionRead();
        }
    }


    internal static class ConsoleAllocator
    {
        [DllImport(@"kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport(@"kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport(@"user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SwHide = 0;
        const int SwShow = 5;


        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                AllocConsole();
            }
            else
            {
                ShowWindow(handle, SwShow);
            }
        }

        public static void HideConsoleWindow()
        {
            var handle = GetConsoleWindow();

            ShowWindow(handle, SwHide);
        }
    }

}
