using System;
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
using System.IO.Pipes;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Renishaw_XL80_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NamedPipeClientStream client = new NamedPipeClientStream("RenishawXL80_Pipe");
        StreamReader reader;
        StreamWriter writer;

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        bool SendInProgress = false;


        public MainWindow()
        {
            InitializeComponent();
            Connection();
        }

        private void checkForMessage_Click(object sender, RoutedEventArgs e)
        {
            /*if(reader.Peek()==-1)
            {
                MessageBox.Show("No message");
                return;
            }*/
            MessageBox.Show(reader.ReadLine());
        }

        private async void sendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (SendInProgress) return;
            SendInProgress = true;
            await writer.WriteLineAsync(messageToSend.Text);
            await writer.FlushAsync();
        }

        private void connectClient_Click(object sender, RoutedEventArgs e)
        {
            client.Connect();
            reader = new StreamReader(client);
            writer = new StreamWriter(client);
            ConstantReadMode();
        }
        private void Connection()
        {
            client.Connect();
            reader = new StreamReader(client);
            writer = new StreamWriter(client);
            ConstantReadMode();
        }

        private async void enterReadMode_Click(object sender, RoutedEventArgs e)
        {
            string cmd;
            while (true)
            {
                cmd = await reader.ReadLineAsync();

                if(cmd=="1")
                {
                    await writer.WriteLineAsync("test data here");
                    await writer.FlushAsync();
                }
                if (cmd == "0")
                    break;
            }

        }

        private async void ConstantReadMode()
        {
            string cmd;
            while (true)
            {
                cmd = await reader.ReadLineAsync();

                if (cmd == "1")
                {
                    await writer.WriteLineAsync(messageToSend.Text);
                    await writer.FlushAsync();
                }
                if (cmd == "0")
                    break;
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}
