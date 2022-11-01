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
    public partial class AddToGraphWindow : Window
    {
        public AddToGraphWindow(ObservableCollection<string> DataHeaders, ObservableCollection<string> FilterValues)
        {
            this.DataHeaders = DataHeaders;
            this.FilterValues = FilterValues;
            InitializeComponent();
            Combo_Header1.ItemsSource = this.DataHeaders;
            Combo_Header2.ItemsSource = this.DataHeaders;
            Combo_Filter.ItemsSource = this.FilterValues;

            foreach(string Header in DataHeaders)
            {
                if(Header == "TargetPosition")
                {
                    Combo_Header2.SelectedItem = "TargetPosition";
                }
            }
        }
        private ObservableCollection<string> DataHeaders;
        private ObservableCollection<string> FilterValues;

        public event EventHandler<NewGraphDataArgs> DialogFinished;
        public void OnDialogFinished()
        {
            if (DialogFinished != null)
                DialogFinished(this, new NewGraphDataArgs(SeriesName.Text, Combo_Header2.SelectedItem as string, Combo_Header1.SelectedItem as string, ((bool)FilterCheckBox.IsChecked), Combo_Filter.SelectedItem as string));
        }


        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(SeriesName.Text) || string.IsNullOrEmpty(Combo_Header1.Text) || string.IsNullOrEmpty(Combo_Header2.Text))
            {
                Console.WriteLine("Invalid input");
                return;
            }
            OnDialogFinished();
            this.Close();
        }
    }
}
