using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object ExportPdf(JObject req)
    {
        string path = req.Value<string>("path");
        string outPath = req.Value<string>("outPath") ?? Path.Combine(Path.GetDirectoryName(path), "prototype.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        var images = new List<string>();
        try
        {
            using (var ctx = LoadContext(path))
            {
                IEnumerable<object> pages;
                if (!string.IsNullOrEmpty(req.Value<string>("page")))
                {
                    var page = FindPage(ctx, req.Value<string>("page"));
                    if (page == null) throw new ArgumentException("page not found");
                    pages = new[] { page };
                }
                else pages = EnumeratePages(ctx).ToList();

                int i = 0;
                foreach (var page in pages)
                {
                    string temp = Path.Combine(Path.GetTempPath(), "axure-pdf-" + Guid.NewGuid().ToString("N") + ".png");
                    object diagram = GetProperty(page, "Diagram");
                    TryInvoke(diagram, "uRW2", true);
                    TryInvoke(page, "n0Km");
                    object image = FindMethod(diagram.GetType(), "r0fQ").Invoke(diagram, new object[] { false, false, false, false });
                    SaveAxureImage(image, temp, "png");
                    images.Add(temp);
                    i++;
                }
            }
            WriteImagePdf(images, outPath);
            return new { ok = true, path = outPath, exists = File.Exists(outPath),
                size = File.Exists(outPath) ? new FileInfo(outPath).Length : 0, pages = images.Count };
        }
        finally
        {
            foreach (var image in images) try { File.Delete(image); } catch { }
        }
    }

    static void WriteImagePdf(List<string> imagePaths, string outPath)
    {
        var objects = new Dictionary<int, byte[]>();
        var pageSizes = new List<(double w, double h)>();
        int pageCount = imagePaths.Count;
        var kids = new List<string>();
        for (int i = 0; i < pageCount; i++) kids.Add($"{3 + i * 3} 0 R");
        objects[1] = Enc("<< /Type /Catalog /Pages 2 0 R >>");
        objects[2] = Enc($"<< /Type /Pages /Kids [{string.Join(" ", kids)}] /Count {pageCount} >>");

        for (int i = 0; i < pageCount; i++)
        {
            int pageObj = 3 + i * 3;
            int imageObj = pageObj + 1;
            int contentObj = pageObj + 2;
            byte[] rgb;
            int width, height;
            using (var bmp = new Bitmap(imagePaths[i]))
            {
                width = bmp.Width;
                height = bmp.Height;
                rgb = BitmapToRgb(bmp);
            }
            double scale = Math.Min(595.0 / width, 842.0 / height);
            double pageW = width * scale;
            double pageH = height * scale;
            pageSizes.Add((pageW, pageH));

            objects[pageObj] = Enc($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(pageW)} {F(pageH)}] " +
                $"/Resources << /XObject << /Im0 {imageObj} 0 R >> >> /Contents {contentObj} 0 R >>");
            using (var compressed = new MemoryStream())
            {
                using (var deflater = new DeflaterOutputStream(compressed)) deflater.Write(rgb, 0, rgb.Length);
                byte[] imageData = compressed.ToArray();
                string dict = $"<< /Type /XObject /Subtype /Image /Width {width} /Height {height} " +
                    $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {imageData.Length} >>\nstream\n";
                objects[imageObj] = Enc(dict).Concat(imageData).Concat(Enc("\nendstream")).ToArray();
            }
            string content = $"q\n{F(pageW)} 0 0 {F(pageH)} 0 0 cm\n/Im0 Do\nQ\n";
            byte[] contentBytes = Enc(content);
            objects[contentObj] = Enc($"<< /Length {contentBytes.Length} >>\nstream\n").Concat(contentBytes).Concat(Enc("\nendstream")).ToArray();
        }

        using (var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(Enc("%PDF-1.4\n"));
            var offsets = new long[objects.Count + 1];
            for (int i = 1; i <= objects.Count; i++)
            {
                offsets[i] = fs.Position;
                bw.Write(Enc($"{i} 0 obj\n"));
                bw.Write(objects[i]);
                bw.Write(Enc("\nendobj\n"));
            }
            long xref = fs.Position;
            bw.Write(Enc($"xref\n0 {objects.Count + 1}\n"));
            bw.Write(Enc("0000000000 65535 f \n"));
            for (int i = 1; i <= objects.Count; i++) bw.Write(Enc($"{offsets[i]:D10} 00000 n \n"));
            bw.Write(Enc($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"));
        }
    }

    static byte[] BitmapToRgb(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var rgb = new byte[bmp.Width * bmp.Height * 3];
        try
        {
            var row = new byte[bmp.Width * 4];
            int pos = 0;
            for (int y = 0; y < bmp.Height; y++)
            {
                MarshalCopy(data.Scan0, y * data.Stride, row, 0, row.Length);
                for (int x = 0; x < bmp.Width; x++)
                {
                    int b = row[x * 4], g = row[x * 4 + 1], r = row[x * 4 + 2], a = row[x * 4 + 3];
                    rgb[pos++] = (byte)((r * a + 255 * (255 - a)) / 255);
                    rgb[pos++] = (byte)((g * a + 255 * (255 - a)) / 255);
                    rgb[pos++] = (byte)((b * a + 255 * (255 - a)) / 255);
                }
            }
        }
        finally { bmp.UnlockBits(data); }
        return rgb;
    }

    static void MarshalCopy(IntPtr src, int srcOffset, byte[] dst, int dstOffset, int count)
        => System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(src, srcOffset), dst, dstOffset, count);

    static byte[] Enc(string text) => Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
    static string F(double value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
