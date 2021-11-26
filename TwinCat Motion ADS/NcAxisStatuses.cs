using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TwinCat_Motion_ADS
{
    public partial class NcAxis
    {
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
            set { _axisPosition = value; OnPropertyChanged(); }
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

        public async Task<bool> checkForError(CancellationToken wToken)
        {
            bool errorStatus = await read_bError();
            while (errorStatus != true)
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

        public async Task<bool> read_bDone()
        {
            var thisTask = Plc.TcAds.ReadAnyAsync<bool>(bDoneHandle, CancellationToken.None);
            await thisTask;
            if (thisTask.Status == TaskStatus.Faulted)
            {
                throw new Exception();
            }
            else
            {
                return thisTask.Result.Value; ;
            }
        }

        public async Task<double> read_AxisPosition()
        {
            if (!ValidCommand()) return -9999999;
            
            

            var thisTask = Plc.TcAds.ReadAnyAsync<double>(fActPositionHandle, CancellationToken.None);
            Task.WaitAny(thisTask);
            if(thisTask == null)
            {
                Console.WriteLine("Shit went wrong");
            }
            //await thisTask;
            if (thisTask.Status == TaskStatus.Faulted)
            {
                //StopPositionRead();
                return -9999999;
            }
            else
            {
                AxisPosition = thisTask.Result.Value;
                return thisTask.Result.Value; ;
            }
        }

        public async Task<bool> read_bBusy()
        {
            try
            {
                var thisTask = Plc.TcAds.ReadAnyAsync<bool>(bBusyHandle, CancellationToken.None);
                await thisTask;
                if (thisTask.Status == TaskStatus.Faulted)
                {
                    throw new Exception();
                }
                else
                {
                    return thisTask.Result.Value; ;
                }
            }
            catch
            {
                return false;
            }
            
        }

        public async Task<bool> read_bEnabled()
        {
            try
            {
                var thisTask = Plc.TcAds.ReadAnyAsync<bool>(bEnabledHandle, CancellationToken.None);
                await thisTask;
                if (thisTask.Status == TaskStatus.Faulted)
                {
                    throw new Exception();
                }
                else
                {
                    AxisEnabled = thisTask.Result.Value;
                    return thisTask.Result.Value; ;
                }
            }
            catch
            {
                return false;
            }
            
        }

        public async Task<bool> read_bFwEnabled()
        {
            try
            {
                var thisTask = Plc.TcAds.ReadAnyAsync<bool>(bFwEnabledHandle, CancellationToken.None);
                await thisTask;
                if (thisTask.Status == TaskStatus.Faulted)
                {
                    throw new Exception();
                }
                else
                {
                    AxisFwEnabled = thisTask.Result.Value;
                    return AxisFwEnabled;
                }

                //var result = await Plc.TcAds.ReadAnyAsync<bool>(bFwEnabledHandle, CancellationToken.None);
                //AxisFwEnabled = result.Value;
               // return result.Value;
            }
            catch
            {
                return false;
            }
            
        }

        public async Task<bool> read_bBwEnabled()
        {
            try
            {
                var thisTask = Plc.TcAds.ReadAnyAsync<bool>(bBwEnabledHandle, CancellationToken.None);
                await thisTask;
                if (thisTask.Status == TaskStatus.Faulted)
                {
                    throw new Exception();
                }
                else
                {
                    AxisBwEnabled = thisTask.Result.Value;
                    return AxisBwEnabled;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> read_bError()
        {
            try
            {
                var thisTask = Plc.TcAds.ReadAnyAsync<bool>(bErrorHandle, CancellationToken.None);
                await thisTask;
                if (thisTask.Status == TaskStatus.Faulted)
                {
                    throw new Exception();
                }
                else
                {
                    Error = thisTask.Result.Value;
                    return Error;
                }
            }
            catch
            {
                return false;
            }
        }

        
        ActionBlock<DateTimeOffset> taskPos;
        ActionBlock<DateTimeOffset> taskEnabled;
        ActionBlock<DateTimeOffset> taskFwEnabled;
        ActionBlock<DateTimeOffset> taskBwEnabled;
        ActionBlock<DateTimeOffset> taskError;

        CancellationTokenSource readToken;
        CancellationTokenSource readToken1;
        CancellationTokenSource readToken2;
        CancellationTokenSource readToken3;
        CancellationTokenSource readToken4;
        public void StartPositionRead()
        {
            try
            {
                readToken = new CancellationTokenSource();
                readToken1 = new CancellationTokenSource();
                readToken2 = new CancellationTokenSource();
                readToken3 = new CancellationTokenSource();
                readToken4 = new CancellationTokenSource();
                taskPos = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_AxisPosition(), readToken.Token, TimeSpan.FromMilliseconds(50));
                taskEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bEnabled(), readToken1.Token, TimeSpan.FromMilliseconds(200));
                taskFwEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bFwEnabled(), readToken2.Token, TimeSpan.FromMilliseconds(200));
                taskBwEnabled = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bBwEnabled(), readToken3.Token, TimeSpan.FromMilliseconds(200));
                taskError = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(async now => await read_bError(), readToken4.Token, TimeSpan.FromMilliseconds(200));
                taskPos.Post(DateTimeOffset.Now);
                taskEnabled.Post(DateTimeOffset.Now);
                taskFwEnabled.Post(DateTimeOffset.Now);
                taskBwEnabled.Post(DateTimeOffset.Now);
                taskError.Post(DateTimeOffset.Now);
            }
            catch(Exception)
            {
                Console.WriteLine("Lost connection to controller");
            }
            
        }
        public void StopPositionRead()
        {
            if (readToken == null) return;
            using (readToken) readToken.Cancel();

            if (readToken1 == null) return;
            using (readToken1) readToken1.Cancel();
            if (readToken2 == null) return;
            using (readToken2) readToken2.Cancel();
            if (readToken3 == null) return;
            using (readToken3) readToken3.Cancel();
            if (readToken4 == null) return;
            using (readToken4) readToken4.Cancel();


            readToken = null;
            readToken1 = null;
            readToken2 = null;
            readToken3 = null;
            readToken4 = null;

            taskPos = null;
            taskEnabled = null;
            taskFwEnabled = null;
            taskBwEnabled = null;
            taskError = null;
        }

    }
}
