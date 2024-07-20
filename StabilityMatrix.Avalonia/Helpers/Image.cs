using System;

namespace StabilityMatrix.Avalonia.Helpers
{
    public class Image
    {
        /// <summary>The raw binary data.</summary>
        public byte[] ImageData;

        /// <summary>The type of image data this image holds.</summary>
        public ImageType Type;

        /// <summary>File extension for this image.</summary>
        public string Extension;

        public Image(byte[] data, ImageType type, string extension)
        {
            Extension = extension;
            Type = type;
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            else if (data.Length == 0)
            {
                throw new ArgumentException("Data is empty!", nameof(data));
            }
            ImageData = data;
        }

        public enum ImageType
        {
            IMAGE = 0,

            /// <summary>ie animated gif</summary>
            ANIMATION = 1,
            VIDEO = 2
        }
    }
}
