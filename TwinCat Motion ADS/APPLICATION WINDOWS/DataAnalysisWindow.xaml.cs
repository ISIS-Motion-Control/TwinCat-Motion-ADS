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
            var dataAnalysisItem = new DataAnalysisItem { Data_A = Data_A.Text, Operation = OpComboBox.Text, Data_B = Data_B.Text, Name = DataAnalysisName.Text };

            DataAnalysisCollection.Add(dataAnalysisItem);

            string ListText = dataAnalysisItem.Data_A + " " + dataAnalysisItem.Operation + " " + dataAnalysisItem.Data_B + " = " + dataAnalysisItem.Name;

            DataAnalysisList.Items.Add(ListText);

        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            DataAnalysisList.Items.Remove(DataAnalysisList.SelectedItem);
        }


        
    }

    public class DataAnalysisItem
    {
        public string Data_A;
        public string Operation;
        public string Data_B;
        public string Name;
        public string Value;
    }

    public partial class DataAnalsysis_Calculate
    {
        public string Data_A;
        public string Data_B;
        public Operations OperationType;
        public string Value;
        

        public DataAnalsysis_Calculate()
        {
            switch (OperationType)
            {

                case Operations.NoneSelected:
                    {
                        return;
                    }
                case Operations.AddValues:
                    {
                        Value = AddValues(Data_A, Data_B);

                        return;
                    }
                case Operations.MinusValues:
                    {
                        Value = MinusValues(Data_A, Data_B);
                        return;
                    }

                default:
                    break;
            }


        }


        #region Operations
        public static string AddValues(string StringA, string StringB)
        {
            double Data = double.Parse(StringA) + double.Parse(StringB);
            string Data_Out = Data.ToString("D");
            return Data_Out;
        }
        public static string MinusValues(string StringA, string StringB)
        {
            double Data = double.Parse(StringA) - double.Parse(StringB);
            String Data_Out = Data.ToString("D");
            return Data_Out;
        }
        #endregion
    }

    public enum Operations
    {
        NoneSelected,
        AddValues,
        MinusValues
        
    }
}