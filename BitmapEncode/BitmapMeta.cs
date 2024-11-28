using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using System.Drawing;
using Color = BitmapEncode.Images.Color;

namespace BitmapEncode
{
    public class BitmapMeta<TBitmap>
        where TBitmap : IBitmap, new()
    {
        public long OriginalSize;
        public long CompressedSize;
        private IBitmap _bitmap;

        public BitmapMeta(string inputPath, string outputPath, IBitmap bitmap)
        {
            OriginalSize = new FileInfo(inputPath).Length;
            CompressedSize = new FileInfo(outputPath).Length;
            _bitmap = new CustomBitmap(outputPath, bitmap.Depth);
        }

        public double CalculateEntropy()
        {
            var colorFrequency = new Dictionary<Color, int>();
            int totalPixels = _bitmap.Width * _bitmap.Height;

            for (int y = 0; y < _bitmap.Height; y++)
            {
                for (int x = 0; x < _bitmap.Width; x++)
                {
                    Color color = _bitmap.GetPixel(x, y);
                    if (colorFrequency.ContainsKey(color))
                        colorFrequency[color]++;
                    else
                        colorFrequency[color] = 1;
                }
            }

            double entropy = 0.0;

            foreach (var kvp in colorFrequency)
            {
                double probability = (double)kvp.Value / totalPixels;
                entropy -= probability * Math.Log(probability, 2);
            }

            return entropy;
        }

        public double CalculateRedundancy()
        {
            int colorDepth = (int)_bitmap.Depth;
            double entropy = CalculateEntropy();
            double maxEntropy = colorDepth;

            return 1.0 - (entropy / maxEntropy);
        }

        public double CalculateCompressionRate()
        {
            double compressionRatePercentage = (1.0 - ((double)CompressedSize / OriginalSize)) * 100;
            return compressionRatePercentage;
        }
    }
}
