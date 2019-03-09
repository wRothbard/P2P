using Newtonsoft.Json;
using P2P.BuidlCash;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2P
{
    class Program
    {
        private static ISet<HostPort> peers = new HashSet<HostPort>();
        private static bool serverRunning = true;

        static void Main(string[] args)
        {
            var port = 0;
            if (args.Length > 0)
                port = Convert.ToInt32(args[0]);
            port = StartServerThread(port);
            Console.WriteLine(port);
            if (args.Length > 1)
            {
                var peer = new HostPort(args[1]);
                peers.Add(peer);
                dynamic command = new { port = port };
                SendCommand(peer, command);
            }
        }

        private static dynamic SendCommand(HostPort peer, dynamic cmd)
        {
            using (var tcpClient = peer.CreateTcpClient())
            using (var ns = tcpClient.GetStream())
            using (var sr = new StreamReader(ns))
            using (var sw = new StreamWriter(ns))
            {
                sw.WriteLine(JsonConvert.SerializeObject(cmd));
                sw.Flush();
                return JsonConvert.DeserializeObject(sr.ReadToEnd());
            }
        }

        private static int StartServerThread(int port)
        {
            var listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            listener.Start();
            port = ((IPEndPoint)(listener.LocalEndpoint)).Port;
            new Thread(
            () =>
            {
                while (serverRunning)
                {
                    var client = listener.AcceptTcpClient();
                    var remoteEndPoint = client.Client.RemoteEndPoint;
                    new Thread(
                        () =>
                        {
                            using (var ns = client.GetStream())
                            using (var sr = new StreamReader(ns))
                            using (var sw = new StreamWriter(ns))
                            {
                                dynamic cmd = JsonConvert.DeserializeObject(sr.ReadLine());
                                int peerPort = Convert.ToInt32(cmd["port"]);
                                if (remoteEndPoint is System.Net.IPEndPoint)
                                    peers.Add(new HostPort((IPEndPoint)remoteEndPoint, peerPort));
                                sw.WriteLine(JsonConvert.SerializeObject(peers));
                            }
                        }).Start();
                }
            }).Start();
            return port;
        }
    }
}
