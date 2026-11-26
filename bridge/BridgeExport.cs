using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object ExportWord(JObject req)
    {
        string path = req.Value<string>("path");
        string outPath = req.Value<string>("outPath") ?? Path.Combine(Path.GetDirectoryName(path), "specification.docx");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        using (var ctx = LoadContext(path))
        {
            FindMethod(T("SYLk.zYLC"), "CYJ1").Invoke(null, new object[] { ctx.Manager, outPath });
            return new { ok = true, path = outPath, exists = File.Exists(outPath),
                size = File.Exists(outPath) ? new FileInfo(outPath).Length : 0 };
        }
    }

    static object ExportPageImage(JObject req)
    {
        string path = req.Value<string>("path");
        string outPath = req.Value<string>("outPath");
        if (string.IsNullOrEmpty(outPath)) throw new ArgumentException("outPath required");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object diagram = GetProperty(page, "Diagram");
            TryInvoke(diagram, "uRW2", true);
            TryInvoke(page, "n0Km");
            object image = FindMethod(diagram.GetType(), "r0fQ").Invoke(diagram, new object[] { false, false, false, false });
            SaveAxureImage(image, outPath, req.Value<string>("format") ?? "png");
            return new { ok = true, page = req.Value<string>("page"), path = outPath,
                exists = File.Exists(outPath), size = File.Exists(outPath) ? new FileInfo(outPath).Length : 0 };
        }
    }

    static object ExportAllImages(JObject req)
    {
        string path = req.Value<string>("path");
        string outDir = req.Value<string>("outDir") ?? Path.Combine(Path.GetDirectoryName(path), "images");
        Directory.CreateDirectory(outDir);
        var files = new JArray();
        using (var ctx = LoadContext(path))
        {
            foreach (var page in EnumeratePages(ctx))
            {
                string name = GetProperty(page, "Name")?.ToString() ?? "page";
                string outPath = Path.Combine(outDir, SafeFileName(name) + "." + (req.Value<string>("format") ?? "png"));
                object diagram = GetProperty(page, "Diagram");
                TryInvoke(diagram, "uRW2", true);
                TryInvoke(page, "n0Km");
                object image = FindMethod(diagram.GetType(), "r0fQ").Invoke(diagram, new object[] { false, false, false, false });
                SaveAxureImage(image, outPath, req.Value<string>("format") ?? "png");
                files.Add(new JObject { ["page"] = name, ["path"] = outPath,
                    ["size"] = File.Exists(outPath) ? new FileInfo(outPath).Length : 0 });
            }
        }
        return new { ok = true, outDir, count = files.Count, files };
    }

    static void SaveAxureImage(object image, string outPath, string format)
    {
        var formatType = T("Axure.Platform.AxImageFormat");
        string formatName = format.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "Jpg",
            "gif" => "Gif",
            "bmp" => "Bmp",
            _ => "Png"
        };
        object formatValue = Enum.Parse(formatType, formatName);
        FindMethod(T("NO8X.pO8S"), "NOS1").Invoke(null, new object[] { image, outPath, formatValue });
        try { if (image is IDisposable d) d.Dispose(); } catch { }
    }

    static string SafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Trim().TrimEnd('.');
    }

    static object OptimizeImageFile(JObject req)
    {
        string input = req.Value<string>("inputPath");
        string output = req.Value<string>("outputPath") ?? input;
        if (!File.Exists(input)) throw new FileNotFoundException(input);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var stream = File.OpenRead(input);
        var imageType = T("gar0.gar9");
        object source = Activator.CreateInstance(imageType,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
            new object[] { stream, Path.GetExtension(input), null, true }, null);
        object optimized = null;
        string message = null;
        try
        {
            var optimize = imageType.Assembly.GetType("XGbv.EGby", true).GetMethod("OptimizeImage",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, new Type[] { imageType, typeof(object), typeof(string).MakeByRefType() }, null);
            object[] args = { source, input, null };
            optimized = optimize.Invoke(null, args);
            message = args[2]?.ToString();
        }
        catch { optimized = source; }
        if (optimized == null) optimized = source;
        try
        {
            if (!(bool)FindMethod(optimized.GetType(), "TarV").Invoke(optimized, null)) optimized = source;
        }
        catch { }
        Stream outStream = FindMethod(optimized.GetType(), "Oar3").Invoke(optimized, null) as Stream;
        if (outStream == null) throw new IOException("optimized image stream unavailable");
        outStream.Position = 0;
        using (var file = File.Create(output)) outStream.CopyTo(file);
        return new { ok = true, input, output, message, size = new FileInfo(output).Length };
    }

    static object ExportCsvReport(JObject req)
    {
        string path = req.Value<string>("path");
        string outPath = req.Value<string>("outPath") ?? Path.Combine(Path.GetDirectoryName(path), "report.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        using (var ctx = LoadContext(path))
        {
            // Built-in Y9n2 generator blocks waiting for a UI context; emit the
            // inventory directly from the object model instead.
            var sb = new StringBuilder();
            sb.AppendLine("type,name,id,index,text,x,y,w,h");
            int pageIndex = 0;
            foreach (var page in EnumeratePages(ctx))
            {
                string pageName = GetProperty(page, "Name")?.ToString() ?? "";
                sb.AppendLine($"Page,{Csv(pageName)},{SafeGet(page, "PackageId")},{pageIndex++},,,,,");
                int widgetIndex = 0;
                foreach (var widget in GetWidgets(page))
                {
                    object loc = SafeGet(widget, "Location");
                    object size = SafeGet(widget, "Size");
                    string text = SafeGet(widget, "Text")?.ToString() ?? "";
                    sb.AppendLine($"Widget,{Csv(pageName)}/{Csv(SafeGet(widget, "Name")?.ToString() ?? "")}," +
                        $"{SafeGet(widget, "PackageId")},{widgetIndex++},{Csv(text)}," +
                        $"{GetProp(loc, "X")},{GetProp(loc, "Y")},{GetProp(size, "Width")},{GetProp(size, "Height")}");
                }
            }
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(true));
            return new { ok = true, path = outPath, exists = File.Exists(outPath),
                size = new FileInfo(outPath).Length, rows = sb.ToString().Split('\n').Length - 1 };
        }
    }

    static string Csv(string value)
    {
        if (value == null) return "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    static object TryInvoke(object target, string name, params object[] args)
    {
        try { return FindMethod(target.GetType(), name).Invoke(target, args); }
        catch { return null; }
    }
}
