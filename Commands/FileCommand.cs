using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commands
{
    public class FileCommand
    {
        public static void GetGallery(string id, string name, string series = "")
        {
            var subPath = id.Substring(0, 2);
            string srcPath =
                $"\\\\DESKTOP-JEFF\\CornerClub\\ImageGallery\\CornerClub.WebUI\\g"
                + subPath
                + "\\"
                + id
                + "\\original";
            var destPath = $"D:\\Comics\\" + name;
            if (!string.IsNullOrEmpty(series))
            {
                destPath = $"D:\\Comics\\" + series + "\\" + name;
            }
            CopyDirectory(srcPath, destPath);
        }

        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Create the destination directory if it doesn't already exist.
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy them to the new location.
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true); // Set to true to overwrite existing files
            }

            // Copy all subdirectories by recursively calling this method.
            foreach (string subdirectory in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subdirectory));
                CopyDirectory(subdirectory, destSubDir);
            }
        }
    }
}
