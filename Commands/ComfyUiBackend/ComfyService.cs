using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Commands.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Commands.ComfyUiBackend
{
    public class ImageRetrieval
    {
        /// <summary>Internal HTTP handler.</summary>
        public static HttpClient HttpClient = NetworkBackendUtils.MakeHttpClient();
        private static readonly HttpClient client = new HttpClient();
        private static string serverAddress = "127.0.0.1:8188";
        private static string address = "http://127.0.0.1:8188";
        private static string clientId = Guid.NewGuid().ToString();
        private static Random random = new Random();
        public ConcurrentQueue<ReusableSocket> ReusableSockets = new ConcurrentQueue<ReusableSocket>();
        public bool CanIdle = true;

        public static async Task<string> UploadFileAsync(
            string filePath,
            string subfolder = "",
            bool overwrite = false
        )
        {
            string url = $"http://{serverAddress}/upload/image";
            try
            {
                using (var content = new MultipartFormDataContent())
                {
                    // Add the file content to the multipart form data
                    var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                        "application/octet-stream"
                    );
                    content.Add(fileContent, "image", Path.GetFileName(filePath));

                    // Add additional data parameters as needed
                    if (overwrite)
                        content.Add(new StringContent("true"), "overwrite");
                    if (!string.IsNullOrEmpty(subfolder))
                        content.Add(new StringContent(subfolder), "subfolder");

                    // Send the request
                    var response = await client.PostAsync(url, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseData);
                        string path = data.name;
                        if (data.subfolder != null && data.subfolder != "")
                            path = $"{data.subfolder}/{path}";

                        return path;
                    }
                    else
                    {
                        Console.WriteLine($"{response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>Runs a job with live feedback (progress updates, previews, etc.)</summary>
        /// <param name="workflow">The workflow JSON to use.</param>
        /// <param name="batchId">Local batch-ID for this generation.</param>
        /// <param name="takeOutput">Takes an output object: Image for final images, JObject for anything else.</param>
        /// <param name="user_input">Original user input data.</param>
        /// <param name="interrupt">Interrupt token to use.</param>
        /// <param name="listen">Whether to listen for updates.</param>
        public async Task AwaitJobLive(
            string workflow,
            string batchId,
            Action<object> takeOutput,
            CancellationToken interrupt,
            bool listen
        )
        {
            if (interrupt.IsCancellationRequested)
            {
                return;
            }
            Console.WriteLine("Will await a job, do parse...");
            JObject workflowJson = JsonConvert.DeserializeObject<JObject>(workflow);
            Console.WriteLine("JSON parsed.");
            int expectedNodes = workflowJson.Count;
            string id = null;
            ClientWebSocket socket = null;
            try
            {
                while (ReusableSockets.TryDequeue(out ReusableSocket oldSocket))
                {
                    if (oldSocket.Socket.State == WebSocketState.Open)
                    {
                        Console.WriteLine("Reuse existing websocket");
                        id = oldSocket.ID;
                        socket = oldSocket.Socket;
                        break;
                    }
                    else
                    {
                        oldSocket.Socket.Dispose();
                    }
                }
                if (socket is null)
                {
                    Console.WriteLine("Need to connect a websocket...");
                    id = Guid.NewGuid().ToString();
                    socket = await NetworkBackendUtils.ConnectWebsocket(serverAddress, $"ws?clientId={id}");
                    Console.WriteLine("Connected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Websocket comfy connection failed: {ex}");
                throw;
            }

            int nodesDone = 0;
            float curPercent = 0;
            void yieldProgressUpdate()
            {
                if (takeOutput != null)
                {
                    takeOutput(
                        new JObject()
                        {
                            ["batch_index"] = batchId,
                            ["overall_percent"] = nodesDone / (float)expectedNodes,
                            ["current_percent"] = curPercent
                        }
                    );
                }
            }
            try
            {
                workflow = $"{{\"prompt\": {workflow}, \"client_id\": \"{id}\"}}";
                JObject promptResult = await HttpClient.PostJSONString(
                    $"{address}/prompt",
                    workflow,
                    interrupt
                );
                if (promptResult.ContainsKey("error"))
                {
                    throw new InvalidDataException($"ComfyUI errored: {promptResult}");
                }

                if (listen)
                {
                    string promptId = $"{promptResult["prompt_id"]}";
                    long firstStep = 0;
                    bool hasInterrupted = false;
                    bool isReceivingOutputs = false;
                    bool isExpectingVideo = false;
                    string currentNode = "";
                    bool isMe = false;
                    while (true)
                    {
                        if (interrupt.IsCancellationRequested && !hasInterrupted)
                        {
                            hasInterrupted = true;
                            Console.WriteLine("ComfyUI Interrupt requested");
                            await HttpClient.PostAsync(
                                $"{address}/interrupt",
                                new StringContent(""),
                                Program.GlobalProgramCancel
                            );
                        }
                        byte[] output = await socket.ReceiveData(
                            100 * 1024 * 1024,
                            Program.GlobalProgramCancel
                        );
                        if (output != null)
                        {
                            if (Encoding.ASCII.GetString(output, 0, 8) == "{\"type\":")
                            {
                                JObject json = JsonConvert.DeserializeObject<JObject>(
                                    Encoding.UTF8.GetString(output)
                                );
                                string type = $"{json["type"]}";
                                if (!isMe)
                                {
                                    if (type == "execution_start")
                                    {
                                        if ($"{json["data"]["prompt_id"]}" == promptId)
                                        {
                                            isMe = true;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                switch (type)
                                {
                                    case "executing":
                                        string nodeId = $"{json["data"]["node"]}";
                                        if (nodeId == "") // Not true null for some reason, so, ... this.
                                        {
                                            goto endloop;
                                        }
                                        currentNode = nodeId;
                                        goto case "execution_cached";
                                    case "execution_cached":
                                        nodesDone++;
                                        curPercent = 0;
                                        hasInterrupted = false;
                                        yieldProgressUpdate();
                                        break;
                                    case "progress":
                                        int max = json["data"].Value<int>("max");
                                        curPercent = json["data"].Value<float>("value") / max;
                                        isReceivingOutputs = max == 12345 || max == 12346;
                                        isExpectingVideo = max == 12346;
                                        yieldProgressUpdate();
                                        break;
                                    case "executed":
                                        nodesDone = expectedNodes;
                                        curPercent = 0;
                                        yieldProgressUpdate();
                                        break;
                                    case "execution_start":
                                        if (firstStep == 0)
                                        {
                                            firstStep = Environment.TickCount;
                                        }
                                        break;
                                    case "status": // queuing
                                        break;
                                    default:
                                        Console.WriteLine($"Ignore type {json["type"]}");
                                        break;
                                }
                            }
                            else
                            {
                                (string formatLabel, int index, int eventId) =
                                    ComfyRawWebsocketOutputToFormatLabel(output);
                                //Console.WriteLine($"ComfyUI Websocket sent: {output.Length} bytes of image data as event {eventId} in format {formatLabel} to index {index}");
                                if (isReceivingOutputs)
                                {
                                    Image.ImageType type = ComfyFormatLabelToImageType(formatLabel);
                                    if (isExpectingVideo && type == Image.ImageType.IMAGE)
                                    {
                                        type = Image.ImageType.VIDEO;
                                    }
                                    bool isReal = true;
                                    if (
                                        currentNode != null
                                        && int.TryParse(currentNode, out int nodeIdNum)
                                        && ((nodeIdNum < 100 && nodeIdNum != 9) || nodeIdNum >= 50000)
                                    )
                                    {
                                        // Reserved nodes that aren't the final output are intermediate outputs, or nodes in the 50,000+ range.
                                        isReal = false;
                                    }
                                    //takeOutput(new T2IEngine.ImageOutput() { Img = new Image(output[8..], type, formatLabel), IsReal = isReal, GenTimeMS = firstStep == 0 ? -1 : (Environment.TickCount64 - firstStep) });
                                }
                                else
                                {
                                    string dataType = "";
                                    switch (formatLabel)
                                    {
                                        case "jpg":
                                            dataType = "image/jpeg";
                                            break;
                                        case "png":
                                            dataType = "image/png";
                                            break;
                                        case "webp":
                                            dataType = "image/webp";
                                            break;
                                        case "gif":
                                            dataType = "image/gif";
                                            break;
                                        case "mp4":
                                            dataType = "video/mp4";
                                            break;
                                        case "webm":
                                            dataType = "video/webm";
                                            break;
                                        default:
                                            dataType = "image/jpeg";
                                            break;
                                    }

                                    string batchIndex = batchId;
                                    int batchInt = 0;
                                    if (index == 0 || int.TryParse(batchId, out batchInt))
                                    {
                                        batchIndex = (batchInt + index).ToString();
                                    }
                                    if (takeOutput != null)
                                    {
                                        takeOutput(
                                            new JObject()
                                            {
                                                ["batch_index"] = batchIndex,
                                                ["preview"] =
                                                    $"data:{dataType};base64,"
                                                    + Convert.ToBase64String(output, 8, output.Length - 8),
                                                ["overall_percent"] = nodesDone / (float)expectedNodes,
                                                ["current_percent"] = curPercent
                                            }
                                        );
                                    }
                                }
                            }
                        }
                        if (socket.CloseStatus.HasValue)
                        {
                            return;
                        }
                    }
                    endloop:
                    if (takeOutput != null)
                    {
                        JObject historyOut = await SendGet<JObject>($"history/{promptId}");
                        if (historyOut.Properties().Any())
                        {
                            foreach (
                                Image image in await GetAllImagesForHistory(historyOut[promptId], interrupt)
                            )
                            {
                                takeOutput(
                                    new ImageOutput()
                                    {
                                        Img = image,
                                        IsReal = true,
                                        GenTimeMS = firstStep == 0 ? -1 : (Environment.TickCount - firstStep)
                                    }
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                ReusableSockets.Enqueue(new ReusableSocket(id, socket));
            }
        }

        public static Image.ImageType ComfyFormatLabelToImageType(string formatLabel)
        {
            switch (formatLabel)
            {
                case "jpg":
                    return Image.ImageType.IMAGE;
                case "png":
                    return Image.ImageType.IMAGE;
                case "webp":
                    return Image.ImageType.IMAGE;
                case "gif":
                    return Image.ImageType.ANIMATION;
                case "mp4":
                    return Image.ImageType.VIDEO;
                case "webm":
                    return Image.ImageType.VIDEO;
                case "mov":
                    return Image.ImageType.VIDEO;
                default:
                    return Image.ImageType.IMAGE;
            }
        }

        public static (string, int, int) ComfyRawWebsocketOutputToFormatLabel(byte[] output)
        {
            int eventId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(output, 0));
            int format = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(output, 4));
            int index = 0;
            if (format > 2)
            {
                index = (format >> 4) & 0xffff;
                format &= 7;
            }
            string formatLabel = "";
            switch (format)
            {
                case 1:
                    formatLabel = "jpg";
                    break;
                case 2:
                    formatLabel = "png";
                    break;
                case 3:
                    formatLabel = "webp";
                    break;
                case 4:
                    formatLabel = "gif";
                    break;
                case 5:
                    formatLabel = "mp4";
                    break;
                case 6:
                    formatLabel = "webm";
                    break;
                case 7:
                    formatLabel = "mov";
                    break;
                case 8:
                    formatLabel = "jpeg";
                    break;
                default:
                    formatLabel = "jpg";
                    break;
            }
            return (formatLabel, index, eventId);
        }

        public Task<JType> SendGet<JType>(string url)
            where JType : class
        {
            return SendGet<JType>(url, Program.GlobalProgramCancel);
        }

        public async Task<JType> SendGet<JType>(string url, CancellationToken token)
            where JType : class
        {
            return await NetworkBackendUtils.Parse<JType>(
                await HttpClient.GetAsync($"{address}/{url}", token)
            );
        }

        private async Task<Image[]> GetAllImagesForHistory(JToken output, CancellationToken interrupt)
        {
            if (
                (output as JObject).TryGetValue("status", out JToken status)
                && (status as JObject).TryGetValue("messages", out JToken messages)
            )
            {
                foreach (JToken msg in messages)
                {
                    if (
                        msg[0].ToString() == "execution_error"
                        && (msg[1] as JObject).TryGetValue("exception_message", out JToken actualMessage)
                    )
                    {
                        throw new InvalidOperationException($"ComfyUI execution error: {actualMessage}");
                    }
                }
            }
            List<Image> outputs = new List<Image>();
            List<string> outputFailures = new List<string>();
            foreach (JToken outData in output["outputs"].Values())
            {
                if (outData is null)
                {
                    outputFailures.Add($"Null output block (???)");
                    continue;
                }
                async Task LoadImage(JObject outImage, Image.ImageType type)
                {
                    string imType = "output";
                    string fname = outImage["filename"].ToString();
                    if ($"{outImage["type"]}" == "temp")
                    {
                        imType = "temp";
                    }
                    string ext = fname.Substring(fname.LastIndexOf(".") + 1);
                    string format =
                        (outImage.TryGetValue("format", out JToken formatTok) ? formatTok.ToString() : "")
                        ?? "";
                    if (ext == "gif")
                    {
                        type = Image.ImageType.ANIMATION;
                    }
                    else if (ext == "mp4" || ext == "mov" || ext == "webm" || format.StartsWith("video/"))
                    {
                        type = Image.ImageType.VIDEO;
                    }
                    byte[] image = await (
                        await HttpClient.GetAsync(
                            $"{address}/view?filename={HttpUtility.UrlEncode(fname)}&type={imType}",
                            interrupt
                        )
                    ).Content.ReadAsByteArrayAsync();
                    if (image == null || image.Length == 0)
                    {
                        Console.WriteLine(
                            $"Invalid/null/empty image data from ComfyUI server for '{fname}', under"
                        );
                        return;
                    }
                    outputs.Add(new Image(image, type, ext));
                    PostResultCallback(fname);
                }
                if (outData["images"] != null)
                {
                    foreach (JToken outImage in outData["images"])
                    {
                        await LoadImage(outImage as JObject, Image.ImageType.IMAGE);
                    }
                }
                else if (outData["gifs"] != null)
                {
                    foreach (JToken outGif in outData["gifs"])
                    {
                        await LoadImage(outGif as JObject, Image.ImageType.ANIMATION);
                    }
                }
                else
                {
                    outputFailures.Add($"Invalid/empty output block");
                }
            }
            if (!output.Any())
            {
                if (outputFailures.Any())
                {
                    Console.WriteLine(
                        $"Comfy backend gave no valid output, but did give unrecognized outputs (enable Debug logs for more details)"
                    );
                }
                else
                {
                    Console.WriteLine($"Comfy backend gave no valid output");
                }
            }
            return outputs.ToArray();
        }

        public void PostResultCallback(string filename)
        {
            string path =
                "D:\\StabilityMatrix\\StabilityMatrix.Avalonia\\bin\\Debug\\net8.0\\Data\\Packages\\ComfyUI\\output/output/{filename}";
            Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            });
        }

        #region NotUsed

        public static void PromptToImage(
            string workflowJson,
            string positivePrompt,
            string negativePrompt = "",
            bool savePreviews = false
        )
        {
            // Parse the JSON string into a JObject
            var prompt = JObject.Parse(workflowJson);

            // Extract id_to_class_type mapping
            var idToClassType = new Dictionary<string, string>();
            foreach (var pair in prompt)
            {
                idToClassType[pair.Key] = pair.Value["class_type"].ToString();
            }

            // Find the first KSampler class type
            var kSampler = idToClassType.FirstOrDefault(x => x.Value == "KSampler").Key;

            // Set a random seed
            long seed =
                (long)random.NextDouble() * (long)(Math.Pow(10, 15) - Math.Pow(10, 14))
                + (long)Math.Pow(10, 14);
            prompt[kSampler]["inputs"]["seed"] = seed;

            // Set the positive prompt
            var positiveInputId = prompt[kSampler]["inputs"]["positive"][0].ToString();
            prompt[positiveInputId]["inputs"]["text"] = positivePrompt;

            // Optionally set the negative prompt
            if (!string.IsNullOrEmpty(negativePrompt))
            {
                var negativeInputId = prompt[kSampler]["inputs"]["negative"][0].ToString();
                prompt[negativeInputId]["inputs"]["text"] = negativePrompt;
            }

            // Serialize the updated prompt
            var updatedPromptJson = prompt.ToString(Formatting.Indented);

            // Call the method to generate the image by prompt
            //GenerateImageByPrompt(updatedPromptJson, "./output/", savePreviews);
        }

        #endregion
    }
}
