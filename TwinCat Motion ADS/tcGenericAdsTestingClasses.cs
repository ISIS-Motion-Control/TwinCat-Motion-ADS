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
        private uint bErrorHandle;
        private uint bEnableHandle;
        private uint bResetHandle;


        private uint dti1Handle;
        private uint dti2Handle;

        private bool _error;
        public bool Error
        {
            get { return _error; }
            set { _error = value; OnPropertyChanged(); }
        }

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
        private string _testDirectory = string.Empty;
        public string TestDirectory
        {
            get { return _testDirectory; }
            set { _testDirectory = value; }
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
        private uint _axisID;
        public uint AxisID
        {
            get { return _axisID; }
            set { _axisID = value; }
        }

        public Axis(uint axisID, PLC plc, bool dti1Present = false, bool dti2Present = false)
        {
            Plc = plc;
            AxisID = axisID;
            updateInstance(axisID,dti1Present,dti2Present);
        }
        public void updateInstance(uint axisID, bool dti1Present = false, bool dti2Present = false)
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
        public async Task setEnable(bool enable)
        {
            await Plc.TcAds.WriteAnyAsync(bEnableHandle, enable, CancellationToken.None);
        }
        public async Task Reset()
        {
            await Plc.TcAds.WriteAnyAsync(bResetHandle, true, CancellationToken.None);
        }
        public async Task TriggerDti1()
        {
            if (dti1Handle == 0)
            {
                return;
            }
            await Plc.TcAds.WriteAnyAsync(dti1Handle, true, CancellationToken.None);
        }
        public async Task TriggerDti2()
        {
            if (dti2Handle == 0)
            {
                return;
            }
            await Plc.TcAds.WriteAnyAsync(dti2Handle, true, CancellationToken.None);
        }

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

        public async Task<bool> HighLimitReversal(double velocity, int timeout, int extraReversalTime, int settleTime)
        {
            if (await read_bFwEnabled() == true)
            {
                Console.WriteLine("Not on high limit. Reversal command rejected");
                return false;
            }
            if (velocity < 0)
            {
                velocity = velocity *-1;

            }
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
            

            //Start a task to check the bool that only returns when flag is hit (BwEnabled == false)
            Task checkFwEnableTask = Task<bool>.Run(async () =>
            {
                while (await read_bFwEnabled() == false)
                {
                    await Task.Delay(10);
                }
                await Task.Delay(TimeSpan.FromSeconds(extraReversalTime));
                return true;
            });
            
            var cancelToken = new CancellationTokenSource();
            if (await Task.WhenAny(checkFwEnableTask, Task.Delay(TimeSpan.FromSeconds(timeout), cancelToken.Token)) == checkFwEnableTask)
            {
                //Console.WriteLine("Reversal complete");     
                await moveStop();
                cancelToken.Cancel();
            }
            else
            {
                Console.WriteLine("Timeout on reversal");
                await moveStop();
                return false;
            }
            //Now to move back on to fwLimit
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Approach high limit command rejected ");
                return false;
            }
            checkFwEnableTask = Task<bool>.Run(async () =>
            {
                while (await read_bFwEnabled() == true)
                {
                    await Task.Delay(1);    //much faster check time
                }
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                return true;
            });
            var cancelToken2 = new CancellationTokenSource();
            if (await Task.WhenAny(checkFwEnableTask, Task.Delay(TimeSpan.FromSeconds(timeout), cancelToken2.Token)) == checkFwEnableTask)
            {
                //Console.WriteLine("On high limit");
                cancelToken2.Cancel();
                return true;
            }
            else
            {
                Console.WriteLine("Timeout on move to limit");
                await moveStop();
                return false;
            }
        }

        public async Task<bool> LowLimitReversal(double velocity, int timeout, int extraReversalTime, int settleTime)
        {
            if (await read_bBwEnabled() == true)
            {
                Console.WriteLine("Not on low limit. Reversal command rejected");
                return false;
            }
            if (velocity > 0)
            {
                velocity = velocity * -1;
            }
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


            //Start a task to check the bool that only returns when flag is hit (BwEnabled == false)
            Task checkBwEnableTask = Task<bool>.Run(async () =>
            {
                while (await read_bBwEnabled() == false)
                {
                    await Task.Delay(10);
                }
                await Task.Delay(TimeSpan.FromSeconds(extraReversalTime));
                return true;
            });

            var cancelToken = new CancellationTokenSource();
            if (await Task.WhenAny(checkBwEnableTask, Task.Delay(TimeSpan.FromSeconds(timeout), cancelToken.Token)) == checkBwEnableTask)
            {
                //Console.WriteLine("Reversal complete");     
                await moveStop();
                cancelToken.Cancel();
            }
            else
            {
                Console.WriteLine("Timeout on reversal");
                await moveStop();
                return false;
            }
            //Now to move back on to fwLimit
            if (await moveVelocity(velocity) == false)
            {
                Console.WriteLine("Approach low limit command rejected ");
                return false;
            }
            checkBwEnableTask = Task<bool>.Run(async () =>
            {
                while (await read_bBwEnabled() == true)
                {
                    await Task.Delay(1);    //much faster check time
                }
                await Task.Delay(TimeSpan.FromSeconds(settleTime));
                return true;
            });
            var cancelToken2 = new CancellationTokenSource();
            if (await Task.WhenAny(checkBwEnableTask, Task.Delay(TimeSpan.FromSeconds(timeout), cancelToken2.Token)) == checkBwEnableTask)
            {
                //Console.WriteLine("On high limit");
                cancelToken2.Cancel();
                return true;
            }
            else
            {
                Console.WriteLine("Timeout on move to limit");
                await moveStop();
                return false;
            }
        }

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

        public async Task<bool> end2endCycleTestingWithReversal(double setVelocity, double reversalVelocity, int timeout, int cycleDelay, int cycles, int resersalExtraTime, int reversalSettleTime, bool dtiPresent = false)
        {
            var currentTime = DateTime.Now;
            string formattedTitle = string.Format("{0:yyyyMMdd}--{0:HH}h-{0:mm}m-{0:ss}s-End2EndwithReversalTest-setVelo({1}) revVelo({2}) settleTime({3}) - {4} cycles", currentTime,setVelocity,reversalVelocity,reversalSettleTime,cycles);

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

                //If no timeout set. Make it really high and likely not timeout
                if (timeout <= 0)
            {
                timeout = 100000;
            }

            Stopwatch stopWatch = new Stopwatch(); //Create stopwatch for rough end to end timing
            setVelocity = Math.Abs(setVelocity);
            //Start low
            if (await moveToLowLimit(-setVelocity, timeout) == false)
            {
                Console.WriteLine("Failed to move to low limit for start of test");
                return false;
            }
            
            //Create an ongoing task to monitor for a cancellation request. This will only trigger on start of each test cycle.
            var cancelTaskToken = new CancellationTokenSource();
            var cancelTask = Task.Run(() =>
            {
                while (CancelTest == false) ;
            }, cancelTaskToken.Token);
            var pauseTaskToken = new CancellationTokenSource();

            //Start running test cycles
            end2endReversalCSV record1;
            end2endReversalCSV record2;
            for (int i = 1; i <= cycles; i++)
            {
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
}
