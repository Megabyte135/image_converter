using BitmapEncode.BitmapVariants;

namespace BitmapEncode.Images.Interfaces
{
    public interface IImageHandler
    {
        IBitmap Load(string path, PixelFormat depth);
    }
}
