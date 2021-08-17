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

namespace TwinCat_Motion_ADS
{
    /* TO DO
    ALL NEEDS TO BE UDPATED TO ASYNC METHODS
    Check PLC is running
    Check axis is enabled
    Check forward and backwards enabled
    Test functions:-
        move2high
        move2low
        end2end cycle timing
    Need to do a done or error on some of these methods
    */
    public sealed class TestClass
    {
        private static TestClass instance = null;
        private static readonly object padlock = new object();
        TestClass()
        {
        }
        public static TestClass Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new TestClass();
                    }
                    return instance;
                }
            }
        }

        public Axis axis1; //public for now 
        public Axis testAxis;
        PLC plc;

        public void setupPLC()
        {
            plc = new PLC("5.65.74.200.1.1", 852);
            if (plc.checkConnection())
            { Console.WriteLine("Connection established"); };

            try
            {
                //Could check Run/Stop/Invalid status of this
                ResultReadDeviceState testTask = plc.TcAds.ReadStateAsync(CancellationToken.None).Result;
                Console.WriteLine(testTask.State.AdsState.ToString());

            }
            catch(TwinCAT.Ads.AdsErrorException)
            {
                Console.WriteLine("Target port not accessible");
            }
            testAxis = new Axis(1, plc);

        }
        public void axisInstance(uint axisId)
        {
            testAxis.updateInstance(axisId);
        }
    }

    public class PLC
    {
        private AdsClient _tcAds = new AdsClient();
        public AdsClient TcAds
        {
            get { return _tcAds; }
            set { _tcAds = value; }
        }

        public PLC(string ID, int PORT)
        {
            TcAds.Connect(ID, PORT);
        }
        public bool checkConnection()
        {
            return TcAds.IsConnected;
        }
    }


    //want to switch this around, let PLC inherit Test axis and let 
    public class Axis : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        const byte eMoveAbsolute = 0;
        const byte eMoveRelative = 1;
        const byte eMoveVelocity = 3;
        const byte eHome = 10;
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

        private bool _pauseTest = false;
        public bool PauseTest
        {
            get { return _pauseTest; }
            set { _pauseTest = value;
                OnPropertyChanged();
            }
        }
        private bool _cancelTest = false;
        public bool CancelTest
        {
            get { return _cancelTest; }
            set { _cancelTest = value;
                OnPropertyChanged();
            }
        }

        private double _axisPosition;
        public double AxisPosition
        {
            get { return _axisPosition; }
            set { _axisPosition = value;
                OnPropertyChanged();
            }
        }

        private bool _axisEnabled;
        public bool AxisEnabled
        {
            get { return _axisEnabled; }
            set { _axisEnabled = value; OnPropertyChanged(); }
        }
        private bool _axisFwEnabled;
        public bool AxisFwEnabled
        {
            get { return _axisFwEnabled; }
            set { _axisFwEnabled = value; OnPropertyChanged(); }
        }
        private bool _axisBwEnabled;
        public bool AxisBwEnabled
        {
            get { return _axisBwEnabled; }
            set { _axisBwEnabled = value; OnPropertyChanged(); }
        }


        private string _dtiPosition = string.Empty;
        public string DtiPosition
        {
            //bit of a convoluted GET method but basically gives a "one-shot" get of the value before clearing. This can be used to check we've actually had a successful read from the DTI using an "isEmpty" comparator.
            get {
                string tempPosition = _dtiPosition;
                _dtiPosition = string.Empty;
                return tempPosition;
            }
            set { _dtiPosition = value; }
        }
        public async Task setDtiPosition(string dtiPos)
        {
            await Task.Run(() => DtiPosition = dtiPos);
        }




        private PLC _plc;
        public PLC Plc
        {
            get { return _plc; }
            set { _plc = value; }
        }

        public Axis(uint axisID, PLC plc)
        {
            Plc = plc;
            updateInstance(axisID);
        }
        public void updateInstance(uint axisID)
        {
            StopPositionRead();
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
            bStopHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.bStop");
            StartPositionRead();
        }


        private async Task setCommand(byte command)
        {
            await Plc.TcAds.WriteAnyAsync(eCommandHandle, command, CancellationToken.None);
        }
        private async Task setVelocity(double velocity)
        {
            await Plc.TcAds.WriteAnyAsync(fVelocityHandle, velocity, CancellationToken.None);
        }
        private async Task setPosition(double position)
        {
            await Plc.TcAds.WriteAnyAsync(fPositionHandle, position, CancellationToken.None);
        }
        private async Task execute()
        {
            await Plc.TcAds.WriteAnyAsync(bExecuteHandle, true, CancellationToken.None);
        }

        public async Task<bool> moveAbsolute(double position, double velocity)
        {
            if (await read_bBusy())
            {
                return false;   //command fails if axis already busy
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
        public async Task<bool> moveAbsoluteAndWait(double position, double velocity)
        {
            if (await moveAbsolute(position, velocity))
            {
                await Task.Delay(40);   //delay to system to allow PLC to react to move command
                await waitForDone();
                return true;
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }
        public async Task<bool> moveRelative(double position, double velocity)
        {
            if (await read_bBusy())
            {
                return false;   //command fails if axis already busy
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
        public async Task<bool> moveRelativeAndWait(double position, double velocity)
        {
            if (await moveRelative(position,velocity))
            {
                await Task.Delay(40);
                await waitForDone();
                return true;
            }
            Console.WriteLine("Axis busy - command rejected");
            return false;
        }
        public async Task<bool> moveVelocity(double velocity)
        {
            if (await read_bBusy())
            {
                return false;   //command fails if axis already busy
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
        public async Task moveStop()
        {
            await Plc.TcAds.WriteAnyAsync(bStopHandle, true, CancellationToken.None);
        }
     
        public async Task<bool> waitForDone()
        {
            bool doneStatus = await read_bDone();
            while (doneStatus != true)
            {
                doneStatus = await read_bDone();
            }
            Console.WriteLine("!!!DONE!!!");
            return true;
        }
        
        public async Task<bool> moveToHighLimit(double velocity, int timeout)
        {
            if(await read_bFwEnabled() == false)
            {
                Console.WriteLine("Already at high limit");
                return true;
            }
            if(velocity <0)
            {
                Console.WriteLine("Incorrect velocity setting");
                return false;
            }
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Command rejected");
                return false;
            }; //starts the velocity move
            
            //Start a task to check the FwEnabled bool that only returns when flag is hit (fwEnabled == false)
            Task checkFwHitTask = Task<bool>.Run(async () =>
            {
                while (await read_bFwEnabled() == true)
                {
                    await Task.Delay(50);
                }
                return true;
            });
            var cancelToken = new CancellationTokenSource();
            if(await Task.WhenAny(checkFwHitTask, Task.Delay(TimeSpan.FromSeconds(timeout),cancelToken.Token))==checkFwHitTask)
            {
                Console.WriteLine("Forward limit reached");
                cancelToken.Cancel();
                return true;
            }
            else
            {
                Console.WriteLine("Timeout on move to forward limit");
                await moveStop();
                return false;
            }
        }

        public async Task<bool> moveToLowLimit(double velocity, int timeout)
        {
            if (await read_bBwEnabled() == false)
            {
                Console.WriteLine("Already at low limit");
                return true;
            }
            if (velocity > 0)
            {
                Console.WriteLine("Incorrect velocity setting");
                return false;
            }
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Command rejected");
                return false;
            }; //starts the velocity move

            //Start a task to check the BwEnabled bool that only returns when flag is hit (BwEnabled == false)
            Task checkBwHitTask = Task<bool>.Run(async () =>
            {
                while (await read_bBwEnabled() == true)
                {
                    await Task.Delay(50);
                }
                return true;
            });
            var cancelToken = new CancellationTokenSource();
            if (await Task.WhenAny(checkBwHitTask, Task.Delay(TimeSpan.FromSeconds(timeout),cancelToken.Token)) == checkBwHitTask)
            {
                Console.WriteLine("Backward limit reached");
                cancelToken.Cancel();
                return true;
            }
            else
            {
                Console.WriteLine("Timeout on move to backward limit");
                await moveStop();
                return false;
            }
        }

        /*
         * 
         * public async Task<bool> pauseTestTask()
        {
            var cancelToken = new CancellationTokenSource();
            var pausingTask = Task.Run(async () =>
            {
                while (true)
                {
                    dtiRB = DtiPosition;
                    await Task.Delay(50);
                    if (dtiRB != string.Empty)
                    {
                        return dtiRB;
                    }
                }
            });

            if (await Task.WhenAny(getDtiTask, Task.Delay(2000, cancelToken.Token)) == getDtiTask)
            {
                cancelToken.Cancel();
                return getDtiTask.Result;
            }
            else
            {
                return "*No DTI data*";
            }
        }*/



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

            //Just some test stuff for now
            //Not going to be crazy high precision as we don't know time to execute command and frequency of checking enables
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


        public async Task<bool> read_bDone()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bDoneHandle, CancellationToken.None);
            //Console.WriteLine(result.Value.ToString()); //debugging line
            return result.Value;
        }
        public async Task<double> read_AxisPosition()
        {
            var result = await Plc.TcAds.ReadAnyAsync<double>(fActPositionHandle, CancellationToken.None);
            AxisPosition = result.Value;
            return result.Value;
        }
        public async Task<bool> read_bBusy()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bBusyHandle, CancellationToken.None);
            //Console.WriteLine(result.Value.ToString()); //debugging line
            return result.Value;
        }
        public async Task<bool> read_bEnabled()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bEnabledHandle, CancellationToken.None);
            AxisEnabled = result.Value;
            return result.Value;
        }
        public async Task<bool> read_bFwEnabled()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bFwEnabledHandle, CancellationToken.None);
            AxisFwEnabled = result.Value;
            return result.Value;
        }
        public async Task<bool> read_bBwEnabled()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bBwEnabledHandle, CancellationToken.None);
            AxisBwEnabled = result.Value;
            return result.Value;
        }



        CancellationTokenSource wtoken;
        ActionBlock<DateTimeOffset> taskPos;
        ActionBlock<DateTimeOffset> taskEnabled;
        ActionBlock<DateTimeOffset> taskFwEnabled;
        ActionBlock<DateTimeOffset> taskBwEnabled;

        void StartPositionRead()
        {
            wtoken = new CancellationTokenSource();
            taskPos = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_AxisPosition(), wtoken.Token, TimeSpan.FromMilliseconds(50));
            taskEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bEnabled(), wtoken.Token, TimeSpan.FromMilliseconds(200));
            taskFwEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bFwEnabled(), wtoken.Token, TimeSpan.FromMilliseconds(200));
            taskBwEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bBwEnabled(), wtoken.Token, TimeSpan.FromMilliseconds(200));
            taskPos.Post(DateTimeOffset.Now);
            taskEnabled.Post(DateTimeOffset.Now);
            taskFwEnabled.Post(DateTimeOffset.Now);
            taskBwEnabled.Post(DateTimeOffset.Now);
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
                    dtiRB = DtiPosition;
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
    }
}
