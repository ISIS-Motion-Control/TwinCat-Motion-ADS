using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using CsvHelper;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Xml;

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
            EstimatedTimeRemaining = new();
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
                Console.WriteLine("Low limit hit");
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




        /* 
         *       _________    _______       ________       _________    ________      
                |\___   ___\ |\  ___ \     |\   ____\     |\___   ___\ |\   ____\     
                \|___ \  \_| \ \   __/|    \ \  \___|_    \|___ \  \_| \ \  \___|_    
                     \ \  \   \ \  \_|/__   \ \_____  \        \ \  \   \ \_____  \   
                      \ \  \   \ \  \_|\ \   \|____|\  \        \ \  \   \|____|\  \  
                       \ \__\   \ \_______\    ____\_\  \        \ \__\    ____\_\  \ 
                        \|__|    \|_______|   |\_________\        \|__|   |\_________\
                                              \|_________|                \|_________|
                                                                     */


        public async Task<bool> LimitToLimitTestwithReversingSequence(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            if (!ValidCommand()) return false;
            if (testSettings.Cycles.Val == 0)
            {
                Console.WriteLine("0 cycle count invalid");
                return false;
            }
            if (testSettings.ReversalVelocity.Val == 0)
            {
                Console.WriteLine("0 reversal velocity invalid");
                return false;
            }
            if (testSettings.Velocity.Val == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }

            progScaler = 1/Convert.ToDouble(testSettings.Cycles.Val);
            Console.WriteLine("Progress scaler is: " + progScaler);
            TestProgress = 0;

            var currentTime = DateTime.Now;
            string newTitle = string.Format(@"{0:yyMMdd} {0:HH}h{0:mm}m{0:ss}s Axis {1}~ " + testSettings.TestTitle.UiVal, currentTime, AxisID);
            Console.WriteLine(newTitle);
            string settingFileFullPath = TestDirectory + @"\" + newTitle + ".xml";
            string csvFileFullPath = TestDirectory + @"\" + newTitle + ".csv";
            SaveSettingsFile(testSettings, settingFileFullPath, "Limit to Limit Test");

            StartCSV(csvFileFullPath, devices);

            Stopwatch stopWatch = new(); //Create stopwatch for rough end to end timing
            testSettings.Velocity.Val = Math.Abs(testSettings.Velocity.Val);
            //Start low
            if (await MoveToLowLimit(-testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
            {
                Console.WriteLine("Failed to move to low limit for start of test");
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds.Val));

            CancellationTokenSource ctToken = new();
            CancellationTokenSource ptToken = new();
            Task<bool> cancelRequestTask = CheckCancellationRequestTask(ctToken.Token);

            //Start running test cycles

            stopWatch.Reset();
            stopWatch.Start();  //Clear and start the stopwatch

            EstimatedTimeRemaining.StartTime = DateTime.Now;
            for (int i = 1; i <= testSettings.Cycles.Val; i++)
            {
                //Single cycle time
                if (i == 2)
                {
                    EstimatedTimeRemaining.CycleTime = DateTime.Now - EstimatedTimeRemaining.StartTime;
                }
                //Estimate the end time based on remaining cycles and single cycle time
                if (i > 1)
                {
                    EstimatedTimeRemaining.EstimatedEndTime = DateTime.Now + EstimatedTimeRemaining.CycleTime * (testSettings.Cycles.Val - i + 1);
                }

                TestProgress = Convert.ToDouble(i-1) * progScaler;

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
                
                if (await MoveToHighLimit(testSettings.Velocity.Val, (int)testSettings.Timeout.Val))
                {
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds.Val));//Allow axis to settle before reversal
                    if (await HighLimitReversal(testSettings.ReversalVelocity.Val, (int)testSettings.Timeout.Val, (int)testSettings.ReversalExtraTimeSeconds.Val, (int)testSettings.ReversalSettleTimeSeconds.Val))
                    {

                        StandardCSVData tmpCSV = new StandardCSVData((uint)i, 0, "Moving to high limit", 0, AxisPosition);
                        if (await WriteToCSV(csvFileFullPath, tmpCSV, devices) == false)
                        {
                            Console.WriteLine("Failed to write data to file, exiting test");
                            stopWatch.Stop();
                            ctToken.Cancel();
                            ptToken.Cancel();
                            return false;
                        }
                        Console.WriteLine("Cycle " + i + "- Low limit to high limit: " + "ms. High limit triggered at " + AxisPosition);
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


                if (await MoveToLowLimit(-testSettings.Velocity.Val, (int)testSettings.Timeout.Val))
                {

                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds.Val));//Allow axis to settle before reversal
                    if (await LowLimitReversal(testSettings.ReversalVelocity.Val, (int)testSettings.Timeout.Val, (int)testSettings.ReversalExtraTimeSeconds.Val, (int)testSettings.ReversalSettleTimeSeconds.Val))
                    {
                        StandardCSVData tmpCSV = new StandardCSVData((uint)i, 0, "Moving to low limit", 0, AxisPosition);
                        if (await WriteToCSV(csvFileFullPath, tmpCSV, devices) == false)
                        {
                            Console.WriteLine("Failed to write data to file, exiting test");
                            stopWatch.Stop();
                            ctToken.Cancel();
                            ptToken.Cancel();
                            return false;
                        }
                        Console.WriteLine("Cycle " + i + "- High limit to low limit: " + "ms. Low limit triggered at " + AxisPosition);

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
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait
            }
            TestProgress = 1;
            ctToken.Cancel();
            return true;
        }

        public async Task<bool> UniDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            //Check settings are valid
            if (!ValidCommand()) return false;
            if (testSettings.Cycles.Val <= 0)
            {
                Console.WriteLine("Cycle count invalid");
                return false;
            }
            if (testSettings.NumberOfSteps.Val <= 0)
            {
                Console.WriteLine("Step count invalid");
                return false;
            }
            if (testSettings.Velocity.Val == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }
            if (testSettings.StepSize.Val == 0)
            {
                Console.WriteLine("0 step size invalid");
                return false;
            }

            progScaler = 1 / Convert.ToDouble(testSettings.Cycles.Val);
            TestProgress = 0;
            stepScaler = progScaler / Convert.ToDouble(testSettings.NumberOfSteps.Val);
            //Ensure positive velocity value
            testSettings.Velocity.Val = Math.Abs(testSettings.Velocity.Val);
            
            //Establish "Reversal" position of test
            double reversalPosition;
            if (testSettings.StepSize.Val > 0)
            {
                reversalPosition = testSettings.InitialSetpoint.Val - testSettings.ReversalDistance.Val;
            }
            else
            {
                reversalPosition = testSettings.InitialSetpoint.Val + testSettings.ReversalDistance.Val;
            }

            //Create file name string
            var currentTime = DateTime.Now;
            string newTitle = string.Format(@"{0:yyMMdd} {0:HH}h{0:mm}m{0:ss}s Axis {1}~ " + testSettings.TestTitle.UiVal, currentTime, AxisID);                  
            string settingFileFullPath = TestDirectory + @"\" + newTitle + ".xml";
            string csvFileFullPath = TestDirectory + @"\" + newTitle + ".csv";
            Console.WriteLine(newTitle);

            //Create settings file and CSV file
            SaveSettingsFile(testSettings, settingFileFullPath, "Unidirectional Accuracy Test");
            StartCSV(csvFileFullPath,devices);

            CancellationTokenSource ctToken = new();
            CancellationTokenSource ptToken = new();
            Task<bool> cancelRequestTask = CheckCancellationRequestTask(ctToken.Token);
            Stopwatch stopWatch = new();

            //Cycles
            stopWatch.Start();
            EstimatedTimeRemaining.StartTime = DateTime.Now;
            for (uint i = 1; i <= testSettings.Cycles.Val; i++)
            {
                //get single cycle time
                if(i ==2)
                {
                    EstimatedTimeRemaining.CycleTime = DateTime.Now - EstimatedTimeRemaining.StartTime;
                }
                //Estimate the end time based on remaining cycles and single cycle time
                if(i>1)
                {
                    EstimatedTimeRemaining.EstimatedEndTime = DateTime.Now + EstimatedTimeRemaining.CycleTime * (testSettings.Cycles.Val - i + 1);
                    Console.WriteLine("Predicted end time is: " + EstimatedTimeRemaining.EstimatedEndTime);
                }
                Console.WriteLine("Cycle " + i);

                //Check for a pause or cancellation request
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

                //Set target position to cycle initial setpoint
                double TargetPosition = testSettings.InitialSetpoint.Val;

                //Start test at reversal position then moving to initial setpoint          
                if (await MoveAbsoluteAndWait(reversalPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    ctToken.Cancel();
                    ptToken.Cancel();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));

                //Steps of cycle
                for (uint j = 0; j <= testSettings.NumberOfSteps.Val; j++)
                {
                    TestProgress = Convert.ToDouble(i-1)*progScaler + Convert.ToDouble(j)*stepScaler;
                    Console.WriteLine("Step: " + j);

                    //Absolute position move (Exit test if failure)
                    if (await MoveAbsoluteAndWait(TargetPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));

                    //Log the data, if test fails to write to fail, exit test
                    StandardCSVData tmpCSV = new StandardCSVData(i, j, "Testing", TargetPosition, AxisPosition);
                    if(await WriteToCSV(csvFileFullPath, tmpCSV, devices)== false)
                    {
                        Console.WriteLine("Failed to write data to file, exiting test");
                        stopWatch.Stop();
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }

                    //Update target position for next step
                    TargetPosition += testSettings.StepSize.Val;
                }
                //Delay between cycles
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait
            }
            TestProgress = 1;
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed + "ms");
            ctToken.Cancel();
            ptToken.Cancel();
            return true;
        }


        public async Task<bool> BiDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            if (!ValidCommand()) return false;           
            if (testSettings.Cycles.Val == 0)
            {
                Console.WriteLine("0 cycle count invalid");
                return false;
            }
            if (testSettings.NumberOfSteps.Val == 0)
            {
                Console.WriteLine("0 step count invalid");
                return false;
            }
            if (testSettings.Velocity.Val == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }
            if (testSettings.StepSize.Val == 0)
            {
                Console.WriteLine("0 step size invalid");
                return false;
            }

            //Ensure positive velocity value
            testSettings.Velocity.Val = Math.Abs(testSettings.Velocity.Val);  //Only want positive velocity
            
            //Establish "Reversal" and "Overshoot" positions of test
            double reversalPosition;
            if (testSettings.StepSize.Val > 0)
            {
                reversalPosition = testSettings.InitialSetpoint.Val - testSettings.ReversalDistance.Val;
            }
            else
            {
                reversalPosition = testSettings.InitialSetpoint.Val + testSettings.ReversalDistance.Val;
            }
            double overshootPosition;
            if (testSettings.StepSize.Val > 0)
            {
                overshootPosition = testSettings.InitialSetpoint.Val + ((testSettings.NumberOfSteps.Val - 1) * testSettings.StepSize.Val) + testSettings.OvershootDistance.Val;
            }
            else
            {
                overshootPosition = testSettings.InitialSetpoint.Val + ((testSettings.NumberOfSteps.Val - 1) * testSettings.StepSize.Val) - testSettings.OvershootDistance.Val;
            }

            progScaler = 1 / Convert.ToDouble(testSettings.Cycles.Val);
            TestProgress = 0;
            stepScaler = progScaler / (Convert.ToDouble(testSettings.NumberOfSteps.Val)*2);


            var currentTime = DateTime.Now;
            string newTitle = string.Format(@"{0:yyMMdd} {0:HH}h{0:mm}m{0:ss}s Axis {1}~ " + testSettings.TestTitle.UiVal, currentTime, AxisID);           
            string settingFileFullPath = TestDirectory + @"\" + newTitle + ".xml";
            string csvFileFullPath = TestDirectory + @"\" + newTitle + ".csv";
            Console.WriteLine(newTitle);
            
            SaveSettingsFile(testSettings, settingFileFullPath, "Bidirectional Accuracy Test");
            StartCSV(csvFileFullPath, devices);


            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            CancellationTokenSource ctToken = new();
            CancellationTokenSource ptToken = new();
            Task<bool> cancelRequestTask = CheckCancellationRequestTask(ctToken.Token);
            Stopwatch stopWatch = new(); //Create stopwatch for rough end to end timing

            //Cycles
            stopWatch.Start();
            string approachUp;
            string approachDown;
            if(testSettings.StepSize.Val > 0)
            {
                approachUp = "Positive";
                approachDown = "Negative";
            }
            else
            {
                approachUp = "Negative";
                approachDown = "Positive";
            }
            EstimatedTimeRemaining.StartTime = DateTime.Now;
                                       
            for (uint i = 1; i <= testSettings.Cycles.Val; i++)
            {
                //Single cycle time   
                if (i == 2)
                {
                    EstimatedTimeRemaining.CycleTime = DateTime.Now - EstimatedTimeRemaining.StartTime;
                }
                //Estimate the end time based on remaining cycles and single cycle time
                if (i > 1)
                {
                    EstimatedTimeRemaining.EstimatedEndTime = DateTime.Now + EstimatedTimeRemaining.CycleTime * (testSettings.Cycles.Val - i + 1);
                }
                Console.WriteLine("Cycle " + i);

                //Check for pause or cancellation
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

                double TargetPosition = testSettings.InitialSetpoint.Val;
                //Start test at reversal position then moving to initial setpoint          
                if (await MoveAbsoluteAndWait(reversalPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));

                //going up the steps
                for (uint j = 0; j <= testSettings.NumberOfSteps.Val; j++)
                {
                    TestProgress = Convert.ToDouble(i - 1) * progScaler + Convert.ToDouble(j) * stepScaler;

                    Console.WriteLine(approachUp + " Move. Step: " + j);
                    
                    //Make the step
                    if (await MoveAbsoluteAndWait(TargetPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));


                    StandardCSVData tmpCSV = new StandardCSVData(i, j, approachUp+ " approach", TargetPosition, AxisPosition);
                    if (await WriteToCSV(csvFileFullPath, tmpCSV, devices) == false)
                    {
                        Console.WriteLine("Failed to write data to file, exiting test");
                        stopWatch.Stop();
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }
                    //Update target position
                    TargetPosition += testSettings.StepSize.Val;
                }
                //END OF APPROACH
                //Overshoot the final position before coming back down
                if (await MoveAbsoluteAndWait(overshootPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to overshoot position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));
                //going down the steps. Need the cast here as we require j to go negative to cancel the loop
                TargetPosition -= testSettings.StepSize.Val;
                for (int j = (int)testSettings.NumberOfSteps.Val; j >= 0; j--)
                {
                    TestProgress = Convert.ToDouble(i - 1) * progScaler + (Convert.ToDouble(testSettings.NumberOfSteps.Val) - Convert.ToDouble(j)) * stepScaler + stepScaler * (Convert.ToDouble(testSettings.NumberOfSteps.Val));
                    Console.WriteLine(approachDown+" Move. Step: " + j);
                    //Do the step move
                    if (await MoveAbsoluteAndWait(TargetPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));

                    StandardCSVData tmpCSV = new StandardCSVData(i, (uint)j, "Negative approach", TargetPosition, AxisPosition);
                    if (await WriteToCSV(csvFileFullPath, tmpCSV, devices) == false)
                    {
                        Console.WriteLine("Failed to write data to file, exiting test");
                        stopWatch.Stop();
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }
                    //Update target position

                    TargetPosition -= testSettings.StepSize.Val;
                }
                //Delay between cycles
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait
            }
            TestProgress = 1;
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed);
            return true;
        }
   



        public void StartCSV(string fp, MeasurementDevices md)
        {
            using (FileStream stream = File.Open(fp, FileMode.Append,FileAccess.Write,FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(stream))
            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<StandardCSVData>();
                if (md!=null)
                {
                    foreach(var device in md.MeasurementDeviceList) //for every device in the list
                    {
                        if (device.Connected)   //if the device is connected
                        {
                            foreach(var channel in device.ChannelList)  //for every channel on the device
                            {
                                csv.WriteField(channel.Item1);  //add a header for it
                            }
                        }
                    }
                }
                csv.NextRecord();
            }          
        }

        public async Task<bool> WriteToCSV(string fp, StandardCSVData csvData, MeasurementDevices md)
        {
            int retryCounter = 0;
            while(true)
            {
                try
                {
                    using (FileStream stream = File.Open(fp, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (StreamWriter writer = new StreamWriter(stream))
                    using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecord(csvData);
                        if (md != null)
                        {

                            foreach (var device in md.MeasurementDeviceList)
                            {
                                if (device.Connected)
                                {
                                    foreach (var channel in device.ChannelList)
                                    {
                                        var measurement = await device.GetChannelMeasurement(channel.Item2);
                                        Console.WriteLine(channel.Item1 + ": " + measurement);
                                        csv.WriteField(measurement);
                                    }
                                }
                            }
                        }
                        csv.NextRecord();
                    }
                    return true;
                }
                catch
                {
                    if(retryCounter == 3)
                    {
                        return false;
                    }
                    retryCounter += 1;
                    MessageBox.Show("File not accesible. Press OK to retry.\n"+ (4-retryCounter) + " attempt(s) remaining.");

                }
            }

            
        }

        private void SaveSettingsFile(NcTestSettings testSettings, string filePath, string testType)
        {
            XmlDocument doc = new();
            XmlNode rootNode = doc.CreateElement("Settings");
            doc.AppendChild(rootNode);

            TestListItem tli = new("1");
            tli.AxisID = AxisID.ToString();
            tli.TestSettings = testSettings;
            
            TestSuite.AddFields(doc,tli, rootNode);
            rootNode.SelectSingleNode("testType").InnerText = testType; //Need to manually go in and change what test type was run
            doc.Save(filePath);
        }
    }
}
