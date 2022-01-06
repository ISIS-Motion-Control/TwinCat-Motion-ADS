using Ookii.Dialogs.Wpf;
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
        readonly MainWindow windowData;
        public NcAxis testAxis;
        public NcTestSettings NcTestSettings = new();
        public string selectedFolder = string.Empty;
        
        public NcAxisView()
        {
            InitializeComponent();
            windowData = (MainWindow)Application.Current.MainWindow;
            SetupBinds();
        }


        private void InitialiseAxis_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                testAxis = new NcAxis(Convert.ToUInt32(axisSelection.Text), windowData.Plc);
            }
            else
            {
                testAxis.UpdateAxisInstance(Convert.ToUInt32(axisSelection.Text), windowData.Plc);
            }
            SetupBinds();
        }
        
        public void SetupBinds()
        {
            //Binds all the UI elemets to properties in the NC Axis
            XamlUI.TextBlockBinding(axisPositionRB, testAxis, "AxisPosition");
            XamlUI.CheckBoxBinding((string)enabledCheck.Content, enabledCheck, testAxis, "AxisEnabled", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)fwEnabledCheck.Content, fwEnabledCheck, testAxis, "AxisFwEnabled", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)bwEnabledCheck.Content, bwEnabledCheck, testAxis, "AxisBwEnabled", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)errorCheck.Content, errorCheck, testAxis, "Error", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)validAxis.Content, validAxis, testAxis, "Valid", BindingMode.OneWay);
            XamlUI.TextBlockBinding(currentAxis, testAxis, "AxisID","D");
            XamlUI.CheckBoxBinding((string)testCancelledCheck.Content, testCancelledCheck, testAxis, "CancelTest", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)testPausedCheck.Content, testPausedCheck, testAxis, "PauseTest", BindingMode.OneWay);
            XamlUI.TextboxBinding(timeoutTB, NcTestSettings.Timeout, "UiVal", UpdateSourceTrigger.LostFocus);

            XamlUI.TextboxBinding(testTitleTB, NcTestSettings.TestTitle, "UiVal", UpdateSourceTrigger.LostFocus);

            XamlUI.TextboxBinding(velocityTB, NcTestSettings.Velocity, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(cycleTB, NcTestSettings, "StrCycles", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(cycleDelayTB, NcTestSettings, "StrCycleDelaySeconds", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(revVelTB, NcTestSettings, "StrReversalVelocity", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(revExtraTB, NcTestSettings, "StrReversalExtraTimeSeconds", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(revSettleTB, NcTestSettings, "StrReversalSettleTimeSeconds", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(initSetpointTB, NcTestSettings, "StrInitialSetpoint", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(NumberOfStepsTB, NcTestSettings, "StrNumberOfSteps", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(stepSizeTB, NcTestSettings, "StrStepSize", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(settleTimeTB, NcTestSettings, "StrSettleTimeSeconds", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(revDistanceTB, NcTestSettings, "StrReversalDistance", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(overshootDistanceTB, NcTestSettings.OvershootDistance, "UiVal", UpdateSourceTrigger.LostFocus);
        }

        private void SelectFolderDirectory_Click(object sender, RoutedEventArgs e)
        {
            if(testAxis == null)
            {
                Console.WriteLine("Initialise an axis first");
                return;
            }
            var fbd = new VistaFolderBrowserDialog();
            selectedFolder = String.Empty;
            if (fbd.ShowDialog() == true)
            {
                selectedFolder = fbd.SelectedPath;
            }
            Console.WriteLine(selectedFolder);
            testAxis.TestDirectory = selectedFolder;
        }

        //Generic method for handling commands to the axis
        private async void AxisCommand_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }

            //Enable or Disable axis
            if(sender as Button == enableButton)
            {
                await testAxis.SetEnable(!testAxis.AxisEnabled);
            }
            //Reset axis
            else if(sender as Button == resetButton)
            {
                await testAxis.Reset();
            }
            else if(sender as Button == moveAbsButton)
            {
                double posCommanded = Convert.ToDouble(positionText.Text);
                await testAxis.MoveAbsoluteAndWait(posCommanded, Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
            }
            //Move relative
            else if(sender as Button == moveRelButton)
            {
                double posCommanded = Convert.ToDouble(positionText.Text);
                await testAxis.MoveRelativeAndWait(posCommanded, Convert.ToDouble(velocityTB.Text));
            }
            //Move velocity
            else if(sender as Button == moveVelButton)
            {
                await testAxis.MoveVelocity(Convert.ToDouble(velocityTB.Text));
            }
            //Move to forward limit
            else if(sender as Button == move2High)
            {
                await testAxis.MoveToHighLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
            }
            //Move to backward limit
            else if(sender as Button == move2Low)
            {
                await testAxis.MoveToLowLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
            }
            //Stop axis
            else if(sender as Button == stopMove)
            {
                await testAxis.MoveStop();
            }
            //Forward limit reversal
            else if(sender as Button == highLimReversal)
            {
                await testAxis.HighLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtraTB.Text), Convert.ToInt32(revSettleTB.Text));
                Console.WriteLine(testAxis.AxisPosition);
            }
            //Backward limit reversal
            else if(sender as Button == lowLimReversal)
            {
                await testAxis.LowLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtraTB.Text), Convert.ToInt32(revSettleTB.Text));
                Console.WriteLine(testAxis.AxisPosition);
            }
        }

        private async void LimitToLimitTest_Click(object sender, RoutedEventArgs e)
        {
            windowData.mainWindowGrid.Focus();
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            if (selectedFolder == string.Empty)
            {
                Console.WriteLine("No save directory selected");
                return;
            }
            cancelTest.IsEnabled = true;
            pauseTest.IsEnabled = true;
            if (await testAxis.LimitToLimitTestwithReversingSequence(NcTestSettings, windowData.MeasurementDevices))
            {
                Console.WriteLine("Test Complete");
            }
            else
            {
                Console.WriteLine("Test did not complete");
            }

        }

        private async void UniDirecitonalTest_Click(object sender, RoutedEventArgs e)
        {
            windowData.mainWindowGrid.Focus();
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            if (selectedFolder == string.Empty)
            {
                Console.WriteLine("No save directory selected");
                return;
            }
            cancelTest.IsEnabled = true;
            pauseTest.IsEnabled = true;

            if (await testAxis.UniDirectionalAccuracyTest(NcTestSettings, windowData.MeasurementDevices))
            {}
            else
            {
                Console.WriteLine("Test did not complete");
            }
        }

        private async void BiDirecitonalTest_Click(object sender, RoutedEventArgs e)
        {
            windowData.mainWindowGrid.Focus();
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            if (selectedFolder == string.Empty)
            {
                Console.WriteLine("No save directory selected");
                return;
            }
            cancelTest.IsEnabled = true;
            pauseTest.IsEnabled = true;

            if (await testAxis.BiDirectionalAccuracyTest(NcTestSettings, windowData.MeasurementDevices))
            {          }
            else
            {
                Console.WriteLine("Test did not complete");
            }
        }

        private void CancelTest_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            testAxis.CancelTest = !testAxis.CancelTest;
        }

        private void PauseTest_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            testAxis.PauseTest = !testAxis.PauseTest;
        }

        

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fbd = new();
            fbd.Filter = "*.settingsfile|*.*";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                NcTestSettings.ImportSettings(selectedFile);
            }
            Console.WriteLine(selectedFolder);
            
        }

        private void ncWindowGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ncWindowGrid.Focus();
        }
    }
}
