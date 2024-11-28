using BitmapEncode.BitmapVariants;
using BitmapEncode.Images.Interfaces;
using System.IO.Compression;
using System.Text;

namespace BitmapEncode.Images.Handlers
{
    public class PngImageHandler : IImageHandler
    {
        public IBitmap Load(string path, PixelFormat depth)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return Load(fs, depth);
            }
        }

        // New method to load from a Stream
        public IBitmap Load(Stream stream, PixelFormat depth)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                byte[] signature = reader.ReadBytes(8);
                byte[] expectedSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
                if (!CompareByteArrays(signature, expectedSignature))
                {
                    throw new Exception("Invalid PNG file signature.");
                }

                int width = 0;
                int height = 0;
                byte bitDepth = 0;
                byte colorType = 0;
                byte compressionMethod = 0;
                byte filterMethod = 0;
                byte interlaceMethod = 0;
                List<byte> compressedData = new List<byte>();

                bool hasIHDR = false;
                bool hasIEND = false;

                while (!hasIEND)
                {
                    uint chunkLength = ReadUInt32BigEndian(reader);
                    string chunkType = Encoding.ASCII.GetString(reader.ReadBytes(4));

                    byte[] chunkData = reader.ReadBytes((int)chunkLength);

                    uint crc = ReadUInt32BigEndian(reader);

                    switch (chunkType)
                    {
                        case "IHDR":
                            if (hasIHDR)
                            {
                                throw new Exception("Multiple IHDR chunks found.");
                            }
                            hasIHDR = true;

                            width = (int)ReadUInt32BigEndian(chunkData, 0);
                            height = (int)ReadUInt32BigEndian(chunkData, 4);
                            bitDepth = chunkData[8];
                            colorType = chunkData[9];
                            compressionMethod = chunkData[10];
                            filterMethod = chunkData[11];
                            interlaceMethod = chunkData[12];

                            if (interlaceMethod != 0)
                            {
                                throw new NotSupportedException("Interlaced PNG images are not supported.");
                            }

                            break;

                        case "IDAT":
                            compressedData.AddRange(chunkData);
                            break;

                        case "IEND":
                            hasIEND = true;
                            break;

                        default:
                            // Ignore other chunks
                            break;
                    }
                }

                // Decompress with ZLibStream
                byte[] imageData = DecompressData(compressedData.ToArray());

                int bytesPerPixel = GetBytesPerPixel(colorType, bitDepth);
                int bitsPerPixel = bytesPerPixel * 8;
                int stride = width * bytesPerPixel;

                byte[] reconstructedData = new byte[height * stride];

                int srcOffset = 0;
                int dstOffset = 0;

                for (int y = 0; y < height; y++)
                {
                    if (srcOffset >= imageData.Length)
                        break;

                    byte filterType = imageData[srcOffset++];
                    byte[] scanline = new byte[stride];

                    Array.Copy(imageData, srcOffset, scanline, 0, stride);
                    srcOffset += stride;

                    ApplyFilter(filterType, scanline, reconstructedData, dstOffset, bytesPerPixel, y > 0 ? reconstructedData : null, dstOffset - stride);

                    dstOffset += stride;
                }

                CustomBitmap bitmap = new CustomBitmap(width, height, depth);

                MapReconstructedDataToBitmap(reconstructedData, bitmap, colorType, bitDepth);

                return bitmap;
            }
        }

        private int GetBytesPerPixel(byte colorType, byte bitDepth)
        {
            int bitsPerPixel = 0;
            switch (colorType)
            {
                case 0: // Grayscale
                    bitsPerPixel = bitDepth;
                    break;
                case 2: // Truecolor
                    bitsPerPixel = bitDepth * 3;
                    break;
                case 3: // Indexed-color
                    bitsPerPixel = bitDepth;
                    break;
                case 4: // Grayscale with alpha
                    bitsPerPixel = bitDepth * 2;
                    break;
                case 6: // Truecolor with alpha
                    bitsPerPixel = bitDepth * 4;
                    break;
                default:
                    throw new NotSupportedException("Unsupported color type.");
            }
            return (bitsPerPixel + 7) / 8;
        }

        private void WriteChunk(BinaryWriter writer, string chunkType, byte[] data)
        {
            WriteUInt32BigEndian(writer, (uint)data.Length);
            byte[] chunkTypeBytes = Encoding.ASCII.GetBytes(chunkType);
            writer.Write(chunkTypeBytes);
            writer.Write(data);

            byte[] crcData = new byte[chunkTypeBytes.Length + data.Length];
            Array.Copy(chunkTypeBytes, 0, crcData, 0, chunkTypeBytes.Length);
            Array.Copy(data, 0, crcData, chunkTypeBytes.Length, data.Length);
            uint crc = Crc32(crcData);

            WriteUInt32BigEndian(writer, crc);
        }

        private void WriteUInt32BigEndian(BinaryWriter writer, uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            writer.Write(bytes);
        }

        private uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;

            foreach (byte b in data)
            {
                crc ^= b;
                for (int k = 0; k < 8; k++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc = crc >> 1;
                }
            }

            return ~crc;
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
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private bool CompareByteArrays(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i])
                    return false;
            return true;
        }

        private uint ReadUInt32BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (bytes.Length < 4)
                throw new EndOfStreamException();
            Array.Reverse(bytes); // Convert from big-endian to little-endian
            return BitConverter.ToUInt32(bytes, 0);
        }

        private uint ReadUInt32BigEndian(byte[] data, int offset)
        {
            byte[] bytes = new byte[4];
            Array.Copy(data, offset, bytes, 0, 4);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private byte[] DecompressData(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (System.IO.Compression.ZLibStream zlibStream = new System.IO.Compression.ZLibStream(ms, CompressionMode.Decompress))
                using (MemoryStream decompressed = new MemoryStream())
                {
                    zlibStream.CopyTo(decompressed);
                    return decompressed.ToArray();
                }
            }
        }

        private void ApplyFilter(byte filterType, byte[] scanline, byte[] reconstructedData, int offset, int bytesPerPixel, byte[] prevLine, int prevLineOffset)
        {
            switch (filterType)
            {
                case 0: // None
                    Array.Copy(scanline, 0, reconstructedData, offset, scanline.Length);
                    break;

                case 1: // Sub
                        // Recon(x) = Scanline(x) + Recon(x - bytesPerPixel)
                    for (int i = 0; i < scanline.Length; i++)
                    {
                        byte left = i >= bytesPerPixel ? reconstructedData[offset + i - bytesPerPixel] : (byte)0;
                        reconstructedData[offset + i] = (byte)(scanline[i] + left);
                    }
                    break;

                case 2: // Up
                        // Recon(x) = Scanline(x) + Recon_prev(x)
                    for (int i = 0; i < scanline.Length; i++)
                    {
                        byte up = prevLine != null ? reconstructedData[prevLineOffset + i] : (byte)0;
                        reconstructedData[offset + i] = (byte)(scanline[i] + up);
                    }
                    break;

                case 3: // Average
                        // Recon(x) = Scanline(x) + floor((Recon(x - bytesPerPixel) + Recon_prev(x)) / 2)
                    for (int i = 0; i < scanline.Length; i++)
                    {
                        byte left = i >= bytesPerPixel ? reconstructedData[offset + i - bytesPerPixel] : (byte)0;
                        byte up = prevLine != null ? reconstructedData[prevLineOffset + i] : (byte)0;
                        reconstructedData[offset + i] = (byte)(scanline[i] + ((left + up) / 2));
                    }
                    break;

                case 4: // Paeth
                        // Recon(x) = Scanline(x) + PaethPredictor(Recon(x - bytesPerPixel), Recon_prev(x), Recon_prev(x - bytesPerPixel))
                    for (int i = 0; i < scanline.Length; i++)
                    {
                        byte left = i >= bytesPerPixel ? reconstructedData[offset + i - bytesPerPixel] : (byte)0;
                        byte up = prevLine != null ? reconstructedData[prevLineOffset + i] : (byte)0;
                        byte upLeft = (i >= bytesPerPixel && prevLine != null) ? reconstructedData[prevLineOffset + i - bytesPerPixel] : (byte)0;

                        reconstructedData[offset + i] = (byte)(scanline[i] + PaethPredictor(left, up, upLeft));
                    }
                    break;

                default:
                    throw new Exception("Unsupported filter type: " + filterType);
            }
        }

        private byte PaethPredictor(byte a, byte b, byte c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc)
                return a;
            else if (pb <= pc)
                return b;
            else
                return c;
        }

        private void MapReconstructedDataToBitmap(byte[] data, CustomBitmap bitmap, byte colorType, byte bitDepth)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int bytesPerPixel = GetBytesPerPixel(colorType, bitDepth);
            int bitsPerPixel = bytesPerPixel * 8;
            int stride = (width * bitsPerPixel + 7) / 8;

            int offset = 0;

            for (int y = 0; y < height; y++)
            {
                int scanlineOffset = y * stride;

                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = scanlineOffset + x * bytesPerPixel;

                    Color color = new Color();

                    switch (colorType)
                    {
                        case 0: // Grayscale
                            byte gray = GetValueFromData(data, pixelOffset, bitDepth);
                            color = new Color { R = gray, G = gray, B = gray, A = 255 };
                            break;
                        case 2: // Truecolor
                            color.R = GetValueFromData(data, pixelOffset, bitDepth);
                            color.G = GetValueFromData(data, pixelOffset + bytesPerSample(bitDepth), bitDepth);
                            color.B = GetValueFromData(data, pixelOffset + 2 * bytesPerSample(bitDepth), bitDepth);
                            color.A = 255;
                            break;
                        case 3: // Indexed
                            throw new NotSupportedException("Indexed-color PNG images are not supported.");
                        case 4: // Grayscale with alpha
                            gray = GetValueFromData(data, pixelOffset, bitDepth);
                            byte alphaGray = GetValueFromData(data, pixelOffset + bytesPerSample(bitDepth), bitDepth);
                            color = new Color { R = gray, G = gray, B = gray, A = alphaGray };
                            break;
                        case 6: // Truecolor with alpha
                            color.R = GetValueFromData(data, pixelOffset, bitDepth);
                            color.G = GetValueFromData(data, pixelOffset + bytesPerSample(bitDepth), bitDepth);
                            color.B = GetValueFromData(data, pixelOffset + 2 * bytesPerSample(bitDepth), bitDepth);
                            color.A = GetValueFromData(data, pixelOffset + 3 * bytesPerSample(bitDepth), bitDepth);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported color type.");
                    }

                    bitmap.SetPixel(x, y, color);
                }
            }
        }

        private int bytesPerSample(byte bitDepth)
        {
            return (bitDepth + 7) / 8;
        }

        private byte GetValueFromData(byte[] data, int offset, byte bitDepth)
        {
            switch (bitDepth)
            {
                case 1:
                    {
                        int byteIndex = offset / 8;
                        int bitIndex = offset % 8;
                        return (byte)((data[byteIndex] >> (7 - bitIndex)) & 1);
                    }
                case 2:
                    {
                        int byteIndex = offset / 4;
                        int bitIndex = (offset % 4) * 2;
                        return (byte)((data[byteIndex] >> (6 - bitIndex)) & 3);
                    }
                case 4:
                    {
                        int byteIndex = offset / 2;
                        if (offset % 2 == 0)
                            return (byte)((data[byteIndex] >> 4) & 0x0F);
                        else
                            return (byte)(data[byteIndex] & 0x0F);
                    }
                case 8:
                    return data[offset];
                case 16:
                    return data[offset]; // For simplicity, we take the high byte only
                default:
                    throw new NotSupportedException("Unsupported bit depth.");
            }
        }
    }
}