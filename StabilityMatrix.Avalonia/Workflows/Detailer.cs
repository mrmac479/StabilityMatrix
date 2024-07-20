using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using StabilityMatrix.Avalonia.ViewModels;
using StabilityMatrix.Core.Helper;

namespace StabilityMatrix.Avalonia.Workflows
{
    public class Detailer : BaseWorkflow
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static async Task SendImageToDetailer(ComfyOrchastrationViewModel image)
        {
            string promptJson = WorkflowLoader.LoadWorkflow("d:/workflows/detailer_workflow_api.json");
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
                    prompt["4"]["inputs"]["image"] = image.UploadedFileName;

                    if (!string.IsNullOrEmpty(image.Prefix))
                        prompt["31"]["inputs"]["filename_prefix"] = image.Prefix;

                    // detailer body
                    prompt["26"]["inputs"]["wildcard"] = Utilities.SegsFileToString(
                        image.SegsFile,
                        "[ASC] high quality, 4k resolution, realistic, "
                    );
                    prompt["26"]["inputs"]["denoise"] = image.DetailerDenoise;

                    // detailer face
                    prompt["30"]["inputs"]["denoise"] = 0.8;
                    prompt["24"]["inputs"]["bbox_threshold"] = 0.5;
                    prompt["24"]["inputs"]["sub_threshold"] = 0.5;

                    // crop body factor
                    prompt["8"]["inputs"]["crop_factor"] = image.DetailerCropFactor;

                    // model merge ratio
                    var ratio = 0.7;
                    prompt["17"]["inputs"]["ratio"] = ratio;
                    prompt["18"]["inputs"]["ratio"] = ratio;

                    // scale image
                    prompt["32"]["inputs"]["scale_by"] = image.ScaleBy;

                    if (image.SoftTouch)
                    {
                        prompt["26"]["inputs"]["denoise"] = 0.3;
                        prompt["30"]["inputs"]["denoise"] = 0.3;
                    }

                    Logger.Debug(prompt);
                    var comfyService = ComfyServiceFactory.Instance;
                    Logger.Info("Queue Detailer Workflow");
                    await comfyService.AwaitJobLive(
                        JsonConvert.SerializeObject(prompt),
                        "0",
                        null,
                        ComfyOrchastrationViewModel.GlobalProgramCancel,
                        false
                    );
                }
            }
        }
    }
}
