using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Commands.Helpers
{
    public enum CleanOptions
    {
        Segs,
        Tags,
        All
    }

    public static class Utilities
    {
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
            return path.Substring(
                path.LastIndexOf('\\') + 1,
                path.LastIndexOf('.') - path.LastIndexOf('\\') - 1
            );
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

        /// <summary>
        /// Get the width of an image
        /// </summary>
        /// <param name="imageFile"></param>
        /// <returns>image width</returns>
        public static int GetImageWidth(string imageFile)
        {
            using (var fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read))
            {
                using (var image = System.Drawing.Image.FromStream(fs, false, false))
                {
                    return image.Width;
                }
            }
        }
    }
}
