using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CsvHelper;
using System.Globalization;
using System.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Windows.Media.Imaging;
using System.Windows.Media;


namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for NcAxisView.xaml
    /// </summary>
    /// 

    public partial class CsvHelperView : UserControl
    {
        readonly MainWindow windowData;

        public string selectedFolder = string.Empty;

        private DataTable _dataTable;
        public DataTable dt
        {
            get { return _dataTable; }
            set
            {
                _dataTable = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> DataHeaders = new();

        private SeriesCollection _SeriesCollection = new SeriesCollection()
        {

        };
        public SeriesCollection SeriesCollection
        {
            get { return _SeriesCollection; }
            set
            {
                _SeriesCollection = SeriesCollection;
            }
        }





        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SettingDouble YAxisMax { get; set; } = new("yAxisMax");
        public SettingDouble YAxisMin { get; set; } = new("yAxisMin");
        public SettingDouble YAxisSep { get; set; } = new("yAxisSep");
        public SettingDouble XAxisMax { get; set; } = new("xAxisMax");
        public SettingDouble XAxisMin { get; set; } = new("xAxisMin");
        public SettingDouble XAxisSep { get; set; } = new("xAxisSep");

        public CsvHelperView()
        {
            InitializeComponent();
            this.DataContext = this;
            windowData = (MainWindow)Application.Current.MainWindow;
            dt = new DataTable();
            csvHeaderList.ItemsSource = DataHeaders;
            TestChart.Update();
            TestChart.LegendLocation = LegendLocation.Right;

            YAxisMax.UiVal = Properties.Settings.Default.yAxisMax;
            YAxisMin.UiVal = Properties.Settings.Default.yAxisMin;
            YAxisSep.UiVal = Properties.Settings.Default.yAxisSep;
            XAxisMax.UiVal = Properties.Settings.Default.xAxisMax;
            XAxisMin.UiVal = Properties.Settings.Default.xAxisMin;
            XAxisSep.UiVal = Properties.Settings.Default.xAxisSep;

            XamlUI.TextboxBinding(SettingY_Scale_Max.SettingValue, YAxisMax, "UiVal");
            XamlUI.TextboxBinding(SettingY_Scale_Min.SettingValue, YAxisMin, "UiVal");
            XamlUI.TextboxBinding(SettingY_Scale_Sep.SettingValue, YAxisSep, "UiVal");
            XamlUI.TextboxBinding(SettingX_Scale_Max.SettingValue, XAxisMax, "UiVal");
            XamlUI.TextboxBinding(SettingX_Scale_Min.SettingValue, XAxisMin, "UiVal");
            XamlUI.TextboxBinding(SettingX_Scale_Sep.SettingValue, XAxisSep, "UiVal");
        }

        private void SelectFolderDirectory_Click(object sender, RoutedEventArgs e)
        {
            
        }



        private void LoadCSVFile_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fbd = new();
            fbd.Filter = "*.csv|*.CSV*";
            string selectedFile = "";
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
            }

            if( String.IsNullOrEmpty(selectedFile))
            {
                return;
            }
            Console.WriteLine(selectedFile.ToString());

            using (FileStream stream = File.Open(selectedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>();
                using (var dr = new CsvDataReader(csv))
                {
                    dt.Clear();
                    dt.Columns.Clear();
                    dt.Load(dr);
                    ClearDataGrid();
                    csvDataGrid.ItemsSource = dt.DefaultView;
                    csvDataGrid.Items.Refresh();
                    UpdateDataHeadersList();
                }
            }
            
            
        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            OpenColumnCreationWindow();
        }

        

        private void OpenColumnCreationWindow()
        {
            AddDataColumnWindow dataWindow = new(DataHeaders);
            dataWindow.DialogFinished += new EventHandler<NewDataArgs>(CreateDataColumn);
            dataWindow.Show();
            
        }
        
        public void CreateDataColumn(object sender, NewDataArgs e)
        {
            AddGridColumn(e.ColumnHeader, typeof(string));
            AddDataRowsToColumn(e.ColumnHeader, e.Var1Header, e.Var2Header);
            //UpdateChart(e.ColumnHeader, e.Var2Header);
        }

        

        private void DeleteErrorCol()
        {
            DeleteGridColumn("Error");
        }

        private void AddGridColumn(string columnName, Type datatype)
        {
            //check column name unique
            foreach(DataColumn col in dt.Columns)
            {
                if(col.ColumnName == columnName)
                {
                    Console.WriteLine("Name " + columnName + " already exists in table. Please choose a unique name");
                    return;
                }
            }
            DataColumn dataColumn = new(columnName, datatype);
            dt.Columns.Add(dataColumn);
            csvDataGrid.Columns.Add(new DataGridTextColumn() { Binding = new Binding(columnName), Header = columnName });
            UpdateDataHeadersList();
        }


        private void AddDataRowsToColumn(string columnName, string header1, string header2)
        {
            //Bit too hardcoded atm but a WIP
            
                foreach (DataRow row in dt.Rows)
                {
                    if (double.TryParse((string)row[header1], out _) && double.TryParse((string)row[header2], out _))
                    {
                        row[columnName] = (double.Parse((string)row[header1])) - (double.Parse((string)row[header2]));
                    }
                }          
        }
        
        private void ClearDataGrid()
        {
            csvDataGrid.ItemsSource = null;
            csvDataGrid.Columns.Clear();
            csvDataGrid.Items.Clear();
            csvDataGrid.Items.Refresh();
        }

        private void DeleteGridColumn(string columnName)
        {
            DataColumn delCol = null;
            DataGridColumn delGridCol = null;
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName == columnName)
                {
                    delCol = col;
                }
            }
            foreach (DataGridColumn col in csvDataGrid.Columns)
            {
                if (col.Header.ToString() == columnName)
                {
                    delGridCol = col;
                }
            }
            if (delCol!= null)
            {
                dt.Columns.Remove(delCol);
                csvDataGrid.Columns.Remove(delGridCol);
            }
            UpdateDataHeadersList();
        }
        
        private void UpdateDataHeadersList()
        {
            DataHeaders.Clear();
            foreach (DataColumn col in dt.Columns)
            {
                DataHeaders.Add(col.ColumnName);
            }
        }

        private void Button_Test2_Click(object sender, RoutedEventArgs e)
        {
            DeleteErrorCol();
        }

        private void Button_DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if(csvHeaderList.SelectedItem == null)
            {
                Console.WriteLine("No item selected");
                return;
            }
            DeleteGridColumn(csvHeaderList.SelectedItem.ToString());
        }

        private void Button_ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            SaveToPng(TestChart, "chart.png");
        }

        private void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            EncodeVisual(visual, fileName, encoder);
        }

        private static void EncodeVisual(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);
            using (var stream = File.Create(fileName)) encoder.Save(stream);
        }

        private void Button_ClearGraph_Click(object sender, RoutedEventArgs e)
        {
            SeriesCollection.Clear();
        }






        //Button Press to add data
        private void Button_AddToGraph_Click(object sender, RoutedEventArgs e)
        {
            AddToGraphWindow dataWindow = new(DataHeaders);
            dataWindow.DialogFinished += new EventHandler<NewGraphDataArgs>(AddDataToGraph);
            dataWindow.Show();
        }
     
        //Submitted data event
        public void AddDataToGraph(object sender, NewGraphDataArgs e)
        {
            UpdateChart(e.ColumnHeader, e.Var1Header, e.Var2Header);
        }
        
        //Add to chart
        public void UpdateChart(string seriesName, string xAxis, string yAxis)
        {
            List<ObservablePoint> dataPoints = new();
            foreach (DataRow row in dt.Rows)
            {
                dataPoints.Add(new ObservablePoint(Double.Parse((string)row[yAxis]), Double.Parse((string)row[xAxis])));
            }

            ScatterSeries myLine = new ScatterSeries { Title = seriesName, Values = new ChartValues<ObservablePoint>() };
            myLine.Values.AddRange(dataPoints);
            myLine.PointGeometry = DefaultGeometries.Cross;
            myLine.StrokeThickness = 2;

            SeriesCollection.Add(myLine);
        }

        private void Button_UpdateGraph_Click(object sender, RoutedEventArgs e)
        {
            TestChart.AxisX.Clear();

            TestChart.AxisY.Clear();

            LiveCharts.Wpf.Separator xSep = new LiveCharts.Wpf.Separator() { IsEnabled = true, Step = XAxisSep.Val };
            LiveCharts.Wpf.Separator ySep = new LiveCharts.Wpf.Separator() { IsEnabled = true, Step = YAxisSep.Val };
            TestChart.AxisY.Add(new() { MaxValue = YAxisMax.Val, MinValue = YAxisMin.Val, Separator = ySep });
            TestChart.AxisX.Add(new() { MaxValue = XAxisMax.Val, MinValue = XAxisMin.Val, Separator = xSep });
        }
    }
    public class NewDataArgs : EventArgs
    {
        private readonly string columnHeader;
        private readonly string var1Header;
        private readonly string var2Header;
        public string ColumnHeader { get; private set; }
        public string Var1Header { get; private set; }
        public string Var2Header { get; private set; }

        public NewDataArgs(string columnHeader, string var1Header, string var2Header)
        {
            ColumnHeader = columnHeader;
            Var1Header = var1Header;
            Var2Header = var2Header;
        }
    }
    public class NewGraphDataArgs : EventArgs
    {
        private readonly string columnHeader;
        private readonly string var1Header;
        private readonly string var2Header;
        public string ColumnHeader { get; private set; }
        public string Var1Header { get; private set; }
        public string Var2Header { get; private set; }

        public NewGraphDataArgs(string columnHeader, string var1Header, string var2Header)
        {
            ColumnHeader = columnHeader;
            Var1Header = var1Header;
            Var2Header = var2Header;
        }
    }


}
