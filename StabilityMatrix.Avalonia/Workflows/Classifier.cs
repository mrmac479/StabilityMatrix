using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using StabilityMatrix.Avalonia.ViewModels;
using StabilityMatrix.Core.Helper;

namespace StabilityMatrix.Avalonia.Workflows
{
    public class Classifier : BaseWorkflow
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static async Task<string> SendImageToClassifier(ComfyOrchastrationViewModel image)
        {
            string promptJson = WorkflowLoader.LoadWorkflow("d:/workflows/classifier_workflow_api.json");
            if (promptJson != null)
            {
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
                    prompt["2"]["inputs"]["image"] = image.UploadedFileName;

                    // segs filename
                    var filePart = image.FileName + "_segs_data";
                    prompt["23"]["inputs"]["file_name"] = filePart;

                    // scores filename
                    var filePart2 = image.FileName + "_scores";
                    prompt["24"]["inputs"]["file_name"] = filePart2;

                    // sensitivity
                    prompt["4"]["inputs"]["bbox_threshold"] = image.DetectionSensitivity;
                    prompt["4"]["inputs"]["sub_threshold"] = image.DetectionSensitivity;

                    var comfyService = ComfyServiceFactory.Instance;
                    Logger.Debug(prompt);
                    await comfyService.AwaitJobLive(
                        JsonConvert.SerializeObject(prompt),
                        "0",
                        null,
                        ComfyOrchastrationViewModel.GlobalProgramCancel,
                        true
                    );
                    image.ClassifierPrompt = prompt;
                    image.SegsData = filePart + ".txt";
                    image.GenderFile = filePart2 + ".txt";

                    image.SegmentsOverlap();
                    return filePart2 + ".txt";
                }
            }
            return null;
        }

        public static Tuple<bool, List<bool>> GetGender(ComfyOrchastrationViewModel image)
        {
            var genderList = new List<bool>();
            // This is a placeholder for the actual implementation
            Console.WriteLine($"Matching genders of images {image.ImageTaggerFile} and {image.GenderFile}.");

            // read input from file1 to string
            string file1Content = File.ReadAllText(_tempFilePath + image.ImageTaggerFile);

            var male = 0;
            var female = 0;

            if (file1Content.Contains("1boy"))
            {
                male++;
            }
            if (file1Content.Contains("1girl"))
            {
                female++;
            }
            if (file1Content.Contains("multiple_girls") || file1Content.Contains("2girls"))
            {
                female += 2;
            }
            if (file1Content.Contains("multiple_boys") || file1Content.Contains("2boys"))
            {
                male += 2;
            }

            var scoreFiles = Utilities.GetTagFiles(image.GenderFile);
            foreach (var item in scoreFiles)
            {
                // JSON-like string input
                string jsonString = File.ReadAllText(item);

                // Deserialize the JSON string to a list of GenderScore objects
                var genderScores = JsonConvert.DeserializeObject<List<GenderScore>>(jsonString);

                // Initialize variables to keep track of which gender has the highest score
                string highestGender = string.Empty;
                double highestScore = 0;
                var person = new Dictionary<string, double>();

                // Loop through each GenderScore object to find the highest score
                foreach (var genderScore in genderScores)
                {
                    person.Add(genderScore.Label, genderScore.Score);
                    if (
                        (
                            genderScore.Score > highestScore && genderScore.Label.ToLower() == "male"
                            || genderScore.Score > highestScore && genderScore.Label.ToLower() == "female"
                        )
                    )
                    {
                        highestScore = genderScore.Score;
                        highestGender = genderScore.Label;
                    }
                }

                genderList.Add(highestGender.ToLower() == "male");
                if (highestGender.ToLower() == "male")
                {
                    --male;
                }
                else
                {
                    --female;
                }
                image.GenderScores.Add(person);
            }

            if (male == 0 && female == 0)
                return new Tuple<bool, List<bool>>(true, genderList);
            return new Tuple<bool, List<bool>>(false, genderList);
        }

        public class GenderScore
        {
            public string Label { get; set; }
            public double Score { get; set; }
        }
    }
}
