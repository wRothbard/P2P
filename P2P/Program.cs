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

        private static void SendCommand(HostPort peer, dynamic cmd)
        {
            var cmdJson = JsonConvert.SerializeObject(cmd);
            SendCommand(peer, cmdJson);
        }

        private static void SendCommand(HostPort peer, string cmdJson)
        {
            byte[] cmdBytes = Encoding.UTF8.GetBytes(cmdJson);
            using (var udpClient = peer.CreateUdpClient())
            {
                udpClient.Send(cmdBytes, cmdBytes.Length);
            }
        }

        private static int StartServerThread()
        {
            var serverIpEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);

            var listener = new UdpClient(serverIpEndpoint);  // XXX this needs to get disposed when Program is disposed, or owned and disposed by some other disposable object
            //listener.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            var port = ((IPEndPoint)(listener.Client.LocalEndPoint)).Port;
            new Thread(() => {
                while (serverRunning)
                {
                    var remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receiveBytes = listener.Receive(ref remoteIpEndPoint);
                    string cmdJson = Encoding.UTF8.GetString(receiveBytes);
                    Console.WriteLine(cmdJson);
                    dynamic cmd = JsonConvert.DeserializeObject(cmdJson);
                    int peerPort = Convert.ToInt32(cmd["port"]);
                    if (remoteIpEndPoint is System.Net.IPEndPoint)
                    {
                        peers.Add(new HostPort((IPEndPoint)remoteIpEndPoint, peerPort));
                    }
                    // XXX handle the received command here
                }
            }).Start();
            return port;
        }
    }
}
