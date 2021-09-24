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
    class TCP
    {
        TcpListener TCPListener;
        private static string GET_HISTORY = ((char)4).ToString();
        private static string MESSAGE = ((char)1).ToString();
        private static string LEFT = ((char)3).ToString();

        public void Listen(List<User> clients, List<string> History, int port)
        {
            TCPListener = new TcpListener(IPAddress.Any, port);
            TCPListener.Start();
            try
            {
                while (true)
                {
                    TcpClient client = TCPListener.AcceptTcpClient();
                    IPAddress SenderIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

                    User Sender = clients.Find(x => x.IPAddr == SenderIP);
                    if (Sender == null)                 
                    {
                        lock (ChatWindow.threadLock)
                        {       
                            User item = new User(null, SenderIP, client);
                            clients.Add(item);
                            Sender = item;
                        }
                    }
                    StartClientReceive(Sender, History, clients);
                }
            }
            catch
            {
                MessageBox.Show("TCP listener connection error");
            }
            finally
            {
                TCPListener.Stop();
            }

            
        }

        public void StartClientReceive(User Client, List<string> History, List<User> Clients)
        {
            Thread ClientThread = new Thread(() => { Client.GetTCPMessages(History, Clients); });
            ClientThread.IsBackground = true;
            ClientThread.Start();
        }

        public void SendMessage(List<User> Clients, string Message)
        {
            byte[] MessageBytes = Encoding.UTF8.GetBytes(MESSAGE + Message);
            foreach (User client in Clients)
            {
                client.Connection.GetStream().Write(MessageBytes, 0, MessageBytes.Length);
            }
        }

        public void SendLeftMessage(List<User> Clients)
        {
            byte[] MessageBytes = Encoding.UTF8.GetBytes(LEFT);
            foreach (User client in Clients)
            {
                client.Connection.GetStream().Write(MessageBytes, 0, MessageBytes.Length);
            }
        }

        public void SendHistoryRequest(User client)
        {
            byte[] AskHistoryBytes = Encoding.UTF8.GetBytes(GET_HISTORY);
            client.Connection.GetStream().Write(AskHistoryBytes, 0, AskHistoryBytes.Length);
        }

    }
}
