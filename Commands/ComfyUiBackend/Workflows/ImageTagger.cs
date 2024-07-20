using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Commands.Helpers;
using Commands.Models;
using Newtonsoft.Json;
using NLog;

namespace Commands.ComfyUiBackend.Workflows
{
    public class ImageTagger : BaseWorkflow
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static async Task<string> TagImages(ComfyImage image)
        {
            // This is a placeholder for the actual implementation
            Logger.Info($"Tagging image {image.FilePath}.");

            string promptJson = WorkflowLoader.LoadWorkflow("d:/workflows/image_tagger_workflow_api.json");
            if (promptJson != null)
            {
                Logger.Debug(promptJson);

                var prompt = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(promptJson);

                if (!string.IsNullOrEmpty(image.FilePath) && File.Exists(image.FilePath))
                {
                    if (image.IsUploaded == false)
                    {
                        image.UploadedFileName = await ImageRetrieval.UploadFileAsync(
                            image.FilePath,
                            "gallery",
                            true
                        );
                        image.IsUploaded = true;
                    }
                    prompt["4"]["inputs"]["image"] = image.UploadedFileName;

                    // threshold
                    prompt["5"]["inputs"]["threshold"] = 0.8;

                    // filename
                    var filePart = image.FileName + "_image_tagger";
                    prompt["6"]["inputs"]["file_name"] = filePart;

                    image.ImageTaggerPrompt = prompt;
                    var comfyService = ComfyServiceFactory.Instance;
                    await comfyService.AwaitJobLive(
                        JsonConvert.SerializeObject(prompt),
                        "0",
                        null,
                        Program.GlobalProgramCancel,
                        false
                    );
                    image.ImageTaggerFile = filePart + ".txt";
                    return filePart + ".txt";
                }
            }
            return null;
        }

        public static async Task<string> TagSegs(ComfyImage image)
        {
            CleanUp(image, CleanOptions.Segs);
            Console.WriteLine($"Tagging image {image.FilePath}.");

            string promptJson = WorkflowLoader.LoadWorkflow("d:/workflows/segs_tagger_workflow_api.json");
            if (promptJson != null)
            {
                Logger.Debug(promptJson);

                var prompt = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(promptJson);

                if (!string.IsNullOrEmpty(image.FilePath) && File.Exists(image.FilePath))
                {
                    if (image.IsUploaded == false)
                    {
                        image.UploadedFileName = await ImageRetrieval.UploadFileAsync(
                            image.FilePath,
                            "gallery",
                            true
                        );
                        image.IsUploaded = true;
                    }
                    prompt["4"]["inputs"]["image"] = image.UploadedFileName;

                    // crop factor
                    prompt["10"]["inputs"]["threshold"] = image.SegCropFactor;

                    // filename
                    var filePart = image.FileName + "_segs_tagger";
                    prompt["6"]["inputs"]["file_name"] = filePart;

                    // sensitivity
                    prompt["8"]["inputs"]["bbox_threshold"] = image.DetectionSensitivity;
                    prompt["8"]["inputs"]["sub_threshold"] = image.DetectionSensitivity;

                    prompt["10"]["inputs"]["threshold"] = image.SegDetectionThreshold;

                    var comfyService = ComfyServiceFactory.Instance;
                    await comfyService.AwaitJobLive(
                        JsonConvert.SerializeObject(prompt),
                        "0",
                        null,
                        Program.GlobalProgramCancel,
                        true
                    );
                    image.SegsFile = filePart + ".txt";
                    image.SegsPrompt = prompt;
                    return filePart + ".txt";
                }
            }
            return null;
        }

        public static bool SubjectMatch(string file1, string file2)
        {
            // This is a placeholder for the actual implementation
            Console.WriteLine($"Matching subjects of images {file1} and {file2}.");

            // read input from file1 to string
            string file1Content = File.ReadAllText(_tempFilePath + file1);

            var file2Content = Utilities.SegsFileToString(file2, "");
            foreach (var item in file2Content.Split(','))
            {
                if (
                    item == "1girl"
                    || item == "2girls"
                    || item == "3girls"
                    || item == "1boy"
                    || item == "2boys"
                    || item == "3boys"
                    || item == "multiple_girls"
                    || item == "multiple_boys"
                )
                {
                    if (!file1Content.Contains(item))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static void CleanUp(ComfyImage image, CleanOptions option)
        {
            var segsFile = Utilities.GetTagFiles(image.SegsFile);
            switch (option)
            {
                case CleanOptions.Segs:

                    foreach (var segFile in segsFile)
                    {
                        File.Delete(segFile);
                    }
                    break;
                case CleanOptions.All:
                    File.Delete(_tempFilePath + image.ImageTaggerFile);
                    File.Delete(_tempFilePath + image.GenderFile.Replace("scores", "segs_data"));
                    var genderFiles = Utilities.GetTagFiles(image.GenderFile);

                    foreach (var segFile in segsFile)
                    {
                        File.Delete(segFile);
                    }

                    foreach (var genderFile in genderFiles)
                    {
                        File.Delete(genderFile);
                    }
                    break;
            }
        }
    }
}
