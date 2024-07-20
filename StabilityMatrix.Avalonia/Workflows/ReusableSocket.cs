using System.Net.WebSockets;

namespace StabilityMatrix.Avalonia.Workflows
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
