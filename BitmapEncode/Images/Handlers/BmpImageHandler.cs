using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using BitmapEncode.Images.Interfaces;
using SkiaSharp;

public class BmpImageHandler : IImageHandler
{
    public IBitmap Load(string path, PixelFormat depth)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("BMP file not found.", path);

        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            return LoadFromStream(fs, depth);
        }
    }

    public IBitmap LoadFromStream(Stream stream, PixelFormat depth)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "Stream cannot be null.");

        SKBitmap skBitmap = SKBitmap.Decode(stream);
        if (skBitmap == null)
        {
            throw new Exception("Failed to decode BMP image.");
        }

        IBitmap bitmap = new CustomBitmap(skBitmap.Width, skBitmap.Height, depth);

        for (int y = 0; y < skBitmap.Height; y++)
        {
            for (int x = 0; x < skBitmap.Width; x++)
            {
                SKColor skColor = skBitmap.GetPixel(x, y);
                bitmap.SetPixel(x, y, new Color
                {
                    R = skColor.Red,
                    G = skColor.Green,
                    B = skColor.Blue,
                    A = skColor.Alpha
                });
            }
        }

        return bitmap;
    }

    public void Save(IBitmap bitmap, string path)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap), "Bitmap cannot be null.");

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        switch (bitmap.Depth)
        {
            case PixelFormat.OneBit:
            case PixelFormat.EightBit:
                SaveIndexedBitmap(bitmap, path);
                break;
            case PixelFormat.TwentyFourBit:
            case PixelFormat.ThirtyTwoBit:
                SaveRgbBitmap(bitmap, path);
                break;
            default:
                throw new NotSupportedException($"Unsupported PixelFormat: {bitmap.Depth}");
        }
    }

    private void SaveRgbBitmap(IBitmap bitmap, string path)
    {
        int bytesPerPixel = bitmap.Depth == PixelFormat.TwentyFourBit ? 3 : 4;
        int rowSize = ((bytesPerPixel * bitmap.Width + 3) / 4) * 4;
        uint fileHeaderSize = 14;
        uint infoHeaderSize = 40;
        uint pixelDataOffset = fileHeaderSize + infoHeaderSize;
        uint imageSize = (uint)(rowSize * bitmap.Height);
        uint fileSize = pixelDataOffset + imageSize;

        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            // BMP File Header
            writer.Write((ushort)0x4D42); // BM
            writer.Write(fileSize);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(pixelDataOffset);

            // BMP Info Header
            writer.Write(infoHeaderSize);
            writer.Write(bitmap.Width);
            writer.Write(bitmap.Height);
            writer.Write((ushort)1); // Planes
            writer.Write((ushort)(bytesPerPixel * 8)); // Bits per pixel
            writer.Write((uint)0); // Compression
            writer.Write(imageSize);
            writer.Write(0); // X Pixels per meter
            writer.Write(0); // Y Pixels per meter
            writer.Write((uint)0); // Colors used
            writer.Write((uint)0); // Important colors

            // Pixel Data
            for (int y = bitmap.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    writer.Write(color.B);
                    writer.Write(color.G);
                    writer.Write(color.R);

                    if (bytesPerPixel == 4)
                    {
                        writer.Write(color.A);
                    }
                }

                // Row padding
                int padding = rowSize - (bitmap.Width * bytesPerPixel);
                for (int i = 0; i < padding; i++)
                {
                    writer.Write((byte)0);
                }
            }
        }
    }

    private void SaveIndexedBitmap(IBitmap bitmap, string path)
    {
        int bitsPerPixel = bitmap.Depth == PixelFormat.OneBit ? 1 : 8;
        int paletteColors = (int)Math.Pow(2, bitsPerPixel);
        List<Color> palette = GeneratePalette(bitmap, paletteColors);
        int rowSize = ((bitsPerPixel * bitmap.Width + 31) / 32) * 4;
        int paletteSize = paletteColors * 4;
        uint fileHeaderSize = 14;
        uint infoHeaderSize = 40;
        uint pixelDataOffset = fileHeaderSize + infoHeaderSize + (uint)paletteSize;
        uint imageSize = (uint)(rowSize * bitmap.Height);
        uint fileSize = pixelDataOffset + imageSize;

        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            writer.Write((ushort)0x4D42);
            writer.Write(fileSize);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(pixelDataOffset);

            writer.Write(infoHeaderSize);
            writer.Write(bitmap.Width);
            writer.Write(bitmap.Height);
            writer.Write((ushort)1);
            writer.Write((ushort)bitsPerPixel);
            writer.Write((uint)0);
            writer.Write(imageSize);
            writer.Write(0);
            writer.Write(0);
            writer.Write((uint)paletteColors);
            writer.Write((uint)0);

            foreach (var color in palette)
            {
                writer.Write(color.B);
                writer.Write(color.G);
                writer.Write(color.R);
                writer.Write((byte)0);
            }

            for (int y = bitmap.Height - 1; y >= 0; y--)
            {
                List<byte> rowBytes = new List<byte>();

                if (bitsPerPixel == 1)
                {
                    byte currentByte = 0;
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        byte paletteIndex = (byte)FindClosestPaletteIndex(color, palette);
                        currentByte = (byte)((currentByte << 1) | (paletteIndex & 0x1));

                        if ((x + 1) % 8 == 0 || x == bitmap.Width - 1)
                        {
                            if (x == bitmap.Width - 1 && (bitmap.Width % 8) != 0)
                            {
                                currentByte = (byte)(currentByte << (8 - (bitmap.Width % 8)));
                            }
                            rowBytes.Add(currentByte);
                            currentByte = 0;
                        }
                    }
                }
                else if (bitsPerPixel == 8)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        byte paletteIndex = (byte)FindClosestPaletteIndex(color, palette);
                        rowBytes.Add(paletteIndex);
                    }
                }

                while (rowBytes.Count % 4 != 0)
                {
                    rowBytes.Add(0);
                }

                writer.Write(rowBytes.ToArray());
            }
        }
    }

    private List<Color> GeneratePalette(IBitmap bitmap, int paletteColors)
    {
        List<Color> palette = new List<Color>();

        if (paletteColors <= 0)
            throw new ArgumentException("Palette color count must be greater than zero.", nameof(paletteColors));

        for (int i = 0; i < paletteColors; i++)
        {
            byte gray = (byte)(i * 255 / (paletteColors - 1));
            palette.Add(new Color(gray, gray, gray, 255));
        }

        return palette;
    }

    private int FindClosestPaletteIndex(Color targetColor, List<Color> palette)
    {
        int closestIndex = 0;
        int minDistanceSquared = int.MaxValue;

        for (int i = 0; i < palette.Count; i++)
        {
            Color paletteColor = palette[i];
            int deltaR = targetColor.R - paletteColor.R;
            int deltaG = targetColor.G - paletteColor.G;
            int deltaB = targetColor.B - paletteColor.B;
            int distanceSquared = deltaR * deltaR + deltaG * deltaG + deltaB * deltaB;

            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                closestIndex = i;

                if (distanceSquared == 0)
                    break;
            }
        }

        return closestIndex;
    }
}