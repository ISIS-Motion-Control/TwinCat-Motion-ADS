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
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bDoneHandle, CancellationToken.None);
                return result.Value;
            }
            catch
            {
                throw new Exception();
            }
            
        }

        public async Task<double> read_AxisPosition()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<double>(fActPositionHandle, CancellationToken.None);
                AxisPosition = result.Value;
                return result.Value;
            }
            catch
            {
                throw new Exception();
            }
        }

        public async Task<bool> read_bBusy()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bBusyHandle, CancellationToken.None);
                return result.Value;
            }
            catch
            {
                throw new Exception();
            }
            
        }

        public async Task<bool> read_bEnabled()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bEnabledHandle, CancellationToken.None);
                AxisEnabled = result.Value;
                return result.Value;
            }
            catch
            {
                throw new Exception();
            }
            
        }

        public async Task<bool> read_bFwEnabled()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bFwEnabledHandle, CancellationToken.None);
                AxisFwEnabled = result.Value;
                return result.Value;
            }
            catch
            {
                throw new Exception();
            }
            
        }

        public async Task<bool> read_bBwEnabled()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bBwEnabledHandle, CancellationToken.None);
                AxisBwEnabled = result.Value;
                return result.Value;
            }
            catch
            {
                throw new Exception();
            }
        }

        public async Task<bool> read_bError()
        {
            try
            {
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bErrorHandle, CancellationToken.None);
                Error = result.Value;
                return result.Value;
            }
            catch
            {
                throw new Exception();
            }
        }

        CancellationTokenSource wtoken;
        ActionBlock<DateTimeOffset> taskPos;
        ActionBlock<DateTimeOffset> taskEnabled;
        ActionBlock<DateTimeOffset> taskFwEnabled;
        ActionBlock<DateTimeOffset> taskBwEnabled;
        ActionBlock<DateTimeOffset> taskError;

        public void StartPositionRead()
        {
            try
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
            catch
            {
                Console.WriteLine("Lost connection to controller");
                StopPositionRead();
                Plc.Disconnect();
            }
            
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
