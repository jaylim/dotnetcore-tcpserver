using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace tcpserver
{
    class Program
    {
        static void Main(string[] args)
        {
            Int32 port = 3030;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            TcpListener server  = new TcpListener(localAddr, port);

            server.Start();
            Console.WriteLine($"Server Started at {localAddr.ToString()}:{port}");

            while (true) {
                Task<TcpClient> tClient = server.AcceptTcpClientAsync();
                if (tClient.Result != null) {
                    Task.Run(() => OnConnection(tClient.Result));
                }
            }
        }

        public static void OnConnection(TcpClient client)
        {
            try {
                IPEndPoint endpoint = (IPEndPoint) client.Client.RemoteEndPoint;
                string remoteAddr   = $"{endpoint.Address.ToString()}:{endpoint.Port}";

                Console.WriteLine($"[{remoteAddr}] New Connection.");

                NetworkStream stream = client.GetStream();
                Task.Run(() => SendHeartBeat(remoteAddr, client));
                Task.Run(() => WaitingCommand(remoteAddr, client));

                // byte[] buffer = new byte[1024];
                // client.GetStream().Read(buffer, 0, buffer.Length);
                // Console.Write($"{remoteAddr} reply: {Encoding.ASCII.GetString(buffer)}");
            } catch (Exception e) {
                Console.WriteLine($"ERROR {e.Message}");
            }
        }

        public static void SendHeartBeat(string remoteAddr, TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            try {
                while (client.Connected) {
                    byte[] data = Encoding.ASCII.GetBytes("BEAT\n");
                    client.GetStream().Write(data, 0, data.Length);

                    Thread.Sleep(5000);
                }
            } catch (Exception e) {
                Console.WriteLine($"[{remoteAddr}] ERROR[{e.GetType().Name}] {e.Message}");
            }
            Console.WriteLine($"[{remoteAddr}] Clean up.");
            // clean up
            client.Close();
        }

        public static void WaitingCommand(string remoteAddr, TcpClient client)
        {
            byte[] buffer = new byte[1024];
            NetworkStream stream = client.GetStream();
            try {
                while (stream.Read(buffer, 0, buffer.Length) > 0) {
                    int idx    = Array.IndexOf(buffer, (byte) '\n');
                    string msg = Encoding.ASCII.GetString(buffer, 0, idx);
                    if (msg.Equals("HELO")) {
                        Console.WriteLine($"[{remoteAddr}] GOT {msg}. Replying EHLO");
                        byte[] data = Encoding.ASCII.GetBytes("EHLO\n");
                        stream.Write(data, 0, data.Length);
                    }
                }
                Console.WriteLine($"[{remoteAddr}] Connection close");
                // clean up
                client.Close();
            } catch (Exception e) {
                Console.WriteLine($"[{remoteAddr}] ERROR-CMD[{e.GetType().Name}] {e.Message}");
            }
        }
    }
}
