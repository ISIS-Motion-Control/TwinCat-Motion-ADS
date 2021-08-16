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
        String keyboardInput = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(Window),Keyboard.KeyUpEvent, new KeyEventHandler(keyUp), true);
            
            ConsoleAllocator.ShowConsoleWindow();
            testSystem.setupPLC();

        }
        
        private async void keyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                //transmit to function
                //testSystem.testAxis.DtiPosition = keyboardInput;
                await testSystem.testAxis.setDtiPosition(keyboardInput);
                Console.WriteLine("The input was " + keyboardInput);
                keyboardInput = string.Empty;
                
            }
            else
            {
                if(e.Key== Key.D0 || e.Key == Key.D1 || e.Key == Key.D2 || e.Key == Key.D3 || e.Key == Key.D4 || e.Key == Key.D5 || e.Key == Key.D6 || e.Key == Key.D7 || e.Key == Key.D8 || e.Key == Key.D9)
                {
                    keyboardInput = keyboardInput + (e.Key.ToString())[1];
                }
                else if (e.Key ==Key.OemPeriod)
                {
                    keyboardInput = keyboardInput + ".";
                }
                else
                {
                    keyboardInput = keyboardInput + e.Key.ToString();
                }
            }
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

        private async void stopPosRead_Click(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine(testSystem.testAxis.DtiPosition);
            //testSystem.testAxis.getDtiReadback();
            //stopPosRead.IsEnabled = false;
            mainWindowGrid.IsEnabled = false;
            keyboardInput = string.Empty;
            Console.WriteLine(await testSystem.testAxis.getDtiPositionValue());
            //stopPosRead.IsEnabled = true;
            mainWindowGrid.IsEnabled = true;
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
