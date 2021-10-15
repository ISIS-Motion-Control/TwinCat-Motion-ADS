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
    //NO DTIS IMPLEMENTED ON METHOD CALLS

    /// <summary>
    /// Interaction logic for NcAxisView.xaml
    /// </summary>
    public partial class NcAxisView : UserControl
    {
        MainWindow windowData;
        public Axis testAxis;
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
                testAxis = new Axis(Convert.ToUInt32(axisSelection.Text), windowData.Plc);
            }
            else
            {
                testAxis.updateInstance(Convert.ToUInt32(axisSelection.Text));
            }
            setupBinds();

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

        private async void end2endTest_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            cancelTest.IsEnabled = true;
            pauseTest.IsEnabled = true;
            await testAxis.end2endCycleTesting(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(cycleDelay.Text), Convert.ToInt32(cycleTB.Text));
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
            if (await testAxis.end2endCycleTestingWithReversal(Convert.ToDouble(velocityTB.Text), Convert.ToDouble(revVel.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(cycleDelay.Text), Convert.ToInt32(cycleTB.Text), Convert.ToInt32(revExtra.Text), Convert.ToInt32(revSettle.Text), windowData.MeasurementDevice1,windowData.MeasurementDevice2,windowData.MeasurementDevice3,windowData.MeasurementDevice4))
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

            if (await testAxis.uniDirectionalAccuracyTest(Convert.ToDouble(initSP.Text), Convert.ToDouble(velocityTB.Text), Convert.ToInt32(cycleTB.Text), Convert.ToInt32(accSteps.Text), Convert.ToDouble(stepSize.Text), Convert.ToInt32(settleTime.Text), Convert.ToDouble(revDistance.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(cycleTB.Text),windowData.MeasurementDevice1, windowData.MeasurementDevice2, windowData.MeasurementDevice3, windowData.MeasurementDevice4))
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

            if (await testAxis.biDirectionalAccuracyTest(Convert.ToDouble(initSP.Text), Convert.ToDouble(velocityTB.Text), Convert.ToInt32(cycleTB.Text), Convert.ToInt32(accSteps.Text), Convert.ToDouble(stepSize.Text), Convert.ToInt32(settleTime.Text), Convert.ToDouble(revDistance.Text),Convert.ToDouble(overshootDistance.Text),Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(cycleTB.Text), windowData.MeasurementDevice1, windowData.MeasurementDevice2, windowData.MeasurementDevice3, windowData.MeasurementDevice4))
            {
                Console.WriteLine("Test Complete");
            }
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
            await testAxis.HighLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtra.Text),Convert.ToInt32(revSettle.Text));
            Console.WriteLine(testAxis.AxisPosition);
        }

        private async void lowLimReversal_Click(object sender, RoutedEventArgs e)
        {
            if (testAxis == null)
            {
                Console.WriteLine("No axis initialised");
                return;
            }
            await testAxis.LowLimitReversal(Convert.ToDouble(velocityTB.Text), Convert.ToInt32(timeoutTB.Text), Convert.ToInt32(revExtra.Text), Convert.ToInt32(revSettle.Text));
            Console.WriteLine(testAxis.AxisPosition);
        }
    }
}
