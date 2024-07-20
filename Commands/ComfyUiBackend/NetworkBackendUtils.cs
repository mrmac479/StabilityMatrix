using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Commands.ComfyUiBackend
{
    public static class NetworkBackendUtils
    {
        public static async Task<JObject> PostJSONString(
            this HttpClient client,
            string route,
            string input,
            CancellationToken interrupt
        )
        {
            return await NetworkBackendUtils.Parse<JObject>(
                await client.PostAsync(
                    route,
                    new StringContent(input, Encoding.UTF8, "application/json"),
                    interrupt
                )
            );
        }

        /// <summary>Receive raw binary data from a WebSocket.</summary>
        public static async Task<byte[]> ReceiveData(
            this WebSocket socket,
            int maxBytes,
            CancellationToken limit
        )
        {
            byte[] buffer = new byte[8192];
            using (MemoryStream ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), limit);
                    ms.Write(buffer, 0, result.Count);
                    if (ms.Length > maxBytes)
                    {
                        throw new IOException($"Received too much data! (over {maxBytes} bytes)");
                    }
                } while (!result.EndOfMessage);
                return ms.ToArray();
            }
        }

        /// <summary>Connects a client websocket to the backend.</summary>
        /// <param name="path">The path to connect on, after the '/', such as 'ws?clientId={uuid}'.</param>
        public static async Task<ClientWebSocket> ConnectWebsocket(string address, string path)
        {
            ClientWebSocket outSocket = new ClientWebSocket();
            outSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            string scheme = "ws";
            await outSocket.ConnectAsync(
                new Uri($"{scheme}://{address}/{path}"),
                Program.GlobalProgramCancel
            );
            return outSocket;
        }

        /// <summary>Create and preconfigure a basic <see cref="HttpClient"/> instance to make web requests with.</summary>
        public static HttpClient MakeHttpClient()
        {
            var handler = new HttpClientHandler
            {
                // Customize settings as needed:
                //PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                //PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 10,
                AllowAutoRedirect = true,
                AutomaticDecompression =
                    System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            HttpClient client = new HttpClient(handler);
            //client.DefaultRequestHeaders.UserAgent.ParseAdd($"SwarmUI/{Utilities.Version}");
            client.Timeout = TimeSpan.FromMinutes(10);
            return client;
        }

        /// <summary>Parses an <see cref="HttpResponseMessage"/> into a JSON object result.</summary>
        /// <exception cref="InvalidOperationException">Thrown when the server returns invalid data (error code or other non-JSON).</exception>
        /// <exception cref="NotImplementedException">Thrown when an invalid JSON type is requested.</exception>
        public static async Task<JType> Parse<JType>(HttpResponseMessage message)
            where JType : class
        {
            string content = await message.Content.ReadAsStringAsync();
            if (content.StartsWith("500 Internal Server Error"))
            {
                throw new InvalidOperationException(
                    $"Server turned 500 Internal Server Error, something went wrong: {content}"
                );
            }
            try
            {
                switch (typeof(JType))
                {
                    case Type t when t == typeof(JObject):
                        return JObject.Parse(content) as JType;
                    case Type t when t == typeof(JArray):
                        return JArray.Parse(content) as JType;
                    case Type t when t == typeof(string):
                        return content as JType;
                    default:
                        throw new NotImplementedException($"Invalid JSON type requested: {typeof(JType)}");
                }
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to read JSON '{content}' with message: {ex.Message}"
                );
            }
        }
    }
}
