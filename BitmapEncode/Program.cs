using BitmapEncode.BitmapVariants;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitmapEncode
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string inputPath = "C:\\Users\\Yemeth\\Desktop\\images\\LIBRARIAN.ppm";
            string outputPath = "C:\\Users\\Yemeth\\Desktop\\images\\Flopping.bmp";
            
            CustomBitmap customBitmap = new(inputPath, Images.PixelFormat.SixteenBit);
            customBitmap.ToImage(outputPath);
        }
    }
}
