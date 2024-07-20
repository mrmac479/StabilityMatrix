using System;

namespace StabilityMatrix.Avalonia.Helpers
{
    public class ImageOutput
    {
        /// <summary>The generated image.</summary>
        public Image Img;

        /// <summary>The time in milliseconds it took to generate, or -1 if unknown.</summary>
        public long GenTimeMS = -1;

        /// <summary>If true, the image is a real final output. If false, there is something non-standard about this image (eg it's a secondary preview) and so should be excluded from grids/etc.</summary>
        public bool IsReal = true;

        /// <summary>An action that will remove/discard this image as relevant.</summary>
        public Action RefuseImage;
    }
}
