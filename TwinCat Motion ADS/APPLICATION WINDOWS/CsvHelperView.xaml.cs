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

namespace TwinCat_Motion_ADS
{
    /// <summary>
    /// Interaction logic for NcAxisView.xaml
    /// </summary>
    public partial class CsvHelperView : UserControl
    {
        readonly MainWindow windowData;

        public string selectedFolder = string.Empty;
        DataTable dt = new DataTable();

        public CsvHelperView()
        {
            InitializeComponent();
            windowData = (MainWindow)Application.Current.MainWindow;
            

        }

        private void SelectFolderDirectory_Click(object sender, RoutedEventArgs e)
        {
            
        }



        private void LoadSettingsFile_Click(object sender, RoutedEventArgs e)
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
            using (FileStream stream = File.Open(selectedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(stream))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>();
                using (var dr = new CsvDataReader(csv))
                {
                    
                    dt.Load(dr);
                    csvDataGrid.DataContext = dt.DefaultView;
                }
            }
            csvHeaderList.DataContext = dt.Columns;
        }

    }
}
