using System;
using System.IO;
using System.Collections.Generic;
using BitmapEncode.BitmapVariants;
using BitmapEncode.Images.Handlers;
using BitmapEncode.Images.Interfaces;
using BitmapEncode.Images;

public class ICOImageHandler : IImageHandler
{
    public IBitmap Load(string path, PixelFormat depth)
    {
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // Read ICONDIR structure
            ushort reserved = reader.ReadUInt16(); // Reserved, should be 0
            if (reserved != 0)
                throw new Exception("Invalid ICO file.");

            ushort type = reader.ReadUInt16(); // Type, should be 1 for icons
            if (type != 1)
                throw new Exception("Invalid ICO file.");

            ushort count = reader.ReadUInt16(); // Number of images
            if (count == 0)
                throw new Exception("ICO file contains no images.");

            // Read ICONDIRENTRY structures
            List<IconDirEntry> entries = new List<IconDirEntry>();
            for (int i = 0; i < count; i++)
            {
                IconDirEntry entry = new IconDirEntry();
                entry.Width = reader.ReadByte();
                entry.Height = reader.ReadByte();
                entry.ColorCount = reader.ReadByte();
                entry.Reserved = reader.ReadByte();
                entry.Planes = reader.ReadUInt16();
                entry.BitCount = reader.ReadUInt16();
                entry.BytesInRes = reader.ReadUInt32();
                entry.ImageOffset = reader.ReadUInt32();
                entries.Add(entry);
            }

            // For simplicity, we'll load the first image
            IconDirEntry selectedEntry = entries[0];

            // Seek to the image data
            fs.Seek(selectedEntry.ImageOffset, SeekOrigin.Begin);

            // Determine if the image is a PNG or BMP
            byte[] header = reader.ReadBytes(8);
            fs.Seek(selectedEntry.ImageOffset, SeekOrigin.Begin); // Reset position

            bool isPNG = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E &&
                         header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A &&
                         header[6] == 0x1A && header[7] == 0x0A;

            IBitmap bitmap;
            if (isPNG)
            {
                IImageHandler pngHandler = new PngImageHandler();
                bitmap = pngHandler.LoadFromStream(fs, depth);
            }
            else
            {
                bitmap = LoadBMPFromICO(reader, selectedEntry, depth);
            }

            return bitmap;
        }
    }

    public void Save(IBitmap bitmap, string path)
    {
        throw new NotImplementedException("Saving ICO files is not implemented.");
    }

    // Helper methods and classes

    private IBitmap LoadBMPFromICO(BinaryReader reader, IconDirEntry entry, PixelFormat depth)
    {
        // Read BITMAPINFOHEADER
        uint biSize = reader.ReadUInt32();
        int width = reader.ReadInt32();
        int height = reader.ReadInt32() / 2; // Height includes XOR and AND masks
        ushort planes = reader.ReadUInt16();
        ushort bitCount = reader.ReadUInt16();
        uint compression = reader.ReadUInt32();
        uint imageSize = reader.ReadUInt32();
        int xPelsPerMeter = reader.ReadInt32();
        int yPelsPerMeter = reader.ReadInt32();
        uint clrUsed = reader.ReadUInt32();
        uint clrImportant = reader.ReadUInt32();

        // Create bitmap
        CustomBitmap bitmap = new CustomBitmap(width, height, depth);

        // Read color table if present
        int colorTableSize = 0;
        if (bitCount <= 8)
        {
            int colorsInTable = (int)(clrUsed == 0 ? 1 << bitCount : clrUsed);
            colorTableSize = colorsInTable * 4; // Each entry is 4 bytes (BGRA)
        }
        byte[] colorTable = reader.ReadBytes(colorTableSize);

        // Read pixel data
        int bytesPerRow = ((width * bitCount + 31) / 32) * 4;
        byte[] pixelData = new byte[bytesPerRow * height];
        for (int i = height - 1; i >= 0; i--)
        {
            int rowOffset = i * bytesPerRow;
            byte[] row = reader.ReadBytes(bytesPerRow);
            Array.Copy(row, 0, pixelData, rowOffset, bytesPerRow);
        }

        // Skip AND mask
        int andMaskSize = ((width + 31) / 32) * 4 * height;
        reader.BaseStream.Seek(andMaskSize, SeekOrigin.Current);

        // Map pixel data to bitmap
        int pixelOffset = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = new Color();
                if (bitCount == 24)
                {
                    color.B = pixelData[pixelOffset++];
                    color.G = pixelData[pixelOffset++];
                    color.R = pixelData[pixelOffset++];
                    color.A = 255;
                }
                else if (bitCount == 32)
                {
                    color.B = pixelData[pixelOffset++];
                    color.G = pixelData[pixelOffset++];
                    color.R = pixelData[pixelOffset++];
                    color.A = pixelData[pixelOffset++];
                }
                else if (bitCount <= 8)
                {
                    int index = 0;
                    int bitsPerPixel = bitCount;
                    int pixelsPerByte = 8 / bitsPerPixel;
                    int shift = ((x % pixelsPerByte) * bitsPerPixel);
                    byte b = pixelData[pixelOffset + (x / pixelsPerByte)];
                    index = (b >> (8 - shift - bitsPerPixel)) & ((1 << bitsPerPixel) - 1);

                    int colorTableIndex = index * 4;
                    color.B = colorTable[colorTableIndex];
                    color.G = colorTable[colorTableIndex + 1];
                    color.R = colorTable[colorTableIndex + 2];
                    color.A = 255;
                }
                else
                {
                    throw new NotSupportedException("Unsupported bit depth.");
                }

                bitmap.SetPixel(x, y, color);
            }
            if (bitCount <= 8)
            {
                pixelOffset += ((width * bitCount + 31) / 32) * 4 - (width * bitCount + 7) / 8;
            }
        }

        return bitmap;
    }

    private class IconDirEntry
    {
        public byte Width { get; set; }
        public byte Height { get; set; }
        public byte ColorCount { get; set; }
        public byte Reserved { get; set; }
        public ushort Planes { get; set; }
        public ushort BitCount { get; set; }
        public uint BytesInRes { get; set; }
        public uint ImageOffset { get; set; }
    }
}
