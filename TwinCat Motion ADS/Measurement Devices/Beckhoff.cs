using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwinCAT.Ads;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Nito.AsyncEx;

namespace TwinCat_Motion_ADS
{
    public class Beckhoff : INotifyPropertyChanged
    {
        public PLC Plc { get; set; }

        //Constants for number of channels of each sensor type, property allows external access to compile time constant
        const int _DIGITAL_INPUT_CHANNELS = 8;
        public int DIGITAL_INPUT_CHANNELS { get { return _DIGITAL_INPUT_CHANNELS; } }
        public bool[] DigitalInputConnected { get; set; }   //Connection status of channels
        private uint[] DigitalInputHandle;                  //PLC handle for channel

        const int _PT100_CHANNELS = 4;       
        public int PT100_CHANNELS { get { return _PT100_CHANNELS; } }
        public bool[] PT100Connected { get; set; }          //Connection status of channels
        private uint[] PT100Handle;                         //PLC handle for channel

        //Publicly accessible channel list for device
        public List<Tuple<string, int>> ChannelList = new();

        public Beckhoff(PLC plc)
        {
            //Setup channel arrays
            DigitalInputConnected = new bool[DIGITAL_INPUT_CHANNELS];
            DigitalInputHandle = new uint[DIGITAL_INPUT_CHANNELS];

            PT100Connected = new bool[PT100_CHANNELS];
            PT100Handle = new uint[PT100_CHANNELS];
            
            Plc = plc;
        }

        //Read digital input channel
        public async Task<string> ReadDigitalInput(int channel)
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(DigitalInputHandle[channel-1], CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig "+ channel+" Read failure*";
            }
        }

        //Read pt100 channel
        public async Task<string> ReadPt100(int channel)
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<double>(PT100Handle[channel-1], CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Pt100-"+channel+" Read failure*";
            }
        }

        //Generic channel measurement method
        public async Task<string> ReadChannel(int channel)
        {
            //Channels are ordered : Digital Inputs => PT100s => Unused
            if(channel == 0)
            {
                return "Not a valid channel";
            }

            if(channel<=DIGITAL_INPUT_CHANNELS)
            {
                return await ReadDigitalInput(channel);
            }
            channel -= DIGITAL_INPUT_CHANNELS;  //If we didn't match a channel, reduce the input number

            if(channel<=PT100_CHANNELS)
            {
                return await ReadPt100(channel);
            }

            return "Not a valid channel";
        }

        //Create all handles
        public async Task<bool> CreateHandles()
        {
            try
            {
                for(int i = 1; i<=DIGITAL_INPUT_CHANNELS;i++)
                {
                    await CreateDigitalInputHandle(i);
                }
                for(int i = 1; i<=PT100_CHANNELS;i++)
                {
                    await CreatePT100Handle(i);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Create digital input handle
        public async Task<bool> CreateDigitalInputHandle(int channel)
        {
            try
            {
                ResultHandle rh = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput" + channel, CancellationToken.None);
                DigitalInputHandle[channel - 1] = rh.Handle;
                return true;
            }
            catch
            {
                Console.WriteLine("Failed");
                return false;
            }
        }

        //Create PT100 handle
        public async Task<bool> CreatePT100Handle(int channel)
        {
            try
            {
                ResultHandle rh = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.Pt" + channel, CancellationToken.None);
                PT100Handle[channel - 1] = rh.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Update internal channel list
        public void UpdateChannelList()
        {
            ChannelList.Clear();
            Tuple<string, int> tmp;

            for(int i=1; i<= DIGITAL_INPUT_CHANNELS; i++)
            {
                if(DigitalInputConnected[i-1])
                {
                    tmp = ("Dig" + i, i).ToTuple();
                    ChannelList.Add(tmp);
                }
            }
            for(int i=1; i<= PT100_CHANNELS; i++)
            {
                if(PT100Connected[i-1])
                {
                    tmp = ("Pt" + i, (i + DIGITAL_INPUT_CHANNELS)).ToTuple();   //Pt100 access channels start after the digital inputs
                    ChannelList.Add(tmp);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }


    public class Beckhoff_V2 : BaseMeasurementDevice, I_MeasurementDevice
    {
        public PLC Plc { get; set; }

        //Constants for number of channels of each sensor type, property allows external access to compile time constant
        const int _DIGITAL_INPUT_CHANNELS = 8;
        public int DIGITAL_INPUT_CHANNELS { get { return _DIGITAL_INPUT_CHANNELS; } }
        public bool[] DigitalInputConnected { get; set; }   //Connection status of channels
        private uint[] DigitalInputHandle;                  //PLC handle for channel

        const int _PT100_CHANNELS = 4;
        public int PT100_CHANNELS { get { return _PT100_CHANNELS; } }
        public bool[] PT100Connected { get; set; }          //Connection status of channels
        private uint[] PT100Handle;                         //PLC handle for channel


        public Beckhoff_V2()
        {
            //Setup channel arrays
            DigitalInputConnected = new bool[DIGITAL_INPUT_CHANNELS];
            DigitalInputHandle = new uint[DIGITAL_INPUT_CHANNELS];

            PT100Connected = new bool[PT100_CHANNELS];
            PT100Handle = new uint[PT100_CHANNELS];
            DeviceType = DeviceTypes.Beckhoff;
            Plc = new("", 852);
            Plc.Port = 852;

        }

        public bool Connect()
        {
            if (Connected)
            {
                Console.WriteLine("Already connected to a device");
                return false;
            }

            if (Plc.Connect())
            {
                if (Plc.IsStateRun())
                {
                    Connected = true;
                    if (!AsyncContext.Run(CreateHandles))   //Try to create variable handles
                    {
                        Connected = false;
                        Plc.Disconnect();
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }
        public bool Disconnect()
        {
            if (!Connected)
            {
                Console.WriteLine("Nothing to disconnect from");
                UpdateChannelList();
                return false; //Nothing to disconnect from
            }
            if (ReadInProgress)
            {
                Console.WriteLine("Cannot disconnect during read");
                return false;
            }
            if (Plc.Disconnect())
            {
                Console.WriteLine("Disconnected");
                Connected = false;
                UpdateChannelList();
                return true;
            }
            else if (Plc.checkConnection())
            {
                return false;
            }
            else
            {
                Connected = false;
                UpdateChannelList();
                return true;
            }
        }

        


        //Read digital input channel
        public async Task<string> ReadDigitalInput(int channel)
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(DigitalInputHandle[channel - 1], CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig " + channel + " Read failure*";
            }
        }

        //Read pt100 channel
        public async Task<string> ReadPt100(int channel)
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<double>(PT100Handle[channel - 1], CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Pt100-" + channel + " Read failure*";
            }
        }

        //Generic channel measurement method
        public async Task<string> ReadChannel(int channel)
        {
            //Channels are ordered : Digital Inputs => PT100s => Unused
            if (channel == 0)
            {
                return "Not a valid channel";
            }

            if (channel <= DIGITAL_INPUT_CHANNELS)
            {
                return await ReadDigitalInput(channel);
            }
            channel -= DIGITAL_INPUT_CHANNELS;  //If we didn't match a channel, reduce the input number

            if (channel <= PT100_CHANNELS)
            {
                return await ReadPt100(channel);
            }

            return "Not a valid channel";
        }

        //Create all handles
        public async Task<bool> CreateHandles()
        {
            try
            {
                for (int i = 1; i <= DIGITAL_INPUT_CHANNELS; i++)
                {
                    await CreateDigitalInputHandle(i);
                }
                for (int i = 1; i <= PT100_CHANNELS; i++)
                {
                    await CreatePT100Handle(i);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Create digital input handle
        public async Task<bool> CreateDigitalInputHandle(int channel)
        {
            try
            {
                ResultHandle rh = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput" + channel, CancellationToken.None);
                DigitalInputHandle[channel - 1] = rh.Handle;
                return true;
            }
            catch
            {
                Console.WriteLine("Failed");
                return false;
            }
        }

        //Create PT100 handle
        public async Task<bool> CreatePT100Handle(int channel)
        {
            try
            {
                ResultHandle rh = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.Pt" + channel, CancellationToken.None);
                PT100Handle[channel - 1] = rh.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Update internal channel list
        public void UpdateChannelList()
        {
            ChannelList.Clear();
            Tuple<string, int> tmp;

            for (int i = 1; i <= DIGITAL_INPUT_CHANNELS; i++)
            {
                if (DigitalInputConnected[i - 1])
                {
                    tmp = ("Dig" + i, i).ToTuple();
                    ChannelList.Add(tmp);
                }
            }
            for (int i = 1; i <= PT100_CHANNELS; i++)
            {
                if (PT100Connected[i - 1])
                {
                    tmp = ("Pt" + i, (i + DIGITAL_INPUT_CHANNELS)).ToTuple();   //Pt100 access channels start after the digital inputs
                    ChannelList.Add(tmp);
                }
            }
            NumberOfChannels = ChannelList.Count;
        }

        public async Task<string> GetMeasurement()
        {
            string measurement = await ReadChannel(1);
            return measurement;
        }

        public async Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            string measurement = await ReadChannel(channelNumber);
            return measurement;
        }
    }
}
