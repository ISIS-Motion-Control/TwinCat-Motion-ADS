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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

namespace TwinCat_Motion_ADS
    {
        /// <summary>
        /// Interaction logic for TestSuite.xaml
        /// </summary>
        public partial class DataAnalysisWindow : Window
        {
            ObservableCollection<TestListItem> testItems = new();
            ObservableCollection<string> statusListItems = new();
            MainWindow wd;
            NcAxis NcAxis;

            public DataAnalysisWindow()
            {
                InitializeComponent();
                wd = (MainWindow)App.Current.MainWindow;

            }

            private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
            {
                if (!wd.windowClosing)
                {
                    e.Cancel = true;
                    this.Visibility = Visibility.Hidden;
                }
            }

            private void TestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
            /*
                XamlUI.TextboxBinding(SettingTitle.SettingValue, testItems[TestList.SelectedIndex].TestSettings.TestTitle, "UiVal");
                XamlUI.TextboxBinding(SettingAxisNumber.SettingValue, testItems[TestList.SelectedIndex], "AxisID");
                XamlUI.TextboxBinding(SettingCycles.SettingValue, testItems[TestList.SelectedIndex].TestSettings.Cycles, "UiVal");
                XamlUI.TextboxBinding(SettingCycleDelay.SettingValue, testItems[TestList.SelectedIndex].TestSettings.CycleDelaySeconds, "UiVal");
                XamlUI.TextboxBinding(SettingVelocity.SettingValue, testItems[TestList.SelectedIndex].TestSettings.Velocity, "UiVal");
                XamlUI.TextboxBinding(SettingTimeout.SettingValue, testItems[TestList.SelectedIndex].TestSettings.Timeout, "UiVal");
                XamlUI.TextboxBinding(SettingReversalVelocity.SettingValue, testItems[TestList.SelectedIndex].TestSettings.ReversalVelocity, "UiVal");
                XamlUI.TextboxBinding(SettingReversalExtraSeconds.SettingValue, testItems[TestList.SelectedIndex].TestSettings.ReversalExtraTimeSeconds, "UiVal");
                XamlUI.TextboxBinding(SettingReversalSettlingSeconds.SettingValue, testItems[TestList.SelectedIndex].TestSettings.ReversalSettleTimeSeconds, "UiVal");
                XamlUI.TextboxBinding(SettingInitialSetpoint.SettingValue, testItems[TestList.SelectedIndex].TestSettings.InitialSetpoint, "UiVal");
                XamlUI.TextboxBinding(SettingAccuracySteps.SettingValue, testItems[TestList.SelectedIndex].TestSettings.NumberOfSteps, "UiVal");
                XamlUI.TextboxBinding(SettingStepSize.SettingValue, testItems[TestList.SelectedIndex].TestSettings.StepSize, "UiVal");
                XamlUI.TextboxBinding(SettingSettlingTime.SettingValue, testItems[TestList.SelectedIndex].TestSettings.SettleTimeSeconds, "UiVal");
                XamlUI.TextboxBinding(SettingReversalDistance.SettingValue, testItems[TestList.SelectedIndex].TestSettings.ReversalDistance, "UiVal");
                XamlUI.TextboxBinding(SettingOvershootDistance.SettingValue, testItems[TestList.SelectedIndex].TestSettings.OvershootDistance, "UiVal");
                XamlUI.TextboxBinding(SettingEndSetpoint.SettingValue, testItems[TestList.SelectedIndex].TestSettings.EndSetpoint, "UiVal");
            */
            }

            public async Task<string> AddValues(string Data_A, string Data_B)
            {
                double Data = double.Parse(Data_A) + double.Parse(Data_B);
                String Data_Out = Data.ToString("D");
                return Data_Out;
            }
            public async Task<string> MinusValues(string Data_A, string Data_B)
            {
                double Data = double.Parse(Data_A) - double.Parse(Data_B);
                String Data_Out = Data.ToString("D");
                return Data_Out;
            }
        }

    public enum Operations
        {
            [StringValue("AddValues")]
            AddValues,
            [StringValue("MinusValues")]
            MinusValues,
            [StringValue("NoneSelected")]
            NoneSelected
        }
    }

