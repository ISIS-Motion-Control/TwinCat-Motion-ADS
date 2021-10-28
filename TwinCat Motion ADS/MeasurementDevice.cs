using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;

namespace TwinCat_Motion_ADS
{
    /*
     * The purpose of this class is to preempt the addition of other compatible measurement devices.
     * This class acts as an interface to other measurement devices to generalise their user interface.
     * 
     */
    public class MeasurementDevice
    {
        public DeviceType DeviceType { get; set; }
        public ObservableCollection<string> SerialPortList = new();
        private string _portName;

        public string PortName
        {
            get { return _portName; }
            set
            {
                switch (DeviceType)
                {
                    case DeviceType.DigimaticIndicator:
                        if (dti == null)
                        {
                            _portName = value;
                        }
                        break;
                    case DeviceType.KeyenceTm3000:
                        if (keyence == null)
                        {
                            _portName = value;
                        }
                        //Need an else if port open method from each measurement class
                        break;
                }
            }
        }

        //public string PortName { get; set; }
        private DigimaticIndicator dti;
        private KeyenceTM3000 keyence;

        public MeasurementDevice(string deviceType)
        {
            if(deviceType == "DigimaticIndicator")
            {
                DeviceType = DeviceType.DigimaticIndicator;
            }
            if(deviceType == "KeyenceTM3000")
            {
                DeviceType = DeviceType.KeyenceTm3000;
            }
        }

        public void changeDeviceType(string deviceType)
        {
            if (deviceType == "DigimaticIndicator")
            {
                DeviceType = DeviceType.DigimaticIndicator;
            }
            if (deviceType == "KeyenceTM3000")
            {
                DeviceType = DeviceType.KeyenceTm3000;
            }
        }

        public void UpdatePortList()
        {
            SerialPortList.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                SerialPortList.Add(port);
            }
        }

        public bool ConnectToDevice()
        { 
            switch(DeviceType)
            {
                case DeviceType.DigimaticIndicator:
                    if(dti==null)
                    {
                        dti = new DigimaticIndicator(PortName);
                    }
                    else
                    {
                        dti.Portname = PortName;
                    }
                    return dti.OpenPort();
                    
                case DeviceType.KeyenceTm3000:
                    if(keyence==null)
                    {
                        keyence = new KeyenceTM3000(PortName);
                    }
                    else
                    {
                        keyence.Portname = PortName;
                    }
                    return keyence.OpenPort();

                default:
                    break;
            }
            Console.WriteLine("No compatible device type selected");
            return false;
        }

        public bool DisconnectFromDevice()
        {
            switch (DeviceType)
            {
                case DeviceType.DigimaticIndicator:
                    if (dti == null)
                    {
                        return false;
                    }
                    if(dti.ClosePort())
                    {
                        Console.WriteLine("Port closed");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to close");
                        return false;
                    }

                case DeviceType.KeyenceTm3000:
                    if (keyence == null)
                    {
                        return false;
                    }
                    if (keyence.ClosePort())
                    {
                        Console.WriteLine("Port closed");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to close");
                        return false;
                    }

                default:
                    break;
            }
            Console.WriteLine("No compatible device type selected");
            return false;
        }

        public async Task<string> GetMeasurement()
        {
            switch (DeviceType)
            {
                case DeviceType.DigimaticIndicator:
                    if (dti == null)
                    {
                        return "No device connected";
                    }
                    return await dti.GetMeasurementAsync();

                case DeviceType.KeyenceTm3000:
                    if (keyence == null)
                    {
                        return "No device connected";
                    }
                    List<string> measures = await keyence.GetAllMeasures();
                    var retstr = String.Join(",", measures);
                    return retstr;

                default:
                    break;
            }
            Console.WriteLine("No compatible device type selected");
            return string.Empty;
        }


    }


    public enum DeviceType
    {
        DigimaticIndicator,  //SERIAL PORT DEVICE
        KeyenceTm3000
    }

    
}
