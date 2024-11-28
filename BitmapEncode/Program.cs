using BitmapEncode.BitmapVariants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitmapEncode
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CustomBitmap customBitmap = new(809, 715, Images.PixelFormat.SixteenBit);

            customBitmap.FromImage("C:\\Users\\Yemeth\\Desktop\\images\\LIBRARIAN.png");
            customBitmap.ToImage("C:\\Users\\Yemeth\\Desktop\\images\\Flopping.png");
        }
    }
}
