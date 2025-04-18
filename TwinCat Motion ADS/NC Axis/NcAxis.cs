﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using CsvHelper;
using System.IO;
using System.Globalization;
using System.Windows;
using TwinCat_Motion_ADS.MeasurementDevice;
using System.Net;

namespace TwinCat_Motion_ADS
{
    public partial class NcAxis : TestAdmin
    {
        public bool testRunning = false;
        private CancellationTokenSource newCommandIssuedCT = new CancellationTokenSource();
        //private bool MoveToHighInProgress = false;
        //bool MoveToLowInProgress = false;

        #region commandValues
        const byte eMoveAbsolute = 0;
        const byte eMoveRelative = 1;
        const byte eMoveVelocity = 3;
        const byte eHome = 10;
        #endregion

        #region variableHandles
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
        #endregion
        
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

        #region Basic Commands

        private bool newCommandIssued()
        {
            newCommandIssuedCT.Cancel();
            if (!ValidCommand()) return false;
            newCommandIssuedCT.Dispose();
            newCommandIssuedCT = new();
            return true;
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

        public async Task<bool> HomeAxis()
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
            await SetCommand(eHome);
            await Execute();
            return true;
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
            if (velocity <= 0)
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

        public async Task<bool> MoveVelocity(double velocity)
        {
            await ReadStatusesOneShot(CancellationToken.None);
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
            newCommandIssuedCT.Cancel();
        }

        #endregion

        #region Advanced Commands
        public async Task<bool> MoveAbsoluteAndWait(double position, double velocity, int timeout = 0)
        {
            if (!newCommandIssued()) return false;
            CancellationTokenSource ct = new();

            if (await MoveAbsolute(position, velocity))
            {
                await ReadStatusesOneShot(CancellationToken.None);
                await Task.Delay(40);   //delay to system to allow PLC to react to move command
                Task<bool> errorTask = CheckForError(ct.Token);
                Task<bool> doneTask = WaitForDone(ct.Token);
                Task<bool> cancellationTask = CancelTestTask(newCommandIssuedCT.Token);
                Task<bool> limitTask;
                List<Task> waitingTask;

                double currentPosition = AxisPosition;
                if (position > currentPosition)
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
                    waitingTask = new List<Task> { doneTask, errorTask, limitTask, timeoutTask, cancellationTask };
                }
                else
                {
                    waitingTask = new List<Task> { doneTask, errorTask, limitTask, cancellationTask };
                }

                if (await Task.WhenAny(waitingTask) == doneTask)
                {
                    Console.WriteLine("Move absolute complete");
                    ct.Cancel();
                    newCommandIssuedCT.Cancel();
                    return true;
                }
                else if (await Task.WhenAny(waitingTask) == errorTask)
                {
                    Console.WriteLine("Error on move absolute");
                    ct.Cancel();
                    newCommandIssuedCT.Cancel();
                    return false;
                }
                else if (await Task.WhenAny(waitingTask) == limitTask)
                {
                    Console.WriteLine("Limit hit before position reached");
                    ct.Cancel();
                    newCommandIssuedCT.Cancel();
                    return false;
                }
                else if (await Task.WhenAny(waitingTask) == cancellationTask)
                {
                    await Task.Delay(20);

                    ct.Cancel();
                    await MoveStop();
                    return false;
                }
                else
                {
                    Console.WriteLine("Timeout on move absolute");
                    await MoveStop();
                    newCommandIssuedCT.Cancel();
                    ct.Cancel();
                    return false;
                }
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }
        
        public async Task<bool> MoveRelativeAndWait(double position, double velocity, int timeout=0)
        {
            if (!newCommandIssued()) return false;
            CancellationTokenSource ct = new();

            if (await MoveRelative(position,velocity))
            {
                await Task.Delay(40);
                Task<bool> doneTask = WaitForDone(ct.Token);
                Task<bool> errorTask = CheckForError(ct.Token);
                Task<bool> cancellationTask = CancelTestTask(newCommandIssuedCT.Token);
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
                    waitingTask = new List<Task> { doneTask, errorTask, limitTask, timeoutTask, cancellationTask };
                }
                else
                {
                    waitingTask = new List<Task> { doneTask, errorTask, limitTask, cancellationTask };
                }

                if (await Task.WhenAny(waitingTask) == doneTask)
                {
                    Console.WriteLine("Move relative complete");
                    newCommandIssuedCT.Cancel();
                    ct.Cancel();
                    return true;
                }
                else if (await Task.WhenAny(waitingTask) == errorTask)
                {
                    Console.WriteLine("Error on move relative");
                    newCommandIssuedCT.Cancel();
                    ct.Cancel();
                    return false;
                }
                else if (await Task.WhenAny(waitingTask) == limitTask)
                {
                    Console.WriteLine("Limit hit before position reached");
                    newCommandIssuedCT.Cancel();
                    ct.Cancel();
                    return false;
                }
                else if (await Task.WhenAny(waitingTask) == cancellationTask)
                {
                    await Task.Delay(20);
                    
                    ct.Cancel();
                    await MoveStop();
                    return false;
                }
                else
                {
                    Console.WriteLine("Timeout on move relative");
                    await MoveStop();
                    newCommandIssuedCT.Cancel();
                    ct.Cancel();
                    return false;
                }
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }
        
        public async Task<bool> MoveToHighLimit(double velocity, int timeout)
        {
            if (!newCommandIssued()) return false;
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
            //Update statuses here
            await ReadStatusesOneShot(CancellationToken.None);
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckFwLimitTask(true,ct.Token);
            //Task<bool> CancelationTask = CancelTestTask(ct.Token);
            Task<bool> CancelationTask = CancelTestTask(newCommandIssuedCT.Token);
            Task<bool> errorTask = CheckForError(ct.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if(timeout==0)
            {
                waitingTask = new List<Task> { limitTask, CancelationTask, errorTask};
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token), CancelationTask, errorTask};
            }

            if(await Task.WhenAny(waitingTask)==limitTask)
            {
                Console.WriteLine("High limit reached");
                newCommandIssuedCT.Cancel();
                ct.Cancel();
                return true;
            }
            else if (await Task.WhenAny(waitingTask) == errorTask)
            {
                Console.WriteLine("Error on move to high limit");
                newCommandIssuedCT.Cancel();
                ct.Cancel();
                return false;
            }
            else if (await Task.WhenAny(waitingTask) == CancelationTask)
            {
                await Task.Delay(20);
                ct.Cancel();                
                await MoveStop();
                return false;
            }
            else//Timeout on command
            {
                await Task.Delay(20);
                newCommandIssuedCT.Cancel();
                ct.Cancel();
                Console.WriteLine("Timeout on move to high limit");
                await MoveStop();
                return false;
            }
        }

        public async Task<bool> MoveToLowLimit(double velocity, int timeout)
        {
            if (!newCommandIssued()) return false;
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
            //Update statuses here
            await ReadStatusesOneShot(CancellationToken.None);
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckBwLimitTask(true, ct.Token);
            Task<bool> cancellationTask = CancelTestTask(newCommandIssuedCT.Token);
            Task<bool> errorTask = CheckForError(ct.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task.
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask, cancellationTask, errorTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token), cancellationTask, errorTask };
            }

            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                Console.WriteLine("Low limit hit");
                newCommandIssuedCT.Cancel();
                ct.Cancel();
                return true;
            }
            else if (await Task.WhenAny(waitingTask) == errorTask)
            {
                Console.WriteLine("Error on move to low limit");
                ct.Cancel();
                newCommandIssuedCT.Cancel();
                return false;
            }
            else if(await Task.WhenAny(waitingTask) == cancellationTask)
            {
                await Task.Delay(20);
                ct.Cancel();                
                await MoveStop();
                return false;
            }
            else //Timeout on command
            {
                await Task.Delay(20);
                newCommandIssuedCT.Cancel();
                ct.Cancel();
                Console.WriteLine("Timeout on move to lower limit");
                await MoveStop();
                return false;
            }
        }

        public async Task<bool> HighLimitReversal(double velocity, int timeout, int extraReversalTime, int settleTime)
        {
            if (!newCommandIssued()) return false;
            //Only allow the command if already on the high limit
            /*if (await CheckFwLimitTask(false,CancellationToken.None) == true)
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
            //Update statuses here
            await ReadStatusesOneShot(CancellationToken.None); // NEW LINE ADDED- WARNING
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckFwLimitTask(false, ct.Token);
            Task<bool> errorTask = CheckForError(ct.Token);
            Task<bool> cancellationTask = CancelTestTask(newCommandIssuedCT.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask, errorTask, cancellationTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token), errorTask, cancellationTask };
            }
            //Monitor the checkFwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(extraReversalTime));
                await MoveStop();
                ct.Cancel();
                newCommandIssuedCT.Cancel();
            }
            else if (await Task.WhenAny(waitingTask) == errorTask)
            {
                Console.WriteLine("Error on high limit reversal");
                ct.Cancel();
                newCommandIssuedCT.Cancel();
                return false;
            }
            else if (await Task.WhenAny(waitingTask) == cancellationTask)
            {
                await Task.Delay(20);
                
                ct.Cancel();
                await MoveStop();
                return false;
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
            newCommandIssuedCT.Dispose();
            newCommandIssuedCT = new();
            CancellationTokenSource ct2 = new();
            limitTask = CheckFwLimitTask(true, ct2.Token);
            errorTask = CheckForError(ct2.Token);
            cancellationTask = CancelTestTask(newCommandIssuedCT.Token);
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask, errorTask, cancellationTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct2.Token), errorTask, cancellationTask };
            }
            //Monitor the checkFwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                ct2.Cancel();
                newCommandIssuedCT.Cancel();
                return true;
            }
            else if (await Task.WhenAny(waitingTask) == errorTask)
            {
                Console.WriteLine("Error on high limit reversal");
                ct.Cancel();
                newCommandIssuedCT.Cancel();
                return false;
            }
            else if (await Task.WhenAny(waitingTask) == cancellationTask)
            {
                await Task.Delay(20);
                
                ct.Cancel();
                await MoveStop();
                return false;
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
            if (!newCommandIssued()) return false;
            //Only allow the command if already on the low limit
            /*if (await CheckBwLimitTask(false, CancellationToken.None) == true)
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
            //Update statuses here
            await ReadStatusesOneShot(CancellationToken.None); // NEW LINE ADDED- WARNING
            CancellationTokenSource ct = new();
            Task<bool> limitTask = CheckBwLimitTask(false, ct.Token);
            Task<bool> errorTask = CheckForError(ct.Token);
            Task<bool> cancellationTask = CancelTestTask(ct.Token);
            List<Task> waitingTask;
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask, errorTask, cancellationTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token), errorTask, cancellationTask };
            }
            //Monitor the checkBwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(extraReversalTime));
                await MoveStop();
                ct.Cancel();
                newCommandIssuedCT.Cancel();
            }
            else if (await Task.WhenAny(waitingTask) == errorTask)
            {
                Console.WriteLine("Error on low limit reversal");
                ct.Cancel();
                newCommandIssuedCT.Cancel();
                return false;
            }
            else if (await Task.WhenAny(waitingTask) == cancellationTask)
            {
                await Task.Delay(20);               
                ct.Cancel();
                await MoveStop();
                return false;
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
            newCommandIssuedCT.Dispose();
            newCommandIssuedCT = new();
            CancellationTokenSource ct2 = new();
            limitTask = CheckBwLimitTask(true, ct2.Token);
            errorTask = CheckForError(ct2.Token);
            cancellationTask = CancelTestTask(newCommandIssuedCT.Token);
            //Create a new task to monitor a timeoutTask and the fw limit task. 
            if (timeout == 0)
            {
                waitingTask = new List<Task> { limitTask, errorTask, cancellationTask };
            }
            else
            {
                waitingTask = new List<Task> { limitTask, Task.Delay(TimeSpan.FromSeconds(timeout), ct2.Token), errorTask, cancellationTask };
            }
            //Monitor the checkFwEnableTask and a timeout task
            if (await Task.WhenAny(waitingTask) == limitTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                ct2.Cancel();
                newCommandIssuedCT.Cancel();
                return true;
            }
            else if (await Task.WhenAny(waitingTask) == errorTask)
            {
                Console.WriteLine("Error on high limit reversal");
                ct.Cancel();
                newCommandIssuedCT.Cancel();
                return false;
            }
            else if (await Task.WhenAny(waitingTask) == cancellationTask)
            {
                await Task.Delay(20);
                
                ct.Cancel();
                await MoveStop();
                return false;
            }
            else
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on move to limit");
                await MoveStop();
                return false;
            }
        }

        public async Task<bool> HomeAxisAndWait(int timeout = 0)
        {
            if (!newCommandIssued()) return false;
            CancellationTokenSource ct = new();

            if (await HomeAxis())
            {
                await ReadStatusesOneShot(CancellationToken.None);
                await Task.Delay(40);   //delay to system to allow PLC to react to move command
                Task<bool> errorTask = CheckForError(ct.Token);
                Task<bool> doneTask = WaitForDone(ct.Token);
                Task<bool> cancellationTask = CancelTestTask(newCommandIssuedCT.Token);
                List<Task> waitingTask;


                //Check if we need a timeout task
                if (timeout > 0)
                {
                    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), ct.Token);
                    waitingTask = new List<Task> { doneTask, errorTask, timeoutTask, cancellationTask };
                }
                else
                {
                    waitingTask = new List<Task> { doneTask, errorTask, cancellationTask };
                }

                if (await Task.WhenAny(waitingTask) == doneTask)
                {
                    Console.WriteLine("Home complete");
                    ct.Cancel();
                    newCommandIssuedCT.Cancel();
                    return true;
                }
                else if (await Task.WhenAny(waitingTask) == errorTask)
                {
                    Console.WriteLine("Error on Home");
                    ct.Cancel();
                    newCommandIssuedCT.Cancel();
                    return false;
                }

                else if (await Task.WhenAny(waitingTask) == cancellationTask)
                {
                    await Task.Delay(20);

                    ct.Cancel();
                    await MoveStop();
                    return false;
                }
                else
                {
                    Console.WriteLine("Timeout on home");
                    await MoveStop();
                    ct.Cancel();
                    newCommandIssuedCT.Cancel();
                    return false;
                }
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }

        #endregion



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
            //check there is a valid plc connection
            if (!ValidCommand()) return false;
            //check that the current parameters are valid
            if (!SanityCheckSettings(testSettings, TestTypes.EndToEnd)) return false;
            //Update the progress scaler values based on current test and settings
            ResetAndCalculateProgressScalers(testSettings, TestTypes.EndToEnd);
            testSettings.TestType.Val = TestTypes.EndToEnd;
            //Check for pause or cancellation request
            await PauseTask(CancellationToken.None);
            if (IsTestCancelled()) return false;

            //Setup test CSV and setting files
            string settingFileFullPath = GenerateSettingsPath(testSettings);
            string csvFileFullPath = GenerateCSVPath(testSettings);
            SaveSettingsFile(testSettings, settingFileFullPath);
            StartCSV(csvFileFullPath, devices);

            //Set the test is running flag
            testRunning = true;

            //Start stopwatch for time of test
            Stopwatch stopWatch = new();
            stopWatch.Start();  //Clear and start the stopwatch
            
            //Move to the lower limit switch
            if (await MoveToLowLimit(-testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
            {
                Console.WriteLine("Failed to move to low limit for start of test");
                testRunning = false;
                return false;
            }
            //Delay start of sequence by settle time
            await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds.Val));

            //Test Cycles
            for (uint cycleCount = 1; cycleCount <= testSettings.Cycles.Val; cycleCount++)
            {
                //Check for pause or cancellation request
                await PauseTask(CancellationToken.None);
                if (IsTestCancelled())
                {
                    testRunning = false;
                    return false;
                }

                    //Update the estimated time
                    EstimatedTimeRemaining.TimeEstimateUpdate(cycleCount, testSettings.Cycles.Val);
                
                //Move to high limit
                if (await MoveToHighLimit(testSettings.Velocity.Val, (int)testSettings.Timeout.Val))
                {
                    //Calculate current test progress
                    CalculateCurrentProgress(cycleCount - 1, 0);
                    
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds.Val));//Allow axis to settle before reversal
                    if (await HighLimitReversal(testSettings.ReversalVelocity.Val, (int)testSettings.Timeout.Val, (int)testSettings.ReversalExtraTimeSeconds.Val, (int)testSettings.ReversalSettleTimeSeconds.Val))
                    {
                        //Write test data
                        StandardCSVData tmpCSV = new StandardCSVData((uint)cycleCount, 0, "Moving to high limit", 0, AxisPosition);
                        if (await WriteToCSV(csvFileFullPath, tmpCSV, devices) == false)
                        {
                            Console.WriteLine("Failed to write data to file, exiting test");
                            testRunning = false;
                            stopWatch.Stop();
                            return false;
                        }
                        Console.WriteLine("Cycle " + cycleCount + "- Low limit to high limit: " + "ms. High limit triggered at " + AxisPosition);
                    }
                    else
                    {
                        Console.WriteLine("High limit reversal failed");
                        testRunning = false;
                        SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                        return false;
                    }
                }
                else
                {
                    testRunning = false;
                    stopWatch.Stop();
                    Console.WriteLine("Move to high limit failed");
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }
                //Check for pause or cancellation request
                await PauseTask(CancellationToken.None);
                if (IsTestCancelled())
                {
                    testRunning = false;
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }

                //Move to low limit
                if (await MoveToLowLimit(-testSettings.Velocity.Val, (int)testSettings.Timeout.Val))
                {
                    //Calculate current test progress
                    CalculateCurrentProgress(cycleCount - 1, 1);

                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds.Val));//Allow axis to settle before reversal
                    if (await LowLimitReversal(testSettings.ReversalVelocity.Val, (int)testSettings.Timeout.Val, (int)testSettings.ReversalExtraTimeSeconds.Val, (int)testSettings.ReversalSettleTimeSeconds.Val))
                    {
                        //Write test data
                        StandardCSVData tmpCSV = new StandardCSVData((uint)cycleCount, 0, "Moving to low limit", 0, AxisPosition);
                        if (await WriteToCSV(csvFileFullPath, tmpCSV, devices) == false)
                        {
                            Console.WriteLine("Failed to write data to file, exiting test");
                            testRunning = false;
                            stopWatch.Stop();
                            return false;
                        }
                        Console.WriteLine("Cycle " + cycleCount + "- High limit to low limit: " + "ms. Low limit triggered at " + AxisPosition);
                    }
                    else
                    {
                        Console.WriteLine("Low limit reversal failed");
                        testRunning = false;
                        SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                        return false;
                    }
                }
                else
                {
                    testRunning = false;
                    stopWatch.Stop();
                    Console.WriteLine("Move to low limit failed");
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait
                if(cycleCount == 1)
                {
                    SendHttpRequest(testSettings.TestTitle.Val, "First cycle complete");
                }
                else
                {
                    SendHttpRequest(testSettings.TestTitle.Val, "Cycle " + cycleCount + " complete\n \nEstimated finish at " + EstimatedTimeRemaining.EstimatedEndTime.ToString());
                }
            }
            TestProgress = 1;
            testRunning = false;
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed + "ms");
            SendHttpRequest(testSettings.TestTitle.Val, "Test completed");
            return true;
        }

        public async Task<bool> UniDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            //check there is a valid plc connection
            if (!ValidCommand()) return false;
            //check that the current parameters are valid
            if (!SanityCheckSettings(testSettings, TestTypes.UnidirectionalAccuracy)) return false;
            //Update the progress scaler values based on current test and settings
            ResetAndCalculateProgressScalers(testSettings, TestTypes.UnidirectionalAccuracy);
            testSettings.TestType.Val = TestTypes.UnidirectionalAccuracy;
            //Check for pause or cancellation request
            await PauseTask(CancellationToken.None);
            if (IsTestCancelled()) return false;

            //Setup test CSV and setting files
            string settingFileFullPath = GenerateSettingsPath(testSettings);
            string csvFileFullPath = GenerateCSVPath(testSettings);
            SaveSettingsFile(testSettings, settingFileFullPath);
            StartCSV(csvFileFullPath, devices);

            //Set the test is running flag
            testRunning = true;

            //Start stopwatch for time of test
            Stopwatch stopWatch = new();
            stopWatch.Start();  //Clear and start the stopwatch

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

            // Test Cycles
            for (uint cycleCount = 1; cycleCount <= testSettings.Cycles.Val; cycleCount++)
            {
                //Check for pause or cancellation request
                await PauseTask(CancellationToken.None);
                if (IsTestCancelled())
                {
                    testRunning = false;
                    try
                    {
                        SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    }
                    catch { }
                    
                    return false;
                }
               
                EstimatedTimeRemaining.TimeEstimateUpdate(cycleCount, testSettings.Cycles.Val);

                Console.WriteLine("Cycle " + cycleCount);

                //Set target position to cycle initial setpoint
                double TargetPosition = testSettings.InitialSetpoint.Val;

                //Start test at reversal position then moving to initial setpoint          
                if (await MoveAbsoluteAndWait(reversalPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    testRunning = false;
                    stopWatch.Stop();
                    try { SendHttpRequest(testSettings.TestTitle.Val, "Test failed"); }
                    catch { }
                    
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));

                //Steps of cycle
                if (await UniDirectionalSingleCycle(testSettings, cycleCount, TargetPosition, devices, csvFileFullPath) == false)
                {
                    testRunning = false;
                    try { SendHttpRequest(testSettings.TestTitle.Val, "Test failed"); }
                    catch { }
                    
                    return false;
                }
                //Delay between cycles
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait
                if (cycleCount == 1)
                {
                    try { SendHttpRequest(testSettings.TestTitle.Val, "First cycle complete"); }
                    catch { }
                    
                }
                else
                {
                    try
                    {
                        SendHttpRequest(testSettings.TestTitle.Val, "Cycle " + cycleCount + " complete\n \nEstimated finish at " + EstimatedTimeRemaining.EstimatedEndTime.ToString());
                    }
                    catch { }
                }
            }
            TestProgress = 1;
            testRunning = false;
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed + "ms");
            try { SendHttpRequest(testSettings.TestTitle.Val, "Test complete"); }
            catch { }
            
            return true;
        }

        public async Task<bool> BiDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            //check there is a valid plc connection
            if (!ValidCommand()) return false;
            //check that the current parameters are valid
            if (!SanityCheckSettings(testSettings, TestTypes.BidirectionalAccuracy)) return false;
            //Update the progress scaler values based on current test and settings
            ResetAndCalculateProgressScalers(testSettings, TestTypes.BidirectionalAccuracy);
            testSettings.TestType.Val = TestTypes.BidirectionalAccuracy;
            //Check for pause or cancellation request
            await PauseTask(CancellationToken.None);
            if (IsTestCancelled()) return false;

            //Setup test CSV and setting files
            string settingFileFullPath = GenerateSettingsPath(testSettings);
            string csvFileFullPath = GenerateCSVPath(testSettings);
            SaveSettingsFile(testSettings, settingFileFullPath);
            StartCSV(csvFileFullPath, devices);

            //Set the test is running flag
            testRunning = true;

            //Start stopwatch for time of test
            Stopwatch stopWatch = new();
            stopWatch.Start();  //Clear and start the stopwatch

            //Establish "Reversal" and "Overshoot" positions of test
            double reversalPosition;
            double endPosition = testSettings.InitialSetpoint.Val + ((testSettings.NumberOfSteps.Val) * testSettings.StepSize.Val);
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
                overshootPosition = endPosition + testSettings.OvershootDistance.Val;
            }
            else
            {
                overshootPosition = endPosition - testSettings.OvershootDistance.Val;
            }


            //Test Cycles
            for (uint cycleCount = 1; cycleCount <= testSettings.Cycles.Val; cycleCount++)
            {
                //Check for pause or cancellation request
                await PauseTask(CancellationToken.None);
                if (IsTestCancelled())
                {
                    testRunning = false;
                    return false;
                }
                
                //Update time estimate
                EstimatedTimeRemaining.TimeEstimateUpdate(cycleCount, testSettings.Cycles.Val);

                //Print current cycle to console
                Console.WriteLine("Cycle " + cycleCount);
                
                
                //Move to reversal position         
                if (await MoveAbsoluteAndWait(reversalPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    testRunning = false;
                    stopWatch.Stop();
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }
                //delay
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));
                
                //First approach cycle
                if (await UniDirectionalSingleCycle(testSettings, cycleCount, testSettings.InitialSetpoint.Val, devices, csvFileFullPath,0, false) == false)
                {
                    testRunning = false;
                    stopWatch.Stop();
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }

                //Move to overshoot position
                if (await MoveAbsoluteAndWait(overshootPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to overshoot position");
                    testRunning = false;
                    stopWatch.Stop();
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }          
                //delay
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));
                
                //Second approach cycle
                if (await UniDirectionalSingleCycle(testSettings, cycleCount, endPosition, devices, csvFileFullPath,0, true) == false)
                {
                    testRunning = false;
                    stopWatch.Stop();
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }
                //Delay between cycles
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait
                if (cycleCount == 1)
                {
                    SendHttpRequest(testSettings.TestTitle.Val, "First cycle complete");
                }
                else
                {
                    SendHttpRequest(testSettings.TestTitle.Val, "Cycle " + cycleCount + " complete\n \nEstimated finish at " + EstimatedTimeRemaining.EstimatedEndTime.ToString());
                }
            }
            TestProgress = 1;
            testRunning = false;
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed);
            SendHttpRequest(testSettings.TestTitle.Val, "Test complete");
            return true;
        }

        public async Task<bool> ScalingTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            //check there is a valid plc connection
            if (!ValidCommand()) return false;
            //check that the current parameters are valid
            if (!SanityCheckSettings(testSettings, TestTypes.ScalingTest)) return false;
            //Update the progress scaler values based on current test and settings
            ResetAndCalculateProgressScalers(testSettings, TestTypes.ScalingTest);
            testSettings.TestType.Val = TestTypes.ScalingTest;
            //Check for pause or cancellation request
            await PauseTask(CancellationToken.None);
            if (IsTestCancelled()) return false;

            //Setup test CSV and setting files
            string settingFileFullPath = GenerateSettingsPath(testSettings);
            string csvFileFullPath = GenerateCSVPath(testSettings);
            SaveSettingsFile(testSettings, settingFileFullPath);
            StartCSV(csvFileFullPath, devices);

            //Set the test is running flag
            testRunning = true;

            //Start stopwatch for time of test
            Stopwatch stopWatch = new();
            stopWatch.Start();  //Clear and start the stopwatch

            double targetPosition;
            double reversalPosition1;
            double reversalPosition2;
            if (testSettings.StepSize.Val > 0)
            {
                reversalPosition1 = testSettings.InitialSetpoint.Val - testSettings.ReversalDistance.Val;
                reversalPosition2 = testSettings.EndSetpoint.Val - testSettings.ReversalDistance.Val;
            }
                else
            {
                reversalPosition1 = testSettings.InitialSetpoint.Val + testSettings.ReversalDistance.Val;
                reversalPosition2 = testSettings.EndSetpoint.Val + testSettings.ReversalDistance.Val;
            }

            for (uint cycleCount = 1; cycleCount <= testSettings.Cycles.Val; cycleCount++)
            {
                //Update the estimated time
                EstimatedTimeRemaining.TimeEstimateUpdate(cycleCount, testSettings.Cycles.Val);

                //Start test at reversal position then moving to initial setpoint          
                if (await MoveAbsoluteAndWait(reversalPosition1, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    testRunning = false;
                    stopWatch.Stop();
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));

                targetPosition = testSettings.InitialSetpoint.Val;
                if (await UniDirectionalSingleCycle(testSettings, cycleCount, targetPosition, devices, csvFileFullPath) == false)
                {
                    testRunning = false;
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }

                if (await MoveAbsoluteAndWait(reversalPosition2, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    testRunning = false;
                    stopWatch.Stop();
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));
                targetPosition = testSettings.EndSetpoint.Val;
                if (await UniDirectionalSingleCycle(testSettings, cycleCount, targetPosition, devices, csvFileFullPath, testSettings.NumberOfSteps.Val) == false)
                {
                    testRunning = false;
                    SendHttpRequest(testSettings.TestTitle.Val, "Test failed");
                    return false;
                }
                //Delay between cycles
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait

                SendHttpRequest(testSettings.TestTitle.Val, "Cycle " + cycleCount + " complete\nEstimated finish at " + EstimatedTimeRemaining.EstimatedEndTime.ToString());
            }
            TestProgress = 1;
            testRunning = false;
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed + "ms");
            SendHttpRequest(testSettings.TestTitle.Val, "Test complete");
            return true;
        }

        public async Task<bool> BacklashDetectionTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            //check there is a valid plc connection
            if (!ValidCommand()) return false;
            //check that the current parameters are valid
            if (!SanityCheckSettings(testSettings, TestTypes.BacklashDetection)) return false;
            //Update the progress scaler values based on current test and settings
            ResetAndCalculateProgressScalers(testSettings, TestTypes.BacklashDetection);
            testSettings.TestType.Val = TestTypes.BacklashDetection;
            //Check for pause or cancellation request
            await PauseTask(CancellationToken.None);
            if (IsTestCancelled()) return false;

            //Setup test CSV and setting files
            string settingFileFullPath = GenerateSettingsPath(testSettings);
            string csvFileFullPath = GenerateCSVPath(testSettings);
            SaveSettingsFile(testSettings, settingFileFullPath);
            StartCSV(csvFileFullPath, devices);

            //Start stopwatch for time of test
            Stopwatch stopWatch = new();
            stopWatch.Start();  //Clear and start the stopwatch

            //Establish "Reversal" position of test
            double reversalPosition;
            if (testSettings.StepSize.Val > 0)
            {
                reversalPosition = testSettings.InitialSetpoint.Val + testSettings.ReversalDistance.Val;
            }
            else
            {
                reversalPosition = testSettings.InitialSetpoint.Val - testSettings.ReversalDistance.Val;
            }

            // Test Cycles
            for (uint cycleCount = 1; cycleCount <= testSettings.Cycles.Val; cycleCount++)
            {
                //Check for pause or cancellation request
                await PauseTask(CancellationToken.None);
                if (IsTestCancelled()) return false;

                EstimatedTimeRemaining.TimeEstimateUpdate(cycleCount, testSettings.Cycles.Val);

                Console.WriteLine("Cycle " + cycleCount);

                //Set target position to cycle initial setpoint
                double TargetPosition = testSettings.InitialSetpoint.Val;

                //Start test at reversal position then moving to initial setpoint          
                if (await MoveAbsoluteAndWait(reversalPosition, testSettings.Velocity.Val, (int)testSettings.Timeout.Val) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds.Val));

                //Steps of cycle
                if (await UniDirectionalSingleCycle(testSettings, cycleCount, TargetPosition, devices, csvFileFullPath) == false)
                {
                    return false;
                }
                //Delay between cycles
                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds.Val)); //inter-cycle delay wait
            }
            TestProgress = 1;
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed + "ms");
            return true;
        }

        public async Task<bool> HomingRepeatabilityTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {

            return false;
        }


        private async Task<bool> UniDirectionalSingleCycle(NcTestSettings ts, uint currentCycle, double TargetPosition, MeasurementDevices md, string csvFile, uint additionalSteps = 0, bool reverseStepCount = false)
        {
            string approachUp;
            string approachDown;
            if (ts.StepSize.Val > 0)
            {
                approachUp = "Positive";
                approachDown = "Negative";
            }
            else
            {
                approachUp = "Negative";
                approachDown = "Positive";
            }

            if (reverseStepCount==false)
            {
                for (uint stepCount = 0; stepCount <= ts.NumberOfSteps.Val; stepCount++)
                {
                    //Check for pause or cancellation request
                    await PauseTask(CancellationToken.None);
                    if (IsTestCancelled()) return false;
                    CalculateCurrentProgress(currentCycle - 1, stepCount + additionalSteps);
                    Console.WriteLine(approachUp + " Move. Step: " + stepCount);


                    //Absolute position move (Exit test if failure)
                    if (await MoveAbsoluteAndWait(TargetPosition, ts.Velocity.Val, (int)ts.Timeout.Val) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        return false;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(ts.SettleTimeSeconds.Val));

                    //Log the data, if test fails to write to fail, exit test
                    StandardCSVData tmpCSV = new StandardCSVData(currentCycle, stepCount, approachUp + " approach", TargetPosition, AxisPosition);
                    if (await WriteToCSV(csvFile, tmpCSV, md) == false)
                    {
                        Console.WriteLine("Failed to write data to file, exiting test");
                        return false;
                    }
                    TargetPosition += ts.StepSize.Val;
                }
            }
            else
            {
                for(int stepCount = (int)ts.NumberOfSteps.Val;stepCount >=0;stepCount--)
                {
                    //Check for pause or cancellation request
                    await PauseTask(CancellationToken.None);
                    if (IsTestCancelled()) return false;

                    CalculateCurrentProgress(currentCycle - 1, (uint)(2 * ts.NumberOfSteps.Val - stepCount));

                    Console.WriteLine(approachDown + " Move. Step: " + stepCount);
                    //Do the step move
                    if (await MoveAbsoluteAndWait(TargetPosition, ts.Velocity.Val, (int)ts.Timeout.Val) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(ts.SettleTimeSeconds.Val));

                    StandardCSVData tmpCSV = new StandardCSVData(currentCycle, (uint)stepCount, approachDown+" approach", TargetPosition, AxisPosition);
                    if (await WriteToCSV(csvFile, tmpCSV, md) == false)
                    {
                        Console.WriteLine("Failed to write data to file, exiting test");
                        return false;
                    }
                    //Update target position
                    TargetPosition -= ts.StepSize.Val;
                }
            }
            


            return true;
        }

        private static string flowURL = "https://prod-139.westeurope.logic.azure.com:443/workflows/50481148dc2546839ee6d746c2efed50/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=L7MRywsTPKwyTz9I2nEvM_1rRhNQSCHdbvxs6AYQHxQ";

        public void SendHttpRequest(string Id, string Status)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(flowURL);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {

                string json = "{\"Id\":\"" + Id + "\"," + "\"Status\":\"" + Status + "\"}";
                streamWriter.Write(json);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
        }


        #region Helper Methods
        private bool SanityCheckSettings(NcTestSettings ts, TestTypes tt)
        {
            bool checkValid = true;
            if (ts.Cycles.Val <= 0)
            {
                Console.WriteLine("Cycle count cannot be <=0");
                checkValid = false;
            }

            ts.Velocity.Val = Math.Abs(ts.Velocity.Val);
            if (ts.Velocity.Val == 0)
            {
                checkValid = false;
            }
            switch (tt)
            {
                case TestTypes.EndToEnd:
                    if(ts.ReversalVelocity.Val<=0)
                    {
                        Console.WriteLine("Reversal velocity cannot be <= 0");
                        checkValid = false;
                    }
                    if(ts.ReversalExtraTimeSeconds.Val<=0)
                    {
                        Console.WriteLine("Reversal time cannot be <= 0");
                        checkValid = false;
                    }
                    if(ts.ReversalSettleTimeSeconds.Val<=0)
                    {
                        Console.WriteLine("Reversal settle time cannot be <= 0");
                        checkValid = false;
                    }
                    break;
                case TestTypes.UnidirectionalAccuracy:
                case TestTypes.BidirectionalAccuracy:
                case TestTypes.ScalingTest:
                case TestTypes.BacklashDetection:
                    if (ts.NumberOfSteps.Val <= 0)
                    {
                        Console.WriteLine("Number of steps is invalid");
                        checkValid = false;
                    }
                    if (ts.StepSize.Val == 0)
                    {
                        Console.WriteLine("Step size is invalid");
                        checkValid = false;
                    }
                    break;                
            }

            return checkValid;
        }


        private void ResetAndCalculateProgressScalers(NcTestSettings ts, TestTypes tt)
        {
            TestProgress = 0;
            progScaler = 1 / Convert.ToDouble(ts.Cycles.Val);
            switch (tt)
            {
                case TestTypes.EndToEnd:
                    stepScaler = progScaler / 2;
                    break;
                case TestTypes.UnidirectionalAccuracy:
                case TestTypes.BacklashDetection:
                    stepScaler = progScaler / (Convert.ToDouble(ts.NumberOfSteps.Val) + 1);
                    break;
                case TestTypes.BidirectionalAccuracy:
                case TestTypes.ScalingTest:
                    stepScaler = progScaler / ((Convert.ToDouble(ts.NumberOfSteps.Val) + 1) * 2);
                    break;
            }
        }

        private void CalculateCurrentProgress(uint cycle, uint step)
        {
            TestProgress = cycle * progScaler + step * stepScaler;
        }

        public string GenerateTestFileName(NcTestSettings ts)
        {
            DateTime currentTime = DateTime.Now;
            string testTitle = string.Format(@"{0:yyMMdd} {0:HH}h{0:mm}m{0:ss}s Axis {1}~ " + ts.TestTitle.UiVal, currentTime, AxisID);
            string filePath = TestDirectory + @"\" + testTitle;
            return filePath;
        }
        
        public string GenerateCSVPath(NcTestSettings ts)
        {
            return GenerateTestFileName(ts) + ".csv";
        }
       
        public string GenerateSettingsPath(NcTestSettings ts)
        {
            return GenerateTestFileName(ts) + ".xml";
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

        private void SaveSettingsFile(NcTestSettings testSettings, string filePath)
        {
            testSettings.ExportSettingsXml(filePath, "1");
        }

        
        #endregion
    }
}
