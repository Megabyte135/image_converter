using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using BitmapEncode.Images.Interfaces;
using System.Text;

public class BmpImageHandler : IImageHandler
{
    public IBitmap Load(string path, PixelFormat depth)
    {
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            return LoadFromStream(fs, depth);
        }
    }

    public IBitmap LoadFromStream(Stream stream, PixelFormat depth)
    {
        using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true))
        {
            // Read BMP file header (14 bytes)
            ushort bfType = reader.ReadUInt16(); // Signature 'BM'
            if (bfType != 0x4D42) // 'BM' in little-endian
            {
                throw new Exception("Invalid BMP file.");
            }

            uint bfSize = reader.ReadUInt32();      // File size
            ushort bfReserved1 = reader.ReadUInt16(); // Reserved, must be 0
            ushort bfReserved2 = reader.ReadUInt16(); // Reserved, must be 0
            uint bfOffBits = reader.ReadUInt32();     // Offset to pixel data

            uint dibHeaderSize = reader.ReadUInt32(); // DIB header size

            int bmpWidth;
            int bmpHeight;
            ushort bmpPlanes;
            ushort bmpBitCount;
            uint bmpCompression;
            uint bmpSizeImage;
            int bmpXPelsPerMeter;
            int bmpYPelsPerMeter;
            uint bmpClrUsed;
            uint bmpClrImportant;

            if (dibHeaderSize == 12)
            {
                // BITMAPCOREHEADER
                bmpWidth = reader.ReadUInt16();
                bmpHeight = reader.ReadUInt16();
                bmpPlanes = reader.ReadUInt16();
                bmpBitCount = reader.ReadUInt16();
                // Default values for missing fields
                bmpCompression = 0;
                bmpSizeImage = 0;
                bmpXPelsPerMeter = 0;
                bmpYPelsPerMeter = 0;
                bmpClrUsed = 0;
                bmpClrImportant = 0;
            }
            else if (dibHeaderSize == 40 || dibHeaderSize == 52 || dibHeaderSize == 56 || dibHeaderSize == 108 || dibHeaderSize == 124)
            {
                // BITMAPINFOHEADER or newer
                bmpWidth = reader.ReadInt32();
                bmpHeight = reader.ReadInt32();
                bmpPlanes = reader.ReadUInt16();
                bmpBitCount = reader.ReadUInt16();
                bmpCompression = reader.ReadUInt32();
                bmpSizeImage = reader.ReadUInt32();
                bmpXPelsPerMeter = reader.ReadInt32();
                bmpYPelsPerMeter = reader.ReadInt32();
                bmpClrUsed = reader.ReadUInt32();
                bmpClrImportant = reader.ReadUInt32();

                // Skip remaining header bytes
                int remainingHeaderBytes = (int)dibHeaderSize - 40;
                if (remainingHeaderBytes > 0)
                {
                    reader.ReadBytes(remainingHeaderBytes);
                }
            }
            else
            {
                throw new Exception($"Unsupported DIB header size: {dibHeaderSize}");
            }

            if (bmpPlanes != 1)
            {
                throw new Exception("Invalid number of planes.");
            }

            if (bmpCompression != 0)
            {
                throw new Exception($"Compressed BMP files are not supported. Compression type: {bmpCompression}");
            }

            // Determine color table size
            int colorTableSize = 0;
            if (bmpBitCount <= 8)
            {
                int numColors = (int)(bmpClrUsed != 0 ? (int)bmpClrUsed : 1 << bmpBitCount);
                colorTableSize = numColors * (dibHeaderSize == 12 ? 3 : 4); // 3 bytes per color for BITMAPCOREHEADER, 4 bytes otherwise
            }

            byte[] colorTable = reader.ReadBytes(colorTableSize);

            // Calculate stride (row width in bytes, including padding)
            int bytesPerPixel = (bmpBitCount * bmpWidth + 7) / 8;
            int stride = ((bytesPerPixel + 3) / 4) * 4; // Rows are padded to multiples of 4 bytes

            // Read pixel data
            stream.Seek(bfOffBits, SeekOrigin.Begin);
            int pixelArraySize = stride * Math.Abs(bmpHeight);
            byte[] pixelData = reader.ReadBytes(pixelArraySize);

            int imageHeight = Math.Abs(bmpHeight);
            CustomBitmap bitmap = new CustomBitmap(bmpWidth, imageHeight, depth);

            MapBmpDataToBitmap(pixelData, bitmap, bmpWidth, bmpHeight, bmpBitCount, colorTable, stride, dibHeaderSize == 12);

            return bitmap;
        }
    }

    private void MapBmpDataToBitmap(byte[] pixelData, CustomBitmap bitmap, int width, int height, ushort bitCount, byte[] colorTable, int stride, bool isCoreHeader)
    {
        bool isBottomUp = height > 0;
        int absHeight = Math.Abs(height);

        for (int y = 0; y < absHeight; y++)
        {
            int bmpY = isBottomUp ? absHeight - 1 - y : y;
            int rowOffset = bmpY * stride;

            for (int x = 0; x < width; x++)
            {
                Color color = GetBmpPixelColor(pixelData, rowOffset, x, bitCount, colorTable, isCoreHeader);
                bitmap.SetPixel(x, y, color);
            }
        }
    }

    private Color GetBmpPixelColor(byte[] pixelData, int rowOffset, int x, ushort bitCount, byte[] colorTable, bool isCoreHeader)
    {
        switch (bitCount)
        {
            case 1:
                {
                    // Each pixel is 1 bit
                    int byteIndex = rowOffset + x / 8;
                    int bitIndex = 7 - (x % 8);
                    byte pixelByte = pixelData[byteIndex];
                    int colorIndex = (pixelByte >> bitIndex) & 0x1;

                    // Get color from color table
                    int colorTableIndex = colorIndex * (isCoreHeader ? 3 : 4);
                    byte blue = colorTable[colorTableIndex];
                    byte green = colorTable[colorTableIndex + 1];
                    byte red = colorTable[colorTableIndex + 2];
                    return new Color { R = red, G = green, B = blue, A = 255 };
                }
            case 4:
                {
                    // Each pixel is 4 bits
                    int byteIndex = rowOffset + x / 2;
                    byte pixelByte = pixelData[byteIndex];
                    int colorIndex;
                    if (x % 2 == 0)
                    {
                        colorIndex = (pixelByte >> 4) & 0xF;
                    }
                    else
                    {
                        colorIndex = pixelByte & 0xF;
                    }

                    // Get color from color table
                    int colorTableIndex = colorIndex * (isCoreHeader ? 3 : 4);
                    byte blue = colorTable[colorTableIndex];
                    byte green = colorTable[colorTableIndex + 1];
                    byte red = colorTable[colorTableIndex + 2];
                    return new Color { R = red, G = green, B = blue, A = 255 };
                }
            case 8:
                {
                    // Each pixel is 8 bits
                    int byteIndex = rowOffset + x;
                    byte colorIndex = pixelData[byteIndex];

                    // Get color from color table
                    int colorTableIndex = colorIndex * (isCoreHeader ? 3 : 4);
                    byte blue = colorTable[colorTableIndex];
                    byte green = colorTable[colorTableIndex + 1];
                    byte red = colorTable[colorTableIndex + 2];
                    return new Color { R = red, G = green, B = blue, A = 255 };
                }
            case 16:
                {
                    // Each pixel is 16 bits
                    int byteIndex = rowOffset + x * 2;
                    ushort pixelData16 = BitConverter.ToUInt16(pixelData, byteIndex);

                    // Assume 5 bits per channel (5-5-5)
                    byte red = (byte)((pixelData16 >> 10) & 0x1F);
                    byte green = (byte)((pixelData16 >> 5) & 0x1F);
                    byte blue = (byte)(pixelData16 & 0x1F);

                    // Scale to 8 bits
                    red = (byte)((red * 255) / 31);
                    green = (byte)((green * 255) / 31);
                    blue = (byte)((blue * 255) / 31);

                    return new Color { R = red, G = green, B = blue, A = 255 };
                }
            case 24:
                {
                    // Each pixel is 24 bits (3 bytes)
                    int byteIndex = rowOffset + x * 3;
                    byte blue = pixelData[byteIndex];
                    byte green = pixelData[byteIndex + 1];
                    byte red = pixelData[byteIndex + 2];
                    return new Color { R = red, G = green, B = blue, A = 255 };
                }
            case 32:
                {
                    // Each pixel is 32 bits (4 bytes)
                    int byteIndex = rowOffset + x * 4;
                    byte blue = pixelData[byteIndex];
                    byte green = pixelData[byteIndex + 1];
                    byte red = pixelData[byteIndex + 2];
                    byte alpha = pixelData[byteIndex + 3];
                    return new Color { R = red, G = green, B = blue, A = alpha };
                }
            default:
                throw new Exception("Unsupported bit count: " + bitCount);
        }
    }

    public void Save(IBitmap bitmap, string path)
    {
        ushort bitsPerPixel = (ushort)bitmap.Depth;
        uint bytesPerPixel = bitsPerPixel / 8u;

        int padding = (4 - ((int)(bitmap.Width * bytesPerPixel) % 4)) % 4;

        uint imageSize = (uint)((bitmap.Width * bytesPerPixel + padding) * bitmap.Height);

        //Header size (14 bytes) + Info header size (40 bytes)
        uint fileHeaderSize = 14u;
        uint infoHeaderSize = 40u;
        uint masksSize = 0u;

        uint compression = 0u; // BI_RGB

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

            // Pixel data
            switch (bitmap.Depth)
            {
                case PixelFormat.OneBit:
                    Write1BitBitmap(writer, bitmap, padding);
                    break;
                case PixelFormat.EightBit:
                    Write8BitBitmap(writer, bitmap, padding);
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
