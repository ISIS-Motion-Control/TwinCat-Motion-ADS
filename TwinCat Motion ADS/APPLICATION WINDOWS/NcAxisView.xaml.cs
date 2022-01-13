using Ookii.Dialogs.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;



namespace TwinCat_Motion_ADS
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
            //testAxis = new(1, windowData.Plc);
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

            XamlUI.TextboxBinding(SettingVelocity.SettingValue, NcTestSettings.Velocity, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.UserSettingBinding(SettingTestTitle, testAxis, NcTestSettings.TestTitle);
            XamlUI.UserSettingBinding(SettingTimeout, testAxis, NcTestSettings.Timeout);
            XamlUI.UserSettingBinding(SettingCycles, testAxis, NcTestSettings.Cycles);
            XamlUI.UserSettingBinding(SettingCycleDelay, testAxis, NcTestSettings.CycleDelaySeconds);
            XamlUI.UserSettingBinding(SettingReversalVelocity, testAxis, NcTestSettings.ReversalVelocity);
            XamlUI.UserSettingBinding(SettingReversalExtra, testAxis, NcTestSettings.ReversalExtraTimeSeconds);
            XamlUI.UserSettingBinding(SettingReversalSettle, testAxis, NcTestSettings.ReversalSettleTimeSeconds);
            XamlUI.UserSettingBinding(SettingInitialSetpoint, testAxis, NcTestSettings.InitialSetpoint);
            XamlUI.UserSettingBinding(SettingNumberOfSteps, testAxis, NcTestSettings.NumberOfSteps);
            XamlUI.UserSettingBinding(SettingStepSize, testAxis, NcTestSettings.StepSize);
            XamlUI.UserSettingBinding(SettingSettleTime, testAxis, NcTestSettings.SettleTimeSeconds);
            XamlUI.UserSettingBinding(SettingReversal, testAxis, NcTestSettings.ReversalDistance);
            XamlUI.UserSettingBinding(SettingOvershoot, testAxis, NcTestSettings.OvershootDistance);
            XamlUI.UserSettingBinding(SettingEndSepoint, testAxis, NcTestSettings.EndSetpoint);

            XamlUI.ProgressBarBinding(testProgressBar, testAxis, "TestProgress");
            if(testAxis!=null)
            {
                XamlUI.TextBlockBinding(EstimateTime, testAxis.EstimatedTimeRemaining, "TimeRemaining");
                XamlUI.TextBlockBinding(EstimateEndTime, testAxis.EstimatedTimeRemaining, "StrEndTime");

            }
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
                await testAxis.MoveAbsoluteAndWait(posCommanded, NcTestSettings.Velocity.Val, (int)NcTestSettings.Timeout.Val);
            }
            //Move relative
            else if(sender as Button == moveRelButton)
            {
                double posCommanded = Convert.ToDouble(positionText.Text);
                await testAxis.MoveRelativeAndWait(posCommanded, NcTestSettings.Velocity.Val);
            }
            //Move velocity
            else if(sender as Button == moveVelButton)
            {
                await testAxis.MoveVelocity(NcTestSettings.Velocity.Val);
            }
            //Move to forward limit
            else if(sender as Button == move2High)
            {
                await testAxis.MoveToHighLimit(NcTestSettings.Velocity.Val, (int)NcTestSettings.Timeout.Val);
            }
            //Move to backward limit
            else if(sender as Button == move2Low)
            {
                await testAxis.MoveToLowLimit(NcTestSettings.Velocity.Val, (int)NcTestSettings.Timeout.Val);
            }
            //Stop axis
            else if(sender as Button == stopMove)
            {
                await testAxis.MoveStop();
            }
            //Forward limit reversal
            else if(sender as Button == highLimReversal)
            {
                await testAxis.HighLimitReversal(NcTestSettings.Velocity.Val, (int)NcTestSettings.Timeout.Val, (int)NcTestSettings.ReversalExtraTimeSeconds.Val, (int)NcTestSettings.ReversalSettleTimeSeconds.Val);
                Console.WriteLine(testAxis.AxisPosition);
            }
            //Backward limit reversal
            else if(sender as Button == lowLimReversal)
            {
                await testAxis.LowLimitReversal(NcTestSettings.Velocity.Val, (int)NcTestSettings.Timeout.Val, (int)NcTestSettings.ReversalExtraTimeSeconds.Val, (int)NcTestSettings.ReversalSettleTimeSeconds.Val);
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
            if (await testAxis.UniDirectionalAccuracyTest(NcTestSettings,windowData.MeasurementDevices))
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

        private async void ScalingTestButton_Click(object sender, RoutedEventArgs e)
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

            if (await testAxis.ScalingTest(NcTestSettings, windowData.MeasurementDevices))
            { }
            else
            {
                Console.WriteLine("Test did not complete");
            }
        }

        private async void BacklashTestButton_Click(object sender, RoutedEventArgs e)
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

            if (await testAxis.BacklashDetectionTest(NcTestSettings, windowData.MeasurementDevices))
            { }
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

        

        private void LoadSettingsFile_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fbd = new();
            fbd.Filter = "*.xml|*.XML*";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                NcTestSettings.ImportSettingsXML(selectedFile);
            }
            Console.WriteLine(selectedFolder);
            
        }

        private void ncWindowGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ncWindowGrid.Focus();
        }

        
    }
}
