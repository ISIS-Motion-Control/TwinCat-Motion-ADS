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
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
//using LiveCharts.Wpf;
//using LiveCharts.Defaults;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting.Effects;

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for NcAxisView.xaml
    /// </summary>
    /// 

    public partial class CsvHelperView : UserControl, INotifyPropertyChanged
    {
        #region Properties

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

        private ObservableCollection<ISeries> _SeriesCollection = new ObservableCollection<ISeries>() { };
        public ObservableCollection<ISeries> SeriesCollection
        {
            get { return _SeriesCollection; }
            set
            {
                _SeriesCollection = value; OnPropertyChanged();
            }
        }

        private ObservableCollection<RectangularSection> _Sections = new ObservableCollection<RectangularSection>() {};
        public ObservableCollection<RectangularSection> Sections
        {
            get { return _Sections; }
            set { _Sections = value; OnPropertyChanged(); }
        }

        //Graph axis properties
        public SettingDouble YAxisMax { get; set; } = new("yAxisMax");
        public SettingDouble YAxisMin { get; set; } = new("yAxisMin");
        public SettingDouble YAxisSep { get; set; } = new("yAxisSep");
        public SettingDouble XAxisMax { get; set; } = new("xAxisMax");
        public SettingDouble XAxisMin { get; set; } = new("xAxisMin");
        public SettingDouble XAxisSep { get; set; } = new("xAxisSep");
        public SettingString YAxisTitle { get; set; } = new("yAxisTitle");
        public SettingString XAxisTitle { get; set; } = new("xAxisTitle");
        public SettingUint YAxisDec { get; set; } = new("yAxisDec");
        public SettingUint XAxisDec { get; set; } = new("xAxisDec");
        #endregion

        #region Constructors
        public CsvHelperView()
        {
            InitializeComponent();
            this.DataContext = this;
            TestChart.DataContext = this;
            dt = new DataTable();   //initisalise new data table
            csvHeaderList.ItemsSource = DataHeaders;    //Update combobox headers source

            //Setup chart
            TestChart.UpdateLayout();
            TestChart.LegendPosition = LegendPosition.Bottom;

            //Populate default values for chart axis properties
            YAxisMax.UiVal = Properties.Settings.Default.yAxisMax;
            YAxisMin.UiVal = Properties.Settings.Default.yAxisMin;
            YAxisSep.UiVal = Properties.Settings.Default.yAxisSep;
            YAxisTitle.UiVal = Properties.Settings.Default.yAxisTitle;
            YAxisDec.UiVal = Properties.Settings.Default.yAxisDec;
            XAxisMax.UiVal = Properties.Settings.Default.xAxisMax;
            XAxisMin.UiVal = Properties.Settings.Default.xAxisMin;
            XAxisSep.UiVal = Properties.Settings.Default.xAxisSep;
            XAxisTitle.UiVal = Properties.Settings.Default.xAxisTitle;
            XAxisDec.UiVal = Properties.Settings.Default.xAxisDec;

            //Bind UI textboxes to axis properties
            XamlUI.TextboxBinding(SettingY_Scale_Max, YAxisMax, "UiVal");
            XamlUI.TextboxBinding(SettingY_Scale_Min, YAxisMin, "UiVal");
            XamlUI.TextboxBinding(SettingY_Scale_Sep, YAxisSep, "UiVal");
            XamlUI.TextboxBinding(SettingY_Title, YAxisTitle, "UiVal");
            XamlUI.TextboxBinding(SettingX_Scale_Max, XAxisMax, "UiVal");
            XamlUI.TextboxBinding(SettingX_Scale_Min, XAxisMin, "UiVal");
            XamlUI.TextboxBinding(SettingX_Scale_Sep, XAxisSep, "UiVal");
            XamlUI.TextboxBinding(SettingX_Title, XAxisTitle, "UiVal");
            XamlUI.TextboxBinding(SettingY_Scale_Dec, YAxisDec, "UiVal");
            XamlUI.TextboxBinding(SettingX_Scale_Dec, XAxisDec, "UiVal");
        }
        #endregion


        #region ButtonClicks

        private void SelectFolderDirectory_Click(object sender, RoutedEventArgs e)
        {

            var fbd = new VistaFolderBrowserDialog();
            selectedFolder = String.Empty;
            if (fbd.ShowDialog() == true)
            {
                selectedFolder = fbd.SelectedPath;
            }
            Console.WriteLine(selectedFolder);
        }

        private void Button_ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            VistaSaveFileDialog fbd = new();
            fbd.AddExtension = true;
            fbd.DefaultExt = ".csv";
            fbd.Filter = "CSV file (*.csv)|*.*";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                ExportCsv(selectedFile);
            }
        }

        private void ExportCsv(string filepath)
        {
            using (var writer = new StreamWriter(filepath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                foreach (DataColumn column in dt.Columns)
                {
                    csv.WriteField(column.ColumnName);
                }
                csv.NextRecord();

                // Write row values
                foreach (DataRow row in dt.Rows)
                {
                    for (var i = 0; i < dt.Columns.Count; i++)
                    {
                        csv.WriteField(row[i]);
                    }
                    csv.NextRecord();
                }
            }
        }

        //Open a CSV file
        private void LoadCSVFile_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog fbd = new();
            fbd.Filter = "*.csv|*.CSV*";
            string selectedFile = "";
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
            }

            //If no file selected return
            if (String.IsNullOrEmpty(selectedFile))
            {
                return;
            }

            //Populate the datatable with CSV data and update the UI data grid
            try
            {
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

                        //schema by default sets all columns to readonly
                        foreach (DataColumn col in dt.Columns) col.ReadOnly = false;


                        ClearDataGrid();
                        csvDataGrid.ItemsSource = dt.DefaultView;
                        csvDataGrid.Items.Refresh();
                        UpdateDataHeadersList();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to import, check file exists or is not already open");
                return;
            }
            
        }

        private void Button_AddError_Click(object sender, RoutedEventArgs e)
        {
            OpenErrorCreationWindow();
        }

        private void Button_AddDataCol_Click(object sender, RoutedEventArgs e)
        {
            OpenColumnCreationWindow();
        }

        private void Button_DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (csvHeaderList.SelectedItem == null)
            {
                Console.WriteLine("No item selected");
                return;
            }
            DeleteGridColumn(csvHeaderList.SelectedItem.ToString());
        }

        private void Button_ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            VistaSaveFileDialog fbd = new();
            fbd.AddExtension = true;
            fbd.DefaultExt = ".png";
            fbd.Filter = "Image file (*.png)|*.*";
            string selectedFile;
            if (fbd.ShowDialog() == true)
            {
                selectedFile = fbd.FileName;
                SaveToPng(ChartArea, selectedFile);
            }          
        }

        private void Button_ClearGraph_Click(object sender, RoutedEventArgs e)
        {
            SeriesCollection.Clear();
            Sections.Clear();
        }


        private const string addAll = "Add all";
        //Button Press to add data
        private void Button_AddToGraph_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<string> filterValues = new();

            foreach(DataRow row in dt.Rows)
            {
                foreach(DataColumn col in dt.Columns)
                {
                    if (col.ColumnName == "Status")
                    {
                        bool matchFound = false;
                        foreach(string val in filterValues)
                        {
                            if(val == (string)row["Status"])
                            {
                                matchFound = true;
                            }
                        }
                        if(!matchFound)
                        {
                            filterValues.Add((string)row["Status"]);
                        }                      
                    }
                }
            }
            filterValues.Add(addAll);


            AddToGraphWindow dataWindow = new(DataHeaders, filterValues);
            dataWindow.DialogFinished += new EventHandler<NewGraphDataArgs>(AddDataToGraph);
            dataWindow.Show();
        }

        private void Button_AddStraightLine_Click(object sender, RoutedEventArgs e)
        {
            AddStraightLineWindow straightLineWindow = new();
            straightLineWindow.DialogFinished += new EventHandler<NewGraphLineArgs>(AddStraightLine);
            straightLineWindow.Show();
        }

        

        private void Button_UpdateGraph_Click(object sender, RoutedEventArgs e)
        {
            UpdateGraphAxes();
        }

        

        private void Button_TogglePerformanceVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (performanceStackPanel.Visibility == Visibility.Visible)
            {
                performanceStackPanel.Visibility = Visibility.Collapsed;
                return;
            }
            if (performanceStackPanel.Visibility == Visibility.Collapsed)
            {
                performanceStackPanel.Visibility = Visibility.Visible;
                return;
            }
        }
        private void Button_CalcAcc_Click(object sender, RoutedEventArgs e)
        {
            if (csvHeaderList.SelectedItem == null)
            {
                Console.WriteLine("No item selected");
                return;
            }
            CalculateAccuracy(csvHeaderList.SelectedItem.ToString());
            CalculateRepeatability(csvHeaderList.SelectedItem.ToString(), "TargetPosition");
        }
        #endregion





        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }






        private Func<double, string> StringDecimalFormat(uint places)
        {
            switch (places)
            {
                case 0:
                    return (x) => string.Format("{0:0}", x);
                case 1:
                    return (x) => string.Format("{0:0.0}", x);
                case 2:
                    return (x) => string.Format("{0:0.00}", x);
                case 3:
                    return (x) => string.Format("{0:0.000}", x);
                case 4:
                    return (x) => string.Format("{0:0.0000}", x);
                case 5:
                    return (x) => string.Format("{0:0.00000}", x);
                case 6:
                    return (x) => string.Format("{0:0.000000}", x);

                default:
                    return (x) => string.Format("{0:0}", x);
            }
        }


        private void OpenErrorCreationWindow()
        {
            AddDataColumnWindow dataWindow = new(DataHeaders);
            dataWindow.DialogFinished += new EventHandler<NewDataArgs>(CreateDataColumn);
            dataWindow.Show();
            
        }
        private void OpenColumnCreationWindow()
        {
            AddAdvDataColumnWindow dataWindow = new(DataHeaders);
            dataWindow.DialogFinished += new EventHandler<NewDataArgs>(CreateDataColumn);
            dataWindow.Show();

        }

        public void CreateDataColumn(object sender, NewDataArgs e)
        {
            foreach(DataColumn col in dt.Columns)
            {
                if (col.ColumnName == e.ColumnHeader)
                {
                    Console.WriteLine("Unique header required");
                    return;
                }
            }
            AddGridColumn(e.ColumnHeader, typeof(string));

            AddDataRowsToColumn(e);

            
        }

        private void AddDataRowsToColumn(NewDataArgs e)
        {
            //default error condition
            if (e.MathsOperator == null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (double.TryParse((string)row[e.Var1Header], out _) && double.TryParse((string)row[e.Var2Header], out _))
                    {
                        row[e.ColumnHeader] = (double.Parse((string)row[e.Var1Header])) - (double.Parse((string)row[e.Var2Header]));
                    }
                }
            }
            //Advanced column creation
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    double val1 = 0;
                    double val2 = 0;
                    if (e.Var1Header == "CONSTANT")
                    {
                        val1 = e.Var1Val;
                    }
                    else if (double.TryParse((string)row[e.Var1Header], out _))
                    {
                        val1 = double.Parse((string)row[e.Var1Header]);
                    }

                    if (e.Var2Header == "CONSTANT")
                    {
                        val2 = e.Var2Val;
                    }
                    else if (double.TryParse((string)row[e.Var2Header], out _))
                    {
                        val2 = double.Parse((string)row[e.Var2Header]);
                    }

                    switch (e.MathsOperator)
                    {
                        case "+":
                            row[e.ColumnHeader] = val1 + val2;
                            break;

                        case "-":
                            row[e.ColumnHeader] = val1 - val2;
                            break;

                        case "/":
                            if(val2 == 0)
                            {
                                break;
                            }
                            row[e.ColumnHeader] = val1 / val2;
                            break;

                        case "*":
                            row[e.ColumnHeader] = val1 * val2;
                            break;
                    }

                }
            }
            
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
            csvDataGrid.ItemsSource = null;
            csvDataGrid.ItemsSource = dt.DefaultView;
            //csvDataGrid.Columns.Add(new DataGridTextColumn() { Binding = new Binding(columnName), Header = columnName });
            UpdateDataHeadersList();
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



        

        private void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            EncodeVisual(visual, fileName, encoder);
        }

        private static void EncodeVisual(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            Console.WriteLine((int)visual.Width);
            var bitmap = new RenderTargetBitmap((int)(visual.RenderSize.Width * 1.35), (int)(visual.RenderSize.Height * 1.35), 128, 128, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);
            using (var stream = File.Create(fileName)) encoder.Save(stream);
        }

        
        
        
       
     
        //Submitted data event
        public void AddDataToGraph(object sender, NewGraphDataArgs e)
        {
            //THIS NEEDS SOME SERIOUS CLEAN UP
            ObservableCollection<string> filterValues = new();

            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.ColumnName == "Status")
                    {
                        bool matchFound = false;
                        foreach (string val in filterValues)
                        {
                            if (val == (string)row["Status"])
                            {
                                matchFound = true;
                            }
                        }
                        if (!matchFound)
                        {
                            filterValues.Add((string)row["Status"]);
                        }
                    }
                }
            }

            if (e.FilterData && e.FilterValue == addAll)
            {
                foreach (string filter in filterValues)
                {
                    List<ObservablePoint> dataPoints = new();
                    foreach (DataRow row in dt.Rows)
                    {
                        if ((string)row["Status"] == filter)
                        {
                            dataPoints.Add(new ObservablePoint(Double.Parse((string)row[e.XVarHeader]), Double.Parse((string)row[e.YVarHeader])));
                        }
                    }

                    ScatterSeries<ObservablePoint> myLine = new ScatterSeries<ObservablePoint> { Name = e.SeriesName + " (" + filter + ")", Values = new List<ObservablePoint>() };
                    myLine.Values = (dataPoints);
                    myLine.GeometrySize = 2;

                    //myLine.PointGeometry = DefaultGeometries.Cross;
                    //myLine.StrokeThickness = 2;

                    SeriesCollection.Add(myLine);
                }
            }
            else
            {
                List<ObservablePoint> dataPoints = new();
                foreach (DataRow row in dt.Rows)
                {
                    if (e.FilterData)
                    {
                        if ((string)row["Status"] == e.FilterValue)
                        {
                            dataPoints.Add(new ObservablePoint(Double.Parse((string)row[e.XVarHeader]), Double.Parse((string)row[e.YVarHeader])));
                        }
                    }
                    else
                    {
                        dataPoints.Add(new ObservablePoint(Double.Parse((string)row[e.XVarHeader]), Double.Parse((string)row[e.YVarHeader])));
                    }

                }

                ScatterSeries<ObservablePoint> myLine = new ScatterSeries<ObservablePoint> { Name = e.SeriesName, Values = new List<ObservablePoint>() };
                myLine.Values = (dataPoints);
                //myLine.PointGeometry = DefaultGeometries.Cross;
                myLine.GeometrySize = 2;

                SeriesCollection.Add(myLine);
                UpdateGraphAxes();
            }
        }

        private void AddStraightLine(object sender, NewGraphLineArgs e)
        {
            RectangularSection newSection = new();
            if(e.AxisSelection == "Y")
            {
                newSection.Yi = e.Value;
                newSection.Yj = e.Value;
            }
            else if (e.AxisSelection == "X")
            {
                newSection.Xi = e.Value;
                newSection.Xj = e.Value;
            }

            newSection.Stroke = new SolidColorPaint
            {
                Color = SKColors.Red,
                StrokeThickness = 3,
                PathEffect = new DashEffect(new float[] { 6, 6 })
            };
            Sections.Add(newSection);
            TestChart.Sections = Sections;
            //LiveCharts.Wpf.AxisSection axisSection = new();
            //axisSection.Value = e.Value;
            //TestChart.AxisY[0].Sections.Add(axisSection);

        }


        

        public AccuracyLimits CalculateAccuracy(string columnName)
        {
            
            double maxVal = 0;
            double minVal = 0;
            double accuracyVal;
            

            foreach (DataRow row in dt.Rows)
            {
                if (double.TryParse((string)row[columnName], out _))
                {
                    if (double.Parse((string)row[columnName]) > maxVal ) maxVal = double.Parse((string)row[columnName]);
                    if (double.Parse((string)row[columnName]) < minVal ) minVal = double.Parse((string)row[columnName]);
                }
                
            }
            accuracyVal = maxVal - minVal;
            AccuracyLimits accuracyLimits = new(maxVal, minVal, accuracyVal);
            Console.WriteLine(accuracyVal);
            string accuracyString = string.Format("{0:0.000}", accuracyVal);
            AccuracyVal.Text = accuracyString;
            return accuracyLimits;
        }

        public RepeatabilityMeasure CalculateRepeatability(string errorColumn, string targetColumn)
        {
            //want to group things by their target
            ObservableCollection<RepeatabilityMeasure> repeatabilityMeasures = new ObservableCollection<RepeatabilityMeasure>();

            foreach( DataRow row in dt.Rows)
            {
                bool matchFound = false;
                foreach(RepeatabilityMeasure targetRow in repeatabilityMeasures)
                {
                    if(targetRow.TargetPosition == double.Parse((string)row[targetColumn]))
                    {
                        matchFound = true; //Don't need to add a new entry
                        if (double.Parse((string)row[errorColumn]) > targetRow.MaxError) targetRow.MaxError = double.Parse((string)row[errorColumn]);
                        if (double.Parse((string)row[errorColumn]) < targetRow.MinError) targetRow.MinError = double.Parse((string)row[errorColumn]);
                    }
                }
                if(!matchFound)
                {
                    repeatabilityMeasures.Add(new(double.Parse((string)row[targetColumn]), double.Parse((string)row[errorColumn]), double.Parse((string)row[errorColumn])));
                }
            }

            RepeatabilityMeasure worseRepeatability = new(0,0,0);
            double? repeatVal = 999; 
            Console.WriteLine("List size is: " + repeatabilityMeasures.Count);
            foreach(RepeatabilityMeasure measure in repeatabilityMeasures)
            {
                measure.Repeatability = measure.MaxError - measure.MinError;

                if (measure.Repeatability > repeatVal || repeatVal == 999)
                {
                    repeatVal = measure.Repeatability;
                    worseRepeatability.TargetPosition = measure.TargetPosition;
                    worseRepeatability.MinError = measure.MinError;
                    worseRepeatability.MaxError = measure.MaxError;
                    worseRepeatability.Repeatability = measure.Repeatability;
                }


            }

            Console.WriteLine(repeatVal);
            string repeatabilityString = string.Format("{0:0.000}", repeatVal);
            RepeatabilityVal.Text = repeatabilityString;
            return worseRepeatability;
        }

        private void UpdateGraphAxes()
        {
            Func<double, string> xFormat = StringDecimalFormat(XAxisDec.Val);
            Func<double, string> yFormat = StringDecimalFormat(YAxisDec.Val);

            if(YAxisMax.Val<YAxisMin.Val)
            {
                Console.WriteLine("YMax can't be less than YMin");
                return;
            }
            if(YAxisMin.Val==YAxisMax.Val)
            {
                Console.WriteLine("YMax can't be the same as YMin");
                return; 
            }
            if (XAxisMax.Val < XAxisMin.Val)
            {
                Console.WriteLine("XMax can't be less than XMin");
                return;
            }
            if (XAxisMin.Val == XAxisMax.Val)
            {
                Console.WriteLine("XMax can't be the same as XMin");
                return;
            }


            Axis yAxis = new()
            {

                Labeler = yFormat,
                Name = YAxisTitle.Val,
                TextSize = 12,
                MaxLimit = YAxisMax.Val,
                MinLimit = YAxisMin.Val,
                MinStep = YAxisSep.Val,
                ForceStepToMin = true
            };
            Axis xAxis = new()
            {

                Labeler = xFormat,
                Name = XAxisTitle.Val,
                TextSize = 12,
                MaxLimit = XAxisMax.Val,
                MinLimit = XAxisMin.Val,
                MinStep = XAxisSep.Val,
                ForceStepToMin = true
            };


            TestChart.YAxes = new List<Axis>() { yAxis };
            TestChart.XAxes = new List<Axis>() { xAxis };
            //TestChart.AxisX.Add(new() { LabelFormatter = xFormat, Title = "\n" + XAxisTitle.Val, FontSize = 12, MaxValue = XAxisMax.Val, MinValue = XAxisMin.Val, Separator = XAxisSep.Val });

        }

        private void Button_AddPerformanceLines_Click(object sender, RoutedEventArgs e)
        {
            if (csvHeaderList.SelectedItem == null)
            {
                Console.WriteLine("No header item selected");
                return;
            }
            RepeatabilityMeasure repeatability = CalculateRepeatability(csvHeaderList.SelectedItem.ToString(), "TargetPosition");
            AccuracyLimits accuracy = CalculateAccuracy(csvHeaderList.SelectedItem.ToString());

            SolidColorPaint linePaint = new SolidColorPaint
            {
                Color = SKColors.Red,
                StrokeThickness = 3,
                PathEffect = new DashEffect(new float[] { 6, 6 })
            };


            RectangularSection repeatabilitySection = new RectangularSection()
            {
                Xi = repeatability.TargetPosition,
                Xj = repeatability.TargetPosition,
                Yi = repeatability.MinError,
                Yj = repeatability.MaxError
            };

            repeatabilitySection.Stroke = linePaint;


            RectangularSection accuracyHighSection = new RectangularSection()
            {
                Yi = accuracy.MaxError,
                Yj = accuracy.MaxError,
                Stroke = linePaint
                
            };
            RectangularSection accuracyLowSection = new RectangularSection()
            {
                Yi = accuracy.MinError,
                Yj = accuracy.MinError,
                Stroke = linePaint
            };
            Sections.Add(repeatabilitySection);
            Sections.Add(accuracyHighSection);
            Sections.Add(accuracyLowSection);

            ///////
            ///

            /*RectangularSection newSection = new();
            if (e.AxisSelection == "Y")
            {
                newSection.Yi = e.Value;
                newSection.Yj = e.Value;
            }
            else if (e.AxisSelection == "X")
            {
                newSection.Xi = e.Value;
                newSection.Xj = e.Value;
            }

            newSection.Stroke = new SolidColorPaint
            {
                Color = SKColors.Red,
                StrokeThickness = 3,
                PathEffect = new DashEffect(new float[] { 6, 6 })
            };
            Sections.Add(newSection);
            TestChart.Sections = Sections;*/
        }

        private void Button_LineSettings_Click(object sender, RoutedEventArgs e)
        {
            EditLineWindow editWindow = new(SeriesCollection, Sections);
            editWindow.Show();
        }
    }
    public class NewDataArgs : EventArgs
    {
        public string ColumnHeader { get; private set; }
        public string Var1Header { get; private set; }
        public string Var2Header { get; private set; }
        public string MathsOperator { get; private set; }
        public double Var1Val { get; private set; }
        public double Var2Val { get; private set; }   

        public NewDataArgs(string columnHeader, string var1Header, string var2Header)
        {
            ColumnHeader = columnHeader;
            Var1Header = var1Header;
            Var2Header = var2Header;
        }
        public NewDataArgs(string columnHeader, string var1Header, double var1Val, string var2Header, double var2Val, string mathsOperator)
        {
            ColumnHeader = columnHeader;
            Var1Header = var1Header;
            Var2Header = var2Header;
            MathsOperator = mathsOperator;
            Var1Val = var1Val;
            Var2Val = var2Val;
        }
    }
    public class NewGraphDataArgs : EventArgs
    {
        public string SeriesName { get; private set; }
        public string XVarHeader { get; private set; }
        public string YVarHeader { get; private set; }
        public bool FilterData { get; private set; }
        public string FilterValue { get; private set; }

        public NewGraphDataArgs(string columnHeader, string xVarHeader, string yVarHeader, bool filterData, string filterValue)
        {
            SeriesName = columnHeader;
            XVarHeader = xVarHeader;
            YVarHeader = yVarHeader;
            FilterData = filterData;
            FilterValue = filterValue;
        }
    }
    public class NewGraphLineArgs : EventArgs
    {
        public string SeriesName { get; private set; }
        public string AxisSelection { get; private set; }
        public double Value { get; private set; }


        public NewGraphLineArgs(string seriesName, string axisSelection, double value)
        {
            SeriesName = seriesName;
            AxisSelection = axisSelection;
            Value = value;

        }
    }

    public class RepeatabilityMeasure
    {
        public double TargetPosition;
        public double MaxError = 999;
        public double MinError = 999;
        public double Repeatability;

        public RepeatabilityMeasure(double targetPosition, double maxError, double minError)
        {
            TargetPosition = targetPosition;
            MaxError = maxError;
            MinError = minError;
        }
    }

    public class AccuracyLimits
    {
        public double MaxError = 999;
        public double MinError = 999;
        public double Accuracy = 0;

        public AccuracyLimits(double maxError, double minError, double accuracy)
        {
            MaxError = maxError;
            MinError = minError;
            Accuracy = accuracy;
        }
    }


}
