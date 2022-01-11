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

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for AirAxisView.xaml
    /// </summary>
    public partial class AirAxisView : UserControl
    {
        readonly MainWindow windowData;
        public PneumaticAxis pneumaticAxis;
        public string selectedFolder = string.Empty;
        public AirTestSettings TestSettings = new();

        public AirAxisView()
        {
            InitializeComponent();
            windowData = (MainWindow)Application.Current.MainWindow;
            SetupBinds();
        }

        public void SetupBinds()
        {
            XamlUI.CheckBoxBinding("Extended Limit", pneumaticExtended, pneumaticAxis, "ExtendedLimit", BindingMode.OneWay);
            XamlUI.CheckBoxBinding("Retracted Limit", pneumaticRetracted, pneumaticAxis, "RetractedLimit", BindingMode.OneWay);
            XamlUI.CheckBoxBinding("Actuator", CylinderAir, pneumaticAxis, "Cylinder", BindingMode.OneWay);
            XamlUI.TextboxBinding(cycles, TestSettings.Cycles, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(settlingReads, TestSettings.SettlingReads, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(readDelay, TestSettings.ReadDelayMs, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(extendDelay, TestSettings.DelayAfterExtend, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(retractDelay, TestSettings.DelayAfterRetract, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(extendTimeout, TestSettings.ExtendTimeout, "UiVal", UpdateSourceTrigger.LostFocus);
            XamlUI.TextboxBinding(retractTimeout, TestSettings.RetractTimeout, "UiVal", UpdateSourceTrigger.LostFocus);
        }

        private void InitialisePneumatic_Click(object sender, RoutedEventArgs e)
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
            SetupBinds();
            pneumaticAxis.ReadStatuses();
        }

        private async void ExtendCylinderButton_Click(object sender, RoutedEventArgs e)
        {
            if (await pneumaticAxis.ExtendCylinderAndWait() == false)
            {
                Console.WriteLine("FAILED");
            }
        }

        private async void RetractCylinderButton_Click(object sender, RoutedEventArgs e)
        {
            if (await pneumaticAxis.RetractCylinderAndWait() == false)
            {
                Console.WriteLine("FAILED");
            }
        }

        private async void ShutterLimitToLimitTestButton_Click(object sender, RoutedEventArgs e)
        {
            windowData.mainWindowGrid.Focus();
            await pneumaticAxis.End2EndTest(TestSettings,windowData.MeasurementDevices);
        }

        private void SelectTestDirectory_Click(object sender, RoutedEventArgs e)
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
