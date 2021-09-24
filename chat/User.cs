using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace chat
{
    class User
    {
        public string Name { get; set; }
        public IPAddress IPAddr { get; set; }
        public TcpClient Connection;
        private const int BUFFER_SIZE = 64;
        private static string GET_HISTORY = ((char)4).ToString();
        private static string HISTORY = ((char)5).ToString();
        private static string LEFT = ((char)3).ToString();
        private static string NAME = ((char)2).ToString();
        private static string MESSAGE = ((char)1).ToString();



        public User(string Name, IPAddress IPAddr, TcpClient Connection)
        {
            this.Name = Name;
            this.IPAddr = IPAddr;
            this.Connection = Connection;
        }

        public void GetTCPMessages(List<string> History, List<User> Clients)
        {
            NetworkStream OneUserStream = Connection.GetStream();
            try
            {
                while (true)
                {
                    byte[] byteMessage = new byte[BUFFER_SIZE];
                    StringBuilder MessageBuilder = new StringBuilder();
                    string message;
                    int RecBytes = 0;
                    do
                    {
                        RecBytes = OneUserStream.Read(byteMessage, 0, byteMessage.Length);
                        MessageBuilder.Append(Encoding.UTF8.GetString(byteMessage, 0, RecBytes));
                    }
                    while (OneUserStream.DataAvailable);

                    message = MessageBuilder.ToString();
                    if (Name == null)
                    {
                        Name = message;
                    }
                    else if (message[0].ToString() == MESSAGE)
                    {
                        History.Add(Name + "(" + IPAddr.ToString() + ") " + "(" + DateTime.Now.ToLongTimeString() + "): " + message.Substring(1));
                    }
                    else if (message[0].ToString() == HISTORY)
                    {
                        string FullHistory = message.Substring(1);
                        List<string> history = new List<string>();
                        while (FullHistory != "")
                        {
                            history.Add(FullHistory.Substring(0, FullHistory.IndexOf((char)1)));
                            FullHistory = FullHistory.Substring(FullHistory.IndexOf((char)1) + 1);
                        }
                        lock (ChatWindow.threadHistoryLock)
                        {
                            History.Clear();
                            foreach (string items in history)
                            {
                                History.Add(items);
                            }
                        }
                    }
                    else if (message == GET_HISTORY)
                    {
                        HistoryRecieve(History, this);
                    }
                    else if (message == LEFT)
                    {
                        History.Add(Name + "(" + IPAddr.ToString() + ") " + "(" + DateTime.Now.ToLongTimeString() + "):" + " left chat");
                        var address = ((IPEndPoint)Connection.Client.RemoteEndPoint).Address;
                        lock (ChatWindow.threadLock)
                        {
                            Clients.RemoveAll(X => X.IPAddr.ToString() == address.ToString());
                        }
                        break;
                    }
                    
                }
            }
            catch
            {
                History.Add(Name + "(" + IPAddr.ToString() + ") " + "(" + DateTime.Now.ToLongTimeString() + "):" + " left chat");
                var address = ((IPEndPoint)Connection.Client.RemoteEndPoint).Address;
                lock (ChatWindow.threadLock)
                {
                    Clients.RemoveAll(X => X.IPAddr.ToString() == address.ToString());
                }
            }
            finally
            {
                if (OneUserStream != null)
                    OneUserStream.Close();
                if (Connection != null)
                    Connection.Close();

            }

            
        }

        public void HistoryRecieve(List<string> History, User client)
        {
            byte[] HistoryItemBytes;
            string history = "";
            foreach (string HistoryItem in History)
            {
                history += HistoryItem + ((char)1).ToString();
            }

            HistoryItemBytes = Encoding.UTF8.GetBytes(HISTORY + history);
            client.Connection.GetStream().Write(HistoryItemBytes, 0, HistoryItemBytes.Length);
        }
    }
}
