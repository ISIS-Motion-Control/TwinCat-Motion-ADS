using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TwinCAT.Ads;
using System.Threading.Tasks.Dataflow;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using CsvHelper;
using System.IO;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace TwinCat_Motion_ADS
{
    /*To -do list    
     * Add the new cancel and pause tasks to the uni and bidirectional accuracy test
     * Add a repeatability test
     * Anymore cleanup I can do? Some of these tests are very repeat heavy
     */

    public class PLC
    {
        private AdsClient _tcAds = new AdsClient();
        public AdsClient TcAds
        {
            get { return _tcAds; }
            set { _tcAds = value; }
        }
        private AdsState _adsState;
        public AdsState AdsState
        {
            get { return _adsState; }
            set { _adsState = value; }
        }
        public PLC(string ID, int PORT)
        {
            TcAds.Connect(ID, PORT);
        }
        public bool checkConnection()
        {
            return TcAds.IsConnected;
        }
        public AdsState checkAdsState()
        {
            try
            {
                //Could check Run/Stop/Invalid status of this
                AdsState = TcAds.ReadState().AdsState;
                return AdsState;
            }
            catch
            {
                return AdsState.Invalid;
            }
        }
        public AdsState setupPLC()
        {
            if (checkConnection())
            { Console.WriteLine("Port open"); };

            return checkAdsState();
        }
    }

    public class Axis : INotifyPropertyChanged
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
        //Optional PLC handles
        private uint dti1Handle;
        private uint dti2Handle;

        //Axis error
        private bool _error;
        public bool Error
        {
            get { return _error; }
            set { _error = value; OnPropertyChanged(); }
        }
        //Request test pause
        private bool _pauseTest = false;
        public bool PauseTest
        {
            get { return _pauseTest; }
            set { _pauseTest = value; OnPropertyChanged();}
        }
        //Request test cancellation
        private bool _cancelTest = false;
        public bool CancelTest
        {
            get { return _cancelTest; }
            set { _cancelTest = value; OnPropertyChanged();}
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
        //Directory for saving test csv
        public string TestDirectory { get; set; } = string.Empty;

        private string _dtiPosition = string.Empty;
        public string DtiPosition
        {
            get
            {

                return _dtiPosition;
            }
            set { _dtiPosition = value; }
        }
        private string readAndClearDtiPosition()
        {
            string tempPosition = DtiPosition;
            DtiPosition = string.Empty;
            return tempPosition;
        }

        //Asynchronous task for setting the DtiPosition field
        public async Task setDtiPosition(string dtiPos)
        {
            await Task.Run(() => DtiPosition = dtiPos);
        }
        //PLC object to which the test axis belongs
        public PLC Plc { get; set; }
        //Current axis ID
        public uint AxisID { get; set; }

        /// <summary>
        /// Axis Class Constructor
        /// </summary>
        /// <param name="axisID"></param>
        /// <param name="plc"></param>
        /// <param name="dti1Present"></param>
        /// <param name="dti2Present"></param>
        public Axis(uint axisID, PLC plc, bool dti1Present = false, bool dti2Present = false)
        {
            Plc = plc;
            AxisID = axisID;
            updateInstance(axisID,dti1Present,dti2Present);
        }

        /// <summary>
        /// Update current axis instance  - generates new variable handles for testing a different axis ID
        /// </summary>
        /// <param name="axisID"></param>
        /// <param name="dti1Present"></param>
        /// <param name="dti2Present"></param>
        public void updateInstance(uint axisID, bool dti1Present = false, bool dti2Present = false)
        {
            StopPositionRead();
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
            //Create variable handles for DTIs if user requested
            if (dti1Present)
            {
                dti1Handle = Plc.TcAds.CreateVariableHandle("MAIN.bDti1");
            }
            else
            {
                dti1Handle = 0;
            }
            if (dti2Present)
            {
                dti2Handle = Plc.TcAds.CreateVariableHandle("MAIN.bDti2");
            }
            else
            {
                dti2Handle = 0;
            }
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
        /// Trigger a data read of DTI 1
        /// </summary>
        /// <returns></returns>
        public async Task TriggerDti1()
        {
            if (dti1Handle == 0)
            {
                return;
            }
            await Plc.TcAds.WriteAnyAsync(dti1Handle, true, CancellationToken.None);
        }

        /// <summary>
        /// Trigger a data read of DTI 2
        /// </summary>
        /// <returns></returns>
        public async Task TriggerDti2()
        {
            if (dti2Handle == 0)
            {
                return;
            }
            await Plc.TcAds.WriteAnyAsync(dti2Handle, true, CancellationToken.None);
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
        private async Task<bool> checkCancellationRequestTask(CancellationToken wToken)
        {
            while(CancelTest ==false)
            {
                await Task.Delay(10);
                if(wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            return true;
        }
        private async Task<bool> checkPauseRequestTask(CancellationToken wToken)
        {
            if(PauseTest)
            {
                Console.WriteLine("Test Paused");
            }
            while (PauseTest)
            {
                await Task.Delay(10);
                if (CancelTest)
                {
                    return true;
                }
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
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
                Console.WriteLine("new task");
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
                Console.WriteLine("new task");
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

        //No longer in use. Measuring position after stopping not useful if at high velocity
        public async Task<bool> end2endCycleTesting(double velocity, int timeout, int cycleDelay, int cycles, bool readDTI=false)
        {
            if (timeout <= 0)
            {
                timeout = 100000; //I think this is fine to do because i'm using an overload on the Task.Delay methods to provide a cancellation token which should clear up that task
            }
          
            //will need a string fileDir input
            //this ultimately needs to include the data logging and file writing too!
            Stopwatch stopWatch = new Stopwatch();
            velocity = Math.Abs(velocity);
            //Start low
            if(await moveToLowLimit(-velocity,timeout)==false)
            {
                Console.WriteLine("Failed to move to low limit for start of test");
                return false;
            }
            var cancelTaskToken = new CancellationTokenSource();
            var cancelTask = Task.Run(() =>
            {
                while(CancelTest == false)
                {
                }
            },cancelTaskToken.Token);
            var pauseTaskToken = new CancellationTokenSource();

            for (int i = 1; i<=cycles; i++)
            {
                var pauseTask = Task.Run(() =>
                {
                    if (PauseTest)
                    {
                        Console.WriteLine("Test paused");
                    }         
                    while (PauseTest)
                    {
                        if(CancelTest)
                        {
                            return;
                        }
                    }
                }, pauseTaskToken.Token);

                await pauseTask;
                //Do a check each cycle to see if we wanted to cancel
                if (cancelTask.IsCompleted)
                {
                    //Cancelled the test
                    pauseTaskToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(cycleDelay));
                stopWatch.Reset();
                stopWatch.Start();
                if (await moveToHighLimit(velocity, timeout))
                {
                    stopWatch.Stop();
                    if (readDTI)
                    {
                        double tmpAxisPosition = await read_AxisPosition();
                        string tmpDtiPosition = await getDtiPositionValue();
                        Console.WriteLine("Cycle " + i + ": Low limit to high limit:-  " + stopWatch.ElapsedMilliseconds + "ms. High limit at " + tmpAxisPosition + " DTI Read: " + tmpDtiPosition);
                    }
                    else
                    {
                        double tmpAxisPosition = await read_AxisPosition();
                        Console.WriteLine("Cycle " + i + ": Low limit to high limit:-  " + stopWatch.ElapsedMilliseconds + "ms. High limit at " + tmpAxisPosition);
                    }
                    
                }
                else
                {
                    stopWatch.Stop();
                    Console.WriteLine("Move to high failed");
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(cycleDelay));
                stopWatch.Reset();
                stopWatch.Start();
                if (await moveToLowLimit(-velocity, timeout))
                {
                    stopWatch.Stop();
                    if (readDTI)
                    {
                        double tmpAxisPosition = await read_AxisPosition();
                        string tmpDtiPosition = await getDtiPositionValue();
                        Console.WriteLine("Cycle " + i + ": High limit to low limit:-  " + stopWatch.ElapsedMilliseconds + "ms. Low limit at " + tmpAxisPosition + " DTI Read: " + tmpDtiPosition);
                    }
                    else
                    {
                        double tmpAxisPosition = await read_AxisPosition();
                        Console.WriteLine("Cycle " + i + ": High limit to low limit:-  " + stopWatch.ElapsedMilliseconds + "ms. Low limit at " + tmpAxisPosition);
                    }
                }
                else
                {
                    stopWatch.Stop();
                    Console.WriteLine("Move to low failed");
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> end2endCycleTestingWithReversal(double setVelocity, double reversalVelocity, int timeout, int cycleDelay, int cycles, int resersalExtraTime, int reversalSettleTime, bool dti1Present = false, bool dti2Present=false)
        {
            if (cycles == 0)
            {
                Console.WriteLine("0 cycle count invalid");
                return false;
            }
            if (reversalVelocity == 0)
            {
                Console.WriteLine("0 reversal velocity invalid");
                return false;
            }
            if (setVelocity == 0)
            {
                Console.WriteLine("0 velocity invalid");
                return false;
            }

            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {5} -End2EndwithReversalTest-setVelo({1}) revVelo({2}) settleTime({3}) - {4} cycles", currentTime,setVelocity,reversalVelocity,reversalSettleTime,cycles,AxisID);

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
            setVelocity = Math.Abs(setVelocity);
            //Start low
            if (await moveToLowLimit(-setVelocity, timeout) == false)
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
            for (int i = 1; i <= cycles; i++)
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

                await Task.Delay(TimeSpan.FromSeconds(cycleDelay)); //inter-cycle delay wait
                
                stopWatch.Reset();
                stopWatch.Start();  //Clear and start the stopwatch

                if (await moveToHighLimit(setVelocity, timeout))
                {
                    stopWatch.Stop();
                    await Task.Delay(TimeSpan.FromSeconds(reversalSettleTime));//Allow axis to settle before reversal
                    if(await HighLimitReversal(reversalVelocity, timeout, resersalExtraTime, reversalSettleTime))
                    {
                        //THIS IS WHERE WE'D DO A DTI PRESENT BOOL CHECK. Then do an extra dti trigger and save
                        double tmpAxisPosition = await read_AxisPosition();                       
                        record1 = new end2endReversalCSV(i, "Low limit to high limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition);
                        Console.WriteLine("Cycle " + i + "- Low limit to high limit: " + stopWatch.ElapsedMilliseconds + "ms. High limit triggered at "+tmpAxisPosition);
                    }
                    else
                    {
                        Console.WriteLine("High limit reversal failed");
                        return false;
                    }
                }
                else
                {
                    stopWatch.Stop();
                    Console.WriteLine("Move to high failed");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(cycleDelay));
                stopWatch.Reset();
                stopWatch.Start();
                if (await moveToLowLimit(-setVelocity, timeout))
                {
                    stopWatch.Stop();
                    await Task.Delay(TimeSpan.FromSeconds(reversalSettleTime));//Allow axis to settle before reversal
                    if (await LowLimitReversal(reversalVelocity, timeout, resersalExtraTime, reversalSettleTime))
                    {
                        //THIS IS WHERE WE'D DO A DTI PRESENT BOOL CHECK. Then do an extra dti trigger and save
                        double tmpAxisPosition = await read_AxisPosition();
                        record2 = new end2endReversalCSV(i, "High limit to low limit", stopWatch.ElapsedMilliseconds, tmpAxisPosition);
                        Console.WriteLine("Cycle " + i + "- High limit to low limit: " + stopWatch.ElapsedMilliseconds + "ms. Low limit triggered at " + tmpAxisPosition);
                    }
                    else
                    {
                        Console.WriteLine("Low limit reversal failed");
                        return false;
                    }
                }
                else
                {
                    stopWatch.Stop();
                    Console.WriteLine("Move to low failed");
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

        //no timeout implemented
        public async Task<bool> uniDirectionalAccuracyTest(double initialSetpoint, double velocity, uint cycles, uint steps, double stepSize, int settleTime, double reversalDistance, int timeout, int cycleDelay, bool dti1present = false, bool dti2present = false)
        {
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
            List<uniDirectionalAccuracyCSV> recordList = new List<uniDirectionalAccuracyCSV>();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {8} -uniDirectionalAccuracyTest-IntialSP({1}) Velo({2}) Steps({3}) StepSize({4}) SettleTime({5}) ReversalDistance({6}) - {7} cycles", currentTime, initialSetpoint, velocity, steps, stepSize, settleTime, reversalDistance, cycles, AxisID);

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

            //If no timeout set. Make it really high and likely not timeout
            if (timeout <= 0)
            {
                timeout = 100000;
            }

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            velocity = Math.Abs(velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            var cancelTaskToken = new CancellationTokenSource();
            var cancelTask = Task.Run(() =>
            {
                while (CancelTest == false) ;
            }, cancelTaskToken.Token);
            var pauseTaskToken = new CancellationTokenSource();



            double reversalPosition;
            if (stepSize > 0)
            {
                reversalPosition = initialSetpoint- reversalDistance;
            }
            else
            {
                reversalPosition = initialSetpoint + reversalDistance;
            }
            stopWatch.Start();
            for (uint i = 1; i <= cycles; i++)
            {
                Console.WriteLine("Cycle " + i);
                //Create a task each cycle to monitor for the pause. This is done as a task as a basic "while(paused)" would block UI and not allow an unpause
                var pauseTask = Task.Run(() =>
                {
                    if (PauseTest)
                    {
                        Console.WriteLine("Test paused");
                    }
                    while (PauseTest)
                    {
                        if (CancelTest)
                        {
                            return;
                        }
                    }
                }, pauseTaskToken.Token);
                await pauseTask; //awaiting pause task before allowing to continue with this cycle

                //Do a check each cycle to see if we wanted to cancel
                if (cancelTask.IsCompleted)
                {
                    //Cancelled the test
                    pauseTaskToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(cycleDelay)); //inter-cycle delay wait
                double TargetPosition = initialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await moveAbsoluteAndWait(reversalPosition, velocity) == false)
                {
                    Console.WriteLine("Failed to move to reversal position");
                    stopWatch.Stop();
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                

                for (uint j = 0; j <= steps; j++)
                {
                    //Do the step move
                    if (await moveAbsoluteAndWait(TargetPosition, velocity) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(settleTime));
                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, j, "Testing", TargetPosition, tmpAxisPosition));
                    //Update target position

                    TargetPosition = TargetPosition + stepSize;
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
            return true;
        }

        //no timeout implemented
        public async Task<bool> biDirectionalAccuracyTest(double initialSetpoint, double velocity, uint cycles, uint steps, double stepSize, int settleTime, double reversalDistance,double overshoot, int timeout, int cycleDelay, bool dti1present = false, bool dti2present = false)
        {
            List<uniDirectionalAccuracyCSV> recordList = new List<uniDirectionalAccuracyCSV>();
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-Axis {8} -biDirectionalAccuracyTest-IntialSP({1}) Velo({2}) Steps({3}) StepSize({4}) SettleTime({5}) ReversalDistance({6}) - {7} cycles", currentTime, initialSetpoint, velocity, steps, stepSize, settleTime, reversalDistance, cycles, AxisID);

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

            //If no timeout set. Make it really high and likely not timeout
            if (timeout <= 0)
            {
                timeout = 100000;
            }

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            velocity = Math.Abs(velocity);  //Only want positive velocity
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            var cancelTaskToken = new CancellationTokenSource();
            var cancelTask = Task.Run(() =>
            {
                while (CancelTest == false) ;
            }, cancelTaskToken.Token);
            var pauseTaskToken = new CancellationTokenSource();



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
                var pauseTask = Task.Run(() =>
                {
                    if (PauseTest)
                    {
                        Console.WriteLine("Test paused");
                    }
                    while (PauseTest)
                    {
                        if (CancelTest)
                        {
                            return;
                        }
                    }
                }, pauseTaskToken.Token);
                await pauseTask; //awaiting pause task before allowing to continue with this cycle

                //Do a check each cycle to see if we wanted to cancel
                if (cancelTask.IsCompleted)
                {
                    //Cancelled the test
                    pauseTaskToken.Cancel();
                    CancelTest = false;
                    Console.WriteLine("Test cancelled");
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(cycleDelay)); //inter-cycle delay wait
                double TargetPosition = initialSetpoint;

                //Start test at reversal position then moving to initial setpoint          
                if (await moveAbsoluteAndWait(reversalPosition, velocity) == false)
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
                    if (await moveAbsoluteAndWait(TargetPosition, velocity) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(settleTime));
                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, j, "Forward approach", TargetPosition, tmpAxisPosition));
                    //Update target position

                    TargetPosition = TargetPosition + stepSize;
                }
                TargetPosition = TargetPosition - stepSize;
                //Overshoot the final position before coming back down
                if (await moveAbsoluteAndWait(overshootPosition, velocity) == false)
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
                    if (await moveAbsoluteAndWait(TargetPosition, velocity) == false)
                    {
                        Console.WriteLine("Failed to move to target position");
                        stopWatch.Stop();
                        return false;
                    }
                    //Wait for a settle time
                    await Task.Delay(TimeSpan.FromSeconds(settleTime));
                    //Log the data
                    double tmpAxisPosition = await read_AxisPosition();
                    recordList.Add(new uniDirectionalAccuracyCSV(i, (uint)j, "Backward approach", TargetPosition, tmpAxisPosition));
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

        public async Task<string> getDtiPositionValue()
        {
            string dtiRB = string.Empty;
            var cancelToken = new CancellationTokenSource();
            var getDtiTask = Task<string>.Run(async () =>
            {
                while (true)
                {
                    //dtiRB = DtiPosition;
                    dtiRB = readAndClearDtiPosition();
                    await Task.Delay(50);
                    if (dtiRB != string.Empty)
                    {
                        return dtiRB;
                    }
                }
            });
            
            if (await Task.WhenAny(getDtiTask,Task.Delay(2000,cancelToken.Token))==getDtiTask)
            {
                cancelToken.Cancel();
                return getDtiTask.Result;
            }
            else
            {
                return "*No DTI data*";
            }
        }

        ITargetBlock<DateTimeOffset> CreateNeverEndingTask(
        Action<DateTimeOffset> action, CancellationToken cancellationToken, TimeSpan timeSpan)
        {
            // Validate parameters.
            if (action == null) throw new ArgumentNullException("action");

            // Declare the block variable, it needs to be captured.
            ActionBlock<DateTimeOffset> block = null;

            // Create the block, it will call itself, so
            // you need to separate the declaration and
            // the assignment.
            // Async so you can wait easily when the
            // delay comes.
            block = new ActionBlock<DateTimeOffset>(async now => {
                // Perform the action.
                action(now);

                // Wait.
                await Task.Delay(timeSpan, cancellationToken).
                    // Doing this here because synchronization context more than
                    // likely *doesn't* need to be captured for the continuation
                    // here.  As a matter of fact, that would be downright
                    // dangerous.
                    ConfigureAwait(false);

                // Post the action back to the block.
                block.Post(DateTimeOffset.Now);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken
            });

            // Return the block.
            return block;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class end2endReversalCSV
    {
        [Name("Cycle")]
        public int Cycle { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("ElapsedTime")]
        public long ElapsedTime { get; set; }
        [Name("LimitPosition")]
        public double LimitPosition { get; set; }
        [Name("Dti1Position")]
        public string Dti1Position { get; set; }
        [Name("Dti2Position")]
        public string Dti2Position { get; set; }

        public end2endReversalCSV(int cycle, string status, long elapsedTime, double limitPosition, string dti1Position = "",string dti2Position = "")
        {
            Cycle = cycle;
            Status = status;
            ElapsedTime = elapsedTime;
            LimitPosition = limitPosition;
            Dti1Position = dti1Position;
            Dti2Position = dti2Position;
        }    
    }
    public class uniDirectionalAccuracyCSV
    {
        [Name("Cycle")]
        public uint Cycle { get; set; }
        [Name("Step")]
        public uint Step { get; set; }
        [Name("Status")]
        public string Status { get; set; }
        [Name("TargetPosition")]
        public double TargetPosition { get; set; }
        [Name("EncoderPosition")]
        public double EncoderPosition { get; set; }
        [Name("Dti1Position")]
        public string Dti1Position { get; set; }
        [Name("Dti2Position")]
        public string Dti2Position { get; set; }

        public uniDirectionalAccuracyCSV(uint cycle, uint step, string status, double targetPosition, double encoderPosition, string dti1Position = "", string dti2Position = "")
        {
            Cycle = cycle;
            Step = step;
            Status = status;
            TargetPosition = targetPosition;
            EncoderPosition = encoderPosition;
            Dti1Position = dti1Position;
            Dti2Position = dti2Position;
        }
    }
}
