using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using BitmapEncode.Images.Interfaces;

public class BmpImageHandler : IImageHandler
{
    public IBitmap Load(string path, PixelFormat depth)
    {
        // Loading BMP files is not implemented
        throw new NotImplementedException("Loading BMP files is not implemented.");
    }

    public void Save(IBitmap bitmap, string path)
    {
        ushort bitsPerPixel = GetBitsPerPixel(bitmap.Depth);
        uint bytesPerPixel = bitsPerPixel / 8u;

        int padding = (4 - ((int)(bitmap.Width * bytesPerPixel) % 4)) % 4;

        uint imageSize = (uint)((bitmap.Width * bytesPerPixel + padding) * bitmap.Height);

        //Header size (14 bytes) + Info header size (40 bytes)
        uint fileHeaderSize = 14u;
        uint infoHeaderSize = 40u;
        uint masksSize = 0u;

        uint compression = 0u; // BI_RGB

        if (bitmap.Depth == PixelFormat.SixteenBit)
        {
            compression = 3u; // BI_BITFIELDS
            masksSize = 12u;  // 3*4byte
        }

        uint pixelDataOffset = fileHeaderSize + infoHeaderSize + masksSize;
        uint fileSize = pixelDataOffset + imageSize;

        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            writer.Write((ushort)0x4D42); // 'BM'
            writer.Write(fileSize);       // File size
            writer.Write((ushort)0);      // Reserved1
            writer.Write((ushort)0);      // Reserved2
            writer.Write(pixelDataOffset); // Offset

            // Write BITMAPINFOHEADER
            writer.Write(infoHeaderSize);
            writer.Write((int)bitmap.Width);
            writer.Write((int)bitmap.Height);
            writer.Write((ushort)1);               // Planes
            writer.Write(bitsPerPixel);
            writer.Write(compression);
            writer.Write(imageSize);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // Write color masks
            if (bitmap.Depth == PixelFormat.SixteenBit)
            {
                // Red mask / 5 bits
                writer.Write(0x0000F800u);
                // Green mask / 6 bits
                writer.Write(0x000007E0u);
                // Blue mask / 5 bits
                writer.Write(0x0000001Fu);
            }

            // Pixel data
            switch (bitmap.Depth)
            {
                case PixelFormat.OneBit:
                    Write1BitBitmap(writer, bitmap, padding);
                    break;
                case PixelFormat.EightBit:
                    Write8BitBitmap(writer, bitmap, padding);
                    break;
                case PixelFormat.SixteenBit:
                    Write16BitBitmap(writer, bitmap, padding);
                    break;
                case PixelFormat.TwentyFourBit:
                    Write24BitBitmap(writer, bitmap, padding);
                    break;
                case PixelFormat.ThirtyTwoBit:
                    Write32BitBitmap(writer, bitmap, padding);
                    break;
                default:
                    throw new NotSupportedException("Unsupported pixel format.");
            }
        }
    }

    private ushort GetBitsPerPixel(PixelFormat depth)
    {
        return depth switch
        {
            PixelFormat.OneBit => 1,
            PixelFormat.EightBit => 8,
            PixelFormat.SixteenBit => 16,
            PixelFormat.TwentyFourBit => 24,
            PixelFormat.ThirtyTwoBit => 32,
            _ => throw new NotSupportedException("Unsupported pixel format."),
        };
    }

    private void Write1BitBitmap(BinaryWriter writer, IBitmap bitmap, int padding)
    {
        // Color palette (B&W)
        writer.Write((uint)0x00000000); // Black
        writer.Write((uint)0x00FFFFFF); // White

        // Row padding x4
        int bytesPerRow = ((bitmap.Width + 31) / 32) * 4;
        byte[] rowData = new byte[bytesPerRow];

        for (int y = bitmap.Height - 1; y >= 0; y--)
        {
            Array.Clear(rowData, 0, rowData.Length);
            int bitIndex = 0;
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                byte luminance = (byte)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                int byteIndex = bitIndex / 8;
                int bitPosition = 7 - (bitIndex % 8);
                if (luminance > 127)
                {
                    rowData[byteIndex] |= (byte)(1 << bitPosition);
                }
                bitIndex++;
            }
            writer.Write(rowData, 0, bytesPerRow);
        }
    }

    private void Write8BitBitmap(BinaryWriter writer, IBitmap bitmap, int padding)
    {
        List<Color> palette = GeneratePalette(bitmap);

        foreach (var color in palette)
        {
            writer.Write(color.B);
            writer.Write(color.G);
            writer.Write(color.R);
            writer.Write((byte)0);
        }

        byte[] indices = MapPixelsToIndices(bitmap, palette);

        int stride = bitmap.Width;
        int indexOffset = 0;

        for (int y = bitmap.Height - 1; y >= 0; y--)
        {
            writer.Write(indices, indexOffset + y * stride, stride);
            for (int p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }
    }



    private void Write16BitBitmap(BinaryWriter writer, IBitmap bitmap, int padding)
    {
        for (int y = bitmap.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                ushort r = (ushort)(color.R >> 3);
                ushort g = (ushort)(color.G >> 2);
                ushort b = (ushort)(color.B >> 3);
                ushort pixel = (ushort)((r << 11) | (g << 5) | b);
                writer.Write(pixel);
            }

            for (int p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }
    }


    private void Write24BitBitmap(BinaryWriter writer, IBitmap bitmap, int padding)
    {
        for (int y = bitmap.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);

                writer.Write(color.B);
                writer.Write(color.G);
                writer.Write(color.R);
            }

            for (int p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }
    }

    private byte[] MapPixelsToIndices(IBitmap bitmap, List<Color> palette)
    {
        Dictionary<Color, byte> colorToIndex = new Dictionary<Color, byte>();
        for (int i = 0; i < palette.Count; i++)
        {
            colorToIndex[palette[i]] = (byte)i;
        }

        byte[] indices = new byte[bitmap.Width * bitmap.Height];
        int index = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);

                if (colorToIndex.TryGetValue(color, out byte paletteIndex))
                {
                    indices[index++] = paletteIndex;
                }
                else
                {
                    paletteIndex = FindClosestColorIndex(color, palette);
                    indices[index++] = paletteIndex;
                }
            }
        }

        return indices;
    }

    private byte FindClosestColorIndex(Color color, List<Color> palette)
    {
        byte closestIndex = 0;
        double minDistance = double.MaxValue;

        for (int i = 0; i < palette.Count; i++)
        {
            Color paletteColor = palette[i];
            double distance = ColorDistance(color, paletteColor);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = (byte)i;
            }
        }

        return closestIndex;
    }

    private double ColorDistance(Color c1, Color c2)
    {
        int dr = c1.R - c2.R;
        int dg = c1.G - c2.G;
        int db = c1.B - c2.B;
        return dr * dr + dg * dg + db * db;
    }

    private void Write32BitBitmap(BinaryWriter writer, IBitmap bitmap, int padding)
    {
        for (int y = bitmap.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                // BGRA for BMP
                writer.Write(color.B);
                writer.Write(color.G);
                writer.Write(color.R);
                writer.Write(color.A);
            }
            for (int p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }
    }

    private List<Color> GeneratePalette(IBitmap bitmap)
    {
        HashSet<Color> colorSet = new HashSet<Color>();

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                colorSet.Add(color);

                if (colorSet.Count >= 256)
                    break;
            }
            if (colorSet.Count >= 256)
                break;
        }

        List<Color> palette = new List<Color>(colorSet);
        if (palette.Count > 256)
        {
            palette = ReduceColors(palette, 256);
        }

        return palette;
    }

    private List<Color> ReduceColors(List<Color> colors, int maxColors)
    {
        int step = colors.Count / maxColors;
        List<Color> reducedPalette = new List<Color>();
        for (int i = 0; i < colors.Count; i += step)
        {
            int end = Math.Min(i + step, colors.Count);
            int count = end - i;
            int totalR = 0, totalG = 0, totalB = 0;

            for (int j = i; j < end; j++)
            {
                totalR += colors[j].R;
                totalG += colors[j].G;
                totalB += colors[j].B;
            }

            byte avgR = (byte)(totalR / count);
            byte avgG = (byte)(totalG / count);
            byte avgB = (byte)(totalB / count);

            reducedPalette.Add(new Color { R = avgR, G = avgG, B = avgB, A = 255 });
        }

        return reducedPalette;
    }

}
