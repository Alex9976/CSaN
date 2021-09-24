using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace proxy_server
{
    class Proxy
    {
        private const int BACKLOG = 100;
        private const int BUFFER_LENGTH = 20 * 1024;
        private const int ERROR_BUFFER_LENGTH = 512;
        private int port;
        private string host;
        private Socket listener;
        private string[] blackList;
        private byte[] buffer;
        private byte[] errorPage;

        public Proxy(string host, int port, string blacklist)
        {
            this.host = host;
            this.port = port;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Parse(this.host), port));
            blackList = blacklist.Trim().Split(new char[] { '\r', '\n' });
            errorPage = getErrorPage();
        }

        private byte[] getErrorPage()
        {
            string head = $"HTTP/1.1 403 Forbidden\r\nContent-Type: text/html\r\nContent-Length: {ERROR_BUFFER_LENGTH}\r\n\r\n";
            byte[] headBuffer = Encoding.UTF8.GetBytes(head);
            byte[] page = new byte[ERROR_BUFFER_LENGTH];
            byte[] result = new byte[ERROR_BUFFER_LENGTH + head.Length];
            using (FileStream reader = new FileStream("C:\\Proxy\\error_page.html", FileMode.OpenOrCreate))
            {
                reader.Read(page, 0, ERROR_BUFFER_LENGTH);
            }
            Buffer.BlockCopy(headBuffer, 0, result, 0, head.Length);
            Buffer.BlockCopy(page, 0, result, head.Length, ERROR_BUFFER_LENGTH);
            return result;
        }

        public void Listen()
        {
            listener.Listen(BACKLOG);
        }

        public Socket Accept()
        {
            return listener.Accept();
        }

        public void ReceiveData(Socket client)
        {
            NetworkStream stream = new NetworkStream(client);
            buffer = new byte[BUFFER_LENGTH];
            byte[] messageBuffer = new byte[BUFFER_LENGTH];
            int numberOfBytesRead, bufferSize = 0;

            do
            {
                numberOfBytesRead = stream.Read(messageBuffer, 0, buffer.Length);
                Buffer.BlockCopy(messageBuffer, 0, buffer, bufferSize, numberOfBytesRead);
                bufferSize += numberOfBytesRead;
            }
            while (stream.DataAvailable);

            HTTPResponser(buffer, stream);
            client.Dispose();
        }

        public void HTTPResponser(byte[] buffer, NetworkStream browserStream)
        {
            Socket server;
            string responseRecord;
            string responseCode;
            try
            {
                buffer = ConvertToRelPath(buffer);

                string[] temp = Encoding.UTF8.GetString(buffer).Trim().Split(new char[] { '\r', '\n' });

                string request = temp.FirstOrDefault(x => x.Contains("Host"));
                request = request.Substring(request.IndexOf(":") + 2);

                string[] hostAndPort = request.Trim().Split(new char[] { ':' });

                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPHostEntry iPHostEntry = Dns.GetHostEntry(hostAndPort[0]);

                if (hostAndPort.Length == 2)
                {
                    server.Connect(new IPEndPoint(iPHostEntry.AddressList[0], int.Parse(hostAndPort[1])));
                }
                else
                {
                    server.Connect(new IPEndPoint(iPHostEntry.AddressList[0], 80));
                }
                NetworkStream serverStream = new NetworkStream(server);
                if (blackList != null && Array.IndexOf(blackList, request.ToLower()) != -1)
                {
                    browserStream.Write(errorPage, 0, errorPage.Length);
                    responseCode = "403";
                    responseRecord = DateTime.Now + ": " + request + " " + responseCode + " (blocked)";
                    using (StreamWriter writer = new StreamWriter("C:\\Proxy\\log.txt", true))
                    {
                        writer.WriteLine(string.Format(responseRecord));
                        writer.Close();
                    }
                    return;
                }


                serverStream.Write(buffer, 0, buffer.Length);

                var bufResponse = new byte[BUFFER_LENGTH];
                byte[] messageBuffer = new byte[BUFFER_LENGTH];
                int numberOfBytesRead, bufferSize = 0;
                do
                {
                    numberOfBytesRead = serverStream.Read(messageBuffer, 0, 32);
                    Buffer.BlockCopy(messageBuffer, 0, bufResponse, bufferSize, numberOfBytesRead);
                    bufferSize += numberOfBytesRead;
                }
                while (serverStream.DataAvailable && bufferSize <= 32);

                browserStream.Write(bufResponse, 0, bufferSize);

                string[] head = Encoding.UTF8.GetString(bufResponse).Split(new char[] { '\r', '\n' });

                responseCode = head[0].Substring(head[0].IndexOf(" ") + 1);
                responseRecord = DateTime.Now + ": " + request + " " + responseCode;
                using (StreamWriter writer = new StreamWriter("C:\\Proxy\\log.txt", true))
                {
                    writer.WriteLine(string.Format(responseRecord));
                    writer.Close();
                }
                serverStream.CopyTo(browserStream);
            }
            catch
            {
                return;
            }
        }

        private byte[] ConvertToRelPath(byte[] buf)
        {
            string buffer = Encoding.UTF8.GetString(buf);
            Regex regex = new Regex(@"http:\/\/[a-z0-9а-яё\:\.]*");
            MatchCollection matches = regex.Matches(buffer);
            string host = matches[0].Value;
            buffer = buffer.Replace(host, "");
            buf = Encoding.UTF8.GetBytes(buffer);
            return buf;
        }
    }
}
