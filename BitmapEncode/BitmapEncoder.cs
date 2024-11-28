using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;

namespace BitmapEncode
{
    internal class BitmapEncoder<TBitmap>
        where TBitmap : IBitmap, new()
    {
        private int _levels;
        private string _inputPath;
        private string _outputPath;

        public BitmapEncoder(string inputPath, string outputPath, int levels = 0)
        {
            _inputPath = inputPath;
            _outputPath = outputPath;
            _levels = levels;
        }

        public string Encode(IBitmap bitmap)
        {
            var encoder = new BitmapText<TBitmap>(bitmap);
            return encoder.ToString();
        }

        public IBitmap Decode(BitmapText<TBitmap> bitmapText)
        {
            var text = bitmapText.ToString();
            var decoder = BitmapText<TBitmap>.FromString(text);

            return decoder.ToBitmap();
        }

        public double CalculateEntropy(IBitmap image)
        {
            var colorFrequency = new Dictionary<Color, int>();
            int totalPixels = image.Width * image.Height;

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color color = image.GetPixel(x, y);
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

        public double CalculateRedundancy(IBitmap image)
        {
            int colorDepth = (int)image.Depth/8;
            double entropy = CalculateEntropy(image);
            double maxEntropy = colorDepth;

            return 1.0 - (entropy / maxEntropy);
        }

        public double CalculateCompressionRate(long originalSize, long compressedSize)
        {
            double compressionRatePercentage = (1.0 - ((double)compressedSize / originalSize)) * 100;
            return compressionRatePercentage;
        }
    }
}
