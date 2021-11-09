using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TwinCat_Motion_ADS
{
    /*
     * This class acts as an interface to other measurement devices to provide a generic user interface.
     * 
     */
    public class MeasurementDevice : INotifyPropertyChanged
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
                            break;
                        }
                        else if(dti.CheckConnected()== false)
                        {
                            _portName = value;
                        }
                        break;
                    case DeviceType.KeyenceTm3000:
                        if (keyence == null)
                        {
                            _portName = value;
                            break;
                        }
                        else if(keyence.CheckConnected() == false)
                        {
                            _portName = value;
                        }
                        break;
                }
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }


        //Return a string of the device type
        public string DeviceTypeString
        {
            get
            {
                if (DeviceType == DeviceType.DigimaticIndicator)
                {
                    return "DigimaticIndicator";
                }
                else if (DeviceType == DeviceType.KeyenceTm3000)
                {
                    return "KeyenceTM3000";
                }
                else
                {
                    return "";
                }
            }
        }
        public string BaudRate
        {
            get
            {
                if (DeviceType == DeviceType.DigimaticIndicator && dti!= null)
                {
                    return dti.BaudRate.ToString();
                }
                else if (DeviceType == DeviceType.KeyenceTm3000 && keyence!= null)
                {
                    return keyence.BaudRate.ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        //public string PortName { get; set; }
        private DigimaticIndicator dti;
        private KeyenceTM3000 keyence;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public bool Connected { get; private set; }


        public MeasurementDevice(string deviceType)
        {
            Connected = false;
            if(deviceType == "DigimaticIndicator")
            {
                DeviceType = DeviceType.DigimaticIndicator;
            }
            else if(deviceType == "KeyenceTM3000")
            {
                DeviceType = DeviceType.KeyenceTm3000;
            }
            else
            {
                DeviceType = DeviceType.NoneSelected;
            }
            Name = "*NEW DEVICE*";
        }

        public void changeDeviceType(string deviceType)
        {
            if (Connected) return;  //Don't change device type if connected to something
            if (deviceType == "DigimaticIndicator")
            {
                DeviceType = DeviceType.DigimaticIndicator;
            }
            if (deviceType == "KeyenceTM3000")
            {
                DeviceType = DeviceType.KeyenceTm3000;
            }
        }

        /// <summary>
        /// Populate a list of COM serial ports
        /// </summary>
        public void UpdatePortList()
        {
            SerialPortList.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                SerialPortList.Add(port);
            }
        }



        /// <summary>
        /// Connect to selected measurement device type
        /// </summary>
        /// <returns></returns>
        public bool ConnectToDevice()
        {
            if (Connected)
            {
                Console.WriteLine("Already connected");
                return false;
            }
            switch (DeviceType)
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
                    if(dti.OpenPort())
                    {
                        Connected = true;
                    }
                    return Connected;
                    
                case DeviceType.KeyenceTm3000:
                    if(keyence==null)
                    {
                        keyence = new KeyenceTM3000(PortName);
                    }
                    else
                    {
                        keyence.Portname = PortName;
                    }
                    if(keyence.OpenPort())
                    {
                        Connected = true;
                    }
                    return Connected;

                default:
                    break;
            }
            Console.WriteLine("No compatible device type selected");
            return false;
        }

        public bool DisconnectFromDevice()
        {
            if (!Connected)
            {
                Console.WriteLine("Nothing to disconnect from");
                return false; //Nothing to disconnect from
            }
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
                        Connected = false;
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
                        Connected = false;
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
            if(!Connected)
            {
                return "No device connected"; //Nothing to disconnect from
            }
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
                    //Keyence returns a string list which must be converted to single string
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
        NoneSelected,
        DigimaticIndicator,  //SERIAL PORT DEVICE
        KeyenceTm3000
    }

    
}
