using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Commands.ComfyUiBackend
{
    public class WebSocketConnection
    {
        public static async Task<ClientWebSocket> OpenWebSocketConnectionAsync(string clientId)
        {
            string serverAddress = "127.0.0.1:8188";
            string connectionString = $"ws://{serverAddress}/ws?clientId={clientId}";

            ClientWebSocket ws = new ClientWebSocket();
            try
            {
                // Connect to the WebSocket server
                await ws.ConnectAsync(new Uri(connectionString), CancellationToken.None);
                Console.WriteLine($"Connected to WebSocket server at {connectionString}");

                // Return the WebSocket connection
                return ws;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to WebSocket server: {ex.Message}");
                return null; // Return null if the connection fails
            }
        }

        public static async Task CloseWebSocketConnectionAsync(ClientWebSocket ws)
        {
            try
            {
                // Close the WebSocket connection
                await ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing connection",
                    CancellationToken.None
                );
                Console.WriteLine("Closed WebSocket connection");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing WebSocket connection: {ex.Message}");
            }
        }
    }
}
