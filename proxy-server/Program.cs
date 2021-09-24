using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace proxy_server
{
    class Program
    {
        static void Main(string[] args)
        {
            string blacklist = getList("list.conf");
            Proxy proxyServer = new Proxy("127.0.0.1", 45054, blacklist);
            proxyServer.Listen();
            while (true)
            {
                Socket socket = proxyServer.Accept();
                Thread thread = new Thread(() => proxyServer.ReceiveData(socket));
                thread.Start();
            }

        }

        static string getList(string path)
        {
            string blacklist = "";
            try 
            {
                StreamReader reader = new StreamReader(path, System.Text.Encoding.Default);
                blacklist = reader.ReadToEnd();
            }
            catch
            {
                return blacklist;
            }
            return blacklist;
        }
    }
}
