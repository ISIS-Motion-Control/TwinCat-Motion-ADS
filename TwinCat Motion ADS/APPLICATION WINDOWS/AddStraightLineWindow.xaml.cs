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
    public partial class AddStraightLineWindow : Window
    {
        private ObservableCollection<string> AxisSwitch = new ObservableCollection<string>() { "X", "Y"};
        public AddStraightLineWindow()
        {
            InitializeComponent();
            Combo_AxisSelect.ItemsSource = this.AxisSwitch;
            Combo_AxisSelect.SelectedIndex = 1;
        }
        

        public event EventHandler<NewGraphLineArgs> DialogFinished;
        public void OnDialogFinished()
        {           
            if (DialogFinished != null)
            {
                DialogFinished(this, new NewGraphLineArgs(ColumnTitle.Text, Combo_AxisSelect.SelectedItem as string, double.Parse(ValueTextBox.Text)));
            }               
        }


        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ColumnTitle.Text))
            {
                Console.WriteLine("Series name cannot be empty");
                return;
            }

            if (!double.TryParse(ValueTextBox.Text, out _))
            {
                Console.WriteLine("Value is NaN");
                return;
            }

            OnDialogFinished();
            this.Close();
        }
    }
}
