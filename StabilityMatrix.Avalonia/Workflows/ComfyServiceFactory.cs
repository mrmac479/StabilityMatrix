namespace StabilityMatrix.Avalonia.Workflows
{
    public sealed class ComfyServiceFactory
    {
        private static ImageRetrieval _instance = null;
        private static readonly object _padlock = new object();

        public static ImageRetrieval Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new ImageRetrieval();
                    }
                    return _instance;
                }
            }
        }
    }
}
