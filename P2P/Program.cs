using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2P
{
    class Program
    {
        static ISet<IPEndPoint> peers = new HashSet<IPEndPoint>();

        static void Main(string[] args)
        {
            var port = Convert.ToInt32(args[0]);
            var peer = ParseIPEndPoint(args[1]);
            peers.Add(peer);
            Connect(peer);
        }

        private static void Connect(IPEndPoint peer)
        {
            using (var tcpClient = new TcpClient(peer.Address.ToString(), peer.Port))
            using (var ns = tcpClient.GetStream())
            using (var sr = new StreamReader(ns))
            using (var sw = new StreamWriter(ns))
            {
                //sw.WriteLine(JsonConvert.SerializeObject(cmd));
                sw.WriteLine("Ax");
                sw.Flush();
                //return sr.ReadToEnd();
                sr.ReadToEnd();
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
    }
}
