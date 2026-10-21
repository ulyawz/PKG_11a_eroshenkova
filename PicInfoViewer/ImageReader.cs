using System;
using System.IO;

namespace PicInfoViewer;

public class ImageRow
{
    public string Name { get; set; } = "";
    public string Format { get; set; } = "";
    public string Dimensions { get; set; } = "—";
    public string Resolution { get; set; } = "—";
    public string ColorDepth { get; set; } = "—";
    public string Compression { get; set; } = "—";
    public string FullPath { get; set; } = "";

    public static class ImageReader
    {
        const string DefaultResolution = "96 (по умолч.)";

        static uint ReadNumber(Stream s, int n, bool big)
        {
            uint v = 0;
            for (int i = 0; i < n; i++)
            {
                int b = s.ReadByte();
                if (b < 0) throw new EndOfStreamException();

                if (big) v = (v << 8) | (uint)b;
                else v |= (uint)b << (8 * i);
            }
            return v;
        }

        public static ImageRow Read(string path)
        {
            var r = new ImageRow
            {
                Name = Path.GetFileName(path),
                FullPath = path
            };

            try
            {
                using var s = new FileStream(
                    path, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 4096, FileOptions.RandomAccess);

                var h = new byte[8];
                s.Read(h, 0, 8);

                s.Position = 0;

                if (h[0] == 0x89 && h[1] == 0x50) ReadPng(s, r);
                else if (h[0] == 0xFF && h[1] == 0xD8) ReadJpeg(s, r);
                else if (h[0] == 'G' && h[1] == 'I') ReadGif(s, r);
                else if (h[0] == 'B' && h[1] == 'M') ReadBmp(s, r);
                else if ((h[0] == 'I' && h[1] == 'I') ||
                         (h[0] == 'M' && h[1] == 'M'))
                    ReadTiff(s, r, h[0] == 'M');
                else if (h[0] == 0x0A) ReadPcx(s, r);
                else r.Format = "не изображение";

                if (r.Format != "не изображение" && r.Resolution == "—")
                    r.Resolution = DefaultResolution;
            }
            catch (Exception ex)
            {
                r.Compression = "ошибка: " + ex.Message;
            }

            return r;
        }

        static void ReadPng(Stream s, ImageRow r)
        {
            r.Format = "PNG";

            s.Position = 16;
            uint w = ReadNumber(s, 4, true), h = ReadNumber(s, 4, true);

            int bits = s.ReadByte(), colorType = s.ReadByte();
            int channels = colorType switch
            {
                0 => 1,
                2 => 3,
                3 => 1,
                4 => 2,
                _ => 4
            };

            r.Dimensions = $"{w} × {h}";
            r.ColorDepth = $"{bits * channels} бит";
            r.Compression = "Deflate";

            s.Position = 33;
            while (true)
            {
                uint len = ReadNumber(s, 4, true), type = ReadNumber(s, 4, true);

                if (type == 0x70485973)
                {
                    uint px = ReadNumber(s, 4, true);
                    ReadNumber(s, 4, true);

                    if (s.ReadByte() == 1)
                        r.Resolution = (px * 0.0254).ToString("0.##");

                    break;
                }

                if (type == 0x49444154 || type == 0x49454E44) break;

                s.Position += len + 4;
            }
        }

        static void ReadJpeg(Stream s, ImageRow r)
        {
            r.Format = "JPEG";
            s.Position = 2;

            while (true)
            {
                if (s.ReadByte() != 0xFF)
                    throw new Exception("нарушена структура");

                int m = s.ReadByte();
                while (m == 0xFF) m = s.ReadByte();

                if (m < 0) throw new EndOfStreamException();

                if (m == 0xD8 || m == 0x01 || (m >= 0xD0 && m <= 0xD7))
                    continue;

                int len = (int)ReadNumber(s, 2, true);
                long end = s.Position + len - 2;

                if (m == 0xE0 && len >= 16)
                {
                    s.Position += 7;
                    int units = s.ReadByte();
                    uint x = ReadNumber(s, 2, true);

                    if (units == 1) r.Resolution = x.ToString();
                    else if (units == 2)
                        r.Resolution = (x * 2.54).ToString("0.##");
                }
                else if (m >= 0xC0 && m <= 0xCF &&
                         m != 0xC4 && m != 0xC8 && m != 0xCC)
                {
                    int prec = s.ReadByte();
                    uint h = ReadNumber(s, 2, true);
                    uint w = ReadNumber(s, 2, true);
                    int comps = s.ReadByte();

                    r.Dimensions = $"{w} × {h}";
                    r.ColorDepth = $"{prec * comps} бит";

                    r.Compression = m == 0xC0
                        ? "Baseline DCT (Huffman)"
                        : m == 0xC2
                            ? "Progressive DCT (Huffman)"
                            : $"JPEG SOF{m - 0xC0}";

                    return;
                }

                s.Position = end;
            }
        }

        static void ReadGif(Stream s, ImageRow r)
        {
            r.Format = "GIF";
            s.Position = 6;

            uint w = ReadNumber(s, 2, false), h = ReadNumber(s, 2, false);
            int packed = s.ReadByte();

            r.Dimensions = $"{w} × {h}";
            r.ColorDepth = $"{(packed & 7) + 1} бит";
            r.Compression = "LZW";
        }

        static void ReadBmp(Stream s, ImageRow r)
        {
            r.Format = "BMP";
            s.Position = 18;

            int w = (int)ReadNumber(s, 4, false);
            int h = (int)ReadNumber(s, 4, false);

            ReadNumber(s, 2, false);
            uint bpp = ReadNumber(s, 2, false);
            uint comp = ReadNumber(s, 4, false);

            ReadNumber(s, 4, false);
            int xppm = (int)ReadNumber(s, 4, false);

            r.Dimensions = $"{w} × {Math.Abs(h)}";
            r.ColorDepth = $"{bpp} бит";

            if (xppm > 0)
                r.Resolution = (xppm * 0.0254).ToString("0.##");

            r.Compression = comp switch
            {
                0 => "BI_RGB (без сжатия)",
                1 => "BI_RLE8",
                2 => "BI_RLE4",
                3 => "BI_BITFIELDS",
                _ => $"код {comp}"
            };
        }

        static void ReadTiff(Stream s, ImageRow r, bool big)
        {
            r.Format = "TIFF";

            s.Position = 4;
            s.Position = ReadNumber(s, 4, big);
            int n = (int)ReadNumber(s, 2, big);

            uint w = 0, h = 0, bps = 1, spp = 1, comp = 1, unit = 2;
            double xr = 0;

            for (int i = 0; i < n; i++)
            {
                int tag = (int)ReadNumber(s, 2, big);
                int type = (int)ReadNumber(s, 2, big);
                uint cnt = ReadNumber(s, 4, big);

                long vp = s.Position;
                uint v = ReadNumber(s, type == 3 ? 2 : 4, big);
                s.Position = vp + 4;

                switch (tag)
                {
                    case 256: w = v; break;
                    case 257: h = v; break;
                    case 259: comp = v; break;
                    case 277: spp = v; break;
                    case 296: unit = v; break;

                    case 258:
                        if (cnt <= 2) bps = v;
                        else
                        {
                            s.Position = v;
                            bps = ReadNumber(s, 2, big);
                            s.Position = vp + 4;
                        }
                        break;

                    case 282:
                        s.Position = v;
                        uint num = ReadNumber(s, 4, big);
                        uint den = ReadNumber(s, 4, big);
                        xr = den == 0 ? 0 : (double)num / den;
                        s.Position = vp + 4;
                        break;
                }
            }

            r.Dimensions = $"{w} × {h}";
            r.ColorDepth = $"{bps * spp} бит";

            if (xr > 0)
                r.Resolution = (unit == 3 ? xr * 2.54 : xr).ToString("0.##");

            r.Compression = comp switch
            {
                1 => "без сжатия",
                2 => "CCITT RLE",
                3 => "CCITT Group 3",
                4 => "CCITT Group 4",
                5 => "LZW",
                6 or 7 => "JPEG",
                8 or 32946 => "Deflate",
                32773 => "PackBits",
                _ => $"код {comp}"
            };
        }

        static void ReadPcx(Stream s, ImageRow r)
        {
            r.Format = "PCX";
            s.Position = 3;

            int bpp = s.ReadByte();
            int xmin = (int)ReadNumber(s, 2, false);
            int ymin = (int)ReadNumber(s, 2, false);
            int xmax = (int)ReadNumber(s, 2, false);
            int ymax = (int)ReadNumber(s, 2, false);
            uint dpi = ReadNumber(s, 2, false);

            s.Position = 65;
            int planes = s.ReadByte();

            r.Dimensions = $"{xmax - xmin + 1} × {ymax - ymin + 1}";
            r.ColorDepth = $"{bpp * planes} бит";

            if (dpi > 0) r.Resolution = dpi.ToString();

            r.Compression = "RLE";
        }
    }
}