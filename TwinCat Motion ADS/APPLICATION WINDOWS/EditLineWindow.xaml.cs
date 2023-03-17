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
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for AddDataColumnWindow.xaml
    /// </summary>
    public partial class EditLineWindow : Window, INotifyPropertyChanged
    {

        MainWindow wd;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<ISeries> Series { get; set; }
        public ObservableCollection<RectangularSection> Sections { get; set; }

        public ObservableCollection<SKColors> colors;


        private ScatterSeries<ObservablePoint> _CurrentScatter;

        public ScatterSeries<ObservablePoint> CurrentScatter
        {
            get { return _CurrentScatter; }
            set { _CurrentScatter = value; OnPropertyChanged(); }
        }

        public EditLineWindow(ObservableCollection<ISeries> Series, ObservableCollection<RectangularSection> Sections)
        {
            InitializeComponent();
            wd = (MainWindow)App.Current.MainWindow;
            this.Series = Series;
            this.Sections = Sections;
            SeriesListBox.ItemsSource = Series;
            ColourPicker.ItemsSource = typeof(Colors).GetProperties();

        }
        

        public event EventHandler<NewDataArgs> DialogFinished;
        public void OnDialogFinished()
        {
                
        }

        private void SeriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentScatter = SeriesListBox.SelectedItem as ScatterSeries<ObservablePoint>;

            XamlUI.TextboxBinding(SeriesTextbox, CurrentScatter, "Name");
            
            

            //myColour.
           // ColourPicker.SelectedItem = myColour; //((SKColor)((SolidColorPaint)CurrentScatter.Stroke).Color).;
            //Console.WriteLine(((SolidColorPaint)CurrentScatter.Stroke).Color.ToString());
            /*SolidColorPaint linePaint = new SolidColorPaint
            {
                Color = SKColors.Violet,
                StrokeThickness = 3,
                PathEffect = new DashEffect(new float[] { 6, 6 })
            };*/

            //((ScatterSeries<ObservablePoint>)Series[SeriesListBox.SelectedIndex]).Stroke = linePaint;
        }

        private void ColourPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColourPicker.SelectedItem != null)
            {
                var selectedItem = (PropertyInfo)ColourPicker.SelectedItem;
                var color = (Color)selectedItem.GetValue(null, null);

                if(CurrentScatter != null)
                {

                    SolidColorPaint linePaint = new SolidColorPaint
                    {
                        Color = SKColor.Parse(color.ToString()), StrokeThickness = 5

                    };

                    string lowerAlpha = color.ToString();
                    lowerAlpha = lowerAlpha.Remove(0, 3);
                    lowerAlpha = "#40" + lowerAlpha;
                    SolidColorPaint fillPaint = new SolidColorPaint
                    {
                        Color = SKColor.Parse(lowerAlpha),
                    };

                    CurrentScatter.Stroke= linePaint;
                    CurrentScatter.Fill = fillPaint;
                    wd.CsvHelperView.TestChart.UpdateLayout();
                }
                
            }
        }
    }
}
