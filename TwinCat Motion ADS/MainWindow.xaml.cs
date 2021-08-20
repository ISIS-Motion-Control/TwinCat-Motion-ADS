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
using Ookii.Dialogs.Wpf;


namespace TwinCat_Motion_ADS
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //TestClass testSystem = TestClass.Instance;
        String keyboardInput = string.Empty;
        
        PLC Plc = new PLC("5.65.74.200.1.1", 852);
        Axis testAxis;
        string selectedFolder = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(Window),Keyboard.KeyUpEvent, new KeyEventHandler(keyUp), true);
            ConsoleAllocator.ShowConsoleWindow();
            Plc.setupPLC();
            if(Plc.AdsState == AdsState.Invalid)
            {
                Console.WriteLine("Ads state is invalid");
                elementsEnabled(false);
            }
            testAxis = new Axis(1, Plc);

        }

        private async void keyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                await testAxis.setDtiPosition(keyboardInput);
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
            axisPositionBind.Source = testAxis;
            axisPositionBind.Path = new PropertyPath("AxisPosition");
            axisPositionBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            axisPositionBind.StringFormat= "F3";
            BindingOperations.SetBinding(axisPositionRB, TextBlock.TextProperty, axisPositionBind);

            Binding axisEnabledBind = new Binding();
            axisEnabledBind.Mode = BindingMode.OneWay;
            axisEnabledBind.Source = testAxis;
            axisEnabledBind.Path = new PropertyPath("AxisEnabled");
            axisEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(enabledCheck, CheckBox.IsCheckedProperty, axisEnabledBind);

            Binding axisFwEnabledBind = new Binding();
            axisFwEnabledBind.Mode = BindingMode.OneWay;
            axisFwEnabledBind.Source = testAxis;
            axisFwEnabledBind.Path = new PropertyPath("AxisFwEnabled");
            axisFwEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(fwEnabledCheck, CheckBox.IsCheckedProperty, axisFwEnabledBind);

            Binding axisBwEnabledBind = new Binding();
            axisBwEnabledBind.Mode = BindingMode.OneWay;
            axisBwEnabledBind.Source = testAxis;
            axisBwEnabledBind.Path = new PropertyPath("AxisBwEnabled");
            axisBwEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(bwEnabledCheck, CheckBox.IsCheckedProperty, axisBwEnabledBind);

            Binding testCancelledBind = new Binding();
            testCancelledBind.Mode = BindingMode.OneWay;
            testCancelledBind.Source = testAxis;
            testCancelledBind.Path = new PropertyPath("CancelTest");
            testCancelledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testCancelledCheck, CheckBox.IsCheckedProperty, testCancelledBind);

            Binding testPausedBind = new Binding();
            testPausedBind.Mode = BindingMode.OneWay;
            testPausedBind.Source = testAxis;
            testPausedBind.Path = new PropertyPath("PauseTest");
            testPausedBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testPausedCheck, CheckBox.IsCheckedProperty, testPausedBind);

            Binding errorBind = new Binding();
            errorBind.Mode = BindingMode.OneWay;
            errorBind.Source = testAxis;
            errorBind.Path = new PropertyPath("Error");
            errorBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(errorCheck, CheckBox.IsCheckedProperty, errorBind);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testAxis.moveAbsoluteAndWait(posCommanded, Convert.ToDouble(velocityTB.Text));
        }

        private void initAxis_Click(object sender, RoutedEventArgs e)
        {
            if(testAxis ==null)
            {
                testAxis = new Axis(Convert.ToUInt32(axisSelection.Text), Plc);
            }
            else
            {
                testAxis.updateInstance(Convert.ToUInt32(axisSelection.Text));
            }         
            setupBinds();         
        }

        private async void moveRelButton_Click(object sender, RoutedEventArgs e)
        {
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testAxis.moveRelativeAndWait(posCommanded, Convert.ToDouble(velocityTB.Text));
        }

        private async void moveVelButton_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.moveVelocity(Convert.ToDouble(velocityTB.Text));
        }

        private async void stopPosRead_Click(object sender, RoutedEventArgs e)
        {

            mainWindowGrid.IsEnabled = false;
            keyboardInput = string.Empty;
            Console.WriteLine(await testAxis.getDtiPositionValue());
            mainWindowGrid.IsEnabled = true;
        }

        private async void stopMove_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.moveStop();
        }

        private async void move2High_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.moveToHighLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text)); //10 second timeout
        }

        private async void move2Low_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.moveToLowLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text)); //10 second timeout
        }

        private async void end2endTest_Click(object sender, RoutedEventArgs e)
        {           
            keyboardInput = string.Empty;
            elementsEnabled(false);
            cancelTest.IsEnabled = true;
            pauseTest.IsEnabled = true;
            await testAxis.end2endCycleTesting(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), 1, Convert.ToInt32(cycleTB.Text));
            elementsEnabled(true);
            
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void cancelTest_Click(object sender, RoutedEventArgs e)
        {
            testAxis.CancelTest = !testAxis.CancelTest;
        }

        private void pauseTest_Click(object sender, RoutedEventArgs e)
        {
            testAxis.PauseTest = !testAxis.PauseTest;
            if (testAxis.PauseTest)
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
            initAxis.IsEnabled = enable;
            dataOuput.IsEnabled = enable;
            positionText.IsEnabled = enable;
            moveAbsButton.IsEnabled = enable;
            moveRelButton.IsEnabled = enable;
            moveVelButton.IsEnabled = enable;
            stopMove.IsEnabled = enable;
            move2High.IsEnabled = enable;
            move2Low.IsEnabled = enable;
            end2endTest.IsEnabled = enable;
            cancelTest.IsEnabled = enable;
            pauseTest.IsEnabled = enable;
            enableButton.IsEnabled = enable;
            resetButton.IsEnabled = enable;
            cycleTB.IsEnabled = enable;
        }

        private void connect2PlcButton_Click(object sender, RoutedEventArgs e)
        {
            Plc.setupPLC();
            if (Plc.AdsState == AdsState.Invalid)
            {
                Console.WriteLine("Ads state is invalid");
                elementsEnabled(false);
            }
            else if (Plc.AdsState == AdsState.Stop)
            {
                Console.WriteLine("Device connected but PLC not running");
                elementsEnabled(true);
            }
            else if (Plc.AdsState == AdsState.Run)
            {
                Console.WriteLine("Device connected and running");
                elementsEnabled(true);
            }
        }

        private async void enableButton_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.setEnable(!testAxis.AxisEnabled);
        }

        private async void resetButton_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.Reset();
        }

        private async void highLimReversal_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.HighLimitReversal(Convert.ToDouble(velocityTB.Text),60,1,1);
            Console.WriteLine(testAxis.AxisPosition);
        }
        private async void lowLimReversal_Click(object sender, RoutedEventArgs e)
        {
            await testAxis.LowLimitReversal(Convert.ToDouble(velocityTB.Text), 60, 1, 1);
            Console.WriteLine(testAxis.AxisPosition);
        }
        private async void end2endReversal_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFolder == string.Empty)
            {
                Console.WriteLine("No save directory selected");
                return;
            }
            keyboardInput = string.Empty;
            elementsEnabled(false);
            cancelTest.IsEnabled = true;
            pauseTest.IsEnabled = true;
           if(await testAxis.end2endCycleTestingWithReversal(Convert.ToDouble(velocityTB.Text),1,60,1,10,1,1))
            {
                Console.WriteLine("Test Complete");
            }
           else
            {
                Console.WriteLine("Test did no complete");
            }
            elementsEnabled(true);
        }

        private void folderDirSelect_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new VistaFolderBrowserDialog();
            selectedFolder = String.Empty;
            if (fbd.ShowDialog()==true)
            {
                selectedFolder = fbd.SelectedPath;
            }
            Console.WriteLine(selectedFolder);
            testAxis.TestDirectory = selectedFolder;
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
