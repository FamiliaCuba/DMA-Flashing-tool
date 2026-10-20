using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace MakeIco
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("MakeIco src.png dst.ico");
                return;
            }
            Bitmap src = (Bitmap)Image.FromFile(args[0]);
            int[] sizes = new int[] { 256, 64, 48, 32, 16 };
            byte[][] blobs = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
                blobs[i] = (sizes[i] >= 256) ? ToPng(Resize(src, sizes[i])) : ToDib(Resize(src, sizes[i]));

            FileStream fs = File.Create(args[1]);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                bw.Write((byte)(s >= 256 ? 0 : s));
                bw.Write((byte)(s >= 256 ? 0 : s));
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write(blobs[i].Length);
                bw.Write(offset);
                offset += blobs[i].Length;
            }
            for (int i = 0; i < blobs.Length; i++) bw.Write(blobs[i]);
            bw.Flush();
            fs.Close();
            src.Dispose();
        }

        private static Bitmap Resize(Bitmap src, int size)
        {
            Bitmap b = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(b);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(src, 0, 0, size, size);
            g.Dispose();
            return b;
        }

        private static byte[] ToPng(Bitmap b)
        {
            MemoryStream ms = new MemoryStream();
            b.Save(ms, ImageFormat.Png);
            b.Dispose();
            return ms.ToArray();
        }

        private static byte[] ToDib(Bitmap b)
        {
            int w = b.Width, h = b.Height;
            BitmapData data = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int strideSrc = data.Stride;
            byte[] pix = new byte[h * w * 4];
            for (int y = 0; y < h; y++)
            {
                IntPtr row = new IntPtr(data.Scan0.ToInt64() + (long)(h - 1 - y) * strideSrc);
                System.Runtime.InteropServices.Marshal.Copy(row, pix, y * w * 4, w * 4);
            }
            b.UnlockBits(data);
            b.Dispose();
            int maskRow = ((w + 31) / 32) * 4;
            byte[] mask = new byte[maskRow * h];
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(40);
            bw.Write(w);
            bw.Write(h * 2);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write(0);
            bw.Write(pix.Length);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(pix);
            bw.Write(mask);
            bw.Flush();
            return ms.ToArray();
        }
    }
}
