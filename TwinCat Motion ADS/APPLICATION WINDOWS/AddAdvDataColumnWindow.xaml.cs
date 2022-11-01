using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for AddDataColumnWindow.xaml
    /// </summary>
    public partial class AddAdvDataColumnWindow : Window
    {
        private ObservableCollection<string> DataHeaders = new();
        private ObservableCollection<string> MathsOperators = new ObservableCollection<string>() { "+", "-", "/", "*", "Abs" };
        private const string constString = "CONSTANT";
        public AddAdvDataColumnWindow(ObservableCollection<string> dataHeaders)
        {
            foreach (string header in dataHeaders)
            {
                DataHeaders.Add(header);
            }
            DataHeaders.Add(constString);

            InitializeComponent();
            Combo_Header1.ItemsSource = this.DataHeaders;
            Combo_Header2.ItemsSource = this.DataHeaders;
            Combo_Operator.ItemsSource = this.MathsOperators;

            foreach(string Header in DataHeaders)
            {
                if(Header == "TargetPosition")
                {
                    Combo_Header2.SelectedItem = "TargetPosition";
                }
            }
            YConstantTextBox.IsEnabled = false;
            XConstantTextBox.IsEnabled = false;
        }
        

        public event EventHandler<NewDataArgs> DialogFinished;
        public void OnDialogFinished()
        {
            if (DialogFinished != null)
                DialogFinished(this, new NewDataArgs(ColumnTitle.Text, Combo_Header1.Text, Combo_Header2.Text,"test"));
        }


        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(ColumnTitle.Text) || string.IsNullOrEmpty(Combo_Header1.Text) || string.IsNullOrEmpty(Combo_Header2.Text))
            {
                Console.WriteLine("Invalid input");
                return;
            }
            OnDialogFinished();
            this.Close();
        }

        private void Combo_Header1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(Combo_Header1.SelectedItem as string == constString)
            {
                XConstantTextBox.IsEnabled = true;
            }
            else
            {
                XConstantTextBox.IsEnabled = false;
            }
        }
        private void Combo_Header2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Combo_Header2.SelectedItem as string == constString)
            {
                YConstantTextBox.IsEnabled = true;
            }
            else
            {
                YConstantTextBox.IsEnabled = false;
            }
        }
    }
}
