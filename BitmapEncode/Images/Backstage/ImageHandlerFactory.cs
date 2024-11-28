using BitmapEncode.Images.Handlers;
using BitmapEncode.Images.Interfaces;

namespace BitmapEncode.Images.Backstage
{
    public static class ImageHandlerFactory
    {
        public static IImageHandler GetHandler(string extension)
        {
            return extension switch
            {
                ".ico" => new IcoImageHandler(),
                ".bmp" => new BmpImageHandler(),
                ".png" => new PngImageHandler(),
                _ => throw new NotSupportedException($"Unsupported file format: {extension}")
            };
        }
    }
}
