using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
                await testSystem.testAxis.setDtiPosition(keyboardInput);
                //Console.WriteLine("The input was " + keyboardInput);
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

            Binding axisEnabledBind = new Binding();
            axisEnabledBind.Mode = BindingMode.OneWay;
            axisEnabledBind.Source = testSystem.testAxis;
            axisEnabledBind.Path = new PropertyPath("AxisEnabled");
            axisEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(enabledCheck, CheckBox.IsCheckedProperty, axisEnabledBind);

            Binding axisFwEnabledBind = new Binding();
            axisFwEnabledBind.Mode = BindingMode.OneWay;
            axisFwEnabledBind.Source = testSystem.testAxis;
            axisFwEnabledBind.Path = new PropertyPath("AxisFwEnabled");
            axisFwEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(fwEnabledCheck, CheckBox.IsCheckedProperty, axisFwEnabledBind);

            Binding axisBwEnabledBind = new Binding();
            axisBwEnabledBind.Mode = BindingMode.OneWay;
            axisBwEnabledBind.Source = testSystem.testAxis;
            axisBwEnabledBind.Path = new PropertyPath("AxisBwEnabled");
            axisBwEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(bwEnabledCheck, CheckBox.IsCheckedProperty, axisBwEnabledBind);

            Binding testCancelledBind = new Binding();
            testCancelledBind.Mode = BindingMode.OneWay;
            testCancelledBind.Source = testSystem.testAxis;
            testCancelledBind.Path = new PropertyPath("CancelTest");
            testCancelledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testCancelledCheck, CheckBox.IsCheckedProperty, testCancelledBind);

            Binding testPausedBind = new Binding();
            testPausedBind.Mode = BindingMode.OneWay;
            testPausedBind.Source = testSystem.testAxis;
            testPausedBind.Path = new PropertyPath("PauseTest");
            testPausedBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testPausedCheck, CheckBox.IsCheckedProperty, testPausedBind);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testSystem.testAxis.moveAbsoluteAndWait(posCommanded, Convert.ToDouble(velocityTB.Text));
        }

        private void initAxis_Click(object sender, RoutedEventArgs e)
        {
            testSystem.axisInstance(Convert.ToUInt32(axisSelection.Text));
            setupBinds();
            Console.WriteLine(testSystem.testAxis.AxisEnabled);
            
        }

        private async void moveRelButton_Click(object sender, RoutedEventArgs e)
        {
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testSystem.testAxis.moveRelativeAndWait(posCommanded, Convert.ToDouble(velocityTB.Text));
        }

        private async void moveVelButton_Click(object sender, RoutedEventArgs e)
        {
            await testSystem.testAxis.moveVelocity(Convert.ToDouble(velocityTB.Text));
        }

        private async void stopPosRead_Click(object sender, RoutedEventArgs e)
        {

            mainWindowGrid.IsEnabled = false;
            keyboardInput = string.Empty;
            Console.WriteLine(await testSystem.testAxis.getDtiPositionValue());
            mainWindowGrid.IsEnabled = true;
        }

        private async void stopMove_Click(object sender, RoutedEventArgs e)
        {
            await testSystem.testAxis.moveStop();
        }

        private async void move2High_Click(object sender, RoutedEventArgs e)
        {
            await testSystem.testAxis.moveToHighLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text)); //10 second timeout
        }

        private async void move2Low_Click(object sender, RoutedEventArgs e)
        {
            await testSystem.testAxis.moveToLowLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text)); //10 second timeout
        }

        private async void end2endTest_Click(object sender, RoutedEventArgs e)
        {           
            keyboardInput = string.Empty;
            elementsEnabled(false);
            await testSystem.testAxis.end2endCycleTesting(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), 1, 100);
            elementsEnabled(true);
            
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void cancelTest_Click(object sender, RoutedEventArgs e)
        {
            testSystem.testAxis.CancelTest = !testSystem.testAxis.CancelTest;
        }

        private void pauseTest_Click(object sender, RoutedEventArgs e)
        {
            testSystem.testAxis.PauseTest = !testSystem.testAxis.PauseTest;
            if (testSystem.testAxis.PauseTest)
            {
                pauseTest.Background = Brushes.Red;
            }
            else
            {
                pauseTest.Background = Brushes.Green;
            }
        }

        //WIP method to disable UI elements - need one to enable
        private void elementsEnabled(bool enable)
        {
            timeoutTB.IsEnabled = enable;
            velocityTB.IsEnabled = enable;
            dataOuput.IsEnabled = enable;
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
