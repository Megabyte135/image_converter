using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using BitmapEncode.Images.Interfaces;
using BitmapEncode.Images.Handlers;
using System;
using System.IO;

namespace BitmapEncode.Images.Backstage
{
    public class IcoImageHandler : IImageHandler
    {
        public IBitmap Load(string path, PixelFormat depth)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                // Read ICONDIR
                ICONDIR iconDir = new ICONDIR
                {
                    Reserved = reader.ReadUInt16(),
                    Type = reader.ReadUInt16(),
                    Count = reader.ReadUInt16()
                };

                if (iconDir.Reserved != 0 || iconDir.Type != 1 || iconDir.Count == 0)
                {
                    throw new Exception("Invalid ICO file.");
                }

                // Read the first ICONDIRENTRY (16 bytes)
                ICONDIRENTRY entry = new ICONDIRENTRY
                {
                    Width = reader.ReadByte(),
                    Height = reader.ReadByte(),
                    ColorCount = reader.ReadByte(),
                    Reserved = reader.ReadByte(),
                    Planes = reader.ReadUInt16(),
                    BitCount = reader.ReadUInt16(),
                    BytesInRes = reader.ReadUInt32(),
                    ImageOffset = reader.ReadUInt32()
                };

                // Seek to ImageOffset
                fs.Seek(entry.ImageOffset, SeekOrigin.Begin);

                // Read the first 8 bytes to check for PNG signature
                byte[] headerBytes = reader.ReadBytes(8);
                fs.Seek(entry.ImageOffset, SeekOrigin.Begin); // Reset position

                if (IsPng(headerBytes))
                {
                    // Read the PNG data
                    byte[] pngData = reader.ReadBytes((int)entry.BytesInRes);

                    // Use PngImageHandler to load the image from the PNG data
                    using (MemoryStream pngStream = new MemoryStream(pngData))
                    {
                        PngImageHandler pngHandler = new PngImageHandler();
                        IBitmap bitmap = pngHandler.Load(pngStream, depth);
                        return bitmap;
                    }
                }
                else
                {
                    // Read BITMAPINFOHEADER
                    BITMAPINFOHEADER bmpInfoHeader = new BITMAPINFOHEADER
                    {
                        Size = reader.ReadUInt32(),
                        Width = reader.ReadInt32(),
                        Height = reader.ReadInt32(),
                        Planes = reader.ReadUInt16(),
                        BitCount = reader.ReadUInt16(),
                        Compression = reader.ReadUInt32(),
                        SizeImage = reader.ReadUInt32(),
                        XPelsPerMeter = reader.ReadInt32(),
                        YPelsPerMeter = reader.ReadInt32(),
                        ClrUsed = reader.ReadUInt32(),
                        ClrImportant = reader.ReadUInt32()
                    };

                    // Calculate the height of the image (half of total height)
                    int height = bmpInfoHeader.Height / 2;
                    int width = bmpInfoHeader.Width;
                    int bpp = bmpInfoHeader.BitCount;

                    // Create a CustomBitmap
                    CustomBitmap bitmap = new CustomBitmap(width, height, depth);

                    // Rest of the code remains the same...
                    // Read the color palette if necessary
                    Color[] colorTable = null;
                    if (bpp <= 8)
                    {
                        int colorCount = bmpInfoHeader.ClrUsed != 0 ? (int)bmpInfoHeader.ClrUsed : 1 << bpp;
                        colorTable = new Color[colorCount];
                        for (int i = 0; i < colorCount; i++)
                        {
                            byte b = reader.ReadByte();
                            byte g = reader.ReadByte();
                            byte r = reader.ReadByte();
                            byte reserved = reader.ReadByte(); // Usually zero
                            colorTable[i] = new Color(r, g, b);
                        }
                    }

                    // Read the pixel data
                    if (bpp == 32)
                    {
                        Read32bppBitmap(reader, bitmap, width, height);
                    }
                    else if (bpp == 24)
                    {
                        Read24bppBitmap(reader, bitmap, width, height);
                    }
                    else if (bpp == 8)
                    {
                        Read8bppBitmap(reader, bitmap, width, height, colorTable);
                    }
                    else if (bpp == 4)
                    {
                        Read4bppBitmap(reader, bitmap, width, height, colorTable);
                    }
                    else if (bpp == 1)
                    {
                        Read1bppBitmap(reader, bitmap, width, height, colorTable);
                    }
                    else
                    {
                        throw new NotImplementedException($"Bit depth of {bpp} is not supported.");
                    }

                    // Read the AND mask for transparency
                    ReadAndMask(reader, bitmap, width, height);

                    return bitmap;
                }
            }
        }

        private void Read32bppBitmap(BinaryReader reader, CustomBitmap bitmap, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte b = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte r = reader.ReadByte();
                    byte a = reader.ReadByte();
                    Color color = new Color(r, g, b, a);
                    bitmap.SetPixel(x, height - y - 1, color);
                }
            }
        }

        private void Read24bppBitmap(BinaryReader reader, CustomBitmap bitmap, int width, int height)
        {
            int bytesPerPixel = 3;
            int rowSize = ((width * bytesPerPixel + 3) / 4) * 4;
            byte[] rowData = new byte[rowSize];

            for (int y = 0; y < height; y++)
            {
                int readBytes = reader.Read(rowData, 0, rowSize);
                if (readBytes != rowSize)
                {
                    throw new Exception("Unexpected end of file while reading pixel data.");
                }

                int pixelIndex = 0;
                for (int x = 0; x < width; x++)
                {
                    byte b = rowData[pixelIndex++];
                    byte g = rowData[pixelIndex++];
                    byte r = rowData[pixelIndex++];
                    Color color = new Color(r, g, b, 255); // Default alpha is opaque
                    bitmap.SetPixel(x, height - y - 1, color);
                }
            }
        }

        private void Read8bppBitmap(BinaryReader reader, CustomBitmap bitmap, int width, int height, Color[] colorTable)
        {
            int rowSize = ((width + 3) / 4) * 4;
            byte[] rowData = new byte[rowSize];

            for (int y = 0; y < height; y++)
            {
                int readBytes = reader.Read(rowData, 0, rowSize);
                if (readBytes != rowSize)
                {
                    throw new Exception("Unexpected end of file while reading pixel data.");
                }

                for (int x = 0; x < width; x++)
                {
                    byte colorIndex = rowData[x];
                    Color color = colorTable[colorIndex];
                    color.A = 255; // Default alpha is opaque
                    bitmap.SetPixel(x, height - y - 1, color);
                }
            }
        }

        private void Read4bppBitmap(BinaryReader reader, CustomBitmap bitmap, int width, int height, Color[] colorTable)
        {
            int rowSize = ((width + 7) / 8) * 4;
            byte[] rowData = new byte[rowSize];

            for (int y = 0; y < height; y++)
            {
                int readBytes = reader.Read(rowData, 0, rowSize);
                if (readBytes != rowSize)
                {
                    throw new Exception("Unexpected end of file while reading pixel data.");
                }

                int pixelIndex = 0;
                for (int x = 0; x < width; x++)
                {
                    byte data = rowData[pixelIndex / 2];
                    int shift = (1 - (pixelIndex % 2)) * 4;
                    byte colorIndex = (byte)((data >> shift) & 0x0F);
                    Color color = colorTable[colorIndex];
                    color.A = 255; // Default alpha is opaque
                    bitmap.SetPixel(x, height - y - 1, color);
                    pixelIndex++;
                }
            }
        }

        private void Read1bppBitmap(BinaryReader reader, CustomBitmap bitmap, int width, int height, Color[] colorTable)
        {
            int rowSize = ((width + 31) / 32) * 4;
            byte[] rowData = new byte[rowSize];

            for (int y = 0; y < height; y++)
            {
                int readBytes = reader.Read(rowData, 0, rowSize);
                if (readBytes != rowSize)
                {
                    throw new Exception("Unexpected end of file while reading pixel data.");
                }

                int pixelIndex = 0;
                for (int x = 0; x < width; x++)
                {
                    byte data = rowData[pixelIndex / 8];
                    int shift = 7 - (pixelIndex % 8);
                    byte colorIndex = (byte)((data >> shift) & 0x01);
                    Color color = colorTable[colorIndex];
                    color.A = 255; // Default alpha is opaque
                    bitmap.SetPixel(x, height - y - 1, color);
                    pixelIndex++;
                }
            }
        }

        private void ReadAndMask(BinaryReader reader, CustomBitmap bitmap, int width, int height)
        {
            int andRowSize = ((width + 31) / 32) * 4;
            byte[] andRowData = new byte[andRowSize];

            for (int y = 0; y < height; y++)
            {
                int readBytes = reader.Read(andRowData, 0, andRowSize);
                if (readBytes != andRowSize)
                {
                    throw new Exception("Unexpected end of file while reading AND mask.");
                }

                int pixelIndex = 0;
                for (int x = 0; x < width; x++)
                {
                    byte data = andRowData[pixelIndex / 8];
                    int shift = 7 - (pixelIndex % 8);
                    byte maskBit = (byte)((data >> shift) & 0x01);
                    Color color = bitmap.GetPixel(x, height - y - 1);
                    if (maskBit == 1)
                    {
                        // Transparent pixel
                        color.A = 0;
                    }
                    else
                    {
                        // Opaque pixel
                        color.A = 255;
                    }
                    bitmap.SetPixel(x, height - y - 1, color);
                    pixelIndex++;
                }
            }
        }

        private bool IsPng(byte[] headerBytes)
        {
            byte[] pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47,
                                               0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngSignature.Length; i++)
            {
                if (headerBytes[i] != pngSignature[i])
                {
                    return false;
                }
            }
            return true;
        }

        // The rest of the methods (Read32bppBitmap, Read24bppBitmap, etc.) remain the same

        // Structures for ICO file format
        public struct ICONDIR
        {
            public ushort Reserved;
            public ushort Type;
            public ushort Count;
        }

        public struct ICONDIRENTRY
        {
            public byte Width;
            public byte Height;
            public byte ColorCount;
            public byte Reserved;
            public ushort Planes;
            public ushort BitCount;
            public uint BytesInRes;
            public uint ImageOffset;
        }

        public struct BITMAPINFOHEADER
        {
            public uint Size;
            public int Width;
            public int Height;
            public ushort Planes;
            public ushort BitCount;
            public uint Compression;
            public uint SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public uint ClrUsed;
            public uint ClrImportant;
        }
    }
}
