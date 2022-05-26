using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace TwinCat_Motion_ADS
{
    public class NcTestSettings : INotifyPropertyChanged
    {
        /*
         * Class for handling test settings
         * Will handle string/var conversion for tests
         * Will handle "bad" data inputs - string value won't update unless input can be correctly casted
         */
        public NcTestSettings()
        {
            //Settings are initialised with last run values
            TestTitle.UiVal = Properties.Settings.Default.testTitle;
            Velocity.UiVal = Properties.Settings.Default.velocity;
            Timeout.UiVal = Properties.Settings.Default.timeout;
            Cycles.UiVal = Properties.Settings.Default.cycles;
            CycleDelaySeconds.UiVal = Properties.Settings.Default.cycleDelaySeconds;
            ReversalVelocity.UiVal = Properties.Settings.Default.reversalVelocity;
            ReversalExtraTimeSeconds.UiVal = Properties.Settings.Default.reversalExtraTimeSeconds;
            ReversalSettleTimeSeconds.UiVal = Properties.Settings.Default.reversalSettleTimeSeconds;
            InitialSetpoint.UiVal = Properties.Settings.Default.initialSetpoint;
            NumberOfSteps.UiVal = Properties.Settings.Default.numberOfSteps;
            StepSize.UiVal = Properties.Settings.Default.stepSize;
            SettleTimeSeconds.UiVal = Properties.Settings.Default.settleTimeSeconds;
            ReversalDistance.UiVal = Properties.Settings.Default.reversalDistance;
            OvershootDistance.UiVal = Properties.Settings.Default.overshootDistance;
            EndSetpoint.UiVal = Properties.Settings.Default.endSetpoint;
            TestType.UiVal = Properties.Settings.Default.testType;
        }

        public void ResetSettings()
        {
            //Reset all settings to default values
            TestTitle.UiVal = "New Test";
            Velocity.UiVal = "0";
            Timeout.UiVal = "0";
            Cycles.UiVal = "1";
            CycleDelaySeconds.UiVal = "0";
            ReversalVelocity.UiVal = "0";
            ReversalExtraTimeSeconds.UiVal = "0";
            ReversalSettleTimeSeconds.UiVal = "0";
            InitialSetpoint.UiVal = "0";
            NumberOfSteps.UiVal = "1";
            StepSize.UiVal = "0";
            SettleTimeSeconds.UiVal = "0";
            ReversalDistance.UiVal = "0";
            OvershootDistance.UiVal = "0";
            EndSetpoint.UiVal = "0";
            TestType.UiVal = TestTypes.NoneSelected.GetStringValue();
        }
       
        public SettingString TestTitle { get; set; } = new("testTitle");
        public SettingDouble Velocity { get; set; } = new("velocity");
        public SettingUint Timeout { get; set; } = new("timeout");
        public SettingUint Cycles { get; set; } = new("cycles");
        public SettingUint CycleDelaySeconds { get; set; } = new("cycleDelaySeconds");
        public SettingDouble ReversalVelocity { get; set; } = new("reversalVelocity");
        public SettingUint ReversalExtraTimeSeconds { get; set; } = new("reversalExtraTimeSeconds");
        public SettingUint ReversalSettleTimeSeconds { get; set; } = new("reversalSettleTimeSeconds");
        public SettingDouble InitialSetpoint { get; set; } = new("initialSetpoint");
        public SettingUint NumberOfSteps { get; set; } = new("numberOfSteps");
        public SettingDouble StepSize { get; set; } = new("stepSize");
        public SettingUint SettleTimeSeconds { get; set; } = new("settleTimeSeconds");
        public SettingDouble ReversalDistance { get; set; } = new("reversalDistance");
        public SettingDouble OvershootDistance = new("overshootDistance");
        public SettingDouble EndSetpoint { get; set; } = new("endSetpoint");

        public SettingTestType TestType { get; set; } = new("testType");

        //Method to import test settings (Export method is in test suite, is this right?)
        public void ImportSettingsXML(string ImportSettingsFile)
        {
            XmlDocument doc = new();
            doc.Load(ImportSettingsFile);
            XmlNode testSettings = doc.SelectSingleNode("Settings");
            TestListItem tli = new("1"); //container
            TestSuite.ImportSingleTestSettings(tli, testSettings);
            TestTitle.UiVal = tli.TestSettings.TestTitle.UiVal;
            Velocity.UiVal = tli.TestSettings.Velocity.UiVal;
            Timeout.UiVal = tli.TestSettings.Timeout.UiVal; ;
            Cycles.UiVal = tli.TestSettings.Cycles.UiVal; ;
            CycleDelaySeconds.UiVal = tli.TestSettings.CycleDelaySeconds.UiVal; ;
            ReversalVelocity.UiVal = tli.TestSettings.ReversalVelocity.UiVal; ;
            ReversalExtraTimeSeconds.UiVal = tli.TestSettings.ReversalExtraTimeSeconds.UiVal; ;
            ReversalSettleTimeSeconds.UiVal = tli.TestSettings.ReversalSettleTimeSeconds.UiVal; ;
            InitialSetpoint.UiVal = tli.TestSettings.InitialSetpoint.UiVal; ;
            NumberOfSteps.UiVal = tli.TestSettings.NumberOfSteps.UiVal; ;
            StepSize.UiVal = tli.TestSettings.StepSize.UiVal; ;
            SettleTimeSeconds.UiVal = tli.TestSettings.SettleTimeSeconds.UiVal; ;
            ReversalDistance.UiVal = tli.TestSettings.ReversalDistance.UiVal; ;
            OvershootDistance.UiVal = tli.TestSettings.OvershootDistance.UiVal; ;
            EndSetpoint.UiVal = tli.TestSettings.EndSetpoint.UiVal;
        }    

        public void ExportSettingsXml(string ExportSettingsFile, string axisNum)
        {
            XmlDocument doc = new();
            XmlNode rootNode = doc.CreateElement("Settings");
            doc.AppendChild(rootNode);
            AddSettingsFields(doc, rootNode, axisNum);
            doc.Save(ExportSettingsFile);
        }

        public void AddSettingsFields(XmlDocument xmlDoc, XmlNode parentNode, string axisNum)
        {
            CreateAndAppendXmlNode(parentNode, xmlDoc, "testType", this.TestType.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "testTitle", this.TestTitle.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "axisId", axisNum.ToString());
            CreateAndAppendXmlNode(parentNode, xmlDoc, "velocity", this.Velocity.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "timeout", this.Timeout.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "cycles", this.Cycles.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "cycleDelaySeconds", this.CycleDelaySeconds.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "reversalVelocity", this.ReversalVelocity.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "reversalExtraTime", this.ReversalExtraTimeSeconds.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "reversalSettleTime", this.ReversalSettleTimeSeconds.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "initialSetpoint", this.InitialSetpoint.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "numberOfSteps", this.NumberOfSteps.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "stepSize", this.StepSize.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "settleTime", this.SettleTimeSeconds.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "reversalDistance", this.ReversalDistance.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "overshootDistance", this.OvershootDistance.UiVal);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "endSetpoint", this.EndSetpoint.UiVal);
        }

        private static void CreateAndAppendXmlNode(XmlNode parentNode, XmlDocument doc, string ndName, string ndValue)
        {
            var node = CreateXmlNode(doc, ndName, ndValue);
            parentNode.AppendChild(node);
        }

        private static XmlNode CreateXmlNode(XmlDocument doc, string ndName, string ndValue)
        {
            XmlNode xmlNode = doc.CreateElement(ndName);
            xmlNode.InnerText = ndValue;
            return xmlNode;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum TestTypes
    {
        [StringValue("EndToEnd")]
        EndToEnd,
        [StringValue("UnidirectionalAccuracy")]
        UnidirectionalAccuracy,
        [StringValue("BidirectionalAccuracy")]
        BidirectionalAccuracy,
        [StringValue("ScalingTest")]
        ScalingTest,
        [StringValue("BacklashDetection")]
        BacklashDetection,
        [StringValue("UserPrompt")]
        UserPrompt,
        [StringValue("NoneSelected")]
        NoneSelected
    }
}
