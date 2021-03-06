﻿using Newtonsoft.Json;
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
        private static int port;
        private static ISet<HostPort> peers = new HashSet<HostPort>();
        private static bool serverRunning = true;

        static void Main(string[] args)
        {
            port = StartServerThread();
            Console.WriteLine(port);
            if (args.Length > 0)
            {
                var peer = new HostPort(args[0]);
                peers.Add(peer);
                Cmd command = new Cmd(port, Cmd.GETPEERS);
                SendCommand(peer, command);
            }
        }

        private static void SendCommand(HostPort peer, Cmd cmd)
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
                        var newpeer = new HostPort((IPEndPoint)remoteIpEndPoint, peerPort);
                        AddPeer(newpeer);
                        var cmdName = cmd["cmd"];
                        Cmd response = null;
                        if (cmdName == Cmd.GETPEERS)
                        {
                            response = new Cmd(port, Cmd.SENDPEERS);
                            response["peers"] = peers;
                        }
                        else if (cmdName == Cmd.SENDPEERS)
                        {
                            LoadPeers(cmd);
                        }
                        else if (cmdName == Cmd.ADDPEER)
                        {
                            var p = cmd["peer"];
                            var hostname = (string)p["Hostname"];
                            var prt = Convert.ToInt32(p["Port"]);
                            AddPeer(new HostPort(hostname, prt));
                        }
                        else
                        {
                            //response = new Cmd(port, "XXX");
                            //response["errorMsg"] = "Unrecognized command";
                        }
                        if (response != null)
                            SendCommand(newpeer, response);
                        // XXX handle the received command here
                    }
                    // XXX or handle the received command here
                }
            }).Start();
            return port;
        }

        private static void AddPeer(HostPort newpeer)
        {
            lock (peers)
            {
                if (!peers.Contains(newpeer))
                {
                    peers.Add(newpeer);
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        lock (peers)
                        {
                            foreach (var peer in peers)
                            {
                                Cmd cmd = new Cmd(port, Cmd.ADDPEER);
                                cmd.Add("peer", newpeer);
                                SendCommand(peer, cmd);
                            }
                        }
                    });
                }
            }
        }

        private static void LoadPeers(dynamic cmd)
        {
            lock (peers)
            {
                foreach (var peer in cmd["peers"])
                {
                    peers.Add(new HostPort((string)peer["Hostname"], Convert.ToInt32(peer["Port"])));
                }
            }
        }
    }
}
