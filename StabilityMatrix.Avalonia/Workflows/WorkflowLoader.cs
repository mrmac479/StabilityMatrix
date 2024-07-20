using System;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace StabilityMatrix.Avalonia.Workflows
{
    internal class WorkflowLoader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static string LoadWorkflow(string workflowPath)
        {
            Logger.Info($"Loading workflow from {workflowPath}.");
            try
            {
                string jsonContent = File.ReadAllText(workflowPath);
                var jsonData = JsonConvert.DeserializeObject<dynamic>(jsonContent); // Deserialize to check if JSON is valid
                return JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            }
            catch (FileNotFoundException)
            {
                Logger.Warn($"The file {workflowPath} was not found.");
                return null;
            }
            catch (JsonReaderException)
            {
                Logger.Warn($"The file {workflowPath} contains invalid JSON.");
                return null;
            }
        }
    }
}
