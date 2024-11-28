using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitmapEncode.Images
{
    public struct Color
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public static Color FromBytes(byte[] data, int offset, PixelFormat depth)
        {
            return depth switch
            {
                PixelFormat.OneBit => new Color { R = data[offset] > 0 ? (byte)255 : (byte)0 },
                PixelFormat.EightBit => new Color { R = data[offset], G = data[offset], B = data[offset], A = 255 },
                PixelFormat.SixteenBit => new Color { R = data[offset], G = data[offset + 1], A = 255 },
                PixelFormat.TwentyFourBit => new Color { R = data[offset], G = data[offset + 1], B = data[offset + 2], A = 255 },
                PixelFormat.ThirtyTwoBit => new Color { R = data[offset], G = data[offset + 1], B = data[offset + 2], A = data[offset + 3] },
                _ => throw new NotSupportedException("Unsupported pixel format.")
            };
        }

        public byte[] ToBytes(PixelFormat depth)
        {
            return depth switch
            {
                PixelFormat.OneBit => new byte[] { (byte)(R > 127 ? 1 : 0) },
                PixelFormat.EightBit => new byte[] { R },
                PixelFormat.SixteenBit => new byte[] { R, G },
                PixelFormat.TwentyFourBit => new byte[] { R, G, B },
                PixelFormat.ThirtyTwoBit => new byte[] { R, G, B, A },
                _ => throw new NotSupportedException("Unsupported pixel format.")
            };
        }
    }
}
