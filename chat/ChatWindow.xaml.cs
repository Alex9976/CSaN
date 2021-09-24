using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace chat
{

    public partial class ChatWindow : Window
    {
        const int TCPPort = 55555;
        const int UDPPort = 55550;
        List<User> Users = new List<User>();
        List<string> History = new List<string>();
        Thread UDPListenThread = null;
        Thread TCPListenThread = null;

        UDP udp = new UDP();
        TCP tcp = new TCP();
        string Username;
        string Message;
        public static object threadLock = new object();
        public static object threadHistoryLock = new object();

        public static IPAddress myIP;
        public static IPAddress BroadcastIP;

        public ChatWindow(string Name, IPAddress IP)
        {
            InitializeComponent();

            myIP = IP;
            Username = Name;

            BroadcastIP = getBroadcast(myIP);
            UDP.IpAdressBroadcast = BroadcastIP;
            UDP.myIP = myIP;

            Initialize();

            Thread MessageUpdater = new Thread(() => { Update(); });
            MessageUpdater.Start();
            Thread GetHistory = new Thread(() => { getHistory(); });
            GetHistory.Start();

        }

        private IPAddress getBroadcast(IPAddress userIP)
        {
            IPAddress Mask = null;

            NetworkInterface[] allNets = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in allNets)
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (userIP.Equals(unicastIPAddressInformation.Address))
                        {
                            Mask = unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            byte[] byteMask = Mask.GetAddressBytes();
            byte[] userMask = userIP.GetAddressBytes();
            for (int i = 0; i < 4; i++)
            {
                byteMask[i] = (byte)((byte)~byteMask[i] | userMask[i]);
            }
            Mask = new IPAddress(byteMask);

            return Mask;
        }

        public void Initialize()
        {
            udp.Connect(Username, UDPPort, History);

            TCPListenThread = new Thread(() => { tcp.Listen(Users, History, TCPPort); });
            TCPListenThread.Start();

            UDPListenThread = new Thread(() => { udp.Receive(Users, History, Username, TCPPort, UDPPort); });
            UDPListenThread.Start();
        }

        public void getHistory()
        {
            DateTime start, finish = new DateTime();
            TimeSpan elapsedSpan = new TimeSpan();
            bool isHaveHistory = false;
            start = DateTime.Now;
            while (elapsedSpan.TotalMilliseconds <= 5000 && !isHaveHistory)
            {
                if (Users.Count != 0)
                {
                    Thread.Sleep(500);
                    Dispatcher.Invoke(() =>
                    {
                        tcp.SendHistoryRequest(Users[0]);

                    });
                }
                finish = DateTime.Now;
                elapsedSpan = TimeSpan.FromTicks(finish.Ticks - start.Ticks);
                Thread.Sleep(50);
            }

        }

        public void Update()
        {
            int i;
            int ListCount = 0, HistoryCount;
            while (true)
            {
                Dispatcher.Invoke(() =>
                {    
                    lock (threadHistoryLock)
                    {
                        HistoryCount = History.Count;
                        if (HistoryCount > ListCount)
                        {
                            listBox.Items.Clear();
                            for (i = 0; i < History.Count; i++)
                            {
                                if (History[i].Contains(myIP.ToString()))
                                {
                                    listBox.Items.Add("You" + History[i].Substring(History[i].IndexOf(")") + 1));
                                }
                                else
                                {
                                    listBox.Items.Add(History[i]);
                                }
                            }
                            ListCount = listBox.Items.Count;

                            listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                        }

                    }

                });

                Thread.Sleep(200);
            }

        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            Send();
        }

        public void Send()
        {
            Message = message.Text;
            message.Text = "";
            if (Message != "")
            {
                tcp.SendMessage(Users, Message);
                History.Add(Username + "(" + myIP.ToString() + ") (" + DateTime.Now.ToLongTimeString() + ")" + ": " + Message);
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tcp.SendLeftMessage(Users);
            TCPListenThread.IsBackground = true;
            UDPListenThread.IsBackground = true;
            System.Environment.Exit(0);
        }

        private void message_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send();
            }
        }
    }
}
