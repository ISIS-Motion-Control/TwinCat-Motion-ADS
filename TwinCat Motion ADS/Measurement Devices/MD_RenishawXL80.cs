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
using System.Diagnostics;

namespace TwinCat_Motion_ADS
{
    public class MD_RenishawXL80 : BaseMeasurementDevice, I_MeasurementDevice
    {

        private NamedPipeServerStream RenishawServer;
        private StreamReader reader;
        private StreamWriter writer;
        Process RenishawClient;

        public string testString = "Hello";

        public MD_RenishawXL80()
        {
            DeviceType = DeviceTypes.RenishawXL80;

            //Probably need to move these lines:
            
            //StartServer();
            UpdateChannelList();
            

        }

        public bool Connect()
        {
            if (Connected)
            {
                Console.WriteLine("Already connected");
                return false;
            }
            
            if(!Connected)
            {
                RenishawServer = new NamedPipeServerStream("RenishawXL80_Pipe");
                reader = new StreamReader(RenishawServer);
                writer = new StreamWriter(RenishawServer);
                string exeName = "Renishaw_XL80_App.exe";
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName);
                RenishawClient = Process.Start(path);
                CancellationTokenSource ct = new();
                RenishawServer.WaitForConnectionAsync(ct.Token);
                //Task connectionTask = Task.Run(() => RenishawServer.WaitForConnection(),ct.Token);
                Task connectionTask = Task.Run(() => 
                {
                    while (!RenishawServer.IsConnected) 
                    { 
                        if (ct.IsCancellationRequested)
                        { 
                            return; 
                        } 
                    }
                },ct.Token
                );
                if (connectionTask.Wait(TimeSpan.FromSeconds(4)))
                {
                    Connected = true;
                    UpdateChannelList();
                    Console.WriteLine("Connection made to client application. Configure device in client.");
                    return true;
                }
                else
                {
                    ct.Cancel();
                    
                    connectionTask.Dispose();
                    reader.Close();
                    reader.Dispose();
                    RenishawServer.Close();
                    RenishawServer.Dispose();
                    try
                    {
                        RenishawClient.CloseMainWindow();
                        RenishawClient.Close();
                    }
                    catch { }
                    return false;
                }
                    
                //RenishawServer.WaitForConnection();
                
            }
            return false;
        }


        public bool Disconnect()
        {
            try
            {
                RenishawClient.CloseMainWindow();
                RenishawClient.Close();
                RenishawServer.Close();
                //RenishawServer.Dispose();
                Connected = false;
                UpdateChannelList();
                if (RenishawServer == null)
                {
                    Console.WriteLine("Server is null");
                }
                else
                { Console.WriteLine("Server is not null"); }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetChannelMeasurement(int channelNumber = 0)
        {
            if (ReadInProgress) return "Read in progress";

            ReadInProgress = true;
            await writer.WriteLineAsync("1");
            await writer.FlushAsync();
            string messageBack = await reader.ReadLineAsync();
            //await writer.WriteLineAsync("0");
            //await writer.FlushAsync();
            ReadInProgress = false;
            return messageBack;
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
