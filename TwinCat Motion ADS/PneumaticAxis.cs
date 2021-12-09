﻿using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TwinCat_Motion_ADS
{
    public class PneumaticAxis : TestAdmin
    {
        //PLC handles
        private uint bCylinder_Handle;
        private uint bExtendedLimit_Handle;
        private uint bRetractedLimit_Handle;


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

        private async Task cylinderActuation(bool extend)
        {
            
            await Plc.TcAds.WriteAnyAsync(bCylinder_Handle, extend, CancellationToken.None);
        }
        public async Task<bool> extendCylinder(bool ignoreLimits = false)
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
                await cylinderActuation(true);
                return true;
            }
            Console.WriteLine("Retracted limit not hit. Extension prohibited");
            return false;
        }


        public async Task<bool> retractCylinder(bool ignoreLimits = false)
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
                await cylinderActuation(false);
                return true;
            }
            Console.WriteLine("Extended limit not hit. Retraction prohibited");
            return false;
        }

        public async Task<bool> extendCylinderAndWait(int timeout = 0, bool ignoreLimits = false)
        {
            CancellationTokenSource ct = new CancellationTokenSource();

            if (await extendCylinder(ignoreLimits))
            {
                Task<bool> LimitTask = checkExtendedLimitTask(true, ct.Token);
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
        public async Task<bool> retractCylinderAndWait(int timeout = 0, bool ignoreLimits = false)
        {
            CancellationTokenSource ct = new CancellationTokenSource();

            if (await retractCylinder(ignoreLimits))
            {
                Task<bool> LimitTask = checkRetractedLimitTask(true, ct.Token);
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

        private async Task<bool> checkExtendedLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = 50)
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

        private async Task<bool> checkRetractedLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = 50)
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



        public async Task<bool> End2EndTest(int cycles, int settlingReads, int settlingReadDelayMilliSeconds, int extend2RetractDelaySeconds, int retract2ExtendDelaySeconds, int extendTimeoutSeconds = 0, int retractTimeoutSeconds = 0, MeasurementDevice device1 = null, MeasurementDevice device2 = null, MeasurementDevice device3 = null, MeasurementDevice device4 = null)
        {
            Stopwatch testStopwatch = new Stopwatch();
            testStopwatch.Start();
            //Start the test retracted
            if (await retractCylinderAndWait(retractTimeoutSeconds) == false)
            {
                Console.WriteLine("TEST STATUS: Retract init failed");
                testStopwatch.Stop();
                return false;
            }

            //Setup the csv file:
            List<PneumaticEnd2EndCSV> recordList = new List<PneumaticEnd2EndCSV>();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-PneumaticAxis-end2end-settlingReads({1}) settlingReadDelay({2}) ext2retDelay({3}) re2extDelay({4}) - {5} cycles", currentTime, settlingReads, settlingReadDelayMilliSeconds, extend2RetractDelaySeconds, retract2ExtendDelaySeconds, cycles);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory + fileName, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, };

            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, config);

            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<PneumaticEnd2EndCSV>();
                csv.NextRecord();
            }
            Stopwatch stopwatch = new Stopwatch();
            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);

            for (int i = 0; i < cycles; i++)
            {
                Task<bool> pauseTaskRequest = checkPauseRequestTask(ptToken.Token);
                await pauseTaskRequest;
                if (cancelRequestTask.IsCompleted)
                {
                    //Cancelled the test
                    ptToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }


                Console.WriteLine("Starting test cycle " + i);
                stopwatch.Reset();

                //Wait the allotted delay time
                await Task.Delay(TimeSpan.FromSeconds(retract2ExtendDelaySeconds));
                stopwatch.Start();
                if (await extendCylinderAndWait(extendTimeoutSeconds) == false)
                {
                    Console.WriteLine("TEST STATUS: Extension failed");
                    stopwatch.Stop();
                    testStopwatch.Stop();
                    return false;
                }
                stopwatch.Stop();
                for (int j = 0; j < settlingReads; j++)
                {
                    //do a read of DTIs (both should be in extend position)
                    //These will not be highly synchronised reads, very low "read rate".
                    string measurement1 = string.Empty;
                    string measurement2 = string.Empty;
                    string measurement3 = string.Empty;
                    string measurement4 = string.Empty;
                    long measurement1Timestamp = 0;
                    long measurement2Timestamp = 0;
                    long measurement3Timestamp = 0;
                    long measurement4Timestamp = 0;

                    CancellationTokenSource testToken = new CancellationTokenSource();
                    if (device1 != null)
                    {
                        measurement1 = await device1.GetMeasurement();
                        measurement1Timestamp = testStopwatch.ElapsedMilliseconds;
                    }
                    if (device2 != null)
                    {
                        measurement2 = await device2.GetMeasurement();
                        measurement2Timestamp = testStopwatch.ElapsedMilliseconds;
                    }
                    if (device3 != null)
                    {
                        measurement3 = await device3.GetMeasurement();
                        measurement3Timestamp = testStopwatch.ElapsedMilliseconds;
                    }
                    if (device4 != null)
                    {
                        measurement4 = await device4.GetMeasurement();
                        measurement4Timestamp = testStopwatch.ElapsedMilliseconds;
                    }

                    await Task.Delay(settlingReadDelayMilliSeconds);
                    //log a record
                    recordList.Add(new PneumaticEnd2EndCSV((uint)i, (uint)j, "Extending", ExtendedLimit, RetractedLimit, stopwatch.Elapsed, measurement1, measurement1Timestamp, measurement2, measurement2Timestamp, measurement3, measurement3Timestamp, measurement4, measurement4Timestamp));

                }
                //Settling reads finished
                //User delay before retracting cylinder
                await Task.Delay(TimeSpan.FromSeconds(extend2RetractDelaySeconds));
                stopwatch.Reset();
                stopwatch.Start();
                if (await retractCylinderAndWait(retractTimeoutSeconds) == false)
                {
                    Console.WriteLine("TEST STATUS: Retraction failed");
                    stopwatch.Stop();
                    testStopwatch.Stop();
                    return false;
                }
                stopwatch.Stop();
                recordList.Add(new PneumaticEnd2EndCSV((uint)i, 0, "Retracting", ExtendedLimit, RetractedLimit, stopwatch.Elapsed, "", 0, "", 0, "", 0, "", 0));
                //Retract finished and logged. Write to csv
                //Write the cycle data
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(recordList);
                }
                recordList.Clear();
            }
            testStopwatch.Stop();
            Console.WriteLine("Test complete. Time taken: " + testStopwatch.Elapsed);
            return true;
        }

        public async Task<bool> End2EndTest(AirTestSettings ts, MeasurementDevices md =null)
        {
            Stopwatch testStopwatch = new Stopwatch();
            testStopwatch.Start();
            //Start the test retracted
            if (await retractCylinderAndWait(ts.RetractTimeout) == false)
            {
                Console.WriteLine("TEST STATUS: Retract init failed");
                testStopwatch.Stop();
                return false;
            }

            //Setup the csv file:
            List<PneumaticEnd2EndCSVv2> recordList = new List<PneumaticEnd2EndCSVv2>();
            List<PneumaticEnd2EndCSVv2> retractRecord = new List<PneumaticEnd2EndCSVv2>();
            List<string> measurements = new();
            List<List<string>> cycleMeasurements = new();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-PneumaticAxis-end2end-settlingReads({1}) settlingReadDelay({2}) ext2retDelay({3}) re2extDelay({4}) - {5} cycles", currentTime, ts.SettlingReads, ts.ReadDelayMs, ts.DelayAfterExtend, ts.DelayAfterRetract, ts.Cycles);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory + fileName, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, };

            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, config);

            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<PneumaticEnd2EndCSVv2>();
                if (md != null)    //populate CSV headers with device names
                {
                    foreach (var device in md.MeasurementDeviceList)
                    {
                        if (device.Connected)
                        {
                            csv.WriteField(device.Name);
                            csv.WriteField(device.Name + "Timestamp");
                        }
                    }

                }
                csv.NextRecord();
            }
            Stopwatch stopwatch = new Stopwatch();
            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);

            for (int i = 0; i < ts.Cycles; i++)
            {
                measurements.Clear();
                cycleMeasurements.Clear();
                Task<bool> pauseTaskRequest = checkPauseRequestTask(ptToken.Token);
                await pauseTaskRequest;
                if (cancelRequestTask.IsCompleted)
                {
                    //Cancelled the test
                    ptToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }


                Console.WriteLine("Starting test cycle " + i);
                stopwatch.Reset();

                //Wait the allotted delay time
                await Task.Delay(TimeSpan.FromSeconds(ts.DelayAfterRetract));
                stopwatch.Start();
                if (await extendCylinderAndWait(ts.ExtendTimeout) == false)
                {
                    Console.WriteLine("TEST STATUS: Extension failed");
                    stopwatch.Stop();
                    testStopwatch.Stop();
                    return false;
                }
                stopwatch.Stop();
                for (int j = 0; j < ts.SettlingReads; j++)
                {
                    //do a read of DTIs (both should be in extend position)
                    //These will not be highly synchronised reads, very low "read rate".


                    CancellationTokenSource testToken = new CancellationTokenSource();
                    if(md != null)
                    {
                        measurements = new();
                        foreach(var device in md.MeasurementDeviceList)
                        {
                            if(device.Connected)
                            {
                                string measure = string.Empty;
                                string timestamp = string.Empty;
                                measure = await device.GetMeasurement();
                                timestamp = testStopwatch.Elapsed.ToString();
                                measurements.Add(measure);
                                measurements.Add(timestamp);

                                Console.WriteLine(device.Name + ": " + measure + "@" + timestamp);
                            }
                        }
                    }
                    
                    await Task.Delay(ts.ReadDelayMs);
                    //log a record
                    recordList.Add(new PneumaticEnd2EndCSVv2((uint)i, (uint)j, "Extending", ExtendedLimit, RetractedLimit, stopwatch.Elapsed));
                    cycleMeasurements.Add(measurements);
                }
                //Settling reads finished
                //User delay before retracting cylinder
                await Task.Delay(TimeSpan.FromSeconds(ts.DelayAfterExtend));
                stopwatch.Reset();
                stopwatch.Start();
                if (await retractCylinderAndWait(ts.RetractTimeout) == false)
                {
                    Console.WriteLine("TEST STATUS: Retraction failed");
                    stopwatch.Stop();
                    testStopwatch.Stop();
                    return false;
                }
                stopwatch.Stop();
                retractRecord.Add(new PneumaticEnd2EndCSVv2((uint)i, 0, "Retracting", ExtendedLimit, RetractedLimit, stopwatch.Elapsed));
                //Retract finished and logged. Write to csv
                //Write the cycle data
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
                using (writer = new StreamWriter(stream))
                
                
                using (csv = new CsvWriter(writer, config))
                {
                    //csv.WriteRecords(recordList);
                    int loopIndex = 0;
                    foreach(var record in recordList)
                    {
                        csv.WriteRecord(record);
                        if (md != null)
                        {
                            foreach (var measure in cycleMeasurements[loopIndex])
                            {
                                csv.WriteField(measure);
                            }
                        }
                        loopIndex++;
                        csv.NextRecord();
                    }
                    csv.WriteRecords(retractRecord);
                }
                recordList.Clear();
                retractRecord.Clear();
            }
            testStopwatch.Stop();
            Console.WriteLine("Test complete. Time taken: " + testStopwatch.Elapsed);
            return true;
        }



        CancellationTokenSource readToken1 = new();

        public void newLimitRead()
        {
            Task.Run(() => read_bCylinder(readToken1.Token, TimeSpan.FromMilliseconds(20)));
            Task.Run(() => read_ExtendedLimit(readToken1.Token, TimeSpan.FromMilliseconds(20)));
            Task.Run(() => read_RetractedLimit(readToken1.Token, TimeSpan.FromMilliseconds(20)));
        }
        public async Task read_bCylinder(CancellationToken ct, TimeSpan ts)
        {
            while(!ct.IsCancellationRequested)
            {
                if (!ValidCommand()) return;
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bCylinder_Handle, CancellationToken.None);
                Cylinder = result.Value;
            }
            return;
            
        }
        public async Task read_ExtendedLimit(CancellationToken ct, TimeSpan ts)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand()) return;
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bExtendedLimit_Handle, CancellationToken.None);
                ExtendedLimit = result.Value;
            }
            return;
        }
        public async Task read_RetractedLimit(CancellationToken ct, TimeSpan ts)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand()) return;
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bRetractedLimit_Handle, CancellationToken.None);
                RetractedLimit = result.Value;
            }
            return;
        }


        private bool ValidCommand() //always going to check if PLC is valid or not
        {
            if (!Plc.IsStateRun())
            {
                Console.WriteLine("Incorrect PLC configuration");
                Valid = false;
                return false;
            }
            //check some motion parameters???

            Valid = true;
            return true;
        }


    }
}
