using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TwinCat_Motion_ADS.MeasurementDevice;

namespace TwinCat_Motion_ADS
{
    public class PneumaticAxis : TestAdmin
    {
        //PLC handles
        private readonly uint bCylinder_Handle;
        private readonly uint bExtendedLimit_Handle;
        private readonly uint bRetractedLimit_Handle;

        private bool _extendedLimit;
        public bool ExtendedLimit
        {
            get { return _extendedLimit; }
            set { _extendedLimit = value; OnPropertyChanged(); }
        }
        private bool _retractedLimit;
        public bool RetractedLimit
        {
            get { return _retractedLimit; }
            set { _retractedLimit = value; OnPropertyChanged(); }
        }
        private bool _cylinder;
        public bool Cylinder
        {
            get { return _cylinder; }
            set { _cylinder = value; OnPropertyChanged(); }
        }

        private bool _airOnToExtend = true;
        public bool AirOnToExtend
        {
            get { return _airOnToExtend; }
            set { _airOnToExtend = value; OnPropertyChanged(); }
        }

        public PneumaticAxis(PLC plc)
        {
            try
            {
                Plc = plc;
                bCylinder_Handle = Plc.TcAds.CreateVariableHandle("MAIN.bCylinder");
                bExtendedLimit_Handle = Plc.TcAds.CreateVariableHandle("MAIN.bExtendedLimit");
                bRetractedLimit_Handle = Plc.TcAds.CreateVariableHandle("MAIN.bRetractedLimit");
            }
            catch
            {
                Console.WriteLine("Could not find variables required");
            }

        }

        public void ToggleAirBehaviour()
        {
            AirOnToExtend = !AirOnToExtend;
        }

        private async Task CylinderActuation(bool extend)
        {   
            if (AirOnToExtend) { await Plc.TcAds.WriteAnyAsync(bCylinder_Handle, extend, CancellationToken.None); }
            else { await Plc.TcAds.WriteAnyAsync(bCylinder_Handle, !extend, CancellationToken.None); }
            
        }

        public async Task<bool> ExtendCylinder(bool ignoreLimits = false)
        {
            if (!ValidCommand()) return false;
            
            if (ExtendedLimit == false && ignoreLimits == false)
            {
                Console.WriteLine("Cylinder already extended");
                return true;
            }
            if (RetractedLimit == false || ignoreLimits)
            {
                Console.WriteLine("Extending cylinder");
                await CylinderActuation(true);
                return true;
            }
            Console.WriteLine("Retracted limit not hit. Extension prohibited");
            return false;
        }


        public async Task<bool> RetractCylinder(bool ignoreLimits = false)
        {
            if (!ValidCommand()) return false;

            if (RetractedLimit == false && ignoreLimits == false)
            {
                Console.WriteLine("Cylinder already retracted");
                return true;
            }
            //Prohibit a retraction command unless extension limit hit
            if (ExtendedLimit == false || ignoreLimits)
            {
                Console.WriteLine("Retracting cylinder");
                await CylinderActuation(false);
                return true;
            }
            Console.WriteLine("Extended limit not hit. Retraction prohibited");
            return false;
        }

        public async Task<bool> ExtendCylinderAndWait(uint timeout = 0, bool ignoreLimits = false)
        {
            CancellationTokenSource ct = new();

            if (await ExtendCylinder(ignoreLimits))
            {
                Task<bool> LimitTask = CheckExtendedLimitTask(true, ct.Token);
                List<Task> waitingTask;
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token);

                //Check if we need a timeout task and create the "waitingTask". We will monitor for any of these tasks to complete
                if (timeout > 0)
                {
                    waitingTask = new List<Task> { LimitTask, timeoutTask };
                }
                else
                {
                    waitingTask = new List<Task> { LimitTask };
                }
                if (await Task.WhenAny(waitingTask) == LimitTask)
                {
                    Console.WriteLine("Cylinder extended limit hit");
                    ct.Cancel();
                    return true;
                }
                else if (await Task.WhenAny(waitingTask) == timeoutTask)
                {
                    Console.WriteLine("Timeout extending cyclinder");
                    ct.Cancel();
                    return false;
                }
                else
                {
                    Console.WriteLine("How did you get here?\nExtension failed.");
                    ct.Cancel();
                    return false;
                }
            }
            return false;
        }
        public async Task<bool> RetractCylinderAndWait(uint timeout = 0, bool ignoreLimits = false)
        {
            CancellationTokenSource ct = new();

            if (await RetractCylinder(ignoreLimits))
            {
                Task<bool> LimitTask = CheckRetractedLimitTask(true, ct.Token);
                List<Task> waitingTask;
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token);

                //Check if we need a timeout task and create the "waitingTask". We will monitor for any of these tasks to complete
                if (timeout > 0)
                {
                    waitingTask = new List<Task> { LimitTask, timeoutTask };
                }
                else
                {
                    waitingTask = new List<Task> { LimitTask };
                }
                if (await Task.WhenAny(waitingTask) == LimitTask)
                {
                    Console.WriteLine("Cylinder retracted limit hit");
                    ct.Cancel();
                    return true;
                }
                else if (await Task.WhenAny(waitingTask) == timeoutTask)
                {
                    Console.WriteLine("Timeout retracting cyclinder");
                    ct.Cancel();
                    return false;
                }
                else
                {
                    Console.WriteLine("How did you get here?\nRetract failed.");
                    ct.Cancel();
                    return false;
                }
            }
            return false;
        }

        private async Task<bool> CheckExtendedLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = 50)
        {
            while (ExtendedLimit == limitStatus)
            {
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(msDelay);
            }
            return true;
        }

        private async Task<bool> CheckRetractedLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = 50)
        {
            while (RetractedLimit == limitStatus)
            {
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(msDelay);
            }
            return true;
        }

        public async Task<bool> End2EndTest(AirTestSettings ts, MeasurementDevices md =null)
        {
            Stopwatch testStopwatch = new();
            testStopwatch.Start();
            //Start the test retracted
            if (await RetractCylinderAndWait(ts.RetractTimeout.Val) == false)
            {
                Console.WriteLine("TEST STATUS: Retract init failed");
                testStopwatch.Stop();
                return false;
            }
            var currentTime = DateTime.Now;
            string formattedTitle3 = string.Format(@"\{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-PneumaticAxis-end2end-settlingReads({1}) settlingReadDelay({2}) ext2retDelay({3}) re2extDelay({4}) - {5} cycles.csv", currentTime, ts.SettlingReads.UiVal, ts.ReadDelayMs.UiVal, ts.DelayAfterExtend.UiVal, ts.DelayAfterRetract.UiVal, ts.Cycles.UiVal);
            string formattedTitle2 = TestDirectory + formattedTitle3;
            StartCSVAIR(formattedTitle2, md);

            
            Stopwatch stopwatch = new();


            for (int i = 0; i < ts.Cycles.Val; i++)
            {

                Console.WriteLine("Starting test cycle " + i);
                stopwatch.Reset();

                //Wait the allotted delay time
                await Task.Delay(TimeSpan.FromSeconds(ts.DelayAfterRetract.Val));
                stopwatch.Start();
                if (await ExtendCylinderAndWait(ts.ExtendTimeout.Val) == false)
                {
                    Console.WriteLine("TEST STATUS: Extension failed");
                    stopwatch.Stop();
                    testStopwatch.Stop();
                    return false;
                }
                stopwatch.Stop();
                for (int j = 0; j < ts.SettlingReads.Val; j++)
                {
                    //do a read of DTIs (both should be in extend position)
                    //These will not be highly synchronised reads, very low "read rate".

                    PneumaticEnd2EndCSVv2 tmpCSV = new PneumaticEnd2EndCSVv2((uint)i, (uint)j, "Extended", ExtendedLimit, RetractedLimit, stopwatch.Elapsed);
                    if (await WriteToCSVAIR(formattedTitle2, tmpCSV, md) == false)
                    {
                        Console.WriteLine("Failed to write data to file, exiting test");
                        return false;
                    }
                    
                    
                    await Task.Delay((int)ts.ReadDelayMs.Val);
                }
                //Settling reads finished
                //User delay before retracting cylinder
                await Task.Delay(TimeSpan.FromSeconds(ts.DelayAfterExtend.Val));
                stopwatch.Reset();
                stopwatch.Start();
                if (await RetractCylinderAndWait(ts.RetractTimeout.Val) == false)
                {
                    Console.WriteLine("TEST STATUS: Retraction failed");
                    stopwatch.Stop();
                    testStopwatch.Stop();
                    return false;
                }
                stopwatch.Stop();
                for (int j = 0; j < ts.SettlingReads.Val; j++)
                {
                    //do a read of DTIs (both should be in extend position)
                    //These will not be highly synchronised reads, very low "read rate".

                    PneumaticEnd2EndCSVv2 tmpCSV = new PneumaticEnd2EndCSVv2((uint)i, (uint)j, "Retracted", ExtendedLimit, RetractedLimit, stopwatch.Elapsed);
                    if (await WriteToCSVAIR(formattedTitle2, tmpCSV, md) == false)
                    {
                        Console.WriteLine("Failed to write data to file, exiting test");
                        return false;
                    }


                    await Task.Delay((int)ts.ReadDelayMs.Val);
                }
                //Used to only one shot the read
                /*PneumaticEnd2EndCSVv2 tmpCSVretract = new PneumaticEnd2EndCSVv2((uint)i, 0, "Retracting", ExtendedLimit, RetractedLimit, stopwatch.Elapsed);
                if (await WriteToCSVAIR(formattedTitle2, tmpCSVretract, md) == false)
                {
                    Console.WriteLine("Failed to write data to file, exiting test");
                    return false;
                }*/

            //Retract finished and logged. Write to csv
            //Write the cycle data
            
            }
            testStopwatch.Stop();
            Console.WriteLine("Test complete. Time taken: " + testStopwatch.Elapsed);
            return true;
        }

        private readonly CancellationTokenSource readToken1 = new();

        public void ReadStatuses()
        {
            _ = Task.Run(() => ReadCylinder(readToken1.Token));
            _ = Task.Run(() => ReadExtendedLimitStatus(readToken1.Token));
            _ = Task.Run(() => ReadRetractedLimitStatus(readToken1.Token));
        }
        public async Task ReadCylinder(CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                if (!ValidCommand()) return;
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bCylinder_Handle, CancellationToken.None);
                Cylinder = result.Value;
            }
            Cylinder = false;
            return;
        }
        public async Task ReadExtendedLimitStatus(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand()) { return; }
                TwinCAT.Ads.ResultValue<bool> result = await Plc.TcAds.ReadAnyAsync<bool>(bExtendedLimit_Handle, CancellationToken.None);
                ExtendedLimit = result.Value;
            }
            ExtendedLimit = false;
            return;
        }
        public async Task ReadRetractedLimitStatus(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand()) return;
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bRetractedLimit_Handle, CancellationToken.None);
                RetractedLimit = result.Value;
            }
            RetractedLimit = false;
            return;
        }
    }
}
