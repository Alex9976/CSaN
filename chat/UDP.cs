using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace chat
{
    class UDP
    {
        private UdpClient UdpSender;
        public static IPAddress IpAdressBroadcast;
        public static IPAddress myIP;
        private IPEndPoint IPEndPointBroadcast;
        private UdpClient UdpListener = null;
        private static string NAME = ((char)2).ToString();

        public void Connect(string Name, int port, List<string> History)
        {
            try
            {
                UdpSender = new UdpClient(port, AddressFamily.InterNetwork);
                IPEndPointBroadcast = new IPEndPoint(IpAdressBroadcast, port);
                UdpSender.Connect(IPEndPointBroadcast);
                byte[] LoginBytes = Encoding.UTF8.GetBytes(NAME + Name);
                int sendedData = UdpSender.Send(LoginBytes, LoginBytes.Length);
                
                if (sendedData == LoginBytes.Length)
                {
                    History.Add(Name + "(" + ChatWindow.myIP.ToString() + ") " + "(" + DateTime.Now.ToLongTimeString() + "):" + " join chat");

                }
                UdpSender.Close();
            }
            catch
            {
                MessageBox.Show("Error sending broadcast packet");
            }
        }

        public void Receive(List<User> Users, List<string> History, string Name, int TCPPort, int UDPPort)
        {
            UdpListener = new UdpClient();
            try
            {
                IPEndPoint ClientEndPoint = new IPEndPoint(IPAddress.Any, UDPPort);

                UdpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                UdpListener.ExclusiveAddressUse = false;
                UdpListener.Client.Bind(ClientEndPoint);
                while (true)
                {
                    Byte[] data = UdpListener.Receive(ref ClientEndPoint);
                    string UserName = Encoding.UTF8.GetString(data);
                    UserName = UserName.Substring(1);
                    try
                    {
                        TcpClient NewTcp;
                        if (Users.Find(x => x.IPAddr.ToString() == ClientEndPoint.Address.ToString()) == null && myIP.ToString() != ClientEndPoint.Address.ToString())
                        {
                            NewTcp = new TcpClient();
                            NewTcp.Connect(new IPEndPoint(ClientEndPoint.Address, TCPPort));

                            Users.Add(new User(UserName, ClientEndPoint.Address, NewTcp));

                            History.Add(UserName + "(" + ClientEndPoint.Address + ") " + "(" + DateTime.Now.ToLongTimeString() + "):" + " join chat");


                            StartClientReceive(Users[Users.Count - 1], History, Users);

                            byte[] LoginBytes = Encoding.UTF8.GetBytes(Name);
                            NewTcp.GetStream().Write(LoginBytes, 0, LoginBytes.Length);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Connection timed out");
                    }

                }
            }
            catch
            {
                MessageBox.Show("UDP listener connection error");
            }
            finally
            {
                UdpListener.Close();
            }
        }

        public void StartClientReceive(User Client, List<string> History, List<User> Clients)
        {
            Thread ClientThread = new Thread(() => { Client.GetTCPMessages(History, Clients); });
            ClientThread.Start();
        }

    }
}
