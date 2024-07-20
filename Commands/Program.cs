using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commands.ComfyUiBackend;
using Commands.ComfyUiBackend.Workflows;
using Commands.Data;
using Commands.Helpers;
using Commands.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NLog;

namespace Commands
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly CancellationTokenSource GlobalCancelSource = new CancellationTokenSource();

        /// <summary>If this is signalled, the program is cancelled.</summary>
        public static CancellationToken GlobalProgramCancel = GlobalCancelSource.Token;

        public static void Main(string[] args)
        {
            // log args to debug
            Logger.Debug($"Args: {string.Join(", ", args)}");

            if (true)
            {
                // Build a configuration object from command line
                IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();

                if (config["Cmd"] != null && config["Cmd"].ToLower() == "copygallery")
                {
                    // Check if the required parameters are present
                    if (config["Id"] != null || config["Name"] != null)
                    {
                        FileCommand.GetGallery(config["Id"], config["Name"]);
                    }
                }
                else if (config["Cmd"] != null && config["Cmd"].ToLower() == "copyseries")
                {
                    if (config["Series"] != null)
                    {
                        CopySeries(config["Series"]);
                    }
                }
                else if (config["Cmd"] != null && config["Cmd"].ToLower() == "callcomfy")
                {
                    if (config["Directory"] == null && !Directory.Exists(config["Directory"]))
                    {
                        Console.WriteLine("Please provide a valid directory path.");
                    }
                    else
                    {
                        if (int.TryParse(config["SkipFiles"], out int skipFiles))
                        {
                            CallComfy(config["Directory"], config["Name"], skipFiles).Wait();
                        }
                        else
                        {
                            CallComfy(config["Directory"], config["Name"]).Wait();
                        }
                    }
                }
            }
            else
            {
                ComfyOrchestration(
                        "D:\\Comics\\Timmy Strikes Back\\Timmy Strikes Back 2\\b87cd999-3d23-4662-ab0b-0a8d08bc3367_18.jpg",
                        "TSB2"
                    )
                    .Wait();
            }

            // Ensure to flush and close down NLog when the application exits
            LogManager.Shutdown();
        }

        static async Task ComfyOrchestration(string inputFile, string outputPrefix)
        {
            Logger.Info($"Start Comfy Orchestration");
            var image = new ComfyImage(inputFile, outputPrefix, false);
            image.ImageTaggerFile = await ImageTagger.TagImages(image);
            image.GenderFile = await Classifier.SendImageToClassifier(image);
            image.SegsFile = await ImageTagger.TagSegs(image);
            image.ProcessSegsFile().Wait();

            // TODO: Implement logic that will pull segs text and pass it to detailer. Maybe I should anaylze the text first
            // add facial hair to negative prompts and remove if only if a seg detects it,
            // maybe something similar with teens
            // if gender detect is higher than seg dect, then I have people overlapping

            // udpate save file to return the file name so I can log it


            await Detailer.SendImageToDetailer(image);
            image.PrintLog();
            // clean up
            ImageTagger.CleanUp(image, CleanOptions.All);
        }

        static void CopySeries(string series)
        {
            var galleries = Queries.GetGalleriesBySeries(series);

            foreach (var gallery in galleries)
            {
                FileCommand.GetGallery(gallery.GalleryId.ToString(), gallery.Name, series);
                Console.WriteLine($"GalleryId: {gallery.GalleryId}, Name: {gallery.Name}");
            }
        }

        public static async Task CallComfy(string directory, string name, int skipFiles = 0)
        {
            // Check if it's a file
            if (File.Exists(directory))
            {
                await ComfyOrchestration(directory, name);
            }
            else if (Directory.Exists(directory))
            {
                SortFiles(directory, out string[] files);
                foreach (string item in files.Skip(skipFiles))
                {
                    await ComfyOrchestration(item, name);
                }
            }
        }

        private static void SortFiles(string directory, out string[] files)
        {
            files = Directory.GetFiles(directory);
            if (files.Length == 0)
            {
                Console.WriteLine("No files found in the directory.");
                return;
            }

            Array.Sort(files, new NaturalStringComparer());
        }
    }
}
