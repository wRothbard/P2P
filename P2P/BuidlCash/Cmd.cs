using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P.BuidlCash
{
    public class Cmd : Dictionary<string, object>
    {
        public static string GETPEERS = "getpeers";
        public static string SENDPEERS = "sendpeers";

        private static string CMD = "cmd";
        private static string PORT = "port";

        public Cmd(int port, string cmd)
        {
            Add(PORT, port);
            Add(CMD, cmd);
        }
    }
}
