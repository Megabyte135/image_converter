using BitmapEncode.Images;
using BitmapEncode.Images.Backstage;
using BitmapEncode.Images.Interfaces;
using System.Drawing;
using Color = BitmapEncode.Images.Color;

namespace BitmapEncode.BitmapVariants
{
    public class CustomBitmap : IBitmap
    {
        private Color[,] _pixels;

        public int Width { get; set; }
        public int Height { get; set; }
        public PixelFormat Depth { get; set; }

        public CustomBitmap()
        {
            
        }

        public CustomBitmap(string inputPath, PixelFormat depth)
        {
            Image image = Image.FromFile(inputPath);
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            Width = image.Width;
            Height = image.Height;
            Depth = depth;
            _pixels = new Color[Width, Height];
            FromImage(inputPath);
        }

        public CustomBitmap(int width, int height, PixelFormat depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
            _pixels = new Color[Width, Height];
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

        private IBitmap FromImage(string imagePath)
        {
            string extension = Path.GetExtension(imagePath).ToLower();
            IImageHandler loader = ImageHandlerFactory.GetHandler(extension);
            IBitmap bitmap = loader.Load(imagePath, Depth);
            _pixels = bitmap.GetPixels();
            return bitmap;
        }

        public void ToImage(string outputPath)
        {
            string extension = Path.GetExtension(outputPath).ToLower();
            if (extension != ".bmp")
            {
                throw new ArgumentException("The output path must have a .bmp extension.");
            }

            BmpImageHandler saver = new BmpImageHandler();
            saver.Save(this, outputPath);
        }

        public Color[,] GetPixels()
        {
            return _pixels;
        }
    }
}
