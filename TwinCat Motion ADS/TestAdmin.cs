using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TwinCat_Motion_ADS
{
    //Need to remove all the current DTI logic and add new DigimaticIndicator class
    /*
     * Probably have some generic fields for DTI1/2/3/4 then a method that initialises them (so each test axis has access to it through inheritance)
     * Then prep the method that it actually reads with
     * Maybe implement some "if selected but null" methods to check and initialise them
     * 
     */

    //abstract defines this as an inheritance only class
    public abstract class TestAdmin : INotifyPropertyChanged
    {
        //PLC object to which the test axis belongs
        public PLC Plc { get; set; }
        //Directory for saving test csv
        public string TestDirectory { get; set; } = string.Empty;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        //Request test pause
        private bool _pauseTest = false;
        public bool PauseTest
        {
            get { return _pauseTest; }
            set { _pauseTest = value; OnPropertyChanged(); }
        }
        //Request test cancellation
        private bool _cancelTest = false;
        public bool CancelTest
        {
            get { return _cancelTest; }
            set { _cancelTest = value; OnPropertyChanged(); }
        }
        public async Task<bool> checkCancellationRequestTask(CancellationToken wToken)
        {
            while (CancelTest == false)
            {
                await Task.Delay(10);
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            return true;
        }
        public async Task<bool> checkPauseRequestTask(CancellationToken wToken)
        {
            if (PauseTest)
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

        public uint dti1_Handle;
        public uint dti2_Handle;

        public ITargetBlock<DateTimeOffset> CreateNeverEndingTask(
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
        /// <summary>
        /// Trigger a data read of DTI 1
        /// </summary>
        /// <returns></returns>
        public async Task TriggerDti1()
        {
            if (dti1_Handle == 0)
            {
                return;
            }
            await Plc.TcAds.WriteAnyAsync(dti1_Handle, true, CancellationToken.None);
        }

        /// <summary>
        /// Trigger a data read of DTI 2
        /// </summary>
        /// <returns></returns>
        public async Task TriggerDti2()
        {
            if (dti2_Handle == 0)
            {
                return;
            }
            await Plc.TcAds.WriteAnyAsync(dti2_Handle, true, CancellationToken.None);
        }

        private string _dtiPosition = string.Empty;
        public string DtiPosition
        {
            get
            {

                return _dtiPosition;
            }
            set { _dtiPosition = value; }
        }
        public string readAndClearDtiPosition()
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
        public async Task<string> getDtiPositionValue(CancellationTokenSource ct, int timeout = 2000, int delay = 50)
        {
            string dtiRB = string.Empty;
            var getDtiTask = Task<string>.Run(async () =>
            {
                while (true)
                {
                    //dtiRB = DtiPosition;
                    dtiRB = readAndClearDtiPosition();
                    await Task.Delay(delay);
                    if (dtiRB != string.Empty)
                    {
                        return dtiRB;
                    }
                    if (ct.Token.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }
                }
            });

            if (await Task.WhenAny(getDtiTask, Task.Delay(timeout, ct.Token)) == getDtiTask)
            {
                ct.Cancel();
                return getDtiTask.Result;
            }
            else
            {
                ct.Cancel();
                return "*No DTI data*";
            }
        }
    }
}
