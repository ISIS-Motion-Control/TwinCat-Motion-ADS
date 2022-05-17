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

        bool SendInProgress = false;


        public MainWindow()
        {
            InitializeComponent();
            //client.Connect();
            //reader = new StreamReader(client);
            //writer = new StreamWriter(client);
            /*
            while (true)
            {
                //string input = Console.ReadLine();
                //if (String.IsNullOrEmpty(input)) break;
                //writer.WriteLine(input);
                //writer.Flush();
                MessageBox.Show(reader.ReadLine());
                writer.WriteLine("Hello friend");
                writer.Flush();
            }*/
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
            SendInProgress = false;
        }

        private void connectClient_Click(object sender, RoutedEventArgs e)
        {
            client.Connect();
            reader = new StreamReader(client);
            writer = new StreamWriter(client);
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
    }
}
