using System.Configuration;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StabilityMatrix.Core.Helper;

public static partial class Utilities
{
    public static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version == null
            ? "(Unknown)"
            : $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    public static void CopyDirectory(
        string sourceDir,
        string destinationDir,
        bool recursive,
        bool includeReparsePoints = false
    )
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = includeReparsePoints
            ? dir.GetDirectories()
            : dir.GetDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.ReparsePoint));

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            if (file.FullName == targetFilePath)
                continue;
            file.CopyTo(targetFilePath, true);
        }

        if (!recursive)
            return;

        // If recursive and copying subdirectories, recursively call this method
        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }

    public static MemoryStream? GetMemoryStreamFromFile(string filePath)
    {
        var fileBytes = File.ReadAllBytes(filePath);
        var stream = new MemoryStream(fileBytes);
        stream.Position = 0;

        return stream;
    }

    public static string RemoveHtml(string? stringWithHtml)
    {
        var pruned =
            stringWithHtml
                ?.Replace("<br/>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("<br />", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("</p>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("</h1>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("</h2>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("</h3>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("</h4>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("</h5>", $"{Environment.NewLine}{Environment.NewLine}")
                .Replace("</h6>", $"{Environment.NewLine}{Environment.NewLine}") ?? string.Empty;
        pruned = HtmlRegex().Replace(pruned, string.Empty);
        return pruned;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlRegex();

    public static string GetFileName(string path)
    {
        return path.Substring(path.LastIndexOf('\\') + 1);
    }

    public static string GetExtension(string path)
    {
        return path.Substring(path.LastIndexOf('.') + 1);
    }

    public static string GetFileNameWithoutExtension(string path)
    {
        if (path.Contains('/'))
        {
            path = path.Replace('\\', '/');
            return path.Substring(
                path.LastIndexOf('/') + 1,
                path.LastIndexOf('.') - path.LastIndexOf('/') - 1
            );
        }
        return path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1);
    }

    public static string SegsFileToString(string segsFile, string prefix)
    {
        string tempFilePath = ConfigurationManager.AppSettings["TempDirectory"];
        while (!File.Exists(tempFilePath + segsFile))
        {
            // wait one second for file to be created
            Task.Delay(1000).Wait();

            // if file is not created after 10 seconds, return empty string
            if (!File.Exists(tempFilePath + segsFile))
            {
                return string.Empty;
            }
        }

        StringBuilder sb = new StringBuilder(prefix + "," + File.ReadAllText(tempFilePath + segsFile));

        var i = 1;
        var fileName = Path.GetFileNameWithoutExtension(tempFilePath + segsFile);
        while (File.Exists(tempFilePath + fileName + "_" + i.ToString() + ".txt"))
        {
            sb.Append(
                prefix.Replace("[ASC]", "[SEP]")
                    + File.ReadAllText(tempFilePath + fileName + "_" + i.ToString() + ".txt")
            );
            i++;
        }

        return FilterPrompts(sb.ToString());
    }

    public static string FilterPrompts(string prompt)
    {
        string[] removeWords =
        {
            "web_address",
            "artist_name",
            "jewelry",
            "cigarette",
            "blurry",
            "dark-skinned_female",
            "blood_on_clothes",
            "blood",
            "dark_skinned_female",
            "dark_skin",
            "comic",
            "korean_text",
            "english_text",
            "thought_bubble",
            "speech_bubble"
        };
        return prompt.RemoveText(removeWords);
    }

    // Extension method to remove a specified substring from a string
    public static string RemoveText(this string source, string textToRemove)
    {
        if (source == null)
            return source;

        return source.Replace(textToRemove, string.Empty);
    }

    // Extension method to remove an array of substrings from a string
    public static string RemoveText(this string source, string[] textToRemove)
    {
        if (source == null)
            return source;

        foreach (var text in textToRemove)
        {
            source = source.Replace(text, string.Empty);
        }
        return source;
    }

    public static List<string> GetTagFiles(string tagFile)
    {
        string tempFilePath = ConfigurationManager.AppSettings["TempDirectory"];
        var files = new List<string>();
        if (File.Exists(tempFilePath + tagFile))
        {
            files.Add(tempFilePath + tagFile);
        }

        var i = 1;
        var fileName = Path.GetFileNameWithoutExtension(tempFilePath + tagFile);
        while (File.Exists(tempFilePath + fileName + "_" + i.ToString() + ".txt"))
        {
            files.Add(tempFilePath + fileName + "_" + i.ToString() + ".txt");
            ++i;
        }
        return files;
    }
}
