using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for TestSuite.xaml
    /// </summary>
    public partial class TestSuite : Window
    {
        ObservableCollection<TestListItem> testItems = new();
        ObservableCollection<string> statusListItems = new();
        MainWindow wd;
        NcAxis NcAxis;

        public TestSuite()
        {
            InitializeComponent();
            wd = (MainWindow)App.Current.MainWindow;
            NcAxis = wd.NcAxisView.testAxis;

            TestList.ItemsSource = testItems;
            statusList.ItemsSource = statusListItems;
            SettingTestType.ItemsSource = Enum.GetValues(typeof(TestTypes)).Cast<TestTypes>();

            UpdateEnabledUIElements();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!wd.windowClosing)
            {
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestList.SelectedIndex == -1 || TestList.SelectedIndex == 0) return;    //if no item selected or first item selected
            testItems.Move(TestList.SelectedIndex, TestList.SelectedIndex-1);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestList.SelectedIndex == -1 || TestList.SelectedIndex == testItems.Count-1) return;    //if no item selected or last item selected
            testItems.Move(TestList.SelectedIndex, TestList.SelectedIndex+1);
        }

        private void UpdateEnabledUIElements(bool force = false)
        {
            //Based on the current selection and test type of current selection it will enabled or disable the test setting UI elements
            bool enableFlag;
            if(TestList.SelectedIndex == -1)
            {
                enableFlag = false;
            }
            else
            {
                enableFlag = true;
            }
            if (force) enableFlag = false;
            SettingTestType.IsEnabled = enableFlag;
            SettingTitle.SettingValue.IsEnabled = enableFlag;

            if(TestList.SelectedIndex != -1)
            {
                if(testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.NoneSelected || testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.UserPrompt)
                {
                    enableFlag = false;
                }
                else
                {
                    enableFlag = true;
                }
            }
            if (force) enableFlag = false;
            SettingAxisNumber.SettingValue.IsEnabled = enableFlag;
            SettingCycles.SettingValue.IsEnabled = enableFlag;
            SettingCycleDelay.SettingValue.IsEnabled = enableFlag;
            SettingVelocity.SettingValue.IsEnabled = enableFlag;
            SettingTimeout.SettingValue.IsEnabled = enableFlag;

            if (TestList.SelectedIndex != -1)
            {
                if (testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.EndToEnd)
                {
                    enableFlag = true;
                }
                else
                {
                    enableFlag = false;
                }
            }
            if (force) enableFlag = false;
            SettingReversalVelocity.SettingValue.IsEnabled = enableFlag;
            SettingReversalExtraSeconds.SettingValue.IsEnabled = enableFlag;
            SettingReversalSettlingSeconds.SettingValue.IsEnabled = enableFlag;

            if (TestList.SelectedIndex != -1)
            {
                if (testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.UnidirectionalAccuracy || testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.BidirectionalAccuracy || testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.ScalingTest || testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.BacklashDetection)
                {
                    enableFlag = true;
                }
                else
                {
                    enableFlag = false;
                }
            }
            if (force) enableFlag = false;
            SettingInitialSetpoint.SettingValue.IsEnabled = enableFlag;
            SettingAccuracySteps.SettingValue.IsEnabled = enableFlag;
            SettingStepSize.SettingValue.IsEnabled = enableFlag;
            SettingSettlingTime.SettingValue.IsEnabled = enableFlag;
            SettingReversalDistance.SettingValue.IsEnabled = enableFlag;

            if (TestList.SelectedIndex != -1)
            {
                if (testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.BidirectionalAccuracy)
                {
                    enableFlag = true;
                }
                else
                {
                    enableFlag = false;
                }
            }
            if (force) enableFlag = false;
            SettingOvershootDistance.SettingValue.IsEnabled = enableFlag;

            if (TestList.SelectedIndex != -1)
            {
                if (testItems[TestList.SelectedIndex].TestSettings.TestType.Val == TestTypes.ScalingTest)
                {
                    enableFlag = true;
                }
                else
                {
                    enableFlag = false;
                }
            }
            if (force) enableFlag = false;
            SettingEndSetpoint.SettingValue.IsEnabled = enableFlag;
        }

        private void TestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Combobox
            if(TestList.SelectedIndex==-1)
            {
                UpdateEnabledUIElements();
                return;
            }
            SettingTestType.SelectedItem = testItems[TestList.SelectedIndex].TestSettings.TestType.Val;

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

            UpdateEnabledUIElements();
        }

        private void SettingTestType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Update testItem Test type from combo box
            testItems[TestList.SelectedIndex].TestSettings.TestType.Val = (TestTypes)SettingTestType.SelectedItem;
            UpdateEnabledUIElements();
        }

        private string saveDirectory;
        
        private void SelectSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
                       
            if (wd.NcAxisView.testAxis == null)
            {
                Console.WriteLine("Initialise an axis first");
                return;
            }
            var fbd = new VistaFolderBrowserDialog();
            saveDirectory = String.Empty;
            if (fbd.ShowDialog() == true)
            {
                saveDirectory = fbd.SelectedPath;
            }
            Console.WriteLine(saveDirectory);
            wd.NcAxisView.testAxis.TestDirectory = saveDirectory;
        }

        private void AddTestButton_Click(object sender, RoutedEventArgs e)
        {
            testItems.Add(new("1"));
            TestList.SelectedIndex = testItems.Count - 1;
            testItems[TestList.SelectedIndex].TestSettings.ResetSettings();
        }

        private void DeleteTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestList.SelectedIndex == -1) return;
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete selected item: " + testItems[TestList.SelectedIndex].TestSettings.TestTitle.UiVal,"Confirm deletion",MessageBoxButton.YesNo);
            
            if (result == MessageBoxResult.Yes)
            {
                testItems.Remove(testItems[TestList.SelectedIndex]);
                TestList.SelectedIndex = -1;
            }           
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            VistaSaveFileDialog fbd = new();
            fbd.AddExtension = true;
            fbd.DefaultExt = ".XML";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                SaveTestSuite(selectedFile);
            }
        }
        public void SaveTestSuite(string selectedFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("Tests");
            xmlDoc.AppendChild(rootNode);

            //Save settings for each test

            foreach(TestListItem test in testItems)
            {
                XmlNode testNode = xmlDoc.CreateElement("Test");
                rootNode.AppendChild(testNode);
                test.TestSettings.AddSettingsFields(xmlDoc, testNode, test.AxisID);

                //AddFields(xmlDoc, test, testNode);
            }
            xmlDoc.Save(selectedFile);
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fbd = new();
            fbd.Filter = "*.XML|*.xml";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                LoadTestSuite(selectedFile);
            }
        }
        private void LoadTestSuite(string selectedFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(selectedFile);
            XmlNodeList tests = xmlDoc.SelectNodes("Tests/Test");

            testItems.Clear();  //clear the current list
            int testCounter = 0;
            //Add each test in turn
            foreach (XmlNode test in tests)
            {
                
                testItems.Add(new("1"));    //temp axis ID
                TestTypes importedType;
                string testTypeXml = test.SelectSingleNode("testType").InnerText;
                Enum.TryParse(testTypeXml, out importedType);

                //Select the test type
                testItems[testCounter].TestSettings.TestType.Val = importedType;

                //Import all the settings
                ImportSingleTestSettings(testItems[testCounter], test);
                
                //increment the list counter
                testCounter++;
            }
        }
        
        public static void ImportSingleTestSettings(TestListItem tli, XmlNode testNode)
        {
            tli.TestSettings.TestTitle.UiVal = testNode.SelectSingleNode("testTitle").InnerText;
            tli.AxisID = testNode.SelectSingleNode("axisId").InnerText;
            tli.TestSettings.Velocity.UiVal = testNode.SelectSingleNode("velocity").InnerText;
            tli.TestSettings.Timeout.UiVal = testNode.SelectSingleNode("timeout").InnerText;
            tli.TestSettings.Cycles.UiVal = testNode.SelectSingleNode("cycles").InnerText;
            tli.TestSettings.CycleDelaySeconds.UiVal = testNode.SelectSingleNode("cycleDelaySeconds").InnerText;
            tli.TestSettings.ReversalVelocity.UiVal = testNode.SelectSingleNode("reversalVelocity").InnerText;
            tli.TestSettings.ReversalExtraTimeSeconds.UiVal = testNode.SelectSingleNode("reversalExtraTime").InnerText;
            tli.TestSettings.ReversalSettleTimeSeconds.UiVal = testNode.SelectSingleNode("reversalSettleTime").InnerText;
            tli.TestSettings.InitialSetpoint.UiVal = testNode.SelectSingleNode("initialSetpoint").InnerText;
            tli.TestSettings.NumberOfSteps.UiVal = testNode.SelectSingleNode("numberOfSteps").InnerText;
            tli.TestSettings.StepSize.UiVal = testNode.SelectSingleNode("stepSize").InnerText;
            tli.TestSettings.SettleTimeSeconds.UiVal = testNode.SelectSingleNode("settleTime").InnerText;
            tli.TestSettings.ReversalDistance.UiVal = testNode.SelectSingleNode("reversalDistance").InnerText;
            tli.TestSettings.OvershootDistance.UiVal = testNode.SelectSingleNode("overshootDistance").InnerText;
            
            if(testNode.SelectSingleNode("endSetpoint").InnerText != null)
            tli.TestSettings.EndSetpoint.UiVal = testNode.SelectSingleNode("endSetpoint").InnerText;    //Enable older file compatibility
        }

        private async void RunTestButton_Click(object sender, RoutedEventArgs e)
        {
            NcAxis = wd.NcAxisView.testAxis; //was NC axis initialised before screen opened?
            statusListItems.Clear();
            if (string.IsNullOrEmpty(saveDirectory))
            {
                Console.WriteLine("Select a save directory");
                return;
            }
            UpdateEnabledUIElements(true);
            TestList.IsEnabled = false;
            //windowData.NcAxisView.testAxis.

            int testCounter = 0;
            foreach(TestListItem test in testItems)
            {
                
                statusListItems.Add("Running test " + testCounter + ": " + test.TestSettings.TestTitle.UiVal);
                //foreach test we initialise the axis, pass the settings, pass the measurement devices

                //Update axis ID
                
                NcAxis.UpdateAxisInstance(Convert.ToUInt32(test.AxisID),wd.Plc);
                bool testResult;
                switch (test.TestSettings.TestType.Val)
                {
                    case TestTypes.EndToEnd:
                        testResult = await NcAxis.LimitToLimitTestwithReversingSequence(test.TestSettings, wd.MeasurementDevices);
                        if(testResult)
                        {
                            statusListItems.Add("Complete");
                        }
                        else
                        {
                            statusListItems.Add("Failed");
                        }
                        break;
                    case TestTypes.UnidirectionalAccuracy:
                        testResult = await NcAxis.UniDirectionalAccuracyTest(test.TestSettings, wd.MeasurementDevices);
                        if (testResult)
                        {
                            statusListItems.Add("Complete");
                        }
                        else
                        {
                            statusListItems.Add("Failed");
                        }
                        break;
                    case TestTypes.BidirectionalAccuracy:
                        testResult = await NcAxis.BiDirectionalAccuracyTest(test.TestSettings, wd.MeasurementDevices);
                        if (testResult)
                        {
                            statusListItems.Add("Complete");
                        }
                        else
                        {
                            statusListItems.Add("Failed");
                        }
                        break;
                    case TestTypes.ScalingTest:
                        testResult = await NcAxis.ScalingTest(test.TestSettings, wd.MeasurementDevices);
                        if (testResult)
                        {
                            statusListItems.Add("Complete");
                        }
                        else
                        {
                            statusListItems.Add("Failed");
                        }
                        break;
                    case TestTypes.BacklashDetection:
                        testResult = await NcAxis.BacklashDetectionTest(test.TestSettings, wd.MeasurementDevices);
                        if (testResult)
                        {
                            statusListItems.Add("Complete");
                        }
                        else
                        {
                            statusListItems.Add("Failed");
                        }
                        break;
                    case TestTypes.UserPrompt:
                        MessageBoxResult result = MessageBox.Show(test.TestSettings.TestTitle.UiVal + "\nSelect 'cancel' to exit test sequence.", "User breakpoint", MessageBoxButton.OKCancel);
                        if(result == MessageBoxResult.Cancel)
                        {
                            //exit the sequence
                            Console.WriteLine("Test sequence cancelled");
                            TestList.IsEnabled = true;
                            return;
                        }
                        break;
                }
                testCounter++;
            }
            statusListItems.Add("All tests finished");
            TestList.IsEnabled = true;
        }
    }

    public class TestListItem : INotifyPropertyChanged
    {
        private string _AxisID;
        public string AxisID
        {
            get { return _AxisID; }
            set { _AxisID = value;
                OnPropertyChanged();
            }
        }

        public NcTestSettings TestSettings { get; set; }
        public TestListItem(string axisID)
        {
            AxisID = axisID;
            TestSettings = new();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }    
    }
}
