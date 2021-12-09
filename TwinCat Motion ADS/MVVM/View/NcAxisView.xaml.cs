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
            Binding axisPositionBind = new();
            axisPositionBind.Mode = BindingMode.TwoWay;
            axisPositionBind.Source = testAxis;
            axisPositionBind.Path = new PropertyPath("AxisPosition");
            axisPositionBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            axisPositionBind.StringFormat = "F3";
            BindingOperations.SetBinding(axisPositionRB, TextBlock.TextProperty, axisPositionBind);
            
            Binding axisEnabledBind = new();
            axisEnabledBind.Mode = BindingMode.OneWay;
            axisEnabledBind.Source = testAxis;
            axisEnabledBind.Path = new PropertyPath("AxisEnabled");
            axisEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(enabledCheck, CheckBox.IsCheckedProperty, axisEnabledBind);

            Binding axisFwEnabledBind = new();
            axisFwEnabledBind.Mode = BindingMode.OneWay;
            axisFwEnabledBind.Source = testAxis;
            axisFwEnabledBind.Path = new PropertyPath("AxisFwEnabled");
            axisFwEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(fwEnabledCheck, CheckBox.IsCheckedProperty, axisFwEnabledBind);

            Binding axisBwEnabledBind = new();
            axisBwEnabledBind.Mode = BindingMode.OneWay;
            axisBwEnabledBind.Source = testAxis;
            axisBwEnabledBind.Path = new PropertyPath("AxisBwEnabled");
            axisBwEnabledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(bwEnabledCheck, CheckBox.IsCheckedProperty, axisBwEnabledBind);

            Binding errorBind = new();
            errorBind.Mode = BindingMode.OneWay;
            errorBind.Source = testAxis;
            errorBind.Path = new PropertyPath("Error");
            errorBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(errorCheck, CheckBox.IsCheckedProperty, errorBind);

            Binding validBind = new();
            validBind.Mode = BindingMode.OneWay;
            validBind.Source = testAxis;
            validBind.Path = new PropertyPath("Valid");
            validBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(validAxis, CheckBox.IsCheckedProperty, validBind);

            Binding axisNumBind = new();
            axisNumBind.Mode = BindingMode.OneWay;
            axisNumBind.Source = testAxis;
            axisNumBind.Path = new PropertyPath("AxisID");
            axisNumBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(currentAxis, TextBlock.TextProperty, axisNumBind);

            Binding testCancelledBind = new();
            testCancelledBind.Mode = BindingMode.OneWay;
            testCancelledBind.Source = testAxis;
            testCancelledBind.Path = new PropertyPath("CancelTest");
            testCancelledBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testCancelledCheck, CheckBox.IsCheckedProperty, testCancelledBind);

            Binding testPausedBind = new();
            testPausedBind.Mode = BindingMode.OneWay;
            testPausedBind.Source = testAxis;
            testPausedBind.Path = new PropertyPath("PauseTest");
            testPausedBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testPausedCheck, CheckBox.IsCheckedProperty, testPausedBind);

            Binding timeoutTextboxBind = new();
            timeoutTextboxBind.Mode = BindingMode.TwoWay;
            timeoutTextboxBind.Source = NcTestSettings;
            timeoutTextboxBind.Path = new PropertyPath("StrTimeout");
            timeoutTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(timeoutTB, TextBox.TextProperty, timeoutTextboxBind);

            Binding testTitleTextboxBind = new();
            testTitleTextboxBind.Mode = BindingMode.TwoWay;
            testTitleTextboxBind.Source = NcTestSettings;
            testTitleTextboxBind.Path = new PropertyPath("StrTestTitle");
            testTitleTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testTitleTB, TextBox.TextProperty, testTitleTextboxBind);
            
            Binding velocityTextboxBind = new();
            velocityTextboxBind.Mode = BindingMode.TwoWay;
            velocityTextboxBind.Source = NcTestSettings;
            velocityTextboxBind.Path = new PropertyPath("StrVelocity");
            velocityTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(velocityTB, TextBox.TextProperty, velocityTextboxBind);

            Binding cycleTextboxBind = new();
            cycleTextboxBind.Mode = BindingMode.TwoWay;
            cycleTextboxBind.Source = NcTestSettings;
            cycleTextboxBind.Path = new PropertyPath("StrCycles");
            cycleTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(cycleTB, TextBox.TextProperty, cycleTextboxBind);

            Binding cycleDelayTextboxBind = new();
            cycleDelayTextboxBind.Mode = BindingMode.TwoWay;
            cycleDelayTextboxBind.Source = NcTestSettings;
            cycleDelayTextboxBind.Path = new PropertyPath("StrCycleDelaySeconds");
            cycleDelayTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(cycleDelayTB, TextBox.TextProperty, cycleDelayTextboxBind);

            Binding reversalVelTextboxBind = new()
            {
                Mode = BindingMode.TwoWay,
                Source = NcTestSettings,
                Path = new PropertyPath("StrReversalVelocity"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(revVelTB, TextBox.TextProperty, reversalVelTextboxBind);

            Binding reversalExtraTimeTextboxBind = new();
            reversalExtraTimeTextboxBind.Mode = BindingMode.TwoWay;
            reversalExtraTimeTextboxBind.Source = NcTestSettings;
            reversalExtraTimeTextboxBind.Path = new PropertyPath("StrReversalExtraTimeSeconds");
            reversalExtraTimeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(revExtraTB, TextBox.TextProperty, reversalExtraTimeTextboxBind);

            Binding reversalSettleTimeTextboxBind = new();
            reversalSettleTimeTextboxBind.Mode = BindingMode.TwoWay;
            reversalSettleTimeTextboxBind.Source = NcTestSettings;
            reversalSettleTimeTextboxBind.Path = new PropertyPath("StrReversalSettleTimeSeconds");
            reversalSettleTimeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(revSettleTB, TextBox.TextProperty, reversalSettleTimeTextboxBind);

            Binding initialSetpointTextboxBind = new();
            initialSetpointTextboxBind.Mode = BindingMode.TwoWay;
            initialSetpointTextboxBind.Source = NcTestSettings;
            initialSetpointTextboxBind.Path = new PropertyPath("StrInitialSetpoint");
            initialSetpointTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(initSetpointTB, TextBox.TextProperty, initialSetpointTextboxBind);

            Binding numberOfStepsTextboxBind = new();
            numberOfStepsTextboxBind.Mode = BindingMode.TwoWay;
            numberOfStepsTextboxBind.Source = NcTestSettings;
            numberOfStepsTextboxBind.Path = new PropertyPath("StrNumberOfSteps");
            numberOfStepsTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(NumberOfStepsTB, TextBox.TextProperty, numberOfStepsTextboxBind);

            Binding stepSizeTextboxBind = new();
            stepSizeTextboxBind.Mode = BindingMode.TwoWay;
            stepSizeTextboxBind.Source = NcTestSettings;
            stepSizeTextboxBind.Path = new PropertyPath("StrStepSize");
            stepSizeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(stepSizeTB, TextBox.TextProperty, stepSizeTextboxBind);

            Binding settleTimeTextboxBind = new();
            settleTimeTextboxBind.Mode = BindingMode.TwoWay;
            settleTimeTextboxBind.Source = NcTestSettings;
            settleTimeTextboxBind.Path = new PropertyPath("StrSettleTimeSeconds");
            settleTimeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(settleTimeTB, TextBox.TextProperty, settleTimeTextboxBind);

            Binding reversalDistanceTextboxBind = new();
            reversalDistanceTextboxBind.Mode = BindingMode.TwoWay;
            reversalDistanceTextboxBind.Source = NcTestSettings;
            reversalDistanceTextboxBind.Path = new PropertyPath("StrReversalDistance");
            reversalDistanceTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(revDistanceTB, TextBox.TextProperty, reversalDistanceTextboxBind);

            Binding overshootDistanceTextboxBind = new();
            overshootDistanceTextboxBind.Mode = BindingMode.TwoWay;
            overshootDistanceTextboxBind.Source = NcTestSettings;
            overshootDistanceTextboxBind.Path = new PropertyPath("StrOvershootDistance");
            overshootDistanceTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(overshootDistanceTB, TextBox.TextProperty, overshootDistanceTextboxBind);


        }

        private async void MoveAbsoluteButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testAxis.MoveAbsoluteAndWait(posCommanded, Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
        }

        private async void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis!= null)
            {
                await testAxis.SetEnable(!testAxis.AxisEnabled);
            }
            else
            {
                Console.WriteLine("No axis initialised");
            }
            
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.Reset();
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

        private async void MoveRelativeButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testAxis.MoveRelativeAndWait(posCommanded, Convert.ToDouble(velocityTB.Text));
        }

        private async void MoveVelocityButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.MoveVelocity(Convert.ToDouble(velocityTB.Text));
        }

        private async void MoveToForwardLimit_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.MoveToHighLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
        }

        private async void MoveToBackwardLimit_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.MoveToLowLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
        }


        private async void LimitToLimitTest_Click(object sender, RoutedEventArgs e)
        {
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
            //if (await testAxis.end2endCycleTestingWithReversal(NcTestSettings, windowData.MeasurementDevice1,windowData.MeasurementDevice2,windowData.MeasurementDevice3,windowData.MeasurementDevice4))
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

            //if (await testAxis.uniDirectionalAccuracyTest(NcTestSettings,windowData.MeasurementDevice1, windowData.MeasurementDevice2, windowData.MeasurementDevice3, windowData.MeasurementDevice4))
            if (await testAxis.UniDirectionalAccuracyTest(NcTestSettings, windowData.MeasurementDevices))

            {
                //
            }
            else
            {
                Console.WriteLine("Test did not complete");
            }
        }

        private async void BiDirecitonalTest_Click(object sender, RoutedEventArgs e)
        {
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

            //if (await testAxis.biDirectionalAccuracyTest(Convert.ToDouble(initSetpointTB.Text), Convert.ToDouble(velocityTB.Text), Convert.ToInt32(cycleTB.Text), Convert.ToInt32(NumberOfStepsTB.Text), Convert.ToDouble(stepSizeTB.Text), Convert.ToInt32(settleTimeTB.Text), Convert.ToDouble(revDistanceTB.Text),Convert.ToDouble(overshootDistanceTB.Text),Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(cycleTB.Text), windowData.MeasurementDevice1, windowData.MeasurementDevice2, windowData.MeasurementDevice3, windowData.MeasurementDevice4))
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

        private async void StopMoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.MoveStop();
        }

        private async void HighLimitSwitchReversal_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.HighLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtraTB.Text),Convert.ToInt32(revSettleTB.Text));
            Console.WriteLine(testAxis.AxisPosition);
        }

        private async void LowLimitSwitchReversal_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.LowLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtraTB.Text), Convert.ToInt32(revSettleTB.Text));
            Console.WriteLine(testAxis.AxisPosition);
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
    }
}
