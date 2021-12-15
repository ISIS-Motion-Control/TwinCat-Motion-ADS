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
        public async Task<bool> CheckCancellationRequestTask(CancellationToken wToken)
        {
            while (CancelTest == false)
            {
                await Task.Delay(10, wToken);
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            return true;
        }
        public async Task<bool> CheckPauseRequestTask(CancellationToken wToken)
        {
            if (PauseTest)
            {
                Console.WriteLine("Test Paused");
            }
            while (PauseTest)
            {
                await Task.Delay(10, wToken);
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

        private bool _valid;
        public bool Valid
        {
            get { return _valid; }
            set
            {
                _valid = value;
                OnPropertyChanged();
            }
        }

    }
}
