using System.Configuration;

namespace Commands.ComfyUiBackend.Workflows
{
    public class BaseWorkflow
    {
        protected static string _tempFilePath = ConfigurationManager.AppSettings["TempDirectory"];
    }
}
