using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Commands.ComfyUiBackend
{
    public class ReusableSocket
    {
        public ReusableSocket(string ID, ClientWebSocket Socket)
        {
            this.ID = ID;
            this.Socket = Socket;
        }

        public string ID { get; private set; }
        public ClientWebSocket Socket { get; private set; }
    }
}
