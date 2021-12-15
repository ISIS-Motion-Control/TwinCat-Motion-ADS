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

        private bool _done;
        public bool Done
        {
            get { return _done; }
            set { _done = value; OnPropertyChanged(); }
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
        private bool _axisBusy;
        public bool AxisBusy
        {
            get { return _axisBusy; }
            set { _axisBusy = value; OnPropertyChanged(); }
        }


        private async Task<bool> CheckFwLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = defaultReadTime)
        {
            //if limitStatus true, watch for FwEnabled flag to go false and complete the task
            while (AxisFwEnabled == limitStatus)
            {
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(msDelay);
            }
            return true;
        }

        private async Task<bool> CheckBwLimitTask(bool limitStatus, CancellationToken wToken, int msDelay = defaultReadTime)
        {
            //if limitStatus true, watch for BwEnabled flag to go false and complete the task
            while (AxisBwEnabled == limitStatus)
            {
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(msDelay);
            }
            return true;
        }

        public async Task<bool> WaitForDone(CancellationToken wToken)
        {
            bool doneStatus = Done;
            while (doneStatus != true)
            {
                doneStatus = Done;
                //Cancellation method for task otherwise task will never complete if no done signal ever received
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(defaultReadTime, wToken);
            }
            return true;
        }

        public async Task<bool> CheckForError(CancellationToken wToken)
        {
            bool errorStatus = Error;
            while (errorStatus != true)
            {
                errorStatus = Error;
                if (wToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                await Task.Delay(defaultReadTime, wToken);
            }
            Console.WriteLine("Axis error");
            return true;
        }

        private readonly CancellationTokenSource readToken = new();

        public void ReadStatuses()
        {
            Task.Run(() => ReadAllStatuses(readToken.Token));
        }
        
        const int defaultReadTime = 10; //ms

        public async Task ReadAllStatuses(CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                if (!ValidCommand())
                {
                    break;
                }
                AxisPosition = (await Plc.TcAds.ReadAnyAsync<double>(fActPositionHandle, CancellationToken.None)).Value;
                AxisBusy = (await Plc.TcAds.ReadAnyAsync<bool>(bBusyHandle, CancellationToken.None)).Value;
                AxisEnabled = (await Plc.TcAds.ReadAnyAsync<bool>(bEnabledHandle, CancellationToken.None)).Value;
                AxisFwEnabled = (await Plc.TcAds.ReadAnyAsync<bool>(bFwEnabledHandle, CancellationToken.None)).Value;
                AxisBwEnabled = (await Plc.TcAds.ReadAnyAsync<bool>(bBwEnabledHandle, CancellationToken.None)).Value;
                Error = (await Plc.TcAds.ReadAnyAsync<bool>(bErrorHandle, CancellationToken.None)).Value;
                Done = (await Plc.TcAds.ReadAnyAsync<bool>(bDoneHandle, CancellationToken.None)).Value;
            }
            AxisPosition = -999;
            AxisBusy = false;
            AxisEnabled = false;
            AxisFwEnabled = false;
            AxisBwEnabled = false;
            Error = false;
            Done = false;
            return;
        }

        public async Task ReadAxisPositionV2(CancellationToken ct, int ts = defaultReadTime)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand())
                {
                    AxisPosition = -999;
                    return;
                }
                var result = await Plc.TcAds.ReadAnyAsync<double>(fActPositionHandle, CancellationToken.None);
                AxisPosition = result.Value;
                await Task.Delay(ts);
            }
            AxisPosition = -999;
            return;
        }

        public async Task ReadAxisBusyV2(CancellationToken ct, int ts = defaultReadTime)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand())
                {
                    AxisBusy = false;
                    return;
                }
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bBusyHandle, CancellationToken.None);
                AxisBusy = result.Value;
                await Task.Delay(ts);
            }
            AxisBusy = false;
            return;
        }

        public async Task ReadAxisEnabledV2(CancellationToken ct, int ts = 0)
        {
            while (!ct.IsCancellationRequested)
            {
                if(!ValidCommand())
                {
                    AxisEnabled = false;
                    return;
                }
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bEnabledHandle, CancellationToken.None);
                AxisEnabled = result.Value;
                await Task.Delay(ts);
            }
            AxisEnabled = false;
            return;
        }

        public async Task ReadAxisFwEnabledV2(CancellationToken ct, int ts = 0)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand())
                {
                    AxisFwEnabled = false;
                    return;
                }
                var result = await Plc.TcAds.ReadAnyAsync<bool>(bFwEnabledHandle, CancellationToken.None);
                AxisFwEnabled = result.Value;
                await Task.Delay(ts);
            }
            AxisFwEnabled = false;
            return;
        }

        public async Task ReadAxisBwEnabledV2(CancellationToken ct, int ts = 0)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand())
                {
                    AxisBwEnabled = false;
                    return;
                }
                TwinCAT.Ads.ResultValue<bool> result = await Plc.TcAds.ReadAnyAsync<bool>(bBwEnabledHandle, CancellationToken.None);
                AxisBwEnabled = result.Value;
                await Task.Delay(ts);
            }
            AxisBwEnabled = false;
            return;
        }

        public async Task ReadAxisErrorV2(CancellationToken ct, int ts = 0)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand())
                {
                    Error = true;
                    return;
                }
                TwinCAT.Ads.ResultValue<bool> result = await Plc.TcAds.ReadAnyAsync<bool>(bErrorHandle, CancellationToken.None);
                Error = result.Value;
                await Task.Delay(ts);
            }
            Error = true;
            return;
        }
        
        public async Task ReadAxisDoneV2(CancellationToken ct, int ts = 0)
        {
            while (!ct.IsCancellationRequested)
            {
                if (!ValidCommand())
                {
                    Done = true;
                    return;
                }
                TwinCAT.Ads.ResultValue<bool> result = await Plc.TcAds.ReadAnyAsync<bool>(bDoneHandle, CancellationToken.None);
                Done = result.Value;
                await Task.Delay(ts);
            }
            Done = true;
            return;
        }


    }
}
