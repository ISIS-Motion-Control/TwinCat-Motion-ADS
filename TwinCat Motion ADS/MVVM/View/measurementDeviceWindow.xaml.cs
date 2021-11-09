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
using TwinCat_Motion_ADS;

namespace TwinCat_Motion_ADS.MVVM.View
{
    /// <summary>
    /// Interaction logic for subWindow.xaml
    /// </summary>
    public partial class measurementDeviceWindow : Window
    {
        int DeviceIndex;
        public ObservableCollection<string> DeviceTypeList = new ObservableCollection<string>()
        {
            "",
        "DigimaticIndicator",
        "KeyenceTM3000"
        };
        MeasurementDevice MDevice;
        public measurementDeviceWindow(int deviceIndex, MeasurementDevice mDevice)
        {
            
            InitializeComponent();
            DeviceIndex = deviceIndex;
            MDevice = mDevice;
            //Setup device name bind
            Binding deviceNameBind = new();
            deviceNameBind.Mode = BindingMode.TwoWay;
            deviceNameBind.Source = MDevice;
            deviceNameBind.Path = new PropertyPath("Name");
            deviceNameBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(deviceName, TextBox.TextProperty, deviceNameBind);

            DeviceType.ItemsSource = DeviceTypeList;

            Binding deviceTypeBind = new();
            deviceTypeBind.Mode = BindingMode.OneWay;
            deviceTypeBind.Source = MDevice;
            deviceTypeBind.Path = new PropertyPath("DeviceTypeString");
            deviceTypeBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(DeviceType, ComboBox.SelectedValueProperty, deviceTypeBind);


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((MainWindow)(Application.Current.MainWindow)).measurementMenuItems[DeviceIndex].Header = MDevice.Name;
        }

        private void DeviceType_DropDownClosed(object sender, EventArgs e)
        {
            if(!MDevice.Connected)
            {
                MDevice.changeDeviceType((string)DeviceType.SelectedItem);
            }
            updateWindow();
        }
        private void updateWindow()
        {
            Console.WriteLine(MDevice.DeviceTypeString);
            //if (MDevice.)
        }
    }
}
