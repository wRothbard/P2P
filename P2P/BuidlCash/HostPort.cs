using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2P.BuidlCash
{
    public class HostPort
    {
        public string Hostname
        {
            get
            {
                if (ipEndPoint == null)
                    return null;
                return ipEndPoint.Address.ToString();
            }
        }

        public int Port
        {
            get
            {
                if (ipEndPoint == null)
                    return 0;
                return ipEndPoint.Port;
            }
        }

        private IPEndPoint ipEndPoint = null;

        public HostPort(string endpoint, int defaultPort = 0)
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
            {
                ipEndPoint = new IPEndPoint(address, port);
                return;
            }
            throw new FormatException("Unable to obtain host and port from [" + endpoint + "]");
        }

        public HostPort(IPEndPoint ipEndPoint, int port)
        {
            this.ipEndPoint = new IPEndPoint(ipEndPoint.Address, port);
        }

        public UdpClient CreateUdpClient()
        {
            return new UdpClient(Hostname, Port);
        }

        public override bool Equals(System.Object obj)
        {
            if (obj == null)
                return false;

            HostPort p = obj as HostPort;
            return Equals(p);
        }

        public bool Equals(HostPort p)
        {
            if ((System.Object)p == null)
                return false;

            return (Hostname == p.Hostname) && (Port == p.Port);
        }

        public override int GetHashCode()
        {
            return Hostname.GetHashCode() ^ Port.GetHashCode();
        }

        public static bool operator ==(HostPort a, HostPort b)
        {
            if (System.Object.ReferenceEquals(a, b))
                return true;

            if ((object)a == null || (object)b == null)
                return false;

            return a.Hostname == b.Hostname && a.Port == b.Port;
        }

        public static bool operator !=(HostPort a, HostPort b)
        {
            return !(a == b);
        }
    }
}
