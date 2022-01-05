using TwinCat_Motion_ADS.MVVM.View;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using System.Xml.Linq;

namespace TwinCat_Motion_ADS.MVVM.View.Tests
{
    [TestClass()]
    public class TestSuiteTests
    {
        [TestMethod()]
        public void GivenXmlDocUsingCreateXmlNodeThenNodeWithTextInserted()
        {
            string nodeName = "testNodeName";
            string nodeValue = "testNodeValue";
            XmlDocument xmlDoc = new();
            var result = TestSuite.CreateXmlNode(xmlDoc, nodeName, nodeValue);
            Assert.AreEqual(result.Name, nodeName);
            Assert.AreEqual(result.InnerText, nodeValue);
        }

        [TestMethod()]
        public void GivenXmlNodeUsingCreateAndAppendXmlNodeThenChildNodeAddedToParentNode()
        {
            string nodeName = "testNodeName";
            string nodeValue = "testNodeValue";
            XmlDocument xmlDoc = new();
            XmlNode parentNode = xmlDoc.CreateElement("ParentNode");
            TestSuite.CreateAndAppendXmlNode(parentNode, xmlDoc, nodeName, nodeValue);
            Assert.IsNotNull(parentNode.SelectSingleNode(nodeName));
        }

        [TestMethod()]
        public void AddFieldsTest()
        {
            XmlDocument xmlDoc = new();
            XmlNode parentNode = xmlDoc.CreateElement("Test");
            xmlDoc.AppendChild(parentNode);
            TestListItem testListItem = new("1");
            testListItem.TestSettings.ResetSettings();
            string xmlString = string.Format(@"<Test><testType>{14}</testType><testTitle>{0}</testTitle><axisId>{15}</axisId><velocity>{1}</velocity><timeout>{2}</timeout><cycles>{3}</cycles><cycleDelaySeconds>{4}</cycleDelaySeconds><reversalVelocity>{5}</reversalVelocity><reversalExtraTime>{6}</reversalExtraTime><reversalSettleTime>{7}</reversalSettleTime><initialSetpoint>{8}</initialSetpoint><numberOfSteps>{9}</numberOfSteps><stepSize>{10}</stepSize><settleTime>{11}</settleTime><reversalDistance>{12}</reversalDistance><overshootDistance>{13}</overshootDistance></Test>",testListItem.TestSettings.StrTestTitle, testListItem.TestSettings.StrVelocity, testListItem.TestSettings.StrTimeout, testListItem.TestSettings.StrCycles, testListItem.TestSettings.StrCycleDelaySeconds, testListItem.TestSettings.StrReversalVelocity, testListItem.TestSettings.StrReversalExtraTimeSeconds, testListItem.TestSettings.StrReversalSettleTimeSeconds, testListItem.TestSettings.StrInitialSetpoint, testListItem.TestSettings.StrNumberOfSteps, testListItem.TestSettings.StrStepSize, testListItem.TestSettings.StrSettleTimeSeconds, testListItem.TestSettings.StrReversalDistance, testListItem.TestSettings.StrOvershootDistance,"NoneSelected",testListItem.AxisID);

            TestSuite.AddFields(xmlDoc, testListItem, parentNode);

            XmlDocument testAgainstDoc = new();
            testAgainstDoc.LoadXml(xmlString);

            
            Assert.AreEqual(testAgainstDoc.OuterXml, xmlDoc.OuterXml);
        }
    }
}