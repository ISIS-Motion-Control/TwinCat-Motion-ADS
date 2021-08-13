using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TwinCAT.Ads;

namespace TwinCat_Motion_ADS
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConsoleAllocator.ShowConsoleWindow();
            TestClass testSystem = TestClass.Instance;
            testSystem.setupPLC();
        }
    }

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



        Axis axis1;
        Axis axis2;
        PLC plc;

        public void setupPLC()
        {
            plc = new PLC("5.65.74.200.1.1", 852);
            if (plc.checkConnection())
            { Console.WriteLine("Connection established"); };
            axis1 = new Axis(1, plc);
            axis2 = new Axis(2, plc);
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
    public partial class Axis
    {
        const byte eMoveAbsolute = 0;
        const byte eMoveRelative = 1;
        const byte eMoveVelocity = 3;
        const byte eHome = 10;
        private uint eCommandHandle;
        private uint fVelocityHandle;
        private uint fPositionHandle;
        private uint bExecuteHandle;
        private uint fActPositionHandle;

        private PLC _plc;
        public PLC Plc
        {
            get { return _plc; }
            set { _plc = value; }
        }


        public Axis(int axisID, PLC plc)
        {
            Plc = plc;

            eCommandHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.eCommand");
            fVelocityHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.fVelocity");
            fPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.fPosition");
            bExecuteHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stControl.bExecute");
            fActPositionHandle = Plc.TcAds.CreateVariableHandle("GVL.astAxes[" + axisID + "].stStatus.fActPosition");



            moveAbsolute(30, 20);
            double axisPos = (double)Plc.TcAds.ReadAny(fActPositionHandle, typeof(double));
            Console.WriteLine("Current value is: " + axisPos);
        }

        private void setCommand(byte command)
        {
            Plc.TcAds.WriteAny(eCommandHandle, command);
        }
        private void setVelocity(double velocity)
        {
            Plc.TcAds.WriteAny(fVelocityHandle, velocity);
        }
        private void setPosition(double position)
        {
            Plc.TcAds.WriteAny(fPositionHandle, position);
        }
        private void execute()
        {
            Plc.TcAds.WriteAny(bExecuteHandle, true);
        }

        public bool moveAbsolute(double position, double velocity)
        {
            setCommand(eMoveAbsolute);
            setVelocity(velocity);
            setPosition(position);
            execute();
            return true;
        }
    }
    internal static class ConsoleAllocator
    {
        [DllImport(@"kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport(@"kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport(@"user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SwHide = 0;
        const int SwShow = 5;


        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                AllocConsole();
            }
            else
            {
                ShowWindow(handle, SwShow);
            }
        }

        public static void HideConsoleWindow()
        {
            var handle = GetConsoleWindow();

            ShowWindow(handle, SwHide);
        }
    }
}
