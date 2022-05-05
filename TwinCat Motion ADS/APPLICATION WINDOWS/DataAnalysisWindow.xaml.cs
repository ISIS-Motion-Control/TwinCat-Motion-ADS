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
    /// Interaction logic for DataAnalysisWindow.xaml
    /// </summary>
    public partial class DataAnalysisWindow : Window
    {
        MainWindow wd;

        ObservableCollection<DataAnalysisItem> DataAnalysisCollection = new();

        public DataAnalysisWindow()
        {
            InitializeComponent();
            wd = (MainWindow)App.Current.MainWindow;

            //Add data to ComboBoxes
            Data_A.ItemsSource = new double[] { 1, 2, 3, 4 };
            OpComboBox.ItemsSource = Enum.GetValues(typeof(Operations)).Cast<Operations>();
            Data_B.ItemsSource = new double[] { 1, 2, 3, 4 };

        }

        #region Saving & Loading
        private string saveDirectory;

        private void SelectSaveDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {

            var fbd = new VistaFolderBrowserDialog();
            saveDirectory = string.Empty;
            if (fbd.ShowDialog() == true)
            {
                saveDirectory = fbd.SelectedPath;
            }
            Console.WriteLine(saveDirectory);
        }

        private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            VistaSaveFileDialog fbd = new();
            fbd.AddExtension = true;
            fbd.DefaultExt = ".XML";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                SaveDASuite(selectedFile);
            }
        }
        public void SaveDASuite(string selectedFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("DataAnalysis_Suite");
            xmlDoc.AppendChild(rootNode);

            //Save settings for each DataAnalysisItem

            foreach (DataAnalysisItem dataAnalysisItem in DataAnalysisCollection)
            {
                XmlNode DANode = xmlDoc.CreateElement("DataAnalysis_Item");
                rootNode.AppendChild(DANode);

                AddFields(xmlDoc, dataAnalysisItem, DANode);
            }
            xmlDoc.Save(selectedFile);
        }

        public static void AddFields(XmlDocument xmlDoc, DataAnalysisItem dataAnalysisItem, XmlNode parentNode)
        {
            CreateAndAppendXmlNode(parentNode, xmlDoc, "Data_A", dataAnalysisItem.Data_A);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "Operation", dataAnalysisItem.Operation.ToString());
            CreateAndAppendXmlNode(parentNode, xmlDoc, "Data_A", dataAnalysisItem.Data_A);
            CreateAndAppendXmlNode(parentNode, xmlDoc, "Name", dataAnalysisItem.Name);
        }

        public static void CreateAndAppendXmlNode(XmlNode parentNode, XmlDocument doc, string ndName, string ndValue)
        {
            var node = CreateXmlNode(doc, ndName, ndValue);
            parentNode.AppendChild(node);
        }

        public static XmlNode CreateXmlNode(XmlDocument doc, string ndName, string ndValue)
        {
            XmlNode xmlNode = doc.CreateElement(ndName);
            xmlNode.InnerText = ndValue;
            return xmlNode;
        }

        private void LoadFileBtn_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fbd = new();
            fbd.Filter = "*.XML|*.xml";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                LoadDASuite(selectedFile);
            }
        }
        private void LoadDASuite(string selectedFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(selectedFile);
            XmlNodeList DASuite = xmlDoc.SelectNodes("DataAnalysis_Suite");

            DataAnalysisCollection.Clear();  //clear the current list
            int DACounter = 0;
            //Add each test in turn
            foreach (XmlNode DataAnalysis_Item in DASuite)
            {
                DataAnalysisCollection.Add(new());    //temp axis ID

                //Import all the settings
                ImportSingleDASettings(DataAnalysisCollection[DACounter], DataAnalysis_Item);

                //increment the list counter
                DACounter++;
            }
        }
        public static void ImportSingleDASettings(DataAnalysisItem dali, XmlNode DANode)
        {
            dali.Data_A = DANode.SelectSingleNode("Data_A").InnerText;
            dali.Operation = (Operations)Enum.Parse(typeof(Operations),DANode.SelectSingleNode("Operation").InnerText);
            dali.Data_B = DANode.SelectSingleNode("Data_B").InnerText;
            dali.Name = DANode.SelectSingleNode("Name").InnerText;
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!wd.windowClosing)
            {
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
            }
        }

        private void EnableCheck_Click(object sender, RoutedEventArgs e)
        {
            bool enableFlag;

            if (enableCheck.IsChecked == true)
            {
                enableFlag = true;

            }
            else
            {
                enableFlag = false;
            }
        }

        private void OpComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            

        }

        private void DataAnalysisList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }



        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            DataAnalysisItem dataAnalysisItem = new()
            {
                Data_A = Data_A.Text,
                Operation = (Operations)Enum.Parse(typeof(Operations),OpComboBox.Text),
                Data_B = Data_B.Text,
                Name = DataAnalysisName.Text
            };

            DataAnalysisCollection.Add(dataAnalysisItem);

            string ListText = dataAnalysisItem.Data_A + " " + dataAnalysisItem.Operation + " " + dataAnalysisItem.Data_B + " = " + dataAnalysisItem.Name;
            DataAnalysisList.Items.Add(ListText);

        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            DataAnalysisList.Items.Remove(DataAnalysisList.SelectedItem);
        }

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DataAnalysisCollection.Count; i++)
            {

                StringA = DataAnalysisCollection[i].Data_A;
                StringB = DataAnalysisCollection[i].Data_B;
                OperationType = DataAnalysisCollection[i].Operation;
                DataAnalsysis_Calculation();

                DataAnalysisCollection[i].Value = Value;
                MessageBox.Show(DataAnalysisCollection[i].Value);
            }
            
        }

        public string StringA;
        public string StringB;
        public Operations OperationType;
        public string Value;


        public void DataAnalsysis_Calculation()
        {
            if (StringA != null && StringB != null)
            {
                double Data_A = double.Parse(StringA);
                double Data_B = double.Parse(StringB);
                double _value = 0;

                switch (OperationType)
                {

                    case Operations.NoneSelected:
                        {
                            return;
                        }
                    case Operations.AddValues:
                        {
                            _value = Data_A + Data_B;
                            Value = _value.ToString();
                            return;
                        }
                    case Operations.MinusValues:
                        {
                            _value = Data_A - Data_B;
                            Value = _value.ToString();
                            return;
                        }

                    default:
                        break;

                }
            }
            else { return; };
        }
    }

    public class DataAnalysisItem
    {
        public string Data_A;
        public Operations Operation;
        public string Data_B;
        public string Name;
        public string Value;
    }

    public enum Operations
    {
        NoneSelected,
        AddValues,
        MinusValues
        
    }
}