﻿using Ookii.Dialogs.Wpf;
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

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for NcAxisView.xaml
    /// </summary>
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


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public CsvHelperView()
        {
            InitializeComponent();
            this.DataContext = this;
            windowData = (MainWindow)Application.Current.MainWindow;
            dt = new DataTable();
            csvHeaderList.ItemsSource = DataHeaders;

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
                //NcTestSettings.ImportSettingsXML(selectedFile);
            }
            Console.WriteLine(selectedFile.ToString());
            using (FileStream stream = File.Open(selectedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>();
                using (var dr = new CsvDataReader(csv))
                {
                    
                    dt.Load(dr);
                    csvDataGrid.DataContext = dt.DefaultView;
                    csvDataGrid.Items.Refresh();
                    UpdateDataHeadersList();
                }
            }
            
            
        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            AddErrorCol();
        }

        private void AddErrorCol()
        {
            AddDataColumnWindow dataWindow = new();
            dataWindow.Show();
            //AddGridColumn("Error", typeof(string));
            
            //DataColumn ErrorCol = new("Error", typeof(string));
            //dt.Columns.Add(ErrorCol);
            //OnPropertyChanged(nameof(dt));
            /*foreach (DataRow row in dt.Rows)
            {
                row["Error"] = (double.Parse((string)row["EncoderPosition"]))- (double.Parse((string)row["TargetPosition"]));
            }*/
            //csvDataGrid.Columns.Add(new DataGridTextColumn() { Binding = new Binding("Error"), Header = "Error" });
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
    }
}
