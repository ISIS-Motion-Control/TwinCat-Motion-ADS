using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace TwinCat_Motion_ADS
{
    public class Beckhoff : INotifyPropertyChanged
    {
        public PLC Plc { get; set; }
        /*
         * Notes: Should probably make this a little more neat/scalable than it currently is.
         * Also need to implement some additional handles and methods for sensors.
         */
        const int _DIGITAL_INPUT_CHANNELS = 8;
        const int _PT100_CHANNELS = 4;
        public int DIGITAL_INPUT_CHANNELS { get { return _DIGITAL_INPUT_CHANNELS; } }
        public int PT100_CHANNELS { get { return _PT100_CHANNELS; } }



        public List<Tuple<string, int>> ChannelList = new();


        public bool[] DigitalInputConnected { get; set; }
        public bool[] PT100Connected { get; set; }
        private uint[] DigitalInputHandle;
        private uint[] PT100Handle;

        public Beckhoff(PLC plc)
        {
            DigitalInputConnected = new bool[DIGITAL_INPUT_CHANNELS];
            DigitalInputHandle = new uint[DIGITAL_INPUT_CHANNELS];

            PT100Connected = new bool[PT100_CHANNELS];
            PT100Handle = new uint[PT100_CHANNELS];
            Plc = plc;
        }

        //Read Channel Methods
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

        public async Task<string> ReadChannel(int channel)
        {
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


        //Handle creation and checks

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
                return false;
            }
        }

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
}
