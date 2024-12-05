using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            SetupBinds();
            //Cannot use our usual list of enums for item source as it cannot be modified
            //Instead create a temporary holder for the list and create a new list from this with the UserPrompt type removed
            //TestSelectionComboBox.ItemsSource = Enum.GetValues(typeof(TestTypes)).Cast<TestTypes>();
            var tmpArray = Enum.GetValues(typeof(TestTypes)).Cast<TestTypes>();
            List<TestTypes> cbSourceList = new();
            foreach(var tmp in tmpArray)
            {
                cbSourceList.Add(tmp);
            }
            cbSourceList.Remove(TestTypes.UserPrompt);
            
            //Set source for combobox items
            TestSelectionComboBox.ItemsSource = cbSourceList;
            TestSelectionComboBox.SelectedItem = NcTestSettings.TestType.Val;
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
            XamlUI.TextBlockBinding(positionReadback.SettingValue, testAxis, "AxisPosition");
            XamlUI.CheckBoxBinding((string)enabledCheck.Content, enabledCheck, testAxis, "AxisEnabled", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)fwEnabledCheck.Content, fwEnabledCheck, testAxis, "AxisFwEnabled", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)bwEnabledCheck.Content, bwEnabledCheck, testAxis, "AxisBwEnabled", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)errorCheck.Content, errorCheck, testAxis, "Error", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)validAxis.Content, validAxis, testAxis, "Valid", BindingMode.OneWay);
            XamlUI.TextBlockBinding(currentAxisReadback.SettingValue, testAxis, "AxisID","D");
            XamlUI.CheckBoxBinding((string)testCancelledCheck.Content, testCancelledCheck, testAxis, "CancelTest", BindingMode.OneWay);
            XamlUI.CheckBoxBinding((string)testPausedCheck.Content, testPausedCheck, testAxis, "PauseTest", BindingMode.OneWay);
            
            

            //XamlUI.TextboxBinding(testTitleTB, NcTestSettings.TestTitle, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(SettingTitle.SettingValue, NcTestSettings.TestTitle, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(SettingTimeout.SettingValue, NcTestSettings.Timeout, "UiVal", UpdateSourceTrigger.LostFocus);                      
            XamlUI.TextboxBinding(SettingVelocity.SettingValue, NcTestSettings.Velocity, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingCycles.SettingValue, NcTestSettings.Cycles, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingCycleDelay.SettingValue, NcTestSettings.CycleDelaySeconds, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingReversalVelocity.SettingValue, NcTestSettings.ReversalVelocity, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingReversalExtraSeconds.SettingValue, NcTestSettings.ReversalExtraTimeSeconds, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingReversalSettlingSeconds.SettingValue, NcTestSettings.ReversalSettleTimeSeconds, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingInitialSetpoint.SettingValue, NcTestSettings.InitialSetpoint, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingAccuracySteps.SettingValue, NcTestSettings.NumberOfSteps, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingStepSize.SettingValue, NcTestSettings.StepSize, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingSettlingTime.SettingValue, NcTestSettings.SettleTimeSeconds, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingReversalDistance.SettingValue, NcTestSettings.ReversalDistance, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingOvershootDistance.SettingValue, NcTestSettings.OvershootDistance, "UiVal", UpdateSourceTrigger.LostFocus);
            
            XamlUI.TextboxBinding(SettingEndSetpoint.SettingValue, NcTestSettings.EndSetpoint, "UiVal", UpdateSourceTrigger.LostFocus);

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
                double posCommanded = Convert.ToDouble(windowSetPoint.SettingValue.Text);
                await testAxis.MoveAbsoluteAndWait(posCommanded, Convert.ToDouble(windowVelocity.SettingValue.Text), Convert.ToInt32(SettingTimeout.SettingValue.Text));
            }
            //Move relative
            else if(sender as Button == moveRelButton)
            {
                double posCommanded = Convert.ToDouble(windowSetPoint.SettingValue.Text);
                await testAxis.MoveRelativeAndWait(posCommanded, Convert.ToDouble(windowVelocity.SettingValue.Text));
            }
            //Move velocity
            else if(sender as Button == moveVelButton)
            {
                await testAxis.MoveVelocity(Convert.ToDouble(windowVelocity.SettingValue.Text));
            }
            //Move to forward limit
            else if(sender as Button == move2High)
            {
                await testAxis.MoveToHighLimit(Convert.ToDouble(windowVelocity.SettingValue.Text), Convert.ToInt32(SettingTimeout.SettingValue.Text));
            }
            //Move to backward limit
            else if(sender as Button == move2Low)
            {
                await testAxis.MoveToLowLimit(Convert.ToDouble(windowVelocity.SettingValue.Text), Convert.ToInt32(SettingTimeout.SettingValue.Text));
            }
            //Stop axis
            else if(sender as Button == stopMove)
            {
                await testAxis.MoveStop();
            }
            //Forward limit reversal
            else if(sender as Button == highLimReversal)
            {
                await testAxis.HighLimitReversal(Convert.ToDouble(windowVelocity.SettingValue.Text), Convert.ToInt32(SettingTimeout.SettingValue.Text), Convert.ToInt32(SettingReversalExtraSeconds.SettingValue.Text), Convert.ToInt32(SettingReversalSettlingSeconds.SettingValue.Text));
                Console.WriteLine(testAxis.AxisPosition);
            }
            //Backward limit reversal
            else if(sender as Button == lowLimReversal)
            {
                await testAxis.LowLimitReversal(Convert.ToDouble(windowVelocity.SettingValue.Text), Convert.ToInt32(SettingTimeout.SettingValue.Text), Convert.ToInt32(SettingReversalExtraSeconds.SettingValue.Text), Convert.ToInt32(SettingReversalSettlingSeconds.SettingValue.Text));
                Console.WriteLine(testAxis.AxisPosition);
            }
            else if(sender as Button == homeButton)
            {
                await testAxis.HomeAxisAndWait();
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

        private async void RunSelectedTestButton_Click(object sender, RoutedEventArgs e)
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
            SetEnableOnUiElements(true);
            switch(TestSelectionComboBox.SelectedItem)
            {
                case TestTypes.EndToEnd:
                    if (await testAxis.LimitToLimitTestwithReversingSequence(NcTestSettings, windowData.MeasurementDevices))
                    {
                        Console.WriteLine("Test Complete");
                    }
                    else
                    {
                        Console.WriteLine("Test did not complete");
                    }
                    break;
                case TestTypes.UnidirectionalAccuracy:
                    if (await testAxis.UniDirectionalAccuracyTest(NcTestSettings, windowData.MeasurementDevices))
                    { }
                    else
                    {
                        Console.WriteLine("Test did not complete");
                    }
                    break;
                case TestTypes.BidirectionalAccuracy:
                    if (await testAxis.BiDirectionalAccuracyTest(NcTestSettings, windowData.MeasurementDevices))
                    { }
                    else
                    {
                        Console.WriteLine("Test did not complete");
                    }
                    break;
                case TestTypes.ScalingTest:
                    if (await testAxis.ScalingTest(NcTestSettings, windowData.MeasurementDevices))
                    { }
                    else
                    {
                        Console.WriteLine("Test did not complete");
                    }
                    break;
                case TestTypes.BacklashDetection:
                    if (await testAxis.BacklashDetectionTest(NcTestSettings, windowData.MeasurementDevices))
                    { }
                    else
                    {
                        Console.WriteLine("Test did not complete");
                    }
                    break;
                default:
                    break;
            }
            SetEnableOnUiElements();
        }

        private void TestSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NcTestSettings.TestType.UiVal = ((TestTypes)TestSelectionComboBox.SelectedItem).GetStringValue();
            SetEnableOnUiElements();
        }

        private void SetEnableOnUiElements(bool lockout = false)
        {
            if (lockout)
            {
                SettingTitle.IsEnabled = false;
                SettingTimeout.IsEnabled = false;
                SettingVelocity.IsEnabled = false;
                SettingCycles.IsEnabled = false;
                SettingCycleDelay.IsEnabled = false;
                SettingReversalVelocity.IsEnabled = false;
                SettingReversalExtraSeconds.IsEnabled = false;
                SettingReversalSettlingSeconds.IsEnabled = false;
                SettingInitialSetpoint.IsEnabled = false;
                SettingEndSetpoint.IsEnabled = false;
                SettingAccuracySteps.IsEnabled = false;
                SettingStepSize.IsEnabled = false;
                SettingSettlingTime.IsEnabled = false;
                SettingReversalDistance.IsEnabled = false;
                SettingOvershootDistance.IsEnabled = false;
                return;
            }

            switch (NcTestSettings.TestType.Val)
            {
                case TestTypes.EndToEnd:
                    SettingTitle.IsEnabled = true;
                    SettingCycles.IsEnabled = true;
                    SettingCycleDelay.IsEnabled = true;
                    SettingVelocity.IsEnabled = true;
                    SettingTimeout.IsEnabled = true;                                        
                    SettingReversalVelocity.IsEnabled = true;
                    SettingReversalExtraSeconds.IsEnabled = true;
                    SettingReversalSettlingSeconds.IsEnabled = true;
                    SettingInitialSetpoint.IsEnabled = false;
                    SettingEndSetpoint.IsEnabled = false;
                    SettingAccuracySteps.IsEnabled = false;
                    SettingStepSize.IsEnabled = false;
                    SettingSettlingTime.IsEnabled = false;
                    SettingReversalDistance.IsEnabled = false;
                    SettingOvershootDistance.IsEnabled = false;
                    break;
                case TestTypes.UnidirectionalAccuracy:
                    SettingTitle.IsEnabled = true;
                    SettingCycles.IsEnabled = true;
                    SettingCycleDelay.IsEnabled = true;
                    SettingVelocity.IsEnabled = true;
                    SettingTimeout.IsEnabled = true;
                    SettingReversalVelocity.IsEnabled = false;
                    SettingReversalExtraSeconds.IsEnabled = false;
                    SettingReversalSettlingSeconds.IsEnabled = false;
                    SettingInitialSetpoint.IsEnabled = true;
                    SettingEndSetpoint.IsEnabled = false;
                    SettingAccuracySteps.IsEnabled = true;
                    SettingStepSize.IsEnabled = true;
                    SettingSettlingTime.IsEnabled = true;
                    SettingReversalDistance.IsEnabled = true;
                    SettingOvershootDistance.IsEnabled = false;
                    break;
                case TestTypes.BidirectionalAccuracy:
                    SettingTitle.IsEnabled = true;
                    SettingCycles.IsEnabled = true;
                    SettingCycleDelay.IsEnabled = true;
                    SettingVelocity.IsEnabled = true;
                    SettingTimeout.IsEnabled = true;
                    SettingReversalVelocity.IsEnabled = false;
                    SettingReversalExtraSeconds.IsEnabled = false;
                    SettingReversalSettlingSeconds.IsEnabled = false;
                    SettingInitialSetpoint.IsEnabled = true;
                    SettingEndSetpoint.IsEnabled = false;
                    SettingAccuracySteps.IsEnabled = true;
                    SettingStepSize.IsEnabled = true;
                    SettingSettlingTime.IsEnabled = true;
                    SettingReversalDistance.IsEnabled = true;
                    SettingOvershootDistance.IsEnabled = true;
                    break;
                case TestTypes.ScalingTest:
                    SettingTitle.IsEnabled = true;
                    SettingCycles.IsEnabled = true;
                    SettingCycleDelay.IsEnabled = true;
                    SettingVelocity.IsEnabled = true;
                    SettingTimeout.IsEnabled = true;
                    SettingReversalVelocity.IsEnabled = false;
                    SettingReversalExtraSeconds.IsEnabled = false;
                    SettingReversalSettlingSeconds.IsEnabled = false;
                    SettingInitialSetpoint.IsEnabled = true;
                    SettingEndSetpoint.IsEnabled = true;
                    SettingAccuracySteps.IsEnabled = true;
                    SettingStepSize.IsEnabled = true;
                    SettingSettlingTime.IsEnabled = true;
                    SettingReversalDistance.IsEnabled = true;
                    SettingOvershootDistance.IsEnabled = false;
                    break;
                case TestTypes.BacklashDetection:
                    SettingTitle.IsEnabled = true;
                    SettingCycles.IsEnabled = true;
                    SettingCycleDelay.IsEnabled = true;
                    SettingVelocity.IsEnabled = true;
                    SettingTimeout.IsEnabled = true;
                    SettingReversalVelocity.IsEnabled = false;
                    SettingReversalExtraSeconds.IsEnabled = false;
                    SettingReversalSettlingSeconds.IsEnabled = false;
                    SettingInitialSetpoint.IsEnabled = true;
                    SettingEndSetpoint.IsEnabled = false;
                    SettingAccuracySteps.IsEnabled = true;
                    SettingStepSize.IsEnabled = true;
                    SettingSettlingTime.IsEnabled = true;
                    SettingReversalDistance.IsEnabled = true;
                    SettingOvershootDistance.IsEnabled = false;
                    break;
                case TestTypes.NoneSelected:
                    SettingTitle.IsEnabled = true;
                    SettingCycles.IsEnabled = true;
                    SettingCycleDelay.IsEnabled = true;
                    SettingVelocity.IsEnabled = true;
                    SettingTimeout.IsEnabled = true;
                    SettingReversalVelocity.IsEnabled = true;
                    SettingReversalExtraSeconds.IsEnabled = true;
                    SettingReversalSettlingSeconds.IsEnabled = true;
                    SettingInitialSetpoint.IsEnabled = true;
                    SettingEndSetpoint.IsEnabled = true;
                    SettingAccuracySteps.IsEnabled = true;
                    SettingStepSize.IsEnabled = true;
                    SettingSettlingTime.IsEnabled = true;
                    SettingReversalDistance.IsEnabled = true;
                    SettingOvershootDistance.IsEnabled = true;
                    break;
            }
        }
    }
}
