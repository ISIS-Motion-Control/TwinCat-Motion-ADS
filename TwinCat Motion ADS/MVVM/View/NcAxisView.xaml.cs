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
        MainWindow windowData;
        public NcAxis testAxis;
        public NcTestSettings NcTestSettings = new NcTestSettings();
        public string selectedFolder = string.Empty;
        
        public NcAxisView()
        {
            InitializeComponent();
            windowData = (MainWindow)Application.Current.MainWindow;
            setupBinds();
        }


        private void initAxis_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                testAxis = new NcAxis(Convert.ToUInt32(axisSelection.Text), windowData.Plc);
            }
            else
            {
                testAxis.updateInstance(Convert.ToUInt32(axisSelection.Text), windowData.Plc);
            }
            setupBinds();
        }

        private void nukeIt_Click(object sender, RoutedEventArgs e)
        {
            testAxis = null;
        }

        
        public void setupBinds()
        {
            Binding axisPositionBind = new Binding();
            axisPositionBind.Mode = BindingMode.TwoWay;
            axisPositionBind.Source = testAxis;
            axisPositionBind.Path = new PropertyPath("AxisPosition");
            axisPositionBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            axisPositionBind.StringFormat = "F3";
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

            Binding errorBind = new Binding();
            errorBind.Mode = BindingMode.OneWay;
            errorBind.Source = testAxis;
            errorBind.Path = new PropertyPath("Error");
            errorBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(errorCheck, CheckBox.IsCheckedProperty, errorBind);

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

            Binding timeoutTextboxBind = new Binding();
            timeoutTextboxBind.Mode = BindingMode.TwoWay;
            timeoutTextboxBind.Source = NcTestSettings;
            timeoutTextboxBind.Path = new PropertyPath("StrTimeout");
            timeoutTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(timeoutTB, TextBox.TextProperty, timeoutTextboxBind);

            Binding testTitleTextboxBind = new Binding();
            testTitleTextboxBind.Mode = BindingMode.TwoWay;
            testTitleTextboxBind.Source = NcTestSettings;
            testTitleTextboxBind.Path = new PropertyPath("StrTestTitle");
            testTitleTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(testTitleTB, TextBox.TextProperty, testTitleTextboxBind);
            
            Binding velocityTextboxBind = new Binding();
            velocityTextboxBind.Mode = BindingMode.TwoWay;
            velocityTextboxBind.Source = NcTestSettings;
            velocityTextboxBind.Path = new PropertyPath("StrVelocity");
            velocityTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(velocityTB, TextBox.TextProperty, velocityTextboxBind);

            Binding cycleTextboxBind = new Binding();
            cycleTextboxBind.Mode = BindingMode.TwoWay;
            cycleTextboxBind.Source = NcTestSettings;
            cycleTextboxBind.Path = new PropertyPath("StrCycles");
            cycleTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(cycleTB, TextBox.TextProperty, cycleTextboxBind);

            Binding cycleDelayTextboxBind = new Binding();
            cycleDelayTextboxBind.Mode = BindingMode.TwoWay;
            cycleDelayTextboxBind.Source = NcTestSettings;
            cycleDelayTextboxBind.Path = new PropertyPath("StrCycleDelaySeconds");
            cycleDelayTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(cycleDelayTB, TextBox.TextProperty, cycleDelayTextboxBind);

            Binding reversalVelTextboxBind = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = NcTestSettings,
                Path = new PropertyPath("StrReversalVelocity"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(revVelTB, TextBox.TextProperty, reversalVelTextboxBind);

            Binding reversalExtraTimeTextboxBind = new Binding();
            reversalExtraTimeTextboxBind.Mode = BindingMode.TwoWay;
            reversalExtraTimeTextboxBind.Source = NcTestSettings;
            reversalExtraTimeTextboxBind.Path = new PropertyPath("StrReversalExtraTimeSeconds");
            reversalExtraTimeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(revExtraTB, TextBox.TextProperty, reversalExtraTimeTextboxBind);

            Binding reversalSettleTimeTextboxBind = new Binding();
            reversalSettleTimeTextboxBind.Mode = BindingMode.TwoWay;
            reversalSettleTimeTextboxBind.Source = NcTestSettings;
            reversalSettleTimeTextboxBind.Path = new PropertyPath("StrReversalSettleTimeSeconds");
            reversalSettleTimeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(revSettleTB, TextBox.TextProperty, reversalSettleTimeTextboxBind);

            Binding initialSetpointTextboxBind = new Binding();
            initialSetpointTextboxBind.Mode = BindingMode.TwoWay;
            initialSetpointTextboxBind.Source = NcTestSettings;
            initialSetpointTextboxBind.Path = new PropertyPath("StrInitialSetpoint");
            initialSetpointTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(initSetpointTB, TextBox.TextProperty, initialSetpointTextboxBind);

            Binding numberOfStepsTextboxBind = new Binding();
            numberOfStepsTextboxBind.Mode = BindingMode.TwoWay;
            numberOfStepsTextboxBind.Source = NcTestSettings;
            numberOfStepsTextboxBind.Path = new PropertyPath("StrNumberOfSteps");
            numberOfStepsTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(NumberOfStepsTB, TextBox.TextProperty, numberOfStepsTextboxBind);

            Binding stepSizeTextboxBind = new Binding();
            stepSizeTextboxBind.Mode = BindingMode.TwoWay;
            stepSizeTextboxBind.Source = NcTestSettings;
            stepSizeTextboxBind.Path = new PropertyPath("StrStepSize");
            stepSizeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(stepSizeTB, TextBox.TextProperty, stepSizeTextboxBind);

            Binding settleTimeTextboxBind = new Binding();
            settleTimeTextboxBind.Mode = BindingMode.TwoWay;
            settleTimeTextboxBind.Source = NcTestSettings;
            settleTimeTextboxBind.Path = new PropertyPath("StrSettleTimeSeconds");
            settleTimeTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(settleTimeTB, TextBox.TextProperty, settleTimeTextboxBind);

            Binding reversalDistanceTextboxBind = new Binding();
            reversalDistanceTextboxBind.Mode = BindingMode.TwoWay;
            reversalDistanceTextboxBind.Source = NcTestSettings;
            reversalDistanceTextboxBind.Path = new PropertyPath("StrReversalDistance");
            reversalDistanceTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(revDistanceTB, TextBox.TextProperty, reversalDistanceTextboxBind);

            Binding overshootDistanceTextboxBind = new Binding();
            overshootDistanceTextboxBind.Mode = BindingMode.TwoWay;
            overshootDistanceTextboxBind.Source = NcTestSettings;
            overshootDistanceTextboxBind.Path = new PropertyPath("StrOvershootDistance");
            overshootDistanceTextboxBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(overshootDistanceTB, TextBox.TextProperty, overshootDistanceTextboxBind);


        }

        private async void moveAbsButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testAxis.moveAbsoluteAndWait(posCommanded, Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
        }

        private async void enableButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis!= null)
            {
                await testAxis.setEnable(!testAxis.AxisEnabled);
            }
            else
            {
                Console.WriteLine("No axis initialised");
            }
            
        }

        private async void resetButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.Reset();
        }

        private void folderDirSelect_Click(object sender, RoutedEventArgs e)
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

        private async void moveRelButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            double posCommanded = Convert.ToDouble(positionText.Text);
            await testAxis.moveRelativeAndWait(posCommanded, Convert.ToDouble(velocityTB.Text));
        }

        private async void moveVelButton_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.moveVelocity(Convert.ToDouble(velocityTB.Text));
        }

        private async void move2High_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.moveToHighLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
        }

        private async void move2Low_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.moveToLowLimit(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text));
        }


        private async void end2endReversal_Click(object sender, RoutedEventArgs e)
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
            if (await testAxis.end2endCycleTestingWithReversal(NcTestSettings, windowData.MeasurementDevices))
            {
                Console.WriteLine("Test Complete");
            }
            else
            {
                Console.WriteLine("Test did not complete");
            }

        }

        private async void uniDirecitonalTest_Click(object sender, RoutedEventArgs e)
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
            if (await testAxis.uniDirectionalAccuracyTest(NcTestSettings, windowData.MeasurementDevices))

            {
                //
            }
            else
            {
                Console.WriteLine("Test did not complete");
            }
        }

        private async void biDirecitonalTest_Click(object sender, RoutedEventArgs e)
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
            if (await testAxis.biDirectionalAccuracyTest(NcTestSettings, windowData.MeasurementDevices))
            {          }
            else
            {
                Console.WriteLine("Test did not complete");
            }
        }

        private void cancelTest_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            testAxis.CancelTest = !testAxis.CancelTest;
        }

        private void pauseTest_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            testAxis.PauseTest = !testAxis.PauseTest;
        }

        private async void stopMove_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.moveStop();
        }

        private async void highLimReversal_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.HighLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtraTB.Text),Convert.ToInt32(revSettleTB.Text));
            Console.WriteLine(testAxis.AxisPosition);
        }

        private async void lowLimReversal_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.LowLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtraTB.Text), Convert.ToInt32(revSettleTB.Text));
            Console.WriteLine(testAxis.AxisPosition);
        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new VistaOpenFileDialog();
            fbd.Filter = "*.settingsfile|*.*";
            string selectedFile = string.Empty;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                NcTestSettings.ImportSettings(selectedFile);
            }
            Console.WriteLine(selectedFolder);
            
        }
    }
}
