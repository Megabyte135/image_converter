using BitmapEncode.BitmapVariants;
using BitmapEncode.Images;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitmapEncode
{
    public class BitmapText<TBitmap>
                where TBitmap : IBitmap, new()
    {
        private int _width;
        private int _height;
        public Color[,] Pixels;

        public BitmapText(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public BitmapText(IBitmap bitmap) : this(bitmap.Width, bitmap.Height)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
        }

        public TBitmap ToBitmap()
        {
            TBitmap bitmap = new TBitmap();
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    bitmap.SetPixel(x, y, Pixels[x, y]);
                }
            }
            return bitmap;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{_width} {_height}");

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Color color = Pixels[x, y];
                    sb.Append($"{color.R},{color.G},{color.B} ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }


        public static BitmapText<TBitmap> FromString(string text)
        {
            string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            string[] size = lines[0].Split(' ');
            int width = int.Parse(size[0]);
            int height = int.Parse(size[1]);

            var bitmapText = new BitmapText<TBitmap>(width, height);

            for (int y = 0; y < height; y++)
            {
                string[] pixelValues = lines[y + 1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < width; x++)
                {
                    string[] rgb = pixelValues[x].Split(',');
                    byte r = byte.Parse(rgb[0]);
                    byte g = byte.Parse(rgb[1]);
                    byte b = byte.Parse(rgb[2]);
                    bitmapText.Pixels[x, y] = new Color(r, g, b);
                }
            }

            return bitmapText;
        }
    }
}
