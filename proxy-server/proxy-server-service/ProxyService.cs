using proxy_server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace proxy_server_service
{
    public partial class ProxyService : ServiceBase
    {
        public ProxyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            using (StreamWriter writer = new StreamWriter("C:\\Proxy\\log.txt", true))
            {
                writer.WriteLine(string.Format(DateTime.Now + ": Proxy server started"));
                writer.Close();
            }
            Thread thread = new Thread(() => Start());
            thread.Start();
        }

        static void Start()
        {
            string blacklist = getList("C:\\Proxy\\list.conf");
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
                return "";
            }
            return blacklist;
        }

        protected override void OnStop()
        {
            string logPath = "C:\\Proxy\\log.txt";
            using (StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine(string.Format(DateTime.Now + ": Proxy server stoped"));
                writer.Close();
            }
        }
    }
}
