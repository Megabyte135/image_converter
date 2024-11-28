using BitmapEncode.BitmapVariants;

namespace BitmapEncode
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string inputPath = "C:\\Users\\Yemeth\\Desktop\\images\\LIBRARIAN.png";
            string outputPath = "C:\\Users\\Yemeth\\Desktop\\images\\Flopping.bmp";
            
            CustomBitmap customBitmap = new(inputPath, Images.PixelFormat.OneBit);

            customBitmap.ToImage(outputPath);

            BitmapMeta<CustomBitmap> meta = new(inputPath, outputPath, customBitmap);
            Console.WriteLine(meta.OriginalSize);
            Console.WriteLine(meta.CompressedSize);
            Console.WriteLine(meta.CalculateEntropy());
            Console.WriteLine(meta.CalculateRedundancy());
            Console.WriteLine(meta.CalculateCompressionRate());
        }
    }
}
