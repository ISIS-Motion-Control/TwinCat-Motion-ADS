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
    "Enable external encoder" - some bool I can trigger to make it try to take a dti reading without having to manually write code in each time
    Currently won't stop position read task when a new initialisation created. Why two tasks?
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

        private double _axisPosition;
        public double AxisPosition
        {
            get { return _axisPosition; }
            set { _axisPosition = value;
                OnPropertyChanged();
            }
        }

        private string _dtiPosition = string.Empty;
        public  string DtiPosition
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
            await Task.Run(()=>DtiPosition = dtiPos);
        }

        public void getDtiReadback()
        {
            string dtiRB = string.Empty;
            Console.WriteLine("Making thread");
            Thread printDti = new Thread(() =>
            {            
               while (true)
               {
                   
                   dtiRB = DtiPosition;
                   Console.WriteLine("DTIRB is " + dtiRB);
                    if (dtiRB != string.Empty)
                    {
                        Console.WriteLine("WE DID IT REDDIT");
                        break;
                    }
                   Thread.Sleep(50);
               }
            });
            printDti.Start();
        }

        public async Task<string> getDtiPositionValue()
        {
            string dtiRB = string.Empty;
            var getDtiTask = Task<string>.Run(async () =>
            {
                while (true)
                {
                    dtiRB = DtiPosition;
                    await Task.Delay(5);
                    if (dtiRB != string.Empty)
                    {
                        return dtiRB;
                    }

                }
            });

            return await getDtiTask;
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
            eCommandHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.eCommand");
            fVelocityHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.fVelocity");
            fPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.fPosition");
            bExecuteHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.bExecute");
            fActPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.fActPosition");
            bDoneHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bDone");
            bBusyHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.bBusy");
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
            if (await checkBusy())
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
                    Console.WriteLine("Command set");
                }
                else if (finishedTask == velocityTask)
                {
                    Console.WriteLine("Velocity set");
                }
                else if (finishedTask == positionTask)
                {
                    Console.WriteLine("Position set");
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
            if (await checkBusy())
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
                    Console.WriteLine("Command set");
                }
                else if (finishedTask == velocityTask)
                {
                    Console.WriteLine("Velocity set");
                }
                else if (finishedTask == positionTask)
                {
                    Console.WriteLine("Position set");
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
            if (await checkBusy())
            {
                return false;   //command fails if axis already busy
            }
            var commandTask = setCommand(eMoveVelocity);
            var velocityTask = setVelocity(velocity);
            var velocityTasks = new List<Task> { commandTask, velocityTask };
            while (velocityTasks.Count > 0)
            {
                Task finishedTask = await Task.WhenAny(velocityTasks);
                if (finishedTask == commandTask)
                {
                    Console.WriteLine("Command set");
                }
                else if (finishedTask == velocityTask)
                {
                    Console.WriteLine("Velocity set");
                }

                velocityTasks.Remove(finishedTask);
            }
            await execute();
            return true;
        }
        public async Task<bool> waitForDone()
        {
            bool doneStatus = await checkDone();
            while (doneStatus != true)
            {
                doneStatus = await checkDone();
            }
            Console.WriteLine("!!!DONE!!!");
            return true;
        }

        public async Task<bool> checkDone()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bDoneHandle, CancellationToken.None);
            //Console.WriteLine(result.Value.ToString()); //debugging line
            return result.Value;
        }
        public async Task<double> readAxisPosition()
        {
            var result = await Plc.TcAds.ReadAnyAsync<double>(fActPositionHandle, CancellationToken.None);
            AxisPosition = result.Value;
            //Console.WriteLine(AxisPosition.ToString());
            return result.Value;
        }
        public async Task<bool> checkBusy()
        {
            var result = await Plc.TcAds.ReadAnyAsync<bool>(bBusyHandle, CancellationToken.None);
            //Console.WriteLine(result.Value.ToString()); //debugging line
            return result.Value;
        }



        CancellationTokenSource wtoken;
        ActionBlock<DateTimeOffset> task;
        void StartPositionRead()
        {
            wtoken = new CancellationTokenSource();
            task = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await readAxisPosition(), wtoken.Token);
            task.Post(DateTimeOffset.Now);
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
            task = null;
        }
        
        
        ITargetBlock<DateTimeOffset> CreateNeverEndingTask(
        Action<DateTimeOffset> action, CancellationToken cancellationToken)
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
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).
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
