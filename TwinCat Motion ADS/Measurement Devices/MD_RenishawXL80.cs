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
using Nito.AsyncEx.Synchronous;
using System.Windows;

namespace TwinCat_Motion_ADS.MeasurementDevice
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
                MessageBox.Show(path);
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
            await writer.WriteLineAsync(channelNumber.ToString());
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
            string messageBack;
            writer.WriteLine("Laser");
            writer.Flush();
            messageBack = reader.ReadLine();
            if (messageBack == "True")
            {
                Tuple<string, int> ch1 = ("XL80_Measure", 1).ToTuple();
                ChannelList.Add(ch1);
                Tuple<string, int> ch2 = ("XL80_Measure_Valid", 2).ToTuple();
                ChannelList.Add(ch2);
                Tuple<string, int> ch3 = ("XL80_Measure_Signal", 3).ToTuple();
                ChannelList.Add(ch3);
            }

            writer.WriteLine("Weather");
            writer.Flush();
            messageBack = reader.ReadLine();
            if (messageBack == "True")
            {
                Tuple<string, int> ch4 = ("AirTemp_Measure", 4).ToTuple();
                ChannelList.Add(ch4);
                Tuple<string, int> ch5 = ("AirTemp_Valid", 5).ToTuple();
                ChannelList.Add(ch5);

                Tuple<string, int> ch6 = ("AirPressure_Measure", 6).ToTuple();
                ChannelList.Add(ch6);
                Tuple<string, int> ch7 = ("AirPressure_Valid", 7).ToTuple();
                ChannelList.Add(ch7);

                Tuple<string, int> ch8 = ("AirHumidity_Measure", 8).ToTuple();
                ChannelList.Add(ch8);
                Tuple<string, int> ch9 = ("AirHumidity_Valid", 9).ToTuple();
                ChannelList.Add(ch9);

                Tuple<string, int> ch10 = ("MaterialTempAvg_Measure", 10).ToTuple();
                ChannelList.Add(ch10);
                Tuple<string, int> ch11 = ("MaterialTempAvg_Valid", 11).ToTuple();
                ChannelList.Add(ch11);

                Tuple<string, int> ch12 = ("MaterialTemp1_Measure", 12).ToTuple();
                ChannelList.Add(ch12);
                Tuple<string, int> ch13 = ("MaterialTemp1_Valid", 13).ToTuple();
                ChannelList.Add(ch13);

                Tuple<string, int> ch14 = ("MaterialTemp2_Measure", 14).ToTuple();
                ChannelList.Add(ch14);
                Tuple<string, int> ch15 = ("MaterialTemp2_Valid", 15).ToTuple();
                ChannelList.Add(ch15);

                Tuple<string, int> ch16 = ("MaterialTemp3_Measure", 16).ToTuple();
                ChannelList.Add(ch16);
                Tuple<string, int> ch17 = ("MaterialTemp3_Valid", 17).ToTuple();
                ChannelList.Add(ch17);
            }
            NumberOfChannels = ChannelList.Count;
        }
    }
}
