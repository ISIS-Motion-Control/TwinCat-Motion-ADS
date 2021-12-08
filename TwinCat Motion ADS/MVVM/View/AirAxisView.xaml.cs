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
    /// Interaction logic for AirAxisView.xaml
    /// </summary>
    public partial class AirAxisView : UserControl
    {
        MainWindow windowData;
        public PneumaticAxis pneumaticAxis;
        public string selectedFolder = string.Empty;
        public AirTestSettings TestSettings = new();

        public AirAxisView()
        {
            InitializeComponent();
            windowData = (MainWindow)Application.Current.MainWindow;
            setupBinds();
        }

        public void setupBinds()
        {
            Binding pneumaticExtendedBinding = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = pneumaticAxis,
                Path = new PropertyPath("ExtendedLimit"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(pneumaticExtended, CheckBox.IsCheckedProperty, pneumaticExtendedBinding);
            Binding pneumaticRetractedBinding = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = pneumaticAxis,
                Path = new PropertyPath("RetractedLimit"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(pneumaticRetracted, CheckBox.IsCheckedProperty, pneumaticRetractedBinding);
            Binding cylinderBinding = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = pneumaticAxis,
                Path = new PropertyPath("Cylinder"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(CylinderAir, CheckBox.IsCheckedProperty, cylinderBinding);

            Binding cycleBinding = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = TestSettings,
                Path = new PropertyPath("StrCycles"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(cycles, TextBox.TextProperty, cycleBinding);

            Binding settleReadsBinding = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = TestSettings,
                Path = new PropertyPath("StrSettlingReads"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(settlingReads, TextBox.TextProperty, settleReadsBinding);

            Binding ReadDelayBinding = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = TestSettings,
                Path = new PropertyPath("StrReadDelayMs"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(readDelay, TextBox.TextProperty, ReadDelayBinding);

            Binding extendDelayBinding = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = TestSettings,
                Path = new PropertyPath("StrDelayAfterExtend"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(extendDelay, TextBox.TextProperty, extendDelayBinding);

            Binding retractDelayBinding = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = TestSettings,
                Path = new PropertyPath("StrDelayAfterRetract"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(retractDelay, TextBox.TextProperty, retractDelayBinding);

            Binding extendTimeoutBinding = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = TestSettings,
                Path = new PropertyPath("StrExtendTimeout"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(extendTimeout, TextBox.TextProperty, extendTimeoutBinding);

            Binding retractTimeoutBinding = new Binding
            {
                Mode = BindingMode.TwoWay,
                Source = TestSettings,
                Path = new PropertyPath("StrRetractTimeout"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(retractTimeout, TextBox.TextProperty, retractTimeoutBinding);
        }


        private void initPneumatic_Click(object sender, RoutedEventArgs e)
        {
            if (pneumaticAxis == null)
            {
                pneumaticAxis = new PneumaticAxis(windowData.Plc);
            }
            else
            {
                pneumaticAxis = null;
                pneumaticAxis = new PneumaticAxis(windowData.Plc);
            }
            setupBinds();
            pneumaticAxis.startLimitRead();
        }

        private async void extendCylinder_button_Click(object sender, RoutedEventArgs e)
        {
            if (await pneumaticAxis.extendCylinderAndWait() == false)
            {
                Console.WriteLine("FAILED");
            }
        }

        private async void retractCylinder_button_Click(object sender, RoutedEventArgs e)
        {
            if (await pneumaticAxis.retractCylinderAndWait() == false)
            {
                Console.WriteLine("FAILED");
            }
        }

        private async void shutterEnd2End_button_Click(object sender, RoutedEventArgs e)
        {
           // await pneumaticAxis.End2EndTest(Convert.ToInt32(cycles.Text), Convert.ToInt32(settlingReads.Text), Convert.ToInt32(readDelay.Text), Convert.ToInt32(extendDelay.Text), Convert.ToInt32(retractDelay.Text), Convert.ToInt32(extendTimeout.Text), Convert.ToInt32(retractTimeout.Text));
            await pneumaticAxis.End2EndTest(TestSettings,windowData.MeasurementDevices);

        }

        private void shutterTestFolderDir_button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new VistaFolderBrowserDialog();
            selectedFolder = String.Empty;
            if (fbd.ShowDialog() == true)
            {
                selectedFolder = fbd.SelectedPath;
            }
            Console.WriteLine(selectedFolder);
            pneumaticAxis.TestDirectory = selectedFolder;
        }
    }
}
