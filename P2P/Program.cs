using Newtonsoft.Json;
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
        private static ISet<IPEndPoint> peers = new HashSet<IPEndPoint>();
        private static bool serverRunning = true;

        static void Main(string[] args)
        {
            var port = Convert.ToInt32(args[0]);
            var peer = ParseIPEndPoint(args[1]);
            peers.Add(peer);
            StartServerThread(port);
            dynamic command = new { port = port };
            SendCommand(peer, command);
        }

        private static dynamic SendCommand(IPEndPoint peer, dynamic cmd)
        {
            using (var tcpClient = new TcpClient(peer.Address.ToString(), peer.Port))
            using (var ns = tcpClient.GetStream())
            using (var sr = new StreamReader(ns))
            using (var sw = new StreamWriter(ns))
            {
                sw.WriteLine(JsonConvert.SerializeObject(cmd));
                sw.Flush();
                return JsonConvert.DeserializeObject(sr.ReadToEnd());
            }
        }

        private static IPEndPoint ParseIPEndPoint(string endpoint, int defaultPort = 0)
        {
            Uri uri;
            var success = Uri.TryCreate(endpoint, UriKind.Absolute, out uri);
            if (success)
                if (uri.Host.Length < 1)
                    success = false;
            if (!success)
                success = Uri.TryCreate(String.Concat("tcp://", endpoint), UriKind.Absolute, out uri);
            if (!success)
                success = Uri.TryCreate(String.Concat("tcp://", String.Concat("[", endpoint, "]")), UriKind.Absolute, out uri);
            var host = uri.Host;
            var address = Dns.GetHostAddresses(host)[0];
            var port = uri.Port;
            if (port <= IPEndPoint.MinPort)
                port = defaultPort;
            if (success)
                return new IPEndPoint(address, port);
            throw new FormatException("Unable to obtain host and port from [" + endpoint + "]");
        }
        private static void StartServerThread(int port)
        {
            var listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            listener.Start();
            new Thread(
            () =>
            {
                while (serverRunning)
                {
                    var client = listener.AcceptTcpClient();
                    var e = client.Client.RemoteEndPoint;
                    IPAddress peerIpAddress = null;
                    if (e is System.Net.IPEndPoint)
                    {
                        peerIpAddress = ((IPEndPoint)e).Address;
                    }
                    new Thread(
                        () =>
                        {
                            using (var ns = client.GetStream())
                            using (var sr = new StreamReader(ns))
                            using (var sw = new StreamWriter(ns))
                            {
                                dynamic cmd = JsonConvert.DeserializeObject(sr.ReadLine());
                                int peerPort = Convert.ToInt32(cmd["port"]);
                                if (peerIpAddress != null)
                                    peers.Add(new IPEndPoint(peerIpAddress, peerPort));
                                sw.WriteLine(JsonConvert.SerializeObject(peers));
                            }
                        }).Start();
                }
            }).Start();
        }
    }
}
