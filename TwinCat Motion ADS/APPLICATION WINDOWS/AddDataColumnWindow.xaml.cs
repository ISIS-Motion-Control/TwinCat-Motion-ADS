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
    public partial class AddDataColumnWindow : Window
    {
        public AddDataColumnWindow(ObservableCollection<string> DataHeaders)
        {
            this.DataHeaders = DataHeaders;
            InitializeComponent();
            Combo_Header1.ItemsSource = this.DataHeaders;
            Combo_Header2.ItemsSource = this.DataHeaders;

            foreach(string Header in DataHeaders)
            {
                if(Header == "TargetPosition")
                {
                    Combo_Header2.SelectedItem = "TargetPosition";
                }
            }
        }
        private ObservableCollection<string> DataHeaders;

        public event EventHandler<WindowEventArgs> DialogFinished;
        public void OnDialogFinished()
        {
            if (DialogFinished != null)
                DialogFinished(this, new WindowEventArgs(ColumnTitle.Text, Combo_Header1.Text, Combo_Header2.Text));
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


    }
}
