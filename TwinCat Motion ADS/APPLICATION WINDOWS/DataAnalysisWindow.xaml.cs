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

            //OperationsCollection[1].Operation = (Operations)OpComboBox.SelectedItem;

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