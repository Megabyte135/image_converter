namespace BitmapEncode.Images
{
    public struct Color
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public Color()
            : this(0, 0, 0, 0)
        {
            
        }

        public Color(byte r, byte g, byte b)
            : this(r, g, b, 0)
        {
            
        }

        public Color(byte r, byte g, byte b, byte a)
        {
            R = r; G = g; B = b; A = a;
        }
    }
}
