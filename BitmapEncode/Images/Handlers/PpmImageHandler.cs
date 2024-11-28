using BitmapEncode.BitmapVariants;
using BitmapEncode.Images.Interfaces;

namespace BitmapEncode.Images.Handlers
{
    public class PpmImageHandler : IImageHandler
    {
        public IBitmap Load(string path, PixelFormat depth)
        {
            using var reader = new StreamReader(path);

            // Чтение заголовка
            string magicNumber = reader.ReadLine();
            if (magicNumber != "P3" && magicNumber != "P6")
                throw new InvalidDataException("Unsupported PPM format. Only P3 (text) and P6 (binary) are supported.");

            bool isBinary = magicNumber == "P6";

            // Пропускаем комментарии
            string line;
            do
            {
                line = reader.ReadLine();
            } while (line.StartsWith("#"));

            // Чтение размеров
            var dimensions = line.Split(' ');
            int width = int.Parse(dimensions[0]);
            int height = int.Parse(dimensions[1]);

            int maxColorValue = int.Parse(reader.ReadLine());
            if (maxColorValue != 255)
                throw new NotSupportedException("Only 8-bit color depth is supported.");

            var customBitmap = new CustomBitmap(width, height, PixelFormat.TwentyFourBit);

            if (isBinary)
            {
                LoadBinaryData(reader.BaseStream, customBitmap, width, height);
            }
            else
            {
                LoadTextData(reader, customBitmap, width, height);
            }

            return customBitmap;
        }

        private void LoadBinaryData(Stream stream, CustomBitmap bitmap, int width, int height)
        {
            using var binaryReader = new BinaryReader(stream);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte r = binaryReader.ReadByte();
                    byte g = binaryReader.ReadByte();
                    byte b = binaryReader.ReadByte();
                    bitmap.SetPixel(x, y, new Color { R = r, G = g, B = b, A = 255 });
                }
            }
        }

        private void LoadTextData(StreamReader reader, CustomBitmap bitmap, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = int.Parse(reader.ReadLine());
                    int g = int.Parse(reader.ReadLine());
                    int b = int.Parse(reader.ReadLine());
                    bitmap.SetPixel(x, y, new Color { R = (byte)r, G = (byte)g, B = (byte)b, A = 255 });
                }
            }
        }

        public void Save(IBitmap bitmap, string path)
        {
            bool isBinary = Path.GetExtension(path).Equals(".ppm", StringComparison.OrdinalIgnoreCase);

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);

            // Запись заголовка
            writer.WriteLine(isBinary ? "P6" : "P3");
            writer.WriteLine($"{bitmap.Width} {bitmap.Height}");
            writer.WriteLine("255"); // Максимальное значение цвета

            if (isBinary)
            {
                SaveBinaryData(bitmap, stream);
            }
            else
            {
                SaveTextData(bitmap, writer);
            }
        }

        private void SaveBinaryData(IBitmap bitmap, Stream stream)
        {
            using var binaryWriter = new BinaryWriter(stream);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    binaryWriter.Write(color.R);
                    binaryWriter.Write(color.G);
                    binaryWriter.Write(color.B);
                }
            }
        }

        private void SaveTextData(IBitmap bitmap, StreamWriter writer)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    writer.WriteLine(color.R);
                    writer.WriteLine(color.G);
                    writer.WriteLine(color.B);
                }
            }
        }
    }
}
