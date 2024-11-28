using BitmapEncode.Images;

namespace BitmapEncode.BitmapVariants
{
    public interface IBitmap
    {
        int Width { get; set; }
        int Height { get; set; }
        PixelFormat Depth { get; set; }
        void SetPixel(int x, int y, Color color);
        Color GetPixel(int x, int y);
        Color[,] GetPixels();
        IBitmap FromImage(string imagePath);
        void ToImage(string outputPath);
    }
}
