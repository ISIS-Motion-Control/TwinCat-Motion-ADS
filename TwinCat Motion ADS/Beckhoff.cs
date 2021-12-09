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

        //Handle variables for address locations
        private uint Dig1Handle;
        private uint Dig2Handle;
        private uint Dig3Handle;
        private uint Dig4Handle;
        private uint Dig5Handle;
        private uint Dig6Handle;
        private uint Dig7Handle;
        private uint Dig8Handle;
        private uint Pt1Handle;
        private uint Pt2Handle;
        private uint Pt3Handle;
        private uint Pt4Handle;


        //Connection bools are binded to measurement device window
        private bool _dig1Connection;
        public bool Dig1Connection
        {
            get { return _dig1Connection; }
            set 
            {
                _dig1Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _dig2Connection;
        public bool Dig2Connection
        {
            get { return _dig2Connection; }
            set
            {
                _dig2Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _dig3Connection;
        public bool Dig3Connection
        {
            get { return _dig3Connection; }
            set
            {
                _dig3Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _dig4Connection;
        public bool Dig4Connection
        {
            get { return _dig4Connection; }
            set
            {
                _dig4Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _dig5Connection;
        public bool Dig5Connection
        {
            get { return _dig5Connection; }
            set
            {
                _dig5Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _dig6Connection;
        public bool Dig6Connection
        {
            get { return _dig6Connection; }
            set
            {
                _dig6Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _dig7Connection;
        public bool Dig7Connection
        {
            get { return _dig7Connection; }
            set
            {
                _dig7Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _dig8Connection;
        public bool Dig8Connection
        {
            get { return _dig8Connection; }
            set
            {
                _dig8Connection = value;
                OnPropertyChanged();
            }
        }

        private bool _pt1Connection;
        public bool Pt1Connection
        {
            get { return _pt1Connection; }
            set
            {
                _pt1Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _pt2Connection;
        public bool Pt2Connection
        {
            get { return _pt2Connection; }
            set
            {
                _pt2Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _pt3Connection;
        public bool Pt3Connection
        {
            get { return _pt3Connection; }
            set
            {
                _pt3Connection = value;
                OnPropertyChanged();
            }
        }
        private bool _pt4Connection;
        public bool Pt4Connection
        {
            get { return _pt4Connection; }
            set
            {
                _pt4Connection = value;
                OnPropertyChanged();
            }
        }


        public Beckhoff(PLC plc)
        {
            Plc = plc;
        }

        //Read Channel Methods
        public async Task<string> ReadDig1()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig1Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 1 Read failure*";
            }
        }
        public async Task<string> ReadDig2()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig2Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 2 Read failure*";
            }
        }
        public async Task<string> ReadDig3()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig3Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 3 Read failure*";
            }
        }
        public async Task<string> ReadDig4()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig4Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 4 Read failure*";
            }
        }
        public async Task<string> ReadDig5()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig5Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 5 Read failure*";
            }
        }
        public async Task<string> ReadDig6()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig6Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 6 Read failure*";
            }
        }
        public async Task<string> ReadDig7()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig7Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 7 Read failure*";
            }
        }
        public async Task<string> ReadDig8()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(Dig8Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Dig 8 Read failure*";
            }
        }
        public async Task<string> ReadPt1()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<double>(Pt1Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Pt100-1 Read failure*";
            }
        }
        public async Task<string> ReadPt2()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<double>(Pt2Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Pt100-2 Read failure*";
            }
        }
        public async Task<string> ReadPt3()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<double>(Pt3Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Pt100-3 Read failure*";
            }
        }
        public async Task<string> ReadPt4()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<double>(Pt4Handle, CancellationToken.None);
                return result.Value.ToString();
            }
            catch
            {
                return "*Pt100-4 Read failure*";
            }
        }
        
        public async Task<string> ReadChannels()
        {
            List<string> measures = new ();

            //Incredibly manual approach to run through channels
            if(Dig1Connection)
            {
                measures.Add(await ReadDig1());
            }
            if(Dig2Connection)
            {
                measures.Add(await ReadDig2());
            }
            if (Dig3Connection)
            {
                measures.Add(await ReadDig3());
            }
            if (Dig4Connection)
            {
                measures.Add(await ReadDig4());
            }
            if (Dig5Connection)
            {
                measures.Add(await ReadDig5());
            }
            if (Dig6Connection)
            {
                measures.Add(await ReadDig6());
            }
            if (Dig7Connection)
            {
                measures.Add(await ReadDig7());
            }
            if (Dig8Connection)
            {
                measures.Add(await ReadDig8());
            }
            if (Pt1Connection)
            {
                measures.Add(await ReadPt1());
            }
            if (Pt2Connection)
            {
                measures.Add(await ReadPt2());
            }
            if (Pt3Connection)
            {
                measures.Add(await ReadPt3());
            }
            if (Pt4Connection)
            {
                measures.Add(await ReadPt4());
            }

            string retStr = string.Join(",", measures); //Combine all measurements as single CSV string
            return retStr;
        }

        //Handle creation and checks
        public async Task<bool> CreateHandleDig1()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput1",CancellationToken.None);
                Dig1Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }           
        }
        public async Task<bool> CreateHandleDig2()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput2", CancellationToken.None);
                Dig2Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandleDig3()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput3", CancellationToken.None);
                Dig3Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandleDig4()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput4", CancellationToken.None);
                Dig4Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandleDig5()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput5", CancellationToken.None);
                Dig5Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandleDig6()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput6", CancellationToken.None);
                Dig6Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandleDig7()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput7", CancellationToken.None);
                Dig7Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandleDig8()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.DigInput8", CancellationToken.None);
                Dig8Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateHandlePt1()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.Pt1", CancellationToken.None);
                Pt1Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandlePt2()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.Pt2", CancellationToken.None);
                Pt2Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandlePt3()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.Pt3", CancellationToken.None);
                Pt3Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> CreateHandlePt4()
        {
            try
            {
                ResultHandle resultHandle = await Plc.TcAds.CreateVariableHandleAsync("SENSORS.Pt4", CancellationToken.None);
                Pt4Handle = resultHandle.Handle;
                return true;
            }
            catch
            {
                return false;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
