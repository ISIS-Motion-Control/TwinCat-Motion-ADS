using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Nito.AsyncEx;

namespace TwinCat_Motion_ADS
{
    /*
     * This class acts as an interface to other measurement devices to provide a generic user interface.
     */
    public class MeasurementDevice : INotifyPropertyChanged
    {
        #region properties
        public DeviceTypes DeviceType { get; set; }
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
                    case DeviceTypes.DigimaticIndicator:
                        if(dti.CheckConnected()== false)
                        {
                            _portName = value;
                            OnPropertyChanged();
                        }
                        break;
                    case DeviceTypes.KeyenceTM3000:
                        if(keyence.CheckConnected() == false)
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
                if(DeviceType == DeviceTypes.Beckhoff && !Connected)
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
                if (DeviceType == DeviceTypes.Beckhoff && !Connected)
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
        
        public List<Tuple<string,int>> ChannelList = new();   //Channel list contains name of channel (csv header) and channel number for access

        public bool ReadInProgress { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        
        public string BaudRate
        {
            get
            {
                if (DeviceType == DeviceTypes.DigimaticIndicator)
                {
                    return dti.BaudRate.ToString();
                }
                else if (DeviceType == DeviceTypes.KeyenceTM3000)
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
            private set { 
                _connected = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DeviceTypes> DeviceTypeList;

        #endregion

        public MeasurementDevice(DeviceTypes dType = DeviceTypes.NoneSelected)
        {
            DeviceTypeList = new(Enum.GetValues(typeof(DeviceTypes)).Cast<DeviceTypes>());
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
            changeDeviceType(dType);
        }

        /// <summary>
        /// Update the baud rate of RS232 devices
        /// </summary>
        /// <param name="bRate"></param>
        public void UpdateBaudRate(string bRate)
        {
            if (DeviceType == DeviceTypes.DigimaticIndicator)
            {
                if (!Connected) dti.BaudRate = Convert.ToInt32(bRate);
            }
            if (DeviceType == DeviceTypes.KeyenceTM3000)
            {
                if (!Connected) keyence.BaudRate = Convert.ToInt32(bRate);
            }
        }

        /// <summary>
        /// Change the measurement device type
        /// </summary>
        /// <param name="deviceType"></param>
        public void changeDeviceType(DeviceTypes dtype)
        {
            if (Connected) return;  //Don't change device type if connected to something
            DeviceType = dtype;
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
                case DeviceTypes.DigimaticIndicator:
                    dti.Portname = PortName;
                    if(dti.OpenPort())
                    {
                        Connected = true;
                    }
                    break;

                case DeviceTypes.KeyenceTM3000:
                    keyence.Portname = PortName;                   
                    if(keyence.OpenPort())
                    {
                        Connected = true;
                    }
                    break;

                case DeviceTypes.Beckhoff:
                    if(beckhoffPlc.Connect())
                    {
                        if(beckhoffPlc.IsStateRun())
                        {
                            Connected = true;
                            if(!AsyncContext.Run(beckhoff.CreateHandles))   //Try to create variable handles
                            {
                                Connected = false;
                                beckhoffPlc.Disconnect();
                            }
                        }
                    }
                    break;
                case DeviceTypes.MotionChannel:
                    if(motionChannel.Connect())
                    {
                        Connected = true;
                    }
                    break;
                case DeviceTypes.Timestamp:
                    if(Timestamp.Connect())
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
            if(ReadInProgress)
            {
                Console.WriteLine("Cannot disconnect during read");
                return false;
            }
            switch (DeviceType)
            {
                case DeviceTypes.DigimaticIndicator:                   
                    if(dti.ClosePort())                         //Try to close the port
                    {
                        Console.WriteLine("Port closed");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else if (dti.CheckConnected())              //If we failed to close, is the DTI connected
                    {
                        return false;
                    }
                    else
                    {
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }

                case DeviceTypes.KeyenceTM3000:

                    if (keyence.ClosePort())
                    {
                        Console.WriteLine("Port closed");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else if(keyence.CheckConnected())
                    {
                        return false;
                    }
                    else
                    {
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }

                case DeviceTypes.Beckhoff:
                    if(beckhoffPlc.Disconnect())
                    {
                        Console.WriteLine("Disconnected");
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                    else if (beckhoffPlc.checkConnection())
                    {
                        return false;
                    }
                    else
                    {
                        Connected = false;
                        UpdateChannelList();
                        return true;
                    }
                case DeviceTypes.MotionChannel:
                    if(motionChannel.Disconnect())
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

                case DeviceTypes.Timestamp:
                    if(Timestamp.Disconnect())
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
            if(!Connected)
            {
                return "No device connected"; //Nothing to disconnect from
            }
            ReadInProgress = true;
            string measurement;
            switch (DeviceType)
            {
                case DeviceTypes.DigimaticIndicator:
                    measurement= await dti.GetMeasurementAsync();
                    break;
                case DeviceTypes.KeyenceTM3000:
                    List<string> measures = await keyence.GetAllMeasures();
                    measurement = String.Join(",", measures);    //Keyence returns a string list so we can convert to a single CSV string
                    break;
                case DeviceTypes.Beckhoff:
                    measurement = await beckhoff.ReadChannel(1);
                    break;
                case DeviceTypes.MotionChannel:
                    measurement = await motionChannel.GetMeasurementAsync();
                    break;
                case DeviceTypes.Timestamp:
                    measurement = Timestamp.GetMeasurement();
                    break;
                default:
                    measurement = "";
                    break;
            }
            ReadInProgress = false;
            return measurement;
        }

        public async Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            if(!Connected)
            {
                return "No device connected";
            }
            if(NumberOfChannels==0)
            {
                return "No channels on device";
            }
            ReadInProgress = true;
            string measurement;
            switch (DeviceType)
            {
                case DeviceTypes.DigimaticIndicator:
                    measurement = await dti.GetMeasurementAsync();
                    break;
                case DeviceTypes.Beckhoff:
                    measurement = await beckhoff.ReadChannel(channelNumber);
                    break;
                case DeviceTypes.KeyenceTM3000:
                    measurement = await keyence.GetMeasureAsync(channelNumber);
                    break;
                case DeviceTypes.MotionChannel:
                    measurement = await motionChannel.GetMeasurementAsync();
                    break;
                case DeviceTypes.Timestamp:
                    measurement = Timestamp.GetMeasurement();
                    break;
                default:
                    measurement = "Fail";
                    break;
            }
            ReadInProgress = false;
            return measurement;
        }

        public void UpdateChannelList()
        {
            //Don't do anything if not connected
            ChannelList.Clear();
            NumberOfChannels = 0;
            if (!Connected) return;
                          
            switch (DeviceType)
            {
                case DeviceTypes.DigimaticIndicator:    //Single channel device              
                    Tuple<string, int> t1 = (Name, 1).ToTuple();
                    ChannelList.Add(t1);
                    NumberOfChannels = ChannelList.Count;
                    break;

                case DeviceTypes.KeyenceTM3000: //Multi Channel device
                    keyence.UpdateChannelList();
                    ChannelList = keyence.ChannelList;
                    NumberOfChannels = ChannelList.Count;
                    break;
                    
                case DeviceTypes.Beckhoff:   //Multi-channel device                    
                    beckhoff.UpdateChannelList();
                    ChannelList = beckhoff.ChannelList;
                    NumberOfChannels = ChannelList.Count;
                    break;

                case DeviceTypes.MotionChannel:  //Single channel device
                    Tuple<string, int> t2 = (Name, 1).ToTuple();
                    ChannelList.Add(t2);
                    NumberOfChannels = ChannelList.Count;
                    break;

                case DeviceTypes.Timestamp: //Single channel device
                    Tuple<string, int> t3 = (Name, 1).ToTuple();
                    ChannelList.Add(t3);
                    NumberOfChannels = ChannelList.Count;
                    break;

                default:
                    break;
            }
        }

        public void AddToChannelList(Tuple<string,int> ch)
        {
            ChannelList.Add(ch);
            NumberOfChannels = ChannelList.Count;
        }

        public void RemoveFromChannelList(Tuple<string,int> ch)
        {
            ChannelList.Remove(ch);
            NumberOfChannels = ChannelList.Count;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
