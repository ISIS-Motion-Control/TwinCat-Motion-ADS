using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwinCAT.Ads;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Nito.AsyncEx;
using System.IO.Pipes;
using System.IO;
using System.Linq;

namespace TwinCat_Motion_ADS
{
    public class MD_RenishawXL80 : BaseMeasurementDevice, I_MeasurementDevice
    {

        private NamedPipeServerStream RenishawServer = new NamedPipeServerStream("RenishawXL80_Pipe");
        private StreamReader reader;
        private StreamWriter writer;
        
        static void StartServer()
        {
            Task.Factory.StartNew(() =>
            {
                var server = new NamedPipeServerStream("PipesOfPiece");
                server.WaitForConnection();
                StreamReader reader = new StreamReader(server);
                StreamWriter writer = new StreamWriter(server);
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        Console.WriteLine(line.ToString());
                        writer.WriteLine(String.Join("", line.Reverse()));
                        writer.Flush();
                    }
                }
            });
        }

        public MD_RenishawXL80()
        {
            DeviceType = DeviceTypes.RenishawXL80;

            //Probably need to move these lines:
            
            //StartServer();
            UpdateChannelList();

        }

        public bool Connect()
        {
            if(!Connected)
            {
                
                RenishawServer.WaitForConnection();
                Connected = true;
                UpdateChannelList();
                reader = new StreamReader(RenishawServer);
                writer = new StreamWriter(RenishawServer);
                return true;
            }
            return false;
        }

        public bool Disconnect()
        {
            RenishawServer.Dispose();
            Connected = false;
            return true;
        }

        public Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            writer.WriteLine("test");
            writer.Flush();
            string messageBack = reader.ReadLine();
            Console.WriteLine(messageBack);
            return Task.FromResult(messageBack);
        }

        public Task<string> GetMeasurement()
        {
            throw new NotImplementedException();
        }

        public void UpdateChannelList()
        {
            ChannelList.Clear();
            NumberOfChannels = 0;
            if (!Connected) return;

            Tuple<string, int> t3 = (Name, 1).ToTuple();
            ChannelList.Add(t3);
            NumberOfChannels = ChannelList.Count;
        }
    }
}
