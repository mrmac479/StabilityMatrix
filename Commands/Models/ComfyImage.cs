using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Commands.ComfyUiBackend.Workflows;
using Commands.Helpers;
using NLog;

namespace Commands.Models
{
    public class ComfyImage
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        string _tempFilePath = ConfigurationManager.AppSettings["TempDirectory"];
        public string FilePath { get; private set; }
        public string FileName
        {
            get { return Utilities.GetFileNameWithoutExtension(FilePath); }
        }
        public bool IsUploaded { get; set; }
        public string SegsFile { get; set; }
        List<string> _segsText;

        public double DetectionSensitivity { get; private set; }
        public double DetectionConfidence { get; set; }
        public double SegDetectionThreshold
        {
            get
            {
                if (IsOnePerson)
                {
                    return 0.35;
                }
                else if (SoftTouch)
                {
                    return 0.8;
                }
                else if (_isMatched)
                {
                    return 0.5;
                }
                return 0.7;
            }
        }

        public string FileNamePrefix { get; set; }
        public Dictionary<string, dynamic> ImageTaggerPrompt { get; internal set; }
        public string ImageTaggerFile { get; internal set; }
        string _imageTaggerText;
        private bool _isMatched;

        public string ImageTaggerText
        {
            get
            {
                if (string.IsNullOrEmpty(_imageTaggerText) && File.Exists(_tempFilePath + ImageTaggerFile))
                    _imageTaggerText = File.ReadAllText(_tempFilePath + ImageTaggerFile);
                return _imageTaggerText;
            }
        }
        public string UploadedFileName { get; internal set; }
        public Dictionary<string, dynamic> ClassifierPrompt { get; internal set; }
        public string GenderFile { get; internal set; }
        public Dictionary<string, dynamic> SegsPrompt { get; internal set; }
        public bool IsOnePerson
        {
            get
            {
                var genderFileCount = Utilities.GetTagFiles(GenderFile).Count;
                if (
                    genderFileCount != 1
                    || ImageTaggerText.Contains("2boys")
                    || ImageTaggerText.Contains("2girls")
                    || ImageTaggerText.Contains("multiple_girls")
                    || ImageTaggerText.Contains("multiple_boys")
                )
                    return false;
                DetailerDenoise = 0.9;
                DetailerCropFactor = 1.2;
                return true;
            }
        }
        public bool SoftTouch { get; private set; }
        public List<Dictionary<string, double>> GenderScores { get; internal set; }

        double _cropFactor = 0.0;

        public double DetailerDenoise { get; private set; }
        public double DetailerCropFactor { get; private set; }

        public double SegCropFactor
        {
            get
            {
                if (_cropFactor < 0.1)
                {
                    _cropFactor = 1.2;
                }
                return _cropFactor;
            }
        }

        public string SegsData { get; internal set; }

        int _scaleBy = 0;
        public int ScaleBy
        {
            get
            {
                if (_scaleBy == 0)
                {
                    // get image width of the image using FilePath
                    var imageWidth = Utilities.GetImageWidth(FilePath);
                    if (imageWidth > 0)
                    {
                        if (imageWidth > 2000)
                        {
                            _scaleBy = 1;
                        }
                        else
                        {
                            _scaleBy = 2;
                        }
                    }
                }
                return _scaleBy;
            }
        }

        public ComfyImage(string filePath, string prefix, bool softTouch = false)
        {
            FilePath = filePath;
            IsUploaded = false;
            FileNamePrefix = prefix;
            SoftTouch = softTouch;
            GenderScores = new List<Dictionary<string, double>>();
            _segsText = new List<string>();
            DetectionSensitivity = 0.7;
            if (softTouch)
            {
                DetailerDenoise = 0.3;
                DetailerCropFactor = 1.2;
            }
            else
            {
                DetailerDenoise = 0.9;
                DetailerCropFactor = 1.3;
            }
        }

        public bool SegmentsOverlap()
        {
            // soft touch already trigged no need to check
            if (SoftTouch)
            {
                return false;
            }
            List<int[]> cropRegions = new List<int[]>();
            string pattern = @"crop_region=\[(\d+), (\d+), (\d+), (\d+)\]";

            var input = string.Empty;
            if (File.Exists(_tempFilePath + SegsData))
            {
                input = File.ReadAllText(_tempFilePath + SegsData);
            }
            var matches = Regex.Matches(input, pattern);

            foreach (Match match in matches)
            {
                int[] region = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    region[i] = int.Parse(match.Groups[i + 1].Value);
                }
                cropRegions.Add(region);
            }

            // check if any of the regions overlap
            for (int i = 0; i < cropRegions.Count; i++)
            {
                for (int j = i + 1; j < cropRegions.Count; j++)
                {
                    if (
                        cropRegions[i][0] < cropRegions[j][2]
                        && cropRegions[i][2] > cropRegions[j][0]
                        && cropRegions[i][1] < cropRegions[j][3]
                        && cropRegions[i][3] > cropRegions[j][1]
                    )
                    {
                        // regions overlap in a way whre it will be difficult to get a good image
                        _cropFactor = 2.0;
                        DetailerDenoise = 0.5;
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task ProcessSegsFile()
        {
            if (!string.IsNullOrEmpty(SegsFile))
            {
                List<string> segFiles = Utilities.GetTagFiles(SegsFile);

                var genderMatch = Classifier.GetGender(this);
                _isMatched = ImageTagger.SubjectMatch(ImageTaggerFile, SegsFile) && genderMatch.Item1;
                // if segs match that run segmentor workflow at a lower threshold

                if (_isMatched)
                {
                    // if it was detected as one person, then it should have already been ran at a higher threshold
                    if (!IsOnePerson)
                    {
                        await ImageTagger.TagSegs(this);
                    }

                    // Appends the specified string to the file, creating the file if it does not already exist

                    for (int i = 0; i < segFiles.Count; i++)
                    {
                        var segFile = segFiles[i];
                        if (File.Exists(segFile))
                        {
                            var segText = File.ReadAllText(segFile);
                            segText = segText.Replace("1boy", "1boy, teen_boy");
                            File.WriteAllText(segFile, segText);
                            _segsText.Add(segText);
                        }
                    }
                }
                else
                {
                    // identify which gender is supposed to be in segment and remove the other

                    if (segFiles.Count == 1)
                    {
                        // if segText comes back as one unmatched gender use ImageTaggerText
                        var segText = ImageTaggerText;
                        _segsText.Add(segText);
                    }
                    else
                    {
                        // multiple people that couldn't be matched
                        DetailerDenoise = 0.65;
                        for (int i = 0; i < segFiles.Count; i++)
                        {
                            var segFile = segFiles[i];
                            var segText = File.ReadAllText(segFile);
                            if (genderMatch.Item2.Count > i)
                            {
                                if (genderMatch.Item2[i])
                                {
                                    segText = segText.Replace("2girls", "");
                                    segText = segText.Replace("1girl", "");
                                    segText = segText.Replace("multiple_girls", "");
                                    segText = segText.Replace("1boy", "1boy, teen_boy");
                                }
                                else
                                {
                                    segText = segText.Replace("2boys", "");
                                    segText = segText.Replace("1boy", "");
                                    segText = segText.Replace("multiple_boys", "");
                                }
                            }

                            while (segText.Contains(",,"))
                            {
                                segText = segText.Replace(",,", ",");
                            }
                            File.WriteAllText(segFile, segText);
                            _segsText.Add(segText);
                        }
                    }
                }
            }
        }

        public void PrintLog()
        {
            string filePath = _tempFilePath + FileName + "_log.txt";
            string textToAppend = $"Log for {FilePath}.\n";

            File.AppendAllText(filePath, textToAppend + Environment.NewLine);

            if (IsOnePerson)
            {
                File.AppendAllText(filePath, "One Person\n");
            }
            else
            {
                File.AppendAllText(filePath, "Multiple People\n");
            }

            File.AppendAllText(filePath, "Gender Info \n");
            if (_isMatched)
            {
                File.AppendAllText(filePath, "Matched\n");
            }
            else
            {
                File.AppendAllText(filePath, "Not Matched\n");
            }
            for (int i = 0; i < GenderScores.Count; i++)
            {
                File.AppendAllText(filePath, "Person " + i + Environment.NewLine);
                foreach (var item in GenderScores[i])
                {
                    File.AppendAllText(filePath, item.Key + " : " + item.Value + Environment.NewLine);
                }
            }
            File.AppendAllText(filePath, "-------------------------------------------------------- \n");
            File.AppendAllText(filePath, "Image Tagger Text \n");
            File.AppendAllText(filePath, ImageTaggerText + Environment.NewLine);

            File.AppendAllText(filePath, "-------------------------------------------------------- \n");
            // Appending more lines
            File.AppendAllText(filePath, "Segs Text\n");

            File.AppendAllText(
                filePath,
                "Segs Overlap : " + SegmentsOverlap().ToString() + Environment.NewLine
            );
            foreach (var text in _segsText)
            {
                File.AppendAllText(filePath, text + Environment.NewLine);
            }

            File.AppendAllText(filePath, "-------------------------------------------------------- \n");
            File.AppendAllText(filePath, "Run Properties \n");
            File.AppendAllText(filePath, "Soft Touch : " + SoftTouch + Environment.NewLine);
            File.AppendAllText(
                filePath,
                "Detection Sensitivity : " + DetectionSensitivity + Environment.NewLine
            );
            File.AppendAllText(
                filePath,
                "Detection Confidence : " + DetectionConfidence + Environment.NewLine
            );
            File.AppendAllText(
                filePath,
                "Seg Detection Threshold : " + SegDetectionThreshold + Environment.NewLine
            );
            File.AppendAllText(filePath, "Seg Crop Factor : " + SegCropFactor + Environment.NewLine);
            File.AppendAllText(filePath, "Detailer Denoise : " + DetailerDenoise + Environment.NewLine);
            File.AppendAllText(
                filePath,
                "Detailer Crop Factor : " + DetailerCropFactor + Environment.NewLine
            );
            File.AppendAllText(filePath, "Scale By : " + ScaleBy + Environment.NewLine);
        }
    }
}
