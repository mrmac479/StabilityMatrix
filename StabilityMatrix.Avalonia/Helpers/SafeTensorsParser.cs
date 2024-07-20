using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StabilityMatrix.Core.Models;

namespace StabilityMatrix.Avalonia.Helpers
{
    public class SafeTensorsParser
    {
        public static void Parse(string filePath)
        {
            Dictionary<string, string> metadata = ParseSafeTensorsMetadata(filePath, 2);

            foreach (var kvp in metadata)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        public static SafeTensorsInfo? GetSafeTensorsInfo(string json)
        {
            return SafeTensorsInfo.FromJson(json);
        }

        public static Dictionary<string, string> ParseSafeTensorsMetadata(
            string filePath,
            int numberOfPairs = 2
        )
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Read the length of the metadata section
                int metadataLength = reader.ReadInt32();

                // Read the metadata section as a byte array
                byte[] metadataBytes = reader.ReadBytes(metadataLength);

                // Convert the byte array to a string
                string metadataString = Encoding.UTF8.GetString(metadataBytes);

                // Split the metadata string into lines
                string[] lines = metadataString.Split(
                    new[] { '\n', '\r' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                int count = 0;
                foreach (string line in lines)
                {
                    if (count >= numberOfPairs)
                    {
                        break;
                    }

                    // Find the first occurrence of ':' to handle values that might contain ':'
                    int separatorIndex = line.IndexOf(':');
                    if (separatorIndex != -1)
                    {
                        string key = line.Substring(0, separatorIndex).Trim();
                        string value = line.Substring(separatorIndex + 1).Trim();

                        int cnt = 0;
                        for (int i = 0; i < value.Length; i++)
                        {
                            if (value[i] == '{')
                            {
                                cnt++;
                            }
                            else if (value[i] == '}')
                            {
                                cnt--;
                                if (cnt == 0)
                                {
                                    value = value.Substring(0, i + 1);
                                    break;
                                }
                            }
                        }

                        metadata[key] = value;
                        // Convert value to JSON string
                        //string jsonValue = JsonConvert.SerializeObject(value);
                        //metadata[key] = jsonValue;

                        count++;
                    }
                }
            }

            return metadata;
        }

        internal static ConnectedModelInfo? ParseSafeTensorsMetadataAndReturnCMData(string filePath)
        {
            // get file extension
            var extension = Path.GetExtension(filePath);
            if (extension == ".pth" || extension == ".pt" || extension == ".bin" || extension == ".ckpt")
            {
                return null;
            }
            var json = ParseSafeTensorsMetadata(filePath);
            var safeTensors = GetSafeTensorsInfo(json.Values.First());
            var info = new ConnectedModelInfo
            {
                ImportedAt = DateTime.Now,
                BaseModel = safeTensors?.SsSdModelName,
                ModelName = Path.GetFileNameWithoutExtension(filePath),
                TrainedWords = ExtractWordsInQuotes(safeTensors?.SsTagFrequency ?? string.Empty),
                ModelDescription = safeTensors?.ModelspecDescription ?? string.Empty,
                Tags = ExtractWordsInQuotes(safeTensors?.ModelspecTags ?? string.Empty)
            };

            return info;
        }

        public static string[] ExtractWordsInQuotes(string input)
        {
            // Define a regular expression to find text within double quotes
            Regex regex = new Regex("\"([^\"]*)\"");
            MatchCollection matches = regex.Matches(input);

            List<string> results = new List<string>();

            // Iterate through all matches and add the captured values to the list
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    results.Add(match.Groups[1].Value);
                }
            }

            // Convert the list to an array and return it
            return results.ToArray();
        }
    }
}
