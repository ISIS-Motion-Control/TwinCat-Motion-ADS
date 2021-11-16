using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using CsvHelper;
using System.IO;
using System.Globalization;


namespace TwinCat_Motion_ADS
{
    public class NcAxis : TestAdmin
    {    
        //eCommand enumeration values
        const byte eMoveAbsolute = 0;
        const byte eMoveRelative = 1;
        const byte eMoveVelocity = 3;
        const byte eHome = 10;

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

        //Axis error
        private bool _error;
        public bool Error
        {
            get { return _error; }
            set { _error = value; OnPropertyChanged(); }
        }
        
        //Axis position (encoder)
        private double _axisPosition;
        public double AxisPosition
        {
            get { return _axisPosition; }
            set { _axisPosition = value; OnPropertyChanged();}
        }
        //Axis enabled status
        private bool _axisEnabled;
        public bool AxisEnabled
        {
            get { return _axisEnabled; }
            set { _axisEnabled = value; OnPropertyChanged(); }
        }
        //Axis forward enabled status
        private bool _axisFwEnabled;
        public bool AxisFwEnabled
        {
            get { return _axisFwEnabled; }
            set { _axisFwEnabled = value; OnPropertyChanged(); }
        }
        //Axis backward enabled status
        private bool _axisBwEnabled;
        public bool AxisBwEnabled
        {
            get { return _axisBwEnabled; }
            set { _axisBwEnabled = value; OnPropertyChanged(); }
        }

        //Current axis ID
        public uint AxisID { get; set; }

        /// <summary>
        /// Axis Class Constructor
        /// </summary>
        /// <param name="axisID"></param>
        /// <param name="plc"></param>
        /// <param name="dti1Present"></param>
        /// <param name="dti2Present"></param>
        public NcAxis(uint axisID, PLC plc)
        {
            Plc = plc;
            AxisID = axisID;
            updateInstance(axisID);
        }

        /// <summary>
        /// Update current axis instance  - generates new variable handles for testing a different axis ID
        /// </summary>
        /// <param name="axisID"></param>
        /// <param name="dti1Present"></param>
        /// <param name="dti2Present"></param>
        public void updateInstance(uint axisID)
        {
            StopPositionRead();
            AxisID = axisID;
            //These variable handles rely on the twinCAT standard solution naming.
            eCommandHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.eCommand");
            fVelocityHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.fVelocity");
            fPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.fPosition");
            bExecuteHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.bExecute");
            fActPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.fActPosition");
            bDoneHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bDone");
            bBusyHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bBusy");
            bFwEnabledHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bFwEnabled");
            bBwEnabledHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bBwEnabled");
            bEnabledHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bEnabled");
            bStopHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.bHalt");    //bStop causes an error on the axis. bHalt just ends movement
            bErrorHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bError");
            bEnableHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.bEnable");
            bResetHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.bReset");
            StartPositionRead();            

        }

        /// <summary>
        /// Set the command on the axis
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task setCommand(byte command)
        {
            await Plc.TcAds.WriteAnyAsync(eCommandHandle, command, CancellationToken.None);
        }
        
        /// <summary>
        /// Set the velocity on the axis
        /// </summary>
        /// <param name="velocity"></param>
        /// <returns></returns>
        private async Task setVelocity(double velocity)
        {
            await Plc.TcAds.WriteAnyAsync(fVelocityHandle, velocity, CancellationToken.None);
        }

        /// <summary>
        /// Set the position on the axis
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private async Task setPosition(double position)
        {
            await Plc.TcAds.WriteAnyAsync(fPositionHandle, position, CancellationToken.None);
        }

        /// <summary>
        /// Set the execute on the axis TRUE
        /// </summary>
        /// <returns></returns>
        private async Task execute()
        {
            await Plc.TcAds.WriteAnyAsync(bExecuteHandle, true, CancellationToken.None);
        }

        /// <summary>
        /// Set the enable flag on the axis
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public async Task setEnable(bool enable)
        {
            await Plc.TcAds.WriteAnyAsync(bEnableHandle, enable, CancellationToken.None);
        }

        /// <summary>
        /// Reset the axis
        /// </summary>
        /// <returns></returns>
        public async Task Reset()
        {
            await Plc.TcAds.WriteAnyAsync(bResetHandle, true, CancellationToken.None);
        }

        

        /// <summary>
        /// Send an absolute move command to the axis
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <returns>True if the command sucessfully sends. False is the axis is busy, in an error state</returns>
        public async Task<bool> moveAbsolute(double position, double velocity)
        {
            if (await read_bBusy())
            {
                return false;   //command fails if axis already busy
            }
            if (await read_bError())
            {
                return false;
            }
            if(velocity == 0)
            {
                return false;
            }
            

            var commandTask = setCommand(eMoveAbsolute);
            var velocityTask = setVelocity(velocity);
            var positionTask = setPosition(position);

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
            await execute();
            return true;
        }

        /// <summary>
        /// Send a move absolute command to the axis and wait for completion.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <returns>True if move completed sucessfully, False if any part of the command fails.</returns>
        public async Task<bool> moveAbsoluteAndWait(double position, double velocity, int timeout = 0)
        {
            CancellationTokenSource ct = new CancellationTokenSource();

            if (await moveAbsolute(position, velocity))
            {
                await Task.Delay(40);   //delay to system to allow PLC to react to move command
                Task<bool> doneTask = waitForDone(ct.Token);
                Task<bool> errorTask = checkForError(ct.Token);
                Task<bool> limitTask;
                List<Task> waitingTask;
                
                //Check direction of travel for monitoring limits
                double currentPosition = await read_AxisPosition();
                if(position>currentPosition)
                {
                    limitTask = checkFwLimitTask(true, ct.Token);
                }
                else
                {
                    limitTask = checkBwLimitTask(true, ct.Token);
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
                    await moveStop();
                    ct.Cancel();
                    return false;
                }
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }

        private async Task<bool> checkFwLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = 50)
        {
            //if limitStatus true, watch for FwEnabled flag to go false and complete the task
            while (await read_bFwEnabled() == limitStatus)
            {
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(msDelay);
            }
            return true;
        }
        private async Task<bool> checkBwLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = 50)
        {
            //if limitStatus true, watch for BwEnabled flag to go false and complete the task
            while (await read_bBwEnabled() == limitStatus)
            {
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(msDelay);
            }
            return true;
        }
        


        /// <summary>
        /// Send a relative move command to the axis
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <returns>True if the command sucessfully sends. False is the axis is busy, in an error state</returns>
        public async Task<bool> moveRelative(double position, double velocity)
        {
            if (await read_bBusy())
            {
                return false;   //command fails if axis already busy
            }
            if (await read_bError())
            {
                return false;
            }
            if (velocity <=0)
            { return false; }
            var commandTask = setCommand(eMoveRelative);
            var velocityTask = setVelocity(velocity);
            var positionTask = setPosition(position);

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
            await execute();
            return true;
        }

        /// <summary>
        /// Send a move relative command to the axis and wait for completion. There is no timeout on this.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <returns>True if the command sucessfully sends. False is the axis is busy, in an error state</returns>
        public async Task<bool> moveRelativeAndWait(double position, double velocity, int timeout=0)
        {
            CancellationTokenSource ct = new CancellationTokenSource();

            if (await moveRelative(position,velocity))
            {
                await Task.Delay(40);
                Task<bool> doneTask = waitForDone(ct.Token);
                Task<bool> errorTask = checkForError(ct.Token);
                Task<bool> limitTask;
                List<Task> waitingTask;
                
                //Check direction of travel for monitoring limits
                if (position > 0)
                {
                    limitTask = checkFwLimitTask(true, ct.Token);
                }
                else
                {
                    limitTask = checkBwLimitTask(true, ct.Token);
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
                    await moveStop();
                    ct.Cancel();
                    return false;
                }
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }
        
        /// <summary>
        /// Send a continuos velocity move to the axis
        /// </summary>
        /// <param name="velocity"></param>
        /// <returns></returns>
        public async Task<bool> moveVelocity(double velocity)
        {
            if (await read_bBusy())
            {
                return false;   //command fails if axis already busy
            }
            if (await read_bError())
            {
                return false;
            }
            if (velocity == 0)
            {
                return false;
            }
            var commandTask = setCommand(eMoveVelocity);
            var velocityTask = setVelocity(velocity);
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
            await execute();
            return true;
        }
        
        /// <summary>
        /// Send a halt command to the axis
        /// </summary>
        /// <returns></returns>
        public async Task moveStop()
        {
            await Plc.TcAds.WriteAnyAsync(bStopHandle, true, CancellationToken.None);
        }
        
        /// <summary>
        /// Monitor and return true when bDone is true on axis
        /// </summary>
        /// <returns></returns>
        public async Task<bool> waitForDone(CancellationToken wToken)
        {
            bool doneStatus = await read_bDone();
            while (doneStatus != true)
            {
                doneStatus = await read_bDone();
                //Cancellation method for task otherwise task will never complete if no done signal ever received
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }              
            }
            return true;
        }

        /// <summary>
        /// Monitor and return true when bError is true on axis
        /// </summary>
        /// <param name="wToken"></param>
        /// <returns></returns>
        public async Task<bool> checkForError(CancellationToken wToken)
        {
            bool errorStatus = await read_bError();
            while(errorStatus!=true)
            {
                errorStatus = await read_bError();
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }                
            }
            Console.WriteLine("Axis error");
            return true;
        }
        
        /// <summary>
        /// Move the axis to the high limit
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> moveToHighLimit(double velocity, int timeout)
        {
            //Check to see if already on the high limit
            if(await read_bFwEnabled() == false)
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
                velocity = velocity * -1;
            }
            //Send a move velocity command
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Command rejected");
                return false;
            };
            //Start a task to check the FwEnabled bool that only returns when flag is hit (fwEnabled == false)
            CancellationTokenSource ct = new CancellationTokenSource();
            Task<bool> limitTask = checkFwLimitTask(true,ct.Token);
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
                Console.WriteLine("Forward limit reached");
                ct.Cancel();
                return true;
            }
            else //Timeout on command
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on move to forward limit");
                await moveStop();
                return false;
            }
        }

        /// <summary>
        /// Move the axis to the low limit
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> moveToLowLimit(double velocity, int timeout)
        {
            //Check to see if already on the low limit
            if (await read_bBwEnabled() == false)
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
                velocity = velocity * -1;
            }
            //Send a move velocity command
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Command rejected");
                return false;
            };
            //Start a task to check the BwEnabled bool that only returns when flag is hit (BwEnabled == false)
            CancellationTokenSource ct = new CancellationTokenSource();
            Task<bool> limitTask = checkBwLimitTask(true, ct.Token);
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
                Console.WriteLine("Backward limit reached");
                ct.Cancel();
                return true;
            }
            else //Timeout on command
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on move to backward limit");
                await moveStop();
                return false;
            }
        }

        /// <summary>
        /// Perform a reversing sequence to find the high limit position
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="timeout">(seconds)</param>
        /// <param name="extraReversalTime">(seconds) Additional reversal time after regaining the FwEnable signal to back away from the switch</param>
        /// <param name="settleTime">(seconds) Time after hitting the limit for the axis to settle</param>
        /// <returns></returns>
        public async Task<bool> HighLimitReversal(double velocity, int timeout, int extraReversalTime, int settleTime)
        {   
            //Only allow the command if already on the high limit
            if (await read_bFwEnabled() == true)
            {
                Console.WriteLine("Not on high limit. Reversal command rejected");
                return false;
            }
            //Correct the velocity setting if needed
            if (velocity < 0)
            {
                velocity = velocity *-1;
            }
            //Reject 0 velocity value
            if (velocity == 0)
            {
                Console.WriteLine("Cannot have velocity of zero");
                return false;
            }
            //Start a reversal off the limit switch
            if (await moveVelocity(-velocity) == false)
            {
                Console.WriteLine("Reversal command rejected");
                return false;
            }
            //Start a task to monitor when the FwEnable signal is regained
            CancellationTokenSource ct = new CancellationTokenSource();
            Task<bool> limitTask = checkFwLimitTask(false, ct.Token);
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
                await moveStop();
                ct.Cancel();
            }
            else
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on reversal");
                await moveStop();
                return false;
            }
            //Velocity move back on to high limit
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Approach high limit command rejected ");
                return false;
            }
            //Restart the checkFwEnable task to find when it is hit. Run at much faster rate
            waitingTask.Clear();
            CancellationTokenSource ct2 = new CancellationTokenSource();
            limitTask = checkFwLimitTask(true, ct2.Token);
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
                await moveStop();
                return false;
            }
        }
        
        /// <summary>
        /// Perform a reversing sequence to find the low limit position
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="timeout">(seconds)</param>
        /// <param name="extraReversalTime">(seconds) Additional reversal time after regaining the FwEnable signal to back away from the switch</param>
        /// <param name="settleTime">(seconds) Time after hitting the limit for the axis to settle</param>
        /// <returns></returns>
        public async Task<bool> LowLimitReversal(double velocity, int timeout, int extraReversalTime, int settleTime)
        {
            //Only allow the command if already on the low limit
            if (await read_bBwEnabled() == true)
            {
                Console.WriteLine("Not on low limit. Reversal command rejected");
                return false;
            }
            //Correct the velocity setting if needed
            if (velocity > 0)
            {
                velocity = velocity * -1;
            }
            //Reject 0 velocity value
            if (velocity == 0)
            {
                Console.WriteLine("Cannot have velocity of zero");
                return false;
            }
            //Start a reversal off the limit switch
            if (await moveVelocity(-velocity) == false)
            {
                Console.WriteLine("Reversal command rejected");
                return false;
            }
            //Start a task to monitor when the FwEnable signal is regained
            CancellationTokenSource ct = new CancellationTokenSource();
            Task<bool> limitTask = checkBwLimitTask(false, ct.Token);
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
                await moveStop();
                ct.Cancel();
            }
            else
            {
                await Task.Delay(20);
                Console.WriteLine("Timeout on reversal");
                await moveStop();
                return false;
            }
            //Velocity move back on to low limit
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Approach low limit command rejected ");
                return false;
            }
            //Restart the checkBwEnable task to find when it is hit. Run at much faster rate
            waitingTask.Clear();
            CancellationTokenSource ct2 = new CancellationTokenSource();
            limitTask = checkBwLimitTask(true, ct2.Token);
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
                await moveStop();
                return false;
            }
        }

        
        public async Task<bool> end2endCycleTestingWithReversal(NcTestSettings testSettings, MeasurementDevice device1 = null, MeasurementDevice device2 = null, MeasurementDevice device3 = null, MeasurementDevice device4 = null)
        {
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
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {6} -End2EndwithReversalTest-setVelo({1}) revVelo({2}) revExtraTime({3}) settleTime({4}) - {5} cycles", currentTime,testSettings.Velocity,testSettings.ReversalVelocity,testSettings.ReversalExtraTimeSeconds,testSettings.ReversalSettleTimeSeconds,testSettings.Cycles,AxisID);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory+fileName, FileMode.Append);
            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<end2endReversalCSV>();
                csv.NextRecord();
            }

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);
            //Start low
            if (await moveToLowLimit(-testSettings.Velocity, testSettings.Timeout) == false)
            {
                Console.WriteLine("Failed to move to low limit for start of test");
                return false;
            }
            
            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);


            //Start running test cycles
            end2endReversalCSV record1;
            end2endReversalCSV record2;
            for (int i = 1; i <= testSettings.Cycles; i++)
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

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait
                
                stopWatch.Reset();
                stopWatch.Start();  //Clear and start the stopwatch

                if (await moveToHighLimit(testSettings.Velocity, testSettings.Timeout))
                {
                    stopWatch.Stop();
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds));//Allow axis to settle before reversal
                    if(await HighLimitReversal(testSettings.ReversalVelocity, testSettings.Timeout, testSettings.ReversalExtraTimeSeconds, testSettings.ReversalSettleTimeSeconds))
                    {
                        //Do we need to check the DTIs?
                        string measurement1 = string.Empty;
                        string measurement2 = string.Empty;
                        string measurement3 = string.Empty;
                        string measurement4 = string.Empty;

                        ///READ MEASUREMENT DEVICES///
                        ///
                        if (device1 != null)
                        {
                            measurement1 = await device1.GetMeasurement();
                            Console.WriteLine("Device 1: " + measurement1);
                        }
                        if (device2 != null)
                        {
                            measurement2 = await device2.GetMeasurement();
                            Console.WriteLine("Device 2: " + measurement2);
                        }
                        if (device3 != null)
                        {
                            measurement3 = await device3.GetMeasurement();
                            Console.WriteLine("Device 3: " + measurement3);
                        }
                        if (device4 != null)
                        {
                            measurement4 = await device4.GetMeasurement();
                            Console.WriteLine("Device 4: " + measurement4);
                        }

                        double tmpAxisPosition = await read_AxisPosition();                       
                        record1 = new end2endReversalCSV(i, "Low limit to high limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition, measurement1, measurement2, measurement3, measurement4);
                        Console.WriteLine("Cycle " + i + "- Low limit to high limit: " + stopWatch.ElapsedMilliseconds + "ms. High limit triggered at "+tmpAxisPosition);
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
                if (await moveToLowLimit(-testSettings.Velocity, testSettings.Timeout))
                {
                    stopWatch.Stop();
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.ReversalSettleTimeSeconds));//Allow axis to settle before reversal
                    if (await LowLimitReversal(testSettings.ReversalVelocity,testSettings.Timeout,testSettings.ReversalExtraTimeSeconds,testSettings.ReversalSettleTimeSeconds))
                    {
                        //Do we need to check the DTIs?
                        string measurement1 = string.Empty;
                        string measurement2 = string.Empty;
                        string measurement3 = string.Empty;
                        string measurement4 = string.Empty;

                        ///READ MEASUREMENT DEVICES///
                        ///
                        if (device1 != null)
                        {
                            measurement1 = await device1.GetMeasurement();
                            Console.WriteLine("Device 1: " + measurement1);
                        }
                        if (device2 != null)
                        {
                            measurement2 = await device2.GetMeasurement();
                            Console.WriteLine("Device 2: " + measurement2);
                        }
                        if (device3 != null)
                        {
                            measurement3 = await device3.GetMeasurement();
                            Console.WriteLine("Device 3: " + measurement3);
                        }
                        if (device4 != null)
                        {
                            measurement4 = await device4.GetMeasurement();
                            Console.WriteLine("Device 4: " + measurement4);
                        }

                        double tmpAxisPosition = await read_AxisPosition();
                        record2 = new end2endReversalCSV(i, "High limit to low limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition,measurement1,measurement2,measurement3,measurement4);
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
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecord(record1);
                    csv.NextRecord();
                    csv.WriteRecord(record2);
                    csv.NextRecord();
                }
                record1 = null;
                record2 = null;
            }
            ctToken.Cancel();
            return true;
        }



        public async Task<bool> end2endCycleTestingWithReversal(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
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
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {6} -End2EndwithReversalTest-setVelo({1}) revVelo({2}) revExtraTime({3}) settleTime({4}) - {5} cycles", currentTime, testSettings.Velocity, testSettings.ReversalVelocity, testSettings.ReversalExtraTimeSeconds, testSettings.ReversalSettleTimeSeconds, testSettings.Cycles, AxisID);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory + fileName, FileMode.Append);
            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<end2endCSV>();
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

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);
            //Start low
            if (await moveToLowLimit(-testSettings.Velocity, testSettings.Timeout) == false)
            {
                Console.WriteLine("Failed to move to low limit for start of test");
                return false;
            }

            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);


            //Start running test cycles
            end2endCSV record1;
            end2endCSV record2;
            List<string> measurementsHigh = new();
            List<string> measurementsLow = new();
            for (int i = 1; i <= testSettings.Cycles; i++)
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

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait

                stopWatch.Reset();
                stopWatch.Start();  //Clear and start the stopwatch

                if (await moveToHighLimit(testSettings.Velocity, testSettings.Timeout))
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

                        double tmpAxisPosition = await read_AxisPosition();
                        record1 = new end2endCSV(i, "Low limit to high limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition);
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
                if (await moveToLowLimit(-testSettings.Velocity, testSettings.Timeout))
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
                        double tmpAxisPosition = await read_AxisPosition();
                        record2 = new end2endCSV(i, "High limit to low limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition);

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
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
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
                record1 = null;
                record2 = null;
            }
            ctToken.Cancel();
            return true;
        }




        //no timeout implemented
        public async Task<bool> uniDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevice device1 = null, MeasurementDevice device2 = null, MeasurementDevice device3 = null, MeasurementDevice device4 = null)
        {
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
            List<uniDirectionalAccuracyCSV> recordList = new List<uniDirectionalAccuracyCSV>();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {8} -uniDirectionalAccuracyTest-InitialSetpoint({1}) Velo({2}) Steps({3}) StepSize({4}) SettleTime({5}) ReversalDistance({6}) - {7} cycles", currentTime,testSettings.InitialSetpoint,testSettings.Velocity,testSettings.NumberOfSteps,testSettings.StepSize,testSettings.SettleTimeSeconds,testSettings.ReversalDistance,testSettings.Cycles, AxisID);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory + fileName, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {HasHeaderRecord = false,};
            
            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, config);

            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<uniDirectionalAccuracyCSV>();
                csv.NextRecord();
            }

            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.

            double reversalPosition;
            if (testSettings.StepSize > 0)
            {
                reversalPosition = testSettings.InitialSetpoint- testSettings.ReversalDistance;
            }
            else
            {
                reversalPosition = testSettings.InitialSetpoint + testSettings.ReversalDistance;
            }
            stopWatch.Start();
            for (uint i = 1; i <= testSettings.Cycles; i++)
            {
                Console.WriteLine("Cycle " + i);
                //Create a task each cycle to monitor for the pause. This is done as a task as a basic "while(paused)" would block UI and not allow an unpause
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

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait
                double TargetPosition = testSettings.InitialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await moveAbsoluteAndWait(reversalPosition, testSettings.Velocity, testSettings.Timeout) == false)
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
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        ctToken.Cancel();
                        ptToken.Cancel();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));

                    //Do we need to check the DTIs?
                    string measurement1 = string.Empty;
                    string measurement2 = string.Empty;
                    string measurement3 = string.Empty;
                    string measurement4 = string.Empty;

                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (device1 != null)
                    {
                        measurement1 = await device1.GetMeasurement();
                        Console.WriteLine("Device 1: " + measurement1);
                    }
                    if (device2 != null)
                    {
                        measurement2 = await device2.GetMeasurement();
                        Console.WriteLine("Device 2: " + measurement2);
                    }
                    if (device3 != null)
                    {
                        measurement3 = await device3.GetMeasurement();
                        Console.WriteLine("Device 3: " + measurement3);
                    }
                    if (device4 != null)
                    {
                        measurement4 = await device4.GetMeasurement();
                        Console.WriteLine("Device 4: " + measurement4);
                    }


                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, j, "Testing", TargetPosition, tmpAxisPosition,measurement1,measurement2,measurement3,measurement4));
                    //Update target position

                    TargetPosition = TargetPosition + testSettings.StepSize;
                }
                
                //Write the cycle data
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(recordList);
                }
                recordList.Clear();
            }
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took "+stopWatch.Elapsed+"ms");
            ctToken.Cancel();
            ptToken.Cancel();
            return true;
        }


        //no timeout implemented
        public async Task<bool> uniDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
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
            List<uniDirectionalAccCSV> recordList = new List<uniDirectionalAccCSV>();
            


            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {8} -uniDirectionalAccuracyTest-InitialSetpoint({1}) Velo({2}) Steps({3}) StepSize({4}) SettleTime({5}) ReversalDistance({6}) - {7} cycles", currentTime, testSettings.InitialSetpoint, testSettings.Velocity, testSettings.NumberOfSteps, testSettings.StepSize, testSettings.SettleTimeSeconds, testSettings.ReversalDistance, testSettings.Cycles, AxisID);

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
                csv.WriteHeader<uniDirectionalAccCSV>();
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

            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
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

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait
                double TargetPosition = testSettings.InitialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await moveAbsoluteAndWait(reversalPosition, testSettings.Velocity, testSettings.Timeout) == false)
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
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
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
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccCSV(i, j, "Testing", TargetPosition, tmpAxisPosition));
                    cycleMeasurements.Add(measurements);
                    //debug line
                    TargetPosition = TargetPosition + testSettings.StepSize;
                }

                //Write the cycle data
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
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

        public async Task<bool> biDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevices devices = null)
        {
            List<uniDirectionalAccCSV> recordList = new List<uniDirectionalAccCSV>();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {9} -biDirectionalAccuracyTest-InitialSetpoint({1}) Velo({2}) Steps({3}) StepSize({4}) SettleTime({5}) ReversalDistance({6}) OvershootDistance({7}) - {8} cycles", currentTime, testSettings.InitialSetpoint, testSettings.Velocity, testSettings.NumberOfSteps, testSettings.StepSize, testSettings.SettleTimeSeconds, testSettings.ReversalDistance, testSettings.OvershootDistance, testSettings.Cycles, AxisID);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory + fileName, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, };

            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, config);

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
                csv.WriteHeader<uniDirectionalAccCSV>();
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

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);

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

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait
                double TargetPosition = testSettings.InitialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await moveAbsoluteAndWait(reversalPosition, testSettings.Velocity, testSettings.Timeout) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));

                //going up the steps
                for (uint j = 0; j <= testSettings.NumberOfSteps; j++)
                {
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
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
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccCSV(i, j, "Forward approach", TargetPosition, tmpAxisPosition));
                    cycleMeasurements.Add(measurements);
                    //Update target position

                    TargetPosition = TargetPosition + testSettings.StepSize;
                }
                TargetPosition = TargetPosition - testSettings.StepSize;
                //Overshoot the final position before coming back down
                if (await moveAbsoluteAndWait(overshootPosition, testSettings.Velocity, testSettings.Timeout) == false)
                {
                    Console.WriteLine("Failed to move to overshoot position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));
                //going down the steps. Need the cast here as we require j to go negative to cancel the loop
                for (int j = (int)testSettings.NumberOfSteps; j >= 0; j--)
                {
                    Console.WriteLine("Moving down. Step: " + j);
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
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
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccCSV(i, (uint)j, "Backward approach", TargetPosition, tmpAxisPosition));
                    cycleMeasurements.Add(measurements);
                    //Update target position

                    TargetPosition = TargetPosition - testSettings.StepSize;
                }

                //Write the cycle data
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
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



        public async Task<bool> biDirectionalAccuracyTest(NcTestSettings testSettings, MeasurementDevice device1 = null, MeasurementDevice device2 = null, MeasurementDevice device3 = null, MeasurementDevice device4 = null)
        {
            List<uniDirectionalAccuracyCSV> recordList = new List<uniDirectionalAccuracyCSV>();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {9} -biDirectionalAccuracyTest-InitialSetpoint({1}) Velo({2}) Steps({3}) StepSize({4}) SettleTime({5}) ReversalDistance({6}) OvershootDistance({7}) - {8} cycles", currentTime, testSettings.InitialSetpoint, testSettings.Velocity, testSettings.NumberOfSteps, testSettings.StepSize, testSettings.SettleTimeSeconds, testSettings.ReversalDistance, testSettings.OvershootDistance, testSettings.Cycles, AxisID);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory + fileName, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, };

            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, config);

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
                csv.WriteHeader<uniDirectionalAccuracyCSV>();
                csv.NextRecord();
            }

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            testSettings.Velocity = Math.Abs(testSettings.Velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);

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
            for (uint i = 1; i <= testSettings.Cycles; i++)
            {
                Console.WriteLine("Cycle " + i);
                //Create a task each cycle to monitor for the pause. This is done as a task as a basic "while(paused)" would block UI and not allow an unpause
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

                await Task.Delay(TimeSpan.FromSeconds(testSettings.CycleDelaySeconds)); //inter-cycle delay wait
                double TargetPosition = testSettings.InitialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await moveAbsoluteAndWait(reversalPosition, testSettings.Velocity, testSettings.Timeout) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));

                //going up the steps
                for (uint j = 0; j <= testSettings.NumberOfSteps; j++)
                {
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));
                    //Do we need to check the DTIs?
                    string measurement1 = string.Empty;
                    string measurement2 = string.Empty;
                    string measurement3 = string.Empty;
                    string measurement4 = string.Empty;

                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (device1 != null)
                    {
                        measurement1 = await device1.GetMeasurement();
                        Console.WriteLine("Device 1: " + measurement1);
                    }
                    if (device2 != null)
                    {
                        measurement2 = await device2.GetMeasurement();
                        Console.WriteLine("Device 2: " + measurement2);
                    }
                    if (device3 != null)
                    {
                        measurement3 = await device3.GetMeasurement();
                        Console.WriteLine("Device 3: " + measurement3);
                    }
                    if (device4 != null)
                    {
                        measurement4 = await device4.GetMeasurement();
                        Console.WriteLine("Device 4: " + measurement4);
                    }

                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, j, "Forward approach", TargetPosition, tmpAxisPosition, measurement1, measurement2, measurement3, measurement4));
                    //Update target position

                    TargetPosition = TargetPosition + testSettings.StepSize;
                }
                TargetPosition = TargetPosition - testSettings.StepSize;
                //Overshoot the final position before coming back down
                if (await moveAbsoluteAndWait(overshootPosition, testSettings.Velocity, testSettings.Timeout) == false)
                {
                    Console.WriteLine("Failed to move to overshoot position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));
                //going down the steps. Need the cast here as we require j to go negative to cancel the loop
                for (int j = (int)testSettings.NumberOfSteps; j >= 0; j--)
                {
                    Console.WriteLine("Moving down. Step: " + j);
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, testSettings.Velocity, testSettings.Timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(testSettings.SettleTimeSeconds));
                    //Do we need to check the DTIs?
                    string measurement1 = string.Empty;
                    string measurement2 = string.Empty;
                    string measurement3 = string.Empty;
                    string measurement4 = string.Empty;

                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (device1 != null)
                    {
                        measurement1 = await device1.GetMeasurement();
                        Console.WriteLine("Device 1: " + measurement1);
                    }
                    if (device2 != null)
                    {
                        measurement2 = await device2.GetMeasurement();
                        Console.WriteLine("Device 2: " + measurement2);
                    }
                    if (device3 != null)
                    {
                        measurement3 = await device3.GetMeasurement();
                        Console.WriteLine("Device 3: " + measurement3);
                    }
                    if (device4 != null)
                    {
                        measurement4 = await device4.GetMeasurement();
                        Console.WriteLine("Device 4: " + measurement4);
                    }
                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, (uint)j, "Backward approach", TargetPosition, tmpAxisPosition, measurement1, measurement2, measurement3, measurement4));
                    //Update target position

                    TargetPosition = TargetPosition - testSettings.StepSize;
                }

                //Write the cycle data
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(recordList);
                }
                recordList.Clear();
            }
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed);
            return true;
        }


        //no timeout implemented
        public async Task<bool> biDirectionalAccuracyTest(double initialSetpoint, double velocity, int cycles, int steps, double stepSize, int settleTime, double reversalDistance,double overshoot, int timeout, int cycleDelay, MeasurementDevice device1 = null, MeasurementDevice device2 = null, MeasurementDevice device3 = null, MeasurementDevice device4 = null)
        {
            List<uniDirectionalAccuracyCSV> recordList = new List<uniDirectionalAccuracyCSV>();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {9} -biDirectionalAccuracyTest-InitialSetpoint({1}) Velo({2}) Steps({3}) StepSize({4}) SettleTime({5}) ReversalDistance({6}) OvershootDistance({7}) - {8} cycles", currentTime, initialSetpoint, velocity, steps, stepSize, settleTime, reversalDistance, overshoot, cycles, AxisID);

            string fileName = @"\" + formattedTitle + ".csv";
            var stream = File.Open(TestDirectory + fileName, FileMode.Append);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            { HasHeaderRecord = false, };

            StreamWriter writer = new StreamWriter(stream);
            CsvWriter csv = new CsvWriter(writer, config);

            if (cycles == 0)
            {
                Console.WriteLine("0 cycle count invalid");
                return false;
            }
            if (steps == 0)
            {
                Console.WriteLine("0 step count invalid");
                return false;
            }
            if (velocity == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }
            if (stepSize == 0)
            {
                Console.WriteLine("0 step size invalid");
                return false;
            }


            using (stream)
            using (writer)
            using (csv)
            {
                csv.WriteHeader<uniDirectionalAccuracyCSV>();
                csv.NextRecord();
            }

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            velocity = Math.Abs(velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            CancellationTokenSource ctToken = new CancellationTokenSource();
            CancellationTokenSource ptToken = new CancellationTokenSource();
            Task<bool> cancelRequestTask = checkCancellationRequestTask(ctToken.Token);
            
            double reversalPosition;
            if (stepSize > 0)
            {
                reversalPosition = initialSetpoint - reversalDistance;
            }
            else
            {
                reversalPosition = initialSetpoint + reversalDistance;
            }
            double overshootPosition;
            if (stepSize > 0)
            {
                overshootPosition = initialSetpoint + ((steps-1)*stepSize)+ overshoot;
            }
            else
            {
                overshootPosition = initialSetpoint + ((steps - 1) * stepSize) - overshoot;
            }

            stopWatch.Start();
            for (uint i = 1; i <= cycles; i++)
            {
                Console.WriteLine("Cycle " + i);
                //Create a task each cycle to monitor for the pause. This is done as a task as a basic "while(paused)" would block UI and not allow an unpause
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

                await Task.Delay(TimeSpan.FromSeconds(cycleDelay)); //inter-cycle delay wait
                double TargetPosition = initialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await moveAbsoluteAndWait(reversalPosition, velocity,timeout) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(settleTime));

                //going up the steps
                for (uint j = 0; j <= steps; j++)
                {
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, velocity,timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(settleTime));
                    //Do we need to check the DTIs?
                    string measurement1 = string.Empty;
                    string measurement2 = string.Empty;
                    string measurement3 = string.Empty;
                    string measurement4 = string.Empty;

                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (device1 != null)
                    {
                        measurement1 = await device1.GetMeasurement();
                        Console.WriteLine("Device 1: " + measurement1);
                    }
                    if (device2 != null)
                    {
                        measurement2 = await device2.GetMeasurement();
                        Console.WriteLine("Device 2: " + measurement2);
                    }
                    if (device3 != null)
                    {
                        measurement3 = await device3.GetMeasurement();
                        Console.WriteLine("Device 3: " + measurement3);
                    }
                    if (device4 != null)
                    {
                        measurement4 = await device4.GetMeasurement();
                        Console.WriteLine("Device 4: " + measurement4);
                    }

                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, j, "Forward approach", TargetPosition, tmpAxisPosition, measurement1, measurement2, measurement3, measurement4));
                    //Update target position

                    TargetPosition = TargetPosition + stepSize;
                }
                TargetPosition = TargetPosition - stepSize;
                //Overshoot the final position before coming back down
                if (await moveAbsoluteAndWait(overshootPosition, velocity,timeout) == false)
                {
                    Console.WriteLine("Failed to move to overshoot position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                //going down the steps. Need the cast here as we require j to go negative to cancel the loop
                for (int j = (int)steps; j >= 0; j--)
                {
                    Console.WriteLine("Moving down. Step: " + j);
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, velocity, timeout) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(settleTime));
                    //Do we need to check the DTIs?
                    string measurement1 = string.Empty;
                    string measurement2 = string.Empty;
                    string measurement3 = string.Empty;
                    string measurement4 = string.Empty;

                    ///READ MEASUREMENT DEVICES///
                    ///
                    if (device1 != null)
                    {
                        measurement1 = await device1.GetMeasurement();
                        Console.WriteLine("Device 1: " + measurement1);
                    }
                    if (device2 != null)
                    {
                        measurement2 = await device2.GetMeasurement();
                        Console.WriteLine("Device 2: " + measurement2);
                    }
                    if (device3 != null)
                    {
                        measurement3 = await device3.GetMeasurement();
                        Console.WriteLine("Device 3: " + measurement3);
                    }
                    if (device4 != null)
                    {
                        measurement4 = await device4.GetMeasurement();
                        Console.WriteLine("Device 4: " + measurement4);
                    }
                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, (uint)j, "Backward approach", TargetPosition, tmpAxisPosition, measurement1, measurement2, measurement3, measurement4));
                    //Update target position

                    TargetPosition = TargetPosition - stepSize;
                }

                //Write the cycle data
                using (stream = File.Open(TestDirectory + fileName, FileMode.Append))
                using (writer = new StreamWriter(stream))
                using (csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(recordList);
                }
                recordList.Clear();
            }
            stopWatch.Stop();
            Console.WriteLine("Test Complete. Test took " + stopWatch.Elapsed);
            return true;
        }

        /// <summary>
        /// Read the axis bDone status
        /// </summary>
        /// <returns></returns>
        public async Task<bool> read_bDone()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bDoneHandle, CancellationToken.None);
            return result.Value;
        }

        /// <summary>
        /// Read the axis position value
        /// </summary>
        /// <returns></returns>
        public async Task<double> read_AxisPosition()
        {
            var result = await Plc.TcAds.ReadAnyAsync<double>(fActPositionHandle, CancellationToken.None);
            AxisPosition = result.Value;
            return result.Value;
        }

        /// <summary>
        /// Read the axis busy status
        /// </summary>
        /// <returns></returns>
        public async Task<bool> read_bBusy()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bBusyHandle, CancellationToken.None);
            return result.Value;
        }

        /// <summary>
        /// Read the axis bEnabled status
        /// </summary>
        /// <returns></returns>
        public async Task<bool> read_bEnabled()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bEnabledHandle, CancellationToken.None);
            AxisEnabled = result.Value;
            return result.Value;
        }

        /// <summary>
        /// Read the axis forward enabled status
        /// </summary>
        /// <returns></returns>
        public async Task<bool> read_bFwEnabled()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bFwEnabledHandle, CancellationToken.None);
            AxisFwEnabled = result.Value;
            return result.Value;
        }
        
        /// <summary>
        /// Read the axis backward enabled status
        /// </summary>
        /// <returns></returns>
        public async Task<bool> read_bBwEnabled()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bBwEnabledHandle, CancellationToken.None);
            AxisBwEnabled = result.Value;
            return result.Value;
        }
        
        /// <summary>
        /// Read the axis error status
        /// </summary>
        /// <returns></returns>
        public async Task<bool> read_bError()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bErrorHandle, CancellationToken.None);
            Error = result.Value;
            return result.Value;
        }



        CancellationTokenSource wtoken;
        ActionBlock<DateTimeOffset> taskPos;
        ActionBlock<DateTimeOffset> taskEnabled;
        ActionBlock<DateTimeOffset> taskFwEnabled;
        ActionBlock<DateTimeOffset> taskBwEnabled;
        ActionBlock<DateTimeOffset> taskError;

        public void StartPositionRead()
        {
            wtoken = new CancellationTokenSource();
            taskPos = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_AxisPosition(), wtoken.Token, TimeSpan.FromMilliseconds(50));
            taskEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bEnabled(), wtoken.Token, TimeSpan.FromMilliseconds(200));
            taskFwEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bFwEnabled(), wtoken.Token, TimeSpan.FromMilliseconds(200));
            taskBwEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bBwEnabled(), wtoken.Token, TimeSpan.FromMilliseconds(200));
            taskError = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bError(), wtoken.Token, TimeSpan.FromMilliseconds(200));
            taskPos.Post(DateTimeOffset.Now);
            taskEnabled.Post(DateTimeOffset.Now);
            taskFwEnabled.Post(DateTimeOffset.Now);
            taskBwEnabled.Post(DateTimeOffset.Now);
            taskError.Post(DateTimeOffset.Now);
        }
        public void StopPositionRead()
        {
            if (wtoken == null)
            {
                return;
            }
            using (wtoken)
            {
                wtoken.Cancel();
            }
            wtoken = null;
            taskPos = null;
            taskEnabled = null;
            taskFwEnabled = null;
            taskBwEnabled = null;
        }
    }
}
