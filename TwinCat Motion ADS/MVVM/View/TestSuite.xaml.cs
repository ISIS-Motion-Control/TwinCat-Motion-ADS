using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace TwinCat_Motion_ADS.MVVM.View
{
    /// <summary>
    /// Interaction logic for TestSuite.xaml
    /// </summary>
    public partial class TestSuite : Window
    {
        ObservableCollection<TestListItem> testItems = new();

        public TestSuite()
        {
            InitializeComponent();

            testItems.Add(new("1"));
            testItems.Add(new("1"));
            testItems.Add(new("2"));

            //debug code
            testItems[0].TestType = TestType.EndToEnd;
            testItems[1].TestType = TestType.UnidirectionalAccuracy;
            testItems[2].TestType = TestType.BidirectionalAccuracy;

            TestList.ItemsSource = testItems;
            //SettingTestType.ItemsSource = testTypeItems;
            SettingTestType.ItemsSource = Enum.GetValues(typeof(TestType)).Cast<TestType>();

            UpdateEnabledUIElements();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
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

        private void UpdateEnabledUIElements()
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
            SettingTestType.IsEnabled = enableFlag;
            SettingTitle.SettingValue.IsEnabled = enableFlag;

            if(TestList.SelectedIndex != -1)
            {
                if(testItems[TestList.SelectedIndex].TestType == TestType.NoneSelected || testItems[TestList.SelectedIndex].TestType == TestType.UserPrompt)
                {
                    enableFlag = false;
                }
                else
                {
                    enableFlag = true;
                }
            }

            SettingAxisNumber.SettingValue.IsEnabled = enableFlag;
            SettingCycles.SettingValue.IsEnabled = enableFlag;
            SettingCycleDelay.SettingValue.IsEnabled = enableFlag;
            SettingVelocity.SettingValue.IsEnabled = enableFlag;
            SettingTimeout.SettingValue.IsEnabled = enableFlag;

            if (TestList.SelectedIndex != -1)
            {
                if (testItems[TestList.SelectedIndex].TestType == TestType.EndToEnd)
                {
                    enableFlag = true;
                }
                else
                {
                    enableFlag = false;
                }
            }

            SettingReversalVelocity.SettingValue.IsEnabled = enableFlag;
            SettingReversalExtraSeconds.SettingValue.IsEnabled = enableFlag;
            SettingReversalSettlingSeconds.SettingValue.IsEnabled = enableFlag;

            if (TestList.SelectedIndex != -1)
            {
                if (testItems[TestList.SelectedIndex].TestType == TestType.UnidirectionalAccuracy || testItems[TestList.SelectedIndex].TestType == TestType.BidirectionalAccuracy)
                {
                    enableFlag = true;
                }
                else
                {
                    enableFlag = false;
                }
            }

            SettingInitialSetpoint.SettingValue.IsEnabled = enableFlag;
            SettingAccuracySteps.SettingValue.IsEnabled = enableFlag;
            SettingStepSize.SettingValue.IsEnabled = enableFlag;
            SettingSettlingTime.SettingValue.IsEnabled = enableFlag;
            SettingReversalDistance.SettingValue.IsEnabled = enableFlag;

            if (TestList.SelectedIndex != -1)
            {
                if (testItems[TestList.SelectedIndex].TestType == TestType.BidirectionalAccuracy)
                {
                    enableFlag = true;
                }
                else
                {
                    enableFlag = false;
                }
            }
            SettingOvershootDistance.SettingValue.IsEnabled = enableFlag;
        }

        private void TestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Combobox
            if(TestList.SelectedIndex==-1)
            {
                UpdateEnabledUIElements();
                return;
            }
            switch (testItems[TestList.SelectedIndex].TestType)
            {
                case TestType.NoneSelected:
                    SettingTestType.SelectedItem = TestType.NoneSelected;
                    break;
                case TestType.UserPrompt:
                    SettingTestType.SelectedItem = TestType.UserPrompt;
                    break;
                case TestType.EndToEnd:
                    SettingTestType.SelectedItem = TestType.EndToEnd;
                    break;
                case TestType.UnidirectionalAccuracy:
                    SettingTestType.SelectedItem = TestType.UnidirectionalAccuracy;
                    break;
                case TestType.BidirectionalAccuracy:
                    SettingTestType.SelectedItem = TestType.BidirectionalAccuracy;
                    break;
            }

            XamlUI.TextboxBinding(SettingTitle.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrTestTitle");
            XamlUI.TextboxBinding(SettingAxisNumber.SettingValue, testItems[TestList.SelectedIndex], "AxisID");

            XamlUI.TextboxBinding(SettingCycles.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrCycles");
            XamlUI.TextboxBinding(SettingCycleDelay.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrCycleDelaySeconds");
            XamlUI.TextboxBinding(SettingVelocity.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrVelocity");
            XamlUI.TextboxBinding(SettingTimeout.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrTimeout");


            XamlUI.TextboxBinding(SettingReversalVelocity.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrReversalVelocity");
            XamlUI.TextboxBinding(SettingReversalExtraSeconds.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrReversalExtraTimeSeconds");
            XamlUI.TextboxBinding(SettingReversalSettlingSeconds.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrReversalSettleTimeSeconds");

            XamlUI.TextboxBinding(SettingInitialSetpoint.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrInitialSetpoint");
            XamlUI.TextboxBinding(SettingAccuracySteps.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrNumberOfSteps");
            XamlUI.TextboxBinding(SettingStepSize.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrStepSize");
            XamlUI.TextboxBinding(SettingSettlingTime.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrSettleTimeSeconds");
            XamlUI.TextboxBinding(SettingReversalDistance.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrReversalDistance");

            XamlUI.TextboxBinding(SettingOvershootDistance.SettingValue, testItems[TestList.SelectedIndex].TestSettings, "StrOvershootDistance");

            UpdateEnabledUIElements();
        }

        private void SettingTestType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Update testItem Test type from combo box
            testItems[TestList.SelectedIndex].TestType = (TestType)SettingTestType.SelectedItem;
            UpdateEnabledUIElements();
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
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete selected item: " + testItems[TestList.SelectedIndex].TestSettings.StrTestTitle,"Confirm deletion",MessageBoxButton.YesNo);
            
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

                XmlNode testType = xmlDoc.CreateElement("testType");
                testType.InnerText = test.TestType.ToString();
                XmlNode testTitle = xmlDoc.CreateElement("testTitle");
                testTitle.InnerText = test.TestSettings.StrTestTitle;
                XmlNode axisId = xmlDoc.CreateElement("axisId");
                axisId.InnerText = test.AxisID;

                XmlNode velocity = xmlDoc.CreateElement("velocity");
                velocity.InnerText = test.TestSettings.StrVelocity;
                XmlNode timeout = xmlDoc.CreateElement("timeout");
                timeout.InnerText = test.TestSettings.StrTimeout;
                XmlNode cycles = xmlDoc.CreateElement("cycles");
                cycles.InnerText = test.TestSettings.StrCycles;
                XmlNode cycleDelaySeconds = xmlDoc.CreateElement("cycleDelaySeconds");
                cycleDelaySeconds.InnerText = test.TestSettings.StrCycleDelaySeconds;
                XmlNode reversalVelocity = xmlDoc.CreateElement("reversalVelocity");
                reversalVelocity.InnerText = test.TestSettings.StrReversalVelocity;
                XmlNode reversalExtraTime = xmlDoc.CreateElement("reversalExtraTime");
                reversalExtraTime.InnerText = test.TestSettings.StrReversalExtraTimeSeconds;
                XmlNode reversalSettleTime = xmlDoc.CreateElement("reversalSettleTime");
                reversalSettleTime.InnerText = test.TestSettings.StrReversalSettleTimeSeconds;
                XmlNode initialSetpoint = xmlDoc.CreateElement("initialSetpoint");
                initialSetpoint.InnerText = test.TestSettings.StrInitialSetpoint;
                XmlNode numberOfSteps = xmlDoc.CreateElement("numberOfSteps");
                numberOfSteps.InnerText = test.TestSettings.StrNumberOfSteps;
                XmlNode stepSize = xmlDoc.CreateElement("stepSize");
                stepSize.InnerText = test.TestSettings.StrStepSize;
                XmlNode settleTime = xmlDoc.CreateElement("settleTime");
                settleTime.InnerText = test.TestSettings.StrSettleTimeSeconds;
                XmlNode reversalDistance = xmlDoc.CreateElement("reversalDistance");
                reversalDistance.InnerText = test.TestSettings.StrReversalDistance;
                XmlNode overshootDistance = xmlDoc.CreateElement("overshootDistance");
                overshootDistance.InnerText = test.TestSettings.StrOvershootDistance;


                testNode.AppendChild(testType);
                testNode.AppendChild(testTitle);
                testNode.AppendChild(axisId);
            }
            xmlDoc.Save(selectedFile);
        }


        /*

                XmlNode deviceNode = xmlDoc.CreateElement("MeasurementDevice");
                XmlNode nameNode = xmlDoc.CreateElement("Name");
                XmlNode deviceTypeNode = xmlDoc.CreateElement("DeviceType");
                
                //Common setup to all types
                nameNode.InnerText = md.Name;
                deviceTypeNode.InnerText = md.DeviceTypeString;
                deviceNode.AppendChild(nameNode);
                deviceNode.AppendChild(deviceTypeNode);
                rootNode.AppendChild(deviceNode);

                //Unique settings
                switch(md.DeviceTypeString)
                {
                    case "DigimaticIndicator":
                        XmlNode commNode = xmlDoc.CreateElement("Port");
                        commNode.InnerText = md.PortName;
                        XmlNode baudNode = xmlDoc.CreateElement("BaudRate");
                        baudNode.InnerText = md.BaudRate;
                        deviceNode.AppendChild(commNode);
                        deviceNode.AppendChild(baudNode);
                        break;

                    case "KeyenceTM3000":
                        commNode = xmlDoc.CreateElement("Port");
                        commNode.InnerText = md.PortName;
                        baudNode = xmlDoc.CreateElement("BaudRate");
                        baudNode.InnerText = md.BaudRate;
                        deviceNode.AppendChild(commNode);
                        deviceNode.AppendChild(baudNode);

                        for (int i = 0; i<md.keyence.KEYENCE_MAX_CHANNELS;i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("Channel");
                            XmlNode channelName = xmlDoc.CreateElement("Name");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");

                            channelName.InnerText = md.keyence.ChName[i];
                            channelConnection.InnerText = md.keyence.ChConnected[i].ToString();
                            channelNode.AppendChild(channelName);
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        break;

                    case "Beckhoff":
                        XmlNode amsNode = xmlDoc.CreateElement("AmsNetID");
                        amsNode.InnerText = md.AmsNetId;
                        deviceNode.AppendChild(amsNode);

                        //Create digital channles
                        for(int i=0;i<md.beckhoff.DIGITAL_INPUT_CHANNELS;i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("DigChannel");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");
                            channelConnection.InnerText = md.beckhoff.DigitalInputConnected[i].ToString();
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        //create pt100 channels
                        for (int i = 0; i < md.beckhoff.PT100_CHANNELS; i++)
                        {
                            XmlNode channelNode = xmlDoc.CreateElement("PT100Channel");
                            XmlNode channelConnection = xmlDoc.CreateElement("Connected");
                            channelConnection.InnerText = md.beckhoff.PT100Connected[i].ToString();
                            channelNode.AppendChild(channelConnection);
                            deviceNode.AppendChild(channelNode);
                        }
                        break;

                    case "MotionChannel":
                        XmlNode varTypeNode = xmlDoc.CreateElement("VariableType");
                        XmlNode varPathNode = xmlDoc.CreateElement("VariablePath");
                        varTypeNode.InnerText = md.motionChannel.VariableType;
                        varPathNode.InnerText = md.motionChannel.VariableString;
                        deviceNode.AppendChild(varTypeNode);
                        deviceNode.AppendChild(varPathNode);
                        break;

                    case "Timestamp":
                        //No settings to export
                        break;

                }*/




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



        private TestType _testType;

        public TestType TestType
        {
            get { return _testType; }
            set { _testType = value;
                OnPropertyChanged();
            }
        }



        public NcTestSettings TestSettings { get; set; }

        public TestListItem(string axisID)
        {
            AxisID = axisID;
            TestSettings = new();
            TestType = TestType.NoneSelected;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    public enum TestType
    {
        EndToEnd,
        UnidirectionalAccuracy,
        BidirectionalAccuracy,
        UserPrompt,
        NoneSelected
    }
}
