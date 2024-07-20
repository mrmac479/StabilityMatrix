using System.Configuration;

namespace StabilityMatrix.Avalonia.Workflows
{
    public class BaseWorkflow
    {
        protected static string _tempFilePath = ConfigurationManager.AppSettings["TempDirectory"];
    }
}
