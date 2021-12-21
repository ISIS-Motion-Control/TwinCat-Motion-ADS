using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

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

        //Device type instances
        private DigimaticIndicator dti = new();
        public KeyenceTM3000 keyence = new();
        private PLC beckhoffPlc { get; set; }
        public Beckhoff beckhoff { get; set; }         //public for now
        public MotionControllerChannel motionChannel = new();
        private TimestampDevice Timestamp = new();
        //require the plc from the main window now
        readonly MainWindow windowData;

        private string _portName;
        public string PortName
        {
            get { return _portName; }
            set
            {
                switch (DeviceType)
                {
                    case DeviceType.DigimaticIndicator:
                        if (dti.CheckConnected() == false)
                        {
                            _portName = value;
                            OnPropertyChanged();
                        }
                        break;
                    case DeviceType.KeyenceTm3000:
                        if (keyence.CheckConnected() == false)
                        {
                            _portName = value;
                            OnPropertyChanged();
                        }
                        break;
                }
            }
        }

        private string _amsNetID;
        public string AmsNetId
        {
            get { return beckhoffPlc.ID; }
            set
            {
                if (DeviceType == DeviceType.Beckhoff && !Connected)
                {
                    _amsNetID = value;
                    beckhoffPlc.ID = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _plcPort;
        public int PlcPort
        {
            get { return _plcPort; }
            set
            {
                if (DeviceType == DeviceType.Beckhoff && !Connected)
                {
                    _plcPort = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _NumberOfChannels;
        public int NumberOfChannels
        {
            get { return _NumberOfChannels; }
            set
            {
                _NumberOfChannels = value;
                OnPropertyChanged();
            }
        }
        public List<Tuple<string, int>> ChannelList = new();   //Channel list contains name of channel (csv header) and channel number for access

        public bool ReadInProgress { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

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
                else if (DeviceType == DeviceType.Beckhoff)
                {
                    return "Beckhoff";
                }
                else if (DeviceType == DeviceType.MotionChannel)
                {
                    return "MotionChannel";
                }
                else if (DeviceType == DeviceType.Timestamp)
                {
                    return "Timestamp";
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
                if (DeviceType == DeviceType.DigimaticIndicator)
                {
                    return dti.BaudRate.ToString();
                }
                else if (DeviceType == DeviceType.KeyenceTm3000)
                {
                    return keyence.BaudRate.ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            private set
            {
                _connected = value;
                OnPropertyChanged();
            }
        }

        public MeasurementDevice(string deviceType)
        {
            ReadInProgress = false;
            //Provide the motionchannel type access to the mainwindow PLC
            windowData = (MainWindow)Application.Current.MainWindow;
            motionChannel.Plc = windowData.Plc;

            //plc specific startup
            beckhoffPlc = new("", 852); //need to create a separate PLC instance for the measurement device to attach to
            beckhoff = new(beckhoffPlc);
            NumberOfChannels = 0;
            Connected = false;
            Name = "*NEW DEVICE*";
            changeDeviceType(deviceType);
        }

        /// <summary>
        /// Update the baud rate of RS232 devices
        /// </summary>
        /// <param name="bRate"></param>
        public void UpdateBaudRate(string bRate)
        {
            if (DeviceType == DeviceType.DigimaticIndicator)
            {
                if (!Connected) dti.BaudRate = Convert.ToInt32(bRate);
            }
            if (DeviceType == DeviceType.KeyenceTm3000)
            {
                if (!Connected) keyence.BaudRate = Convert.ToInt32(bRate);
            }
        }

        /// <summary>
        /// Change the measurement device type
        /// </summary>
        /// <param name="deviceType"></param>
        public void changeDeviceType(string deviceType)
        {
            if (Connected) return;  //Don't change device type if connected to something
            if (deviceType == "DigimaticIndicator")
            {
                DeviceType = DeviceType.DigimaticIndicator;

            }
            else if (deviceType == "KeyenceTM3000")
            {
                DeviceType = DeviceType.KeyenceTm3000;
            }
            else if (deviceType == "Beckhoff")
            {
                DeviceType = DeviceType.Beckhoff;
            }
            else if (deviceType == "MotionChannel")
            {
                DeviceType = DeviceType.MotionChannel;
            }
            else if (deviceType == "Timestamp")
            {
                DeviceType = DeviceType.Timestamp;
            }
            else
            {
                DeviceType = DeviceType.NoneSelected;
            }
            ChannelList.Clear();
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
                Console.WriteLine("Already connected to a device");
                return false;
            }
            switch (DeviceType)
            {
                case DeviceType.DigimaticIndicator:
                    dti.Portname = PortName;
                    if (dti.OpenPort())
                    {
                        Connected = true;
                    }
                    break;

                case DeviceType.KeyenceTm3000:
                    keyence.Portname = PortName;
                    if (keyence.OpenPort())
                    {
                        Connected = true;
                    }
                    break;

                case DeviceType.Beckhoff:
                    if (beckhoffPlc.Connect())
                    {
                        if (beckhoffPlc.IsStateRun())
                        {
                            Connected = true;
                            if (!AsyncContext.Run(beckhoff.CreateHandles))   //Try to create variable handles
                            {
                                Connected = false;
                                beckhoffPlc.Disconnect();
                            }
                        }
                    }
                    break;
                case DeviceType.MotionChannel:
                    if (motionChannel.Connect())
                    {
                        Connected = true;
                    }
                    break;
                case DeviceType.Timestamp:
                    if (Timestamp.Connect())
                    {
                        Connected = true;
                    }
                    break;
                default:
                    break;
            }
            if (Connected)
            {
                return Connected;
            }
            Console.WriteLine("No compatible device type selected");
            return false;
        }

        /// <summary>
        /// Disconnect from the current device
        /// </summary>
        /// <returns></returns>
        public bool DisconnectFromDevice()
        {
            if (!Connected)
            {
                Console.WriteLine("Nothing to disconnect from");
                return false; //Nothing to disconnect from
            }
            if (ReadInProgress)
            {
                Console.WriteLine("Cannot disconnect during read");
                return false;
            }
            switch (DeviceType)
            {
                case DeviceType.DigimaticIndicator:
                    if (dti.ClosePort())                         //Try to close the port
                    {
                        Console.WriteLine("Port closed");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else if (dti.CheckConnected())              //If we failed to close, is the DTI connected
                    {
                        Console.WriteLine("Failed to close");
                        return false;
                    }
                    else
                    {
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }

                case DeviceType.KeyenceTm3000:

                    if (keyence.ClosePort())
                    {
                        Console.WriteLine("Port closed");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else if (keyence.CheckConnected())
                    {
                        Console.WriteLine("Failed to close");
                        return false;
                    }
                    else
                    {
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }

                case DeviceType.Beckhoff:
                    if (beckhoffPlc.Disconnect())
                    {
                        Console.WriteLine("Disconnected");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else if (beckhoffPlc.checkConnection())
                    {
                        Console.WriteLine("Disconnect failed");
                        return false;
                    }
                    else
                    {
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                case DeviceType.MotionChannel:
                    if (motionChannel.Disconnect())
                    {
                        Console.WriteLine("Disconnected");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case DeviceType.Timestamp:
                    if (Timestamp.Disconnect())
                    {
                        Console.WriteLine("Disconnected");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else
                    {
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
            if (!Connected)
            {
                return "No device connected"; //Nothing to disconnect from
            }
            switch (DeviceType)
            {
                case DeviceType.DigimaticIndicator:
                    return await dti.GetMeasurementAsync();

                case DeviceType.KeyenceTm3000:
                    List<string> measures = await keyence.GetAllMeasures();
                    var retstr = String.Join(",", measures);    //Keyence returns a string list so we can convert to a single CSV string
                    return retstr;

                case DeviceType.Beckhoff:
                    return await beckhoff.ReadChannel(1);

                case DeviceType.MotionChannel:
                    return await motionChannel.GetMeasurementAsync();
                case DeviceType.Timestamp:
                    return Timestamp.GetMeasurement();
                default:
                    break;
            }
            Console.WriteLine("No compatible device type selected");
            return string.Empty;
        }

        public async Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            if (!Connected)
            {
                return "No device connected";
            }
            if (NumberOfChannels == 0)
            {
                return "No channels on device";
            }
            ReadInProgress = true;
            string measurement;
            switch (DeviceType)
            {
                case DeviceType.DigimaticIndicator:
                    measurement = await dti.GetMeasurementAsync();
                    ReadInProgress = false;
                    return measurement;

                case DeviceType.Beckhoff:
                    measurement = await beckhoff.ReadChannel(channelNumber);
                    ReadInProgress = false;
                    return measurement;

                case DeviceType.KeyenceTm3000:
                    measurement = await keyence.GetMeasureAsync(channelNumber);
                    ReadInProgress = false;
                    return measurement;

                case DeviceType.MotionChannel:
                    measurement = await motionChannel.GetMeasurementAsync();
                    ReadInProgress = false;
                    return measurement;

                case DeviceType.Timestamp:
                    measurement = Timestamp.GetMeasurement();
                    ReadInProgress = false;
                    return measurement;
                default:
                    break;
            }



            return "Fail";
        }

        public void UpdateChannelList()
        {
            //Don't do anything if not connected
            ChannelList.Clear();
            NumberOfChannels = 0;
            if (!Connected) return;

            switch (DeviceType)
            {
                case DeviceType.DigimaticIndicator:    //Single channel device              
                    Tuple<string, int> t1 = (Name, 1).ToTuple();
                    ChannelList.Add(t1);
                    NumberOfChannels = ChannelList.Count;
                    break;

                case DeviceType.KeyenceTm3000: //Multi Channel device
                    keyence.UpdateChannelList();
                    ChannelList = keyence.ChannelList;
                    NumberOfChannels = ChannelList.Count;
                    break;

                case DeviceType.Beckhoff:   //Multi-channel device                    
                    beckhoff.UpdateChannelList();
                    ChannelList = beckhoff.ChannelList;
                    NumberOfChannels = ChannelList.Count;
                    break;

                case DeviceType.MotionChannel:  //Single channel device
                    Tuple<string, int> t2 = (Name, 1).ToTuple();
                    ChannelList.Add(t2);
                    NumberOfChannels = ChannelList.Count;
                    break;

                case DeviceType.Timestamp: //Single channel device
                    Tuple<string, int> t3 = (Name, 1).ToTuple();
                    ChannelList.Add(t3);
                    NumberOfChannels = ChannelList.Count;
                    break;

                default:
                    break;
            }
        }

        public void AddToChannelList(Tuple<string, int> ch)
        {
            ChannelList.Add(ch);
            NumberOfChannels = ChannelList.Count;
        }

        public void RemoveFromChannelList(Tuple<string, int> ch)
        {
            ChannelList.Remove(ch);
            NumberOfChannels = ChannelList.Count;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public ObservableCollection<string> DeviceTypeList = new ObservableCollection<string>()
        {
            "",
        "DigimaticIndicator",
        "KeyenceTM3000",
        "Beckhoff",
        "MotionChannel",
        "Timestamp"
        };
    }


    public enum DeviceType
    {
        NoneSelected,
        DigimaticIndicator,
        KeyenceTm3000,
        Beckhoff,
        MotionChannel,
        Timestamp
    }



}
