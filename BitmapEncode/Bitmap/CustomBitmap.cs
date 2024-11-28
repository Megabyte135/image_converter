using BitmapEncode.Images;
using BitmapEncode.Images.Backstage;
using BitmapEncode.Images.Interfaces;

namespace BitmapEncode.BitmapVariants
{
    public class CustomBitmap : IBitmap
    {
        private Color[,] _pixels;

        public int Width { get; set; }
        public int Height { get; set; }
        public PixelFormat Depth { get; set; }

        public CustomBitmap(int width, int height, PixelFormat depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
            _pixels = new Color[width, height];
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException("Pixel coordinates are out of bounds.");
            _pixels[x, y] = color;
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException("Pixel coordinates are out of bounds.");
            return _pixels[x, y];
        }

        public IBitmap FromImage(string imagePath)
        {
            string extension = Path.GetExtension(imagePath).ToLower();
            IImageHandler loader = ImageHandlerFactory.GetHandler(extension);
            var d = loader.Load(imagePath, Depth);
            _pixels = d.GetPixels();
            return d;
        }

        public void ToImage(string outputPath)
        {
            string extension = Path.GetExtension(outputPath).ToLower();
            IImageHandler saver = ImageHandlerFactory.GetHandler(extension);
            saver.Save(this, outputPath);
        }

        public Color[,] GetPixels()
        {
            return _pixels;
        }
    }
}
