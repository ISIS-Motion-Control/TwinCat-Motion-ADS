using System;
using TwinCAT.Ads;

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

        private string _id;
        public string ID
        {
            get { return _id; }
            set { _id = value; }
        }
        private int _port;

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }



        public PLC(string amsID, int port)
        {
            ID = amsID;
            Port = port;
            try
            {
                TcAds.Connect(ID, port);
            }
            catch
            {
                //Console.WriteLine("Invalid AMS NET ID FORMAT");
            }

        }

        public bool Connect()
        {
            try
            {
                TcAds.Connect(ID, Port);
                return true;
            }
            catch
            {
                Console.WriteLine("Invalid configuration");
                return false;
            }
        }

        public bool Disconnect()
        {
            try
            {
                return TcAds.Disconnect();
            }
            catch
            {
                Console.WriteLine("Disconnect Failed");
                return false;
            }
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
        public bool IsStateRun()
        {
            try
            {
                if (TcAds.ReadState().AdsState == AdsState.Run)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }


        public AdsState setupPLC()
        {
            if (checkConnection())
            { Console.WriteLine("Port open"); };
            //Connect();
            return checkAdsState();

        }
    }
}
