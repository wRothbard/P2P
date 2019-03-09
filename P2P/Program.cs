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
            var port = StartServerThread();
            Console.WriteLine(port);
            if (args.Length > 0)
            {
                var peer = new HostPort(args[0]);
                peers.Add(peer);
                dynamic command = new { port = port };
                SendCommand(peer, command);
            }
        }

        private static dynamic SendCommand(HostPort peer, dynamic cmd)
        {
            var cmdJson = JsonConvert.SerializeObject(cmd);
            var resultsJson = SendCommand(peer, cmdJson);
            Console.WriteLine(resultsJson);
            return JsonConvert.DeserializeObject(resultsJson);
        }

        private static string SendCommand(HostPort peer, string cmdJson)
        {
            using (var tcpClient = peer.CreateTcpClient())
            using (var ns = tcpClient.GetStream())
            using (var sr = new StreamReader(ns))
            using (var sw = new StreamWriter(ns))
            {
                sw.WriteLine(cmdJson);
                sw.Flush();
                return sr.ReadToEnd();
            }
        }

        private static int StartServerThread()
        {
            var listener = new TcpListener(IPAddress.IPv6Any, 0);
            listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            listener.Start();
            var port = ((IPEndPoint)(listener.LocalEndpoint)).Port;
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
