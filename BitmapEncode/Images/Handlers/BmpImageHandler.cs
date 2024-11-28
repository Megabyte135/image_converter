using BitmapEncode.BitmapVariants;
using BitmapEncode.Images.Interfaces;

namespace BitmapEncode.Images.Handlers
{
    public class BmpImageHandler : IImageHandler
    {
        public IBitmap Load(string path, PixelFormat depth)
        {
            byte[] data = File.ReadAllBytes(path);

            int width = BitConverter.ToInt32(data, 18);
            int height = BitConverter.ToInt32(data, 22);
            CustomBitmap bitmap = new CustomBitmap(width, height, depth);

            int pixelDataOffset = BitConverter.ToInt32(data, 10);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int colorOffset = pixelDataOffset + (y * width + x) * 3;
                    bitmap.SetPixel(x, y, Color.FromBytes(data, colorOffset, depth));
                }
            }

            return bitmap;
        }

        public void Save(IBitmap bitmap, string path)
        {
            using var stream = new BinaryWriter(File.Open(path, FileMode.Create));

            // BMP File Header (14 bytes)
            stream.Write(new byte[] { 0x42, 0x4D }); // Signature 'BM'
            int fileSize = 14 + 40 + bitmap.Width * bitmap.Height * (int)bitmap.Depth / 8; // File size
            stream.Write(BitConverter.GetBytes(fileSize)); // File size
            stream.Write(new byte[] { 0x00, 0x00 }); // Reserved1
            stream.Write(new byte[] { 0x00, 0x00 }); // Reserved2
            stream.Write(BitConverter.GetBytes(54)); // Pixel data offset

            // DIB Header (40 bytes)
            stream.Write(BitConverter.GetBytes(40)); // DIB header size
            stream.Write(BitConverter.GetBytes(bitmap.Width)); // Width
            stream.Write(BitConverter.GetBytes(bitmap.Height)); // Height
            stream.Write(BitConverter.GetBytes((short)1)); // Planes (must be 1)
            stream.Write(BitConverter.GetBytes((short)CalculateBitsPerPixel(bitmap.Depth))); // Bits per pixel
            stream.Write(BitConverter.GetBytes(0)); // Compression (BI_RGB, no compression)
            int pixelDataSize = bitmap.Width * bitmap.Height * (int)bitmap.Depth / 8;
            stream.Write(BitConverter.GetBytes(pixelDataSize)); // Image size
            stream.Write(BitConverter.GetBytes(2835)); // Horizontal resolution (pixels/meter, arbitrary)
            stream.Write(BitConverter.GetBytes(2835)); // Vertical resolution (pixels/meter, arbitrary)
            stream.Write(BitConverter.GetBytes(0)); // Colors in palette (0 for no palette)
            stream.Write(BitConverter.GetBytes(0)); // Important colors (0 means all)

            // Pixel Data (bottom-to-top row order)
            int bytesPerPixel = (int)bitmap.Depth / 8;
            int rowSize = (bitmap.Width * bytesPerPixel + 3) / 4 * 4; // Each row must be padded to a multiple of 4 bytes
            byte[] rowPadding = new byte[rowSize - bitmap.Width * bytesPerPixel];

            for (int y = bitmap.Height - 1; y >= 0; y--) // BMP stores pixels bottom-to-top
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    byte[] pixelData = color.ToBytes(bitmap.Depth);
                    stream.Write(pixelData);
                }
                stream.Write(rowPadding); // Add padding
            }
        }

        private int CalculateBitsPerPixel(PixelFormat depth)
        {
            return depth switch
            {
                PixelFormat.OneBit => 1,
                PixelFormat.EightBit => 8,
                PixelFormat.SixteenBit => 16,
                PixelFormat.TwentyFourBit => 24,
                PixelFormat.ThirtyTwoBit => 32,
                _ => throw new NotSupportedException("Unsupported pixel format.")
            };
        }
    }
}
