using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using CsvHelper;
using System.IO;
using System.Globalization;

namespace TwinCat_Motion_ADS
{
    public partial class NcAxis : TestAdmin
    {    
        //eCommand enumeration values
        const byte eMoveAbsolute = 0;
        const byte eMoveRelative = 1;
        const byte eMoveVelocity = 3;
        //const byte eHome = 10;

        //Motion PLC Handles - used for reading/writing PLC variables
        private uint eCommandHandle;
        private uint fVelocityHandle;
        private uint fPositionHandle;
        private uint bExecuteHandle;
        private uint fActPositionHandle;
        private uint bDoneHandle;
        private uint bBusyHandle;
        private uint bFwEnabledHandle;
        private uint bBwEnabledHandle;
        private uint bEnabledHandle;
        private uint bStopHandle;
        private uint bErrorHandle;
        private uint bEnableHandle;
        private uint bResetHandle;

       




        //Current axis ID
        private uint _axisID;

        public uint AxisID
        {
            get { return _axisID; }
            set { _axisID = value;
                OnPropertyChanged();
            }
        }


        public NcAxis(uint axisID, PLC plc)
        {
            Plc = plc;
            AxisID = axisID;
            UpdateAxisInstance(AxisID, Plc);
        }

        private bool ValidCommand() //always going to check if PLC is valid or not
        {
            if(!Plc.IsStateRun())
            {
                Console.WriteLine("Incorrect PLC configuration");
                Valid = false;
                return false;
            }
            //check some motion parameters???

            Valid = true;
            return true;
        }

        public void UpdateAxisInstance(uint axisID, PLC plc)
        {
            if (!ValidCommand()) return;
            try
            {
                
                
                AxisID = axisID;
                Plc = plc;
                //These variable handles rely on the twinCAT standard solution naming.
                eCommandHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stControl.eCommand");
                fVelocityHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stControl.fVelocity");
                fPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stControl.fPosition");
                bExecuteHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stControl.bExecute");
                fActPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stStatus.fActPosition");
                bDoneHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stStatus.bDone");
                bBusyHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stStatus.bBusy");
                bFwEnabledHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stStatus.bFwEnabled");
                bBwEnabledHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stStatus.bBwEnabled");
                bEnabledHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stStatus.bEnabled");
                bStopHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stControl.bHalt");    //bStop causes an error on the axis. bHalt just ends movement
                bErrorHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stStatus.bError");
                bEnableHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stControl.bEnable");
                bResetHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + AxisID + "].stControl.bReset");
                //StartPositionRead();
                ReadStatuses();
                
            }
            catch
            {
                Console.WriteLine("Invalid PLC Configuration - unable to create variable handles");
            }
                   

        }

        private async Task SetCommand(byte command)
        {
            if (!ValidCommand()) return;
            await Plc.TcAds.WriteAnyAsync(eCommandHandle, command, CancellationToken.None);
        }
        
        private async Task SetVelocity(double velocity)
        {
            if (!ValidCommand()) return;
            await Plc.TcAds.WriteAnyAsync(fVelocityHandle, velocity, CancellationToken.None);
        }

        private async Task SetPosition(double position)
        {
            if (!ValidCommand()) return;
            await Plc.TcAds.WriteAnyAsync(fPositionHandle, position, CancellationToken.None);
        }

        private async Task Execute()
        {
            if (!ValidCommand()) return;
            await Plc.TcAds.WriteAnyAsync(bExecuteHandle, true, CancellationToken.None);
        }

        public async Task SetEnable(bool enable)
        {
            if (!ValidCommand()) return;
            await Plc.TcAds.WriteAnyAsync(bEnableHandle, enable, CancellationToken.None);
        }

        public async Task Reset()
        {
            if (!ValidCommand()) return;
            await Plc.TcAds.WriteAnyAsync(bResetHandle, true, CancellationToken.None);
        }

        public async Task<bool> MoveAbsolute(double position, double velocity)
        {
            if (!ValidCommand()) return false;
            if (AxisBusy)
            {
                return false;   //command fails if axis already busy
            }
            if (Error)
            {
                return false;
            }
            if(velocity == 0)
            {
                return false;
            }
            

            var commandTask = SetCommand(eMoveAbsolute);
            var velocityTask = SetVelocity(velocity);
            var positionTask = SetPosition(position);

            //just for fun, want to implement the async list so stuff can complete at different times
            var absoluteTasks = new List<Task> { commandTask, velocityTask, positionTask };
            while (absoluteTasks.Count > 0)
            {
                Task finishedTask = await Task.WhenAny(absoluteTasks);
                if (finishedTask == commandTask)
                {
                    //Console.WriteLine("Command set");
                }
                else if (finishedTask == velocityTask)
                {
                    //Console.WriteLine("Velocity set");
                }
                else if (finishedTask == positionTask)
                {
                    //Console.WriteLine("Position set");
                }
                absoluteTasks.Remove(finishedTask);
            }
            await Execute();
            return true;
        }

        public async Task<bool> MoveAbsoluteAndWait(double position, double velocity, int timeout = 0)
        {
            if (!ValidCommand()) return false;
            CancellationTokenSource ct = new();

            if (await MoveAbsolute(position, velocity))
            {
                await Task.Delay(40);   //delay to system to allow PLC to react to move command
                Task<bool> errorTask = CheckForError(ct.Token);
                Task<bool> doneTask = WaitForDone(ct.Token);
                Task<bool> limitTask;
                List<Task> waitingTask;

                double currentPosition = AxisPosition;
                if (position>currentPosition)
                {
                    limitTask = CheckFwLimitTask(true, ct.Token);
                }
                else
                {
                    limitTask = CheckBwLimitTask(true, ct.Token);
                }
                //Check if we need a timeout task
                if (timeout > 0)
                {
                    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token);
                    waitingTask = new List<Task> { doneTask, errorTask,limitTask, timeoutTask };
                }
                else
                {
                    waitingTask = new List<Task> { doneTask, errorTask,limitTask };
                }
                
                if(await Task.WhenAny(waitingTask)==doneTask)
                {
                    Console.WriteLine("Move absolute complete");
                    ct.Cancel();
                    return true;
                }
                else if(await Task.WhenAny(waitingTask) == errorTask)
                {
                    Console.WriteLine("Error on move absolute");
                    ct.Cancel();
                    return false;
                }
                else if(await Task.WhenAny(waitingTask) == limitTask)
                {
                    Console.WriteLine("Limit hit before position reached");
                    ct.Cancel();
                    return false;
                }
                else
                {
                    Console.WriteLine("Timeout on moveabs");
                    await MoveStop();
                    ct.Cancel();
                    return false;
                }
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }

        public async Task<bool> MoveRelative(double position, double velocity)
        {
            if (!ValidCommand()) return false;
            if (AxisBusy)
            {
                return false;   //command fails if axis already busy
            }
            if (Error)
            {
                return false;
            }
            if (velocity <=0)
            { return false; }
            var commandTask = SetCommand(eMoveRelative);
            var velocityTask = SetVelocity(velocity);
            var positionTask = SetPosition(position);

            //just for fun, want to implement the async list so stuff can complete at different times
            var absoluteTasks = new List<Task> { commandTask, velocityTask, positionTask };
            while (absoluteTasks.Count > 0)
            {
                Task finishedTask = await Task.WhenAny(absoluteTasks);
                if (finishedTask == commandTask)
                {
                    //Console.WriteLine("Command set");
                }
                else if (finishedTask == velocityTask)
                {
                    //Console.WriteLine("Velocity set");
                }
                else if (finishedTask == positionTask)
                {
                    //Console.WriteLine("Position set");
                }
                absoluteTasks.Remove(finishedTask);
            }
            await Execute();
            return true;
        }

        public async Task<bool> MoveRelativeAndWait(double position, double velocity, int timeout=0)
        {
            if (!ValidCommand()) return false;
            CancellationTokenSource ct = new();

            if (await MoveRelative(position,velocity))
            {
                await Task.Delay(40);
                Task<bool> doneTask = WaitForDone(ct.Token);
                Task<bool> errorTask = CheckForError(ct.Token);
                Task<bool> limitTask;
                List<Task> waitingTask;
                
                //Check direction of travel for monitoring limits
                if (position > 0)
                {
                    limitTask = CheckFwLimitTask(true, ct.Token);
                }
                else
                {
                    limitTask = CheckBwLimitTask(true, ct.Token);
                }
                //Check if we need a timeout task
                if (timeout > 0)
                {
                    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token);
                    waitingTask = new List<Task> { doneTask, errorTask, limitTask, timeoutTask };
                }
                else
                {
                    waitingTask = new List<Task> { doneTask, errorTask, limitTask };
                }

                if (await Task.WhenAny(waitingTask) == doneTask)
                {
                    Console.WriteLine("Move relative complete");
                    ct.Cancel();
                    return true;
                }
                else if (await Task.WhenAny(waitingTask) == errorTask)
                {
                    Console.WriteLine("Error on move relative");
                    ct.Cancel();
                    return false;
                }
                else if (await Task.WhenAny(waitingTask) == limitTask)
                {
                    Console.WriteLine("Limit hit before position reached");
                    ct.Cancel();
                    return false;
                }
                else
                {
                    Console.WriteLine("Timeout on moverel");
                    await MoveStop();
                    ct.Cancel();
                    return false;
                }
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }

        public async Task<bool> MoveVelocity(double velocity)
        {
            if (!ValidCommand()) return false;
            if (AxisBusy)
            {
                return false;   //command fails if axis already busy
            }
            if (Error)
            {
                return false;
            }
            if (velocity == 0)
            {
                return false;
            }
            var commandTask = SetCommand(eMoveVelocity);
            var velocityTask = SetVelocity(velocity);
            var velocityTasks = new List<Task> { commandTask, velocityTask };
            while (velocityTasks.Count > 0)
            {
                Task finishedTask = await Task.WhenAny(velocityTasks);
                if (finishedTask == commandTask)
                {
                    //Console.WriteLine("Command set");
                }
                else if (finishedTask == velocityTask)
                {
                    //Console.WriteLine("Velocity set");
                }

                velocityTasks.Remove(finishedTask);
            }
            await Execute();
            return true;
        }

        public async Task MoveStop()
        {
            if (!ValidCommand()) return;
            await Plc.TcAds.WriteAnyAsync(bStopHandle, true, CancellationToken.None);
        }

        public async Task<bool> MoveToHighLimit(double velocity, int timeout)
        {
            if (!ValidCommand()) return false;
            //Check to see if already on the high limit
            if (AxisFwEnabled == false)
            {
                Console.WriteLine("Already at high limit");
                return true;
            }
            if (velocity == 0)
            {
                Console.WriteLine("0 velocity not valid");
                return false;
            }
            //"Correct" the velocity setting if required
            if (velocity <0)
            {
                velocity *= -1;
            }
            //Send a move velocity command
            if (await MoveVelocity(velocity) == false)
            {
                Console.WriteLine("Command rejected");
                return false;
            };
            //Start a task to check the FwEnabled bool that only returns when flag is hit (fwEnabled == false)
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckFwLimitTask(true,ct.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if(timeout==0)
            {
                waitingTask = new List<Task> { limitTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token)};
            }

            if(await Task.WhenAny(waitingTask)==limitTask)
            {
                Console.WriteLine("High limit reached");
                ct.Cancel();
                return true;
            }
            else //Timeout on command
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on move to high limit");
                await MoveStop();
                return false;
            }
        }

        public async Task<bool> MoveToLowLimit(double velocity, int timeout)
        {
            if (!ValidCommand()) return false;
            //Check to see if already on the low limit
            if (AxisBwEnabled == false)
            {
                Console.WriteLine("Already at low limit");
                return true;
            }
            if (velocity == 0)
            {
                Console.WriteLine("0 velocity not valid");
                return false;
            }
            //"Correct" the velocity setting if required
            if (velocity > 0)
            {
                velocity *= -1;
            }
            //Send a move velocity command
            if (await MoveVelocity(velocity) == false)
            {
                Console.WriteLine("Command rejected");
                return false;
            };
            //Start a task to check the BwEnabled bool that only returns when flag is hit (BwEnabled == false)
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckBwLimitTask(true, ct.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task.
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token) };
            }

            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                Console.WriteLine("Lower limit hit");
                ct.Cancel();
                return true;
            }
            else //Timeout on command
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on move to lower limit");
                await MoveStop();
                return false;
            }
        }

        public async Task<bool> HighLimitReversal(double velocity, int timeout, int extraReversalTime, int settleTime)
        {
            if (!ValidCommand()) return false;
            //Only allow the command if already on the high limit
            /* if (await read_bFwEnabled() == true)
             {
                 Console.WriteLine("Not on high limit. Reversal command rejected");
                 return false;
             }*/
            //Correct the velocity setting if needed
            if (velocity < 0)
            {
                velocity *= -1;
            }
            //Reject 0 velocity value
            if (velocity == 0)
            {
                Console.WriteLine("Cannot have velocity of zero");
                return false;
            }
            //Start a reversal off the limit switch
            if (await MoveVelocity(-velocity) == false)
            {
                Console.WriteLine("Reversal command rejected");
                return false;
            }
            //Start a task to monitor when the FwEnable signal is regained
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckFwLimitTask(false, ct.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token) };
            }
            //Monitor the checkFwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(extraReversalTime));
                await MoveStop();
                ct.Cancel();
            }
            else
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on reversal");
                await MoveStop();
                return false;
            }
            //Velocity move back on to high limit
            if (await MoveVelocity(velocity) == false)
            {
                Console.WriteLine("Approach high limit command rejected ");
                return false;
            }
            //Restart the checkFwEnable task to find when it is hit. Run at much faster rate
            waitingTask.Clear();
            CancellationTokenSource ct2 = new();
            limitTask = CheckFwLimitTask(true, ct2.Token);
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct2.Token) };
            }
            //Monitor the checkFwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                ct2.Cancel();
                return true;
            }
            else
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on move to limit");
                await MoveStop();
                return false;
            }
        }

        public async Task<bool> LowLimitReversal(double velocity, int timeout, int extraReversalTime, int settleTime)
        {
            if (!ValidCommand()) return false;
            //Only allow the command if already on the low limit
            /* if (await read_bBwEnabled() == true)
             {
                 Console.WriteLine("Not on low limit. Reversal command rejected");
                 return false;
             }*/
            //Correct the velocity setting if needed
            if (velocity > 0)
            {
                velocity *= -1;
            }
            //Reject 0 velocity value
            if (velocity == 0)
            {
                Console.WriteLine("Cannot have velocity of zero");
                return false;
            }
            //Start a reversal off the limit switch
            if (await MoveVelocity(-velocity) == false)
            {
                Console.WriteLine("Reversal command rejected");
                return false;
            }
            //Start a task to monitor when the FwEnable signal is regained
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckBwLimitTask(false, ct.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token) };
            }
            //Monitor the checkBwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(extraReversalTime));
                await MoveStop();
                ct.Cancel();
            }
            else
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on reversal");
                await MoveStop();
                return false;
            }
            //Velocity move back on to low limit
            if (await MoveVelocity(velocity) == false)
            {
                Console.WriteLine("Approach low limit command rejected ");
                return false;
            }
            //Restart the checkBwEnable task to find when it is hit. Run at much faster rate
            waitingTask.Clear();
            CancellationTokenSource ct2 = new();
            limitTask = CheckBwLimitTask(true, ct2.Token);
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct2.Token) };
            }
            //Monitor the checkFwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                ct2.Cancel();
                return true;
            }
            else
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on move to limit");
                await MoveStop();
                return false;
            }
        }

        public async Task<bool> LimitToLimitTestwithReversingSequence(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            if (!ValidCommand()) return false;
            if (testSettings.Cycles == 0)
            {
                Console.WriteLine("0 cycle count invalid");
                return false;
            }
            if (testSettings.ReversalVelocity == 0)
            {
                Console.WriteLine("0 reversal velocity invalid");
                return false;
            }
            if (testSettings.Velocity == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }

            var currentTime = DateTime.Now;
            string newTitle = string.Format(@"{0:yyMMdd} {0:HH};{0:mm};{0:ss} Axis {1}~ " + testSettings.StrTestTitle, currentTime, AxisID);
            Console.WriteLine(newTitle);
            string settingFileFullPath = TestDirectory + @"\" + newTitle + ".settingsfile";
            string csvFileFullPath = TestDirectory + @"\" + newTitle + ".csv";
            SaveSettingsFile(testSettings, settingFileFullPath, "Limit to Limit Test");

            var stream = File.Open(csvFileFullPath, FileMode.Append);
            StreamWriter writer = new(stream);
            CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<End2endCSV>();
                if (devices != null)    //populate CSV headers with device names
                {
                    foreach (var device in devices.MeasurementDeviceList)
                    {
                        if (device.Connected)
                        {
                            csv.WriteField(device.Name);
                        }
                    }

                }
                csv.NextRecord();
            }

            Stopwatch stopWatch = new(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);
            //Start low
            if (await MoveToLowLimit(-testSettings.Velocity, testSettings.Timeout) == false)
            {
                Console.WriteLine("Failed to move to low limit for start of test");
                return false;
            }

            CancellationTokenSource ctToken = new();
            CancellationTokenSource ptToken = new();
            Task<bool> cancelRequestTask = CheckCancellationRequestTask(ctToken.Token);


            //Start running test cycles
            End2endCSV record1;
            End2endCSV record2;
            List<string> measurementsHigh = new();
            List<string> measurementsLow = new();
            for (int i = 1; i <= testSettings.Cycles; i++)
            {
                Task<bool> pauseTaskRequest = CheckPauseRequestTask(ptToken.Token);
                await pauseTaskRequest;
                if (cancelRequestTask.IsCompleted)
                {
                    //Cancelled the test
                    ptToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait

                stopWatch.Reset();
                stopWatch.Start();  //Clear and start the stopwatch

                if (await MoveToHighLimit(testSettings.Velocity, testSettings.Timeout))
                {
                    stopWatch.Stop();
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds));//Allow axis to settle before reversal
                    if (await HighLimitReversal(testSettings.ReversalVelocity, testSettings.Timeout, testSettings.ReversalExtraTimeSeconds, testSettings.ReversalSettleTimeSeconds))
                    {

                        ///READ MEASUREMENT DEVICES///
                        ///
                        if (devices != null)    //If devices input, check for connected
                        {
                            measurementsHigh.Clear();
                            foreach(var device in devices.MeasurementDeviceList)
                            {
                                if (device.Connected)
                                {
                                    string measure = string.Empty;
                                    measure = await device.GetMeasurement();
                                    measurementsHigh.Add(measure);
                                    Console.WriteLine(device.Name + ": " + measure);
                                }
                            }

                        }

                        double tmpAxisPosition = AxisPosition;
                        record1 = new End2endCSV(i, "Low limit to high limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition);
                        Console.WriteLine("Cycle " + i + "- Low limit to high limit: " + stopWatch.ElapsedMilliseconds + "ms. High limit triggered at " + tmpAxisPosition);
                    }
                    else
                    {
                        Console.WriteLine("High limit reversal failed");
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }
                }
                else
                {
                    stopWatch.Stop();
                    Console.WriteLine("Move to high failed");
                    ctToken.Cancel();
                    ptToken.Cancel();
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds));
                stopWatch.Reset();
                stopWatch.Start();
                if (await MoveToLowLimit(-testSettings.Velocity, testSettings.Timeout))
                {
                    stopWatch.Stop();
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds));//Allow axis to settle before reversal
                    if (await LowLimitReversal(testSettings.ReversalVelocity, testSettings.Timeout, testSettings.ReversalExtraTimeSeconds, testSettings.ReversalSettleTimeSeconds))
                    {
                        

                        ///READ MEASUREMENT DEVICES///
                        ///
                        if (devices != null)    //If devices input, check for connected
                        {
                            measurementsLow.Clear();
                            foreach (var device in devices.MeasurementDeviceList)
                            {
                                if (device.Connected)
                                {
                                    string measure = string.Empty;
                                    measure = await device.GetMeasurement();
                                    measurementsLow.Add(measure);
                                    Console.WriteLine(device.Name + ": " + measure);
                                }
                            }
                        }
                        double tmpAxisPosition = AxisPosition;
                        record2 = new End2endCSV(i, "High limit to low limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition);

                        Console.WriteLine("Cycle " + i + "- High limit to low limit: " + stopWatch.ElapsedMilliseconds + "ms. Low limit triggered at " + tmpAxisPosition);
                        
                    }
                    else
                    {
                        Console.WriteLine("Low limit reversal failed");
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }
                }
                else
                {
                    stopWatch.Stop();
                    Console.WriteLine("Move to low failed");
                    ctToken.Cancel();
                    ptToken.Cancel();
                    return false;
                }
                using (stream = File.Open(csvFileFullPath, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecord(record1);
                    foreach(var m in measurementsHigh)
                    {
                        csv.WriteField(m);
                    }
                    csv.NextRecord();
                    csv.WriteRecord(record2);
                    foreach (var m in measurementsLow)
                    {
                        csv.WriteField(m);
                    }
                    csv.NextRecord();
                }
            }
            ctToken.Cancel();
            return true;
        }

        //no timeout implemented
        public async Task<bool> UniDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            if (!ValidCommand()) return false;
            if (testSettings.Cycles == 0)
            {
                Console.WriteLine("0 cycle count invalid");
                return false;
            }
            if (testSettings.NumberOfSteps == 0)
            {
                Console.WriteLine("0 step count invalid");
                return false;
            }
            if (testSettings.Velocity == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }
            if (testSettings.StepSize == 0)
            {
                Console.WriteLine("0 step size invalid");
                return false;
            }
            List<UniDirectionalAccCSV> recordList = new();

            var currentTime = DateTime.Now;
            
            string newTitle = string.Format(@"{0:yyMMdd} {0:HH};{0:mm};{0:ss} Axis {1}~ " + testSettings.StrTestTitle, currentTime, AxisID);
            Console.WriteLine(newTitle);
            string settingFileFullPath = TestDirectory + @"\" + newTitle + ".settingsfile";
            string csvFileFullPath = TestDirectory + @"\" + newTitle + ".csv";
            SaveSettingsFile(testSettings,settingFileFullPath, "Unidirectional Accuracy Test");

            var stream = File.Open(csvFileFullPath, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, };

            StreamWriter writer = new(stream);
            CsvWriter csv = new(writer, config);

            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<UniDirectionalAccCSV>();
                if (devices != null)    //populate CSV headers with device names
                {
                    foreach (var device in devices.MeasurementDeviceList)
                    {
                        if (device.Connected)
                        {
                            csv.WriteField(device.Name);
                        }
                    }

                }
                csv.NextRecord();
            }

            CancellationTokenSource ctToken = new();
            CancellationTokenSource ptToken = new();
            Task<bool> cancelRequestTask = CheckCancellationRequestTask(ctToken.Token);

            Stopwatch stopWatch = new(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.

            double reversalPosition;
            if (testSettings.StepSize > 0)
            {
                reversalPosition = testSettings.InitialSetpoint - testSettings.ReversalDistance;
            }
            else
            {
                reversalPosition = testSettings.InitialSetpoint + testSettings.ReversalDistance;
            }
            stopWatch.Start();

            List<string> measurements = new();
            List<List<string>> cycleMeasurements = new();

            for (uint i = 1; i <= testSettings.Cycles; i++)
            {
                measurements.Clear();
                cycleMeasurements.Clear();
                Console.WriteLine("Cycle " + i);
                //Create a task each cycle to monitor for the pause. This is done as a task as a basic "while(paused)" would block UI and not allow an unpause
                Task<bool> pauseTaskRequest = CheckPauseRequestTask(ptToken.Token);
                await pauseTaskRequest;
                if (cancelRequestTask.IsCompleted)
                {
                    //Cancelled the test
                    ptToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait
                double TargetPosition = testSettings.InitialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await MoveAbsoluteAndWait(reversalPosition, testSettings.Velocity, testSettings.Timeout) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    ctToken.Cancel();
                    ptToken.Cancel();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));

                for (uint j = 0; j <= testSettings.NumberOfSteps; j++)
                {
                    Console.WriteLine("Step: " + j);
                    //Do the step move
                    if (await MoveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));

                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (devices != null)    //If devices input, check for connected
                    {
                        measurements = new();
                        foreach (var device in devices.MeasurementDeviceList)
                        {
                            if (device.Connected)
                            {
                                string measure = string.Empty;
                                measure = await device.GetMeasurement();
                                measurements.Add(measure);
                                Console.WriteLine(device.Name + ": " + measure);
                            }
                        }

                    }


                    //Log the data
                    double tmpAxisPosition = AxisPosition;
                    recordList.Add(new UniDirectionalAccCSV(i, j, "Testing", TargetPosition, tmpAxisPosition));
                    cycleMeasurements.Add(measurements);
                    //debug line
                    TargetPosition += testSettings.StepSize;
                }

                //Write the cycle data
                using (stream = File.Open(csvFileFullPath, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, config))
                {

                    int loopIndex = 0;
                    foreach(var record in recordList)
                    {
                        csv.WriteRecord(record);
                        if(devices!=null)
                        {
                            foreach(var measure in cycleMeasurements[loopIndex])
                            {
                                csv.WriteField(measure);
                            }
                        }

                        loopIndex++;
                        csv.NextRecord();
                    }                  
                }
                recordList.Clear();
                cycleMeasurements.Clear();
            }
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed + "ms");
            ctToken.Cancel();
            ptToken.Cancel();
            return true;
        }

        public async Task<bool> BiDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            if (!ValidCommand()) return false;
            List<UniDirectionalAccCSV> recordList = new();
            var currentTime = DateTime.Now;

            string newTitle = string.Format(@"{0:yyMMdd} {0:HH};{0:mm};{0:ss} Axis {1}~ " + testSettings.StrTestTitle, currentTime, AxisID);
            Console.WriteLine(newTitle);
            string settingFileFullPath = TestDirectory + @"\" + newTitle + ".settingsfile";
            string csvFileFullPath = TestDirectory + @"\" + newTitle + ".csv";
            SaveSettingsFile(testSettings, settingFileFullPath, "Bidirectional Accuracy Test");

            var stream = File.Open(csvFileFullPath, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, };

            StreamWriter writer = new(stream);
            CsvWriter csv = new(writer, config);

            if (testSettings.Cycles == 0)
            {
                Console.WriteLine("0 cycle count invalid");
                return false;
            }
            if (testSettings.NumberOfSteps == 0)
            {
                Console.WriteLine("0 step count invalid");
                return false;
            }
            if (testSettings.Velocity == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }
            if (testSettings.StepSize == 0)
            {
                Console.WriteLine("0 step size invalid");
                return false;
            }


            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<UniDirectionalAccCSV>();
                if (devices != null)    //populate CSV headers with device names
                {
                    foreach (var device in devices.MeasurementDeviceList)
                    {
                        if (device.Connected)
                        {
                            csv.WriteField(device.Name);
                        }
                    }

                }
                csv.NextRecord();
            }

            Stopwatch stopWatch = new(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            CancellationTokenSource ctToken = new();
            CancellationTokenSource ptToken = new();
            Task<bool> cancelRequestTask = CheckCancellationRequestTask(ctToken.Token);

            double reversalPosition;
            if (testSettings.StepSize > 0)
            {
                reversalPosition = testSettings.InitialSetpoint - testSettings.ReversalDistance;
            }
            else
            {
                reversalPosition = testSettings.InitialSetpoint + testSettings.ReversalDistance;
            }
            double overshootPosition;
            if (testSettings.StepSize > 0)
            {
                overshootPosition = testSettings.InitialSetpoint + ((testSettings.NumberOfSteps - 1) * testSettings.StepSize) + testSettings.OvershootDistance;
            }
            else
            {
                overshootPosition = testSettings.InitialSetpoint + ((testSettings.NumberOfSteps - 1) * testSettings.StepSize) - testSettings.OvershootDistance;
            }

            stopWatch.Start();
            List<string> measurements = new();
            List<List<string>> cycleMeasurements = new();
            for (uint i = 1; i <= testSettings.Cycles; i++)
            {
                Console.WriteLine("Cycle " + i);
                //Create a task each cycle to monitor for the pause. This is done as a task as a basic "while(paused)" would block UI and not allow an unpause
                Task<bool> pauseTaskRequest = CheckPauseRequestTask(ptToken.Token);
                await pauseTaskRequest;
                if (cancelRequestTask.IsCompleted)
                {
                    //Cancelled the test
                    ptToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait
                double TargetPosition = testSettings.InitialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await MoveAbsoluteAndWait(reversalPosition, testSettings.Velocity, testSettings.Timeout) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));

                //going up the steps
                for (uint j = 0; j <= testSettings.NumberOfSteps; j++)
                {
                    Console.WriteLine("Positive Move. Step: " + j);
                    //Do the step move
                    if (await MoveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));
                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (devices != null)    //If devices input, check for connected
                    {
                        measurements = new();
                        foreach (var device in devices.MeasurementDeviceList)
                        {
                            if (device.Connected)
                            {
                                string measure = string.Empty;
                                measure = await device.GetMeasurement();
                                measurements.Add(measure);
                                Console.WriteLine(device.Name + ": " + measure);
                            }
                        }

                    }

                    //Log the data
                    double tmpAxisPosition = AxisPosition;
                    recordList.Add(new UniDirectionalAccCSV(i, j, "Positive approach", TargetPosition, tmpAxisPosition));
                    cycleMeasurements.Add(measurements);
                    //Update target position

                    TargetPosition += testSettings.StepSize;
                }
                TargetPosition -= testSettings.StepSize;
                //Overshoot the final position before coming back down
                if (await MoveAbsoluteAndWait(overshootPosition, testSettings.Velocity, testSettings.Timeout) == false)
                {
                    Console.WriteLine("Failed to move to overshoot position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));
                //going down the steps. Need the cast here as we require j to go negative to cancel the loop
                for (int j = (int)testSettings.NumberOfSteps; j >= 0; j--)
                {
                    Console.WriteLine("Negative Move. Step: " + j);
                    //Do the step move
                    if (await MoveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));
                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (devices != null)    //If devices input, check for connected
                    {
                        measurements = new();
                        foreach (var device in devices.MeasurementDeviceList)
                        {
                            if (device.Connected)
                            {
                                string measure = string.Empty;
                                measure = await device.GetMeasurement();
                                measurements.Add(measure);
                                Console.WriteLine(device.Name + ": " + measure);
                            }
                        }

                    }
                    //Log the data
                    double tmpAxisPosition = AxisPosition;
                    recordList.Add(new UniDirectionalAccCSV(i, (uint)j, "Negative approach", TargetPosition, tmpAxisPosition));
                    cycleMeasurements.Add(measurements);
                    //Update target position

                    TargetPosition -= testSettings.StepSize;
                }

                //Write the cycle data
                using (stream = File.Open(csvFileFullPath, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, config))
                {

                    int loopIndex = 0;
                    foreach (var record in recordList)
                    {
                        csv.WriteRecord(record);
                        if (devices != null)
                        {
                            foreach (var measure in cycleMeasurements[loopIndex])
                            {
                                csv.WriteField(measure);
                            }
                        }

                        loopIndex++;
                        csv.NextRecord();
                    }
                }
                recordList.Clear();
                cycleMeasurements.Clear();
            }
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed);
            return true;
        }
   

        private void SaveSettingsFile(NcTestSettings testSettings, string filePath, string testType)
        {
            List<string> settings = new();
            settings.Add("Test Type: " + testType);
            settings.Add("Axis Number: " + AxisID);
            settings.Add("Velocity: " + testSettings.StrVelocity);
            settings.Add("Timeout: " + testSettings.StrTimeout);
            settings.Add("Cycles: " + testSettings.StrCycles);
            settings.Add("Cycle Delay (s): " + testSettings.StrCycleDelaySeconds);
            settings.Add("Reversal Velocity: " + testSettings.StrReversalVelocity);
            settings.Add("Reversal Extra Time (s): " + testSettings.StrReversalExtraTimeSeconds);
            settings.Add("Reversal Settle Time (s): " + testSettings.StrReversalSettleTimeSeconds);
            settings.Add("Initial Setpoint: " + testSettings.StrInitialSetpoint);
            settings.Add("Number of Steps : " + testSettings.StrNumberOfSteps);
            settings.Add("Step Size: " + testSettings.StrStepSize);
            settings.Add("Settle Time (s): " + testSettings.StrSettleTimeSeconds);
            settings.Add("Reversal Distance: " + testSettings.StrReversalDistance);
            settings.Add("Overshoot Distance: " + testSettings.StrOvershootDistance);

            File.WriteAllLines(filePath, settings);
        }
    }
}
