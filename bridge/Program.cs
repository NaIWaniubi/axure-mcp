using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;

class Program
{
    static Assembly asm;
    static string dir = @"D:\Program Files (x86)\Axure\Axure RP 9";

    [STAThread]
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string name = new AssemblyName(e.Name).Name + ".dll";
            string p = Path.Combine(dir, name);
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try
        {
            asm = Assembly.LoadFrom(Path.Combine(dir, "AxureRP9.exe"));
            Console.WriteLine("LOADED " + asm.FullName);
            string mode = args.Length > 0 ? args[0] : "list";
            if (mode == "poc") PocCreate();
            else if (mode == "load") PocLoad(args[1]);
            else if (mode == "newproject") PocNewProject(args[1], args[2]);
            else if (mode == "addpage") PocAddPage(args[1], args[2]);
            else if (mode == "widgets") PocWidgets(args[1], args[2]);
            else if (mode == "addwidget") PocAddWidget(args[1], args[2], args[3], double.Parse(args[4]), double.Parse(args[5]), double.Parse(args[6]), double.Parse(args[7]), args[8]);
            else ListTypes(args.Length > 1 ? args[1].ToLowerInvariant() : "");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR " + ex);
            Environment.ExitCode = 1;
        }
    }

    static Type T(string name) => asm.GetType(name, throwOnError: true);

    static void InitGlobals()
    {
        T("aadD.aadq").GetMethod("iad5").Invoke(null, new object[] { Activator.CreateInstance(T("I0HE.s0Hr")) });
        T("oPc9.hPcp").GetMethod("kPc0").Invoke(null, null);
        var info = Activator.CreateInstance(T("UPVa.BPV7"));
        T("o7RZ.L7Rm").GetMethod("v7RL").Invoke(null, new object[] { info });
        var settings = Activator.CreateInstance(T("nPji.CPjA"));
        var platform = settings.GetType().GetMethod("tf68").Invoke(settings, null);
        string tempDir = Path.Combine(Path.GetTempPath(), "axure-bridge-tmp");
        Directory.CreateDirectory(tempDir);
        var setTemp = platform.GetType().BaseType.GetMethod("tO8b", BindingFlags.NonPublic | BindingFlags.Instance);
        setTemp.Invoke(platform, new object[] { tempDir });
        T("o7RZ.L7Rm").GetMethod("M7RJ").Invoke(null, new object[] { settings });
    }

    static void PocCreate()
    {
        InitGlobals();
        var mgrT = T("m0Sq.A0St");
        var docT = T("Q0IK.W0Iu");
        var mgr = Activator.CreateInstance(mgrT);
        var standard = Enum.Parse(T("Axure.Platform.ObjectPersistanceContext.DesignDocumentType"), "Standard");
        docT.GetMethod("b0Iv", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { mgr, standard, "PoC_Project" });
        var doc = mgrT.GetMethod("F0Ba").Invoke(mgr, null);
        Console.WriteLine("DOC " + doc.GetType().FullName);
        DumpPages(mgr, doc);
    }

    static void PocAddPage(string path, string pageName)
    {
        InitGlobals();
        var mgrT = T("m0Sq.A0St");
        var mgr = Activator.CreateInstance(mgrT);
        var access = Enum.Parse(T("Axure.IO.AccessMode"), "ReadWrite");
        object[] parameters = { path, null, true, access, true };
        if (!(bool)mgrT.GetMethod("Load").Invoke(mgr, parameters)) { Console.WriteLine("LOAD FAILED " + parameters[1]); return; }
        var doc = mgrT.GetMethod("F0Ba").Invoke(mgr, null);
        var pageId = Guid.NewGuid();
        // 1) 活页面
        var cache = doc.GetType().GetMethod("NIkS").Invoke(doc, null);
        var page = Activator.CreateInstance(T("W0u2.m0uQ"), new object[] { doc, cache });
        page.GetType().GetMethod("UayB").Invoke(page, new object[] { pageId });
        // 2) 先建元数据并注册，Name setter 才能通过 Eavp 找到它
        var meta = Activator.CreateInstance(T("paEo.WaEV"), new object[] { doc });
        meta.GetType().GetProperty("PackageId").SetValue(meta, pageId);
        meta.GetType().GetMethod("Day7").Invoke(meta, new object[] { "Axure:Page" });
        meta.GetType().GetMethod("oayM").Invoke(meta, new object[] { pageName });
        doc.GetType().GetMethod("lalu").Invoke(doc, new object[] { meta });
        page.GetType().GetProperty("Name").SetValue(page, pageName);
        // 3) Sitemap 节点
        var sitemap = doc.GetType().GetProperty("Sitemap").GetValue(doc);
        var tree = sitemap.GetType().GetProperty("Tree").GetValue(sitemap);
        var node = sitemap.GetType().GetMethod("I0Hg").Invoke(sitemap, new object[] { null, tree, 0, pageId });
        // 页面节点直接关联 Guid；p03X 仅用于文件夹节点
        // 4) 注册活页面
        mgrT.GetMethod("c0XR").Invoke(mgr, new object[] { page, null });
        // 5) 序列化页面并 gzip 写入存储
        var pageStream = T("jaqN.taqY").GetMethod("IaqR").Invoke(null, new object[] { page, false }) as Stream;
        var storage = mgrT.GetMethod("N0B9").Invoke(mgr, null);
        var dest = storage.GetType().GetMethod("Create", new Type[] { T("zayC.vayw") }).Invoke(storage, new object[] { page }) as Stream;
        using (var gz = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(dest)) { pageStream.Position = 0; pageStream.CopyTo(gz); }
        dest.Flush();
        // 6) 保存
        mgrT.GetMethod("Save").Invoke(mgr, null);
        try { ((IDisposable)mgr).Dispose(); } catch { }
        Console.WriteLine("ADDED PAGE " + pageName + " id=" + pageId + " saved=" + File.Exists(path));
    }

    static void PocNewProject(string output, string name)
    {
        InitGlobals();
        var mgrT = T("m0Sq.A0St");
        var mgr = Activator.CreateInstance(mgrT);
        var standard = Enum.Parse(T("Axure.Platform.ObjectPersistanceContext.DesignDocumentType"), "Standard");
        T("Q0IK.W0Iu").GetMethod("b0Iv", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { mgr, standard, name });
        mgrT.GetProperty("FileName").SetValue(mgr, output);
        var storage = mgrT.GetMethod("N0B9").Invoke(mgr, null);
        storage.GetType().GetProperty("Location").SetValue(storage, output);
        mgrT.GetMethod("Save").Invoke(mgr, null);
        try { ((IDisposable)mgr).Dispose(); } catch { }
        Console.WriteLine("NEW PROJECT exists=" + File.Exists(output) + " size=" + (File.Exists(output) ? new FileInfo(output).Length : 0));
    }

    static void PocLoad(string path)
    {
        InitGlobals();
        var mgrT = T("m0Sq.A0St");
        var mgr = Activator.CreateInstance(mgrT);
        var access = Enum.Parse(T("Axure.IO.AccessMode"), "ReadWrite");
        object[] parameters = { path, null, true, access, true };
        Console.WriteLine("loading " + path);
        bool ok = (bool)mgrT.GetMethod("Load").Invoke(mgr, parameters);
        Console.WriteLine("LOAD=" + ok + " error=" + parameters[1]);
        if (!ok) return;
        var doc = mgrT.GetMethod("F0Ba").Invoke(mgr, null);
        Console.WriteLine("DOC " + doc.GetType().FullName);
        DumpPages(mgr, doc);
        DumpSitemap(mgr, doc);
    }

    static void DumpSitemap(object mgr, object doc)
    {
        var sitemap = doc.GetType().GetProperty("Sitemap").GetValue(doc);
        if (sitemap == null) return;
        var tree = sitemap.GetType().GetProperty("Tree").GetValue(sitemap);
        DumpNode(mgr, sitemap, tree, 0);
    }

    static void DumpNode(object mgr, object sitemap, object node, int depth)
    {
        if (node == null) return;
        var t = node.GetType();
        string name = "";
        try { var np = t.GetProperty("Name"); if (np != null) name = np.GetValue(node)?.ToString() ?? ""; } catch { }
        object raw = null;
        try { raw = sitemap.GetType().GetMethod("s0Hx").Invoke(sitemap, new object[] { node }); } catch { }
        string desc = raw?.GetType().FullName ?? "null";
        if (raw is Guid id)
        {
            desc += ":" + id;
            try
            {
                var doc = mgr.GetType().GetMethod("F0Ba").Invoke(mgr, null);
                var meta = doc.GetType().GetMethod("Vall").Invoke(doc, new object[] { id });
                if (meta != null)
                    desc += " metaType=" + meta.GetType().GetMethod("AayO").Invoke(meta, null) + " metaName=" + meta.GetType().GetMethod("Fay6").Invoke(meta, null);
            }
            catch { }
            try
            {
                var live = mgr.GetType().GetMethod("k0Xc").Invoke(mgr, new object[] { id, true, null, Guid.Empty, null });
                desc += " live=" + (live?.GetType().FullName ?? "null");
                if (live != null)
                {
                    try
                    {
                        var nameP = live.GetType().GetProperty("Name");
                        var objsP = live.GetType().GetProperty("DiagramObjects");
                        desc += " name=" + (nameP?.GetValue(live) ?? "?");
                        if (objsP != null)
                        {
                            var objs = objsP.GetValue(live) as IEnumerable;
                            int n = 0; if (objs != null) foreach (var o in objs) n++;
                            desc += " objects=" + n;
                        }
                    }
                    catch (Exception ex) { desc += " infoERR=" + ex.Message; }
                }
            }
            catch (Exception ex) { desc += " liveERR=" + ex.Message; }
        }
        Console.WriteLine(new string(' ', depth * 2) + "NODE " + t.Name + " name=" + name + " assoc=" + desc);
        var children = t.GetProperty("Children");
        if (children != null)
        {
            var list = children.GetValue(node) as IEnumerable;
            if (list != null) foreach (var c in list) DumpNode(mgr, sitemap, c, depth + 1);
        }
    }

    static void PocAddWidget(string path, string pageName, string text, double x, double y, double w, double h, string output)
    {
        InitGlobals();
        var mgrT = T("m0Sq.A0St");
        var mgr = Activator.CreateInstance(mgrT);
        var access = Enum.Parse(T("Axure.IO.AccessMode"), "ReadWrite");
        object[] parameters = { path, null, true, access, true };
        if (!(bool)mgrT.GetMethod("Load").Invoke(mgr, parameters))
        {
            Console.WriteLine("LOAD FAILED " + parameters[1]); return;
        }
        var doc = mgrT.GetMethod("F0Ba").Invoke(mgr, null);
        var page = FindPage(mgr, doc, pageName);
        if (page == null) { Console.WriteLine("PAGE NOT FOUND " + pageName); return; }

        // 参照现有 VectorShape 的 Location/Size 类型来构造
        var objs = page.GetType().GetProperty("DiagramObjects").GetValue(page) as IEnumerable;
        object sample = null;
        if (objs != null) foreach (var o in objs)
        {
            if (o.GetType().FullName == "Axure.DocumentModel.Objects.VectorShape") { sample = o; break; }
        }
        var vsTypeForGeom = T("Axure.DocumentModel.Objects.VectorShape");
        var locType = sample != null ? sample.GetType().GetProperty("Location").PropertyType : vsTypeForGeom.GetProperty("Location").PropertyType;
        var sizeType = sample != null ? sample.GetType().GetProperty("Size").PropertyType : vsTypeForGeom.GetProperty("Size").PropertyType;
        var loc = Activator.CreateInstance(locType, new object[] { x, y });
        var size = Activator.CreateInstance(sizeType, new object[] { w, h });

        object widget;
        if (sample != null)
        {
            var copyMethod = sample.GetType().GetMethod("Copy", new Type[] { T("zayC.vayw") });
            widget = copyMethod.Invoke(sample, new object[] { page });
        }
        else
        {
            var vsType = T("Axure.DocumentModel.Objects.VectorShape");
            var ctor = vsType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            var ps = ctor.GetParameters();
            var shape = Enum.Parse(ps[1].ParameterType, "Rectangle");
            var legacy = Enum.Parse(ps[3].ParameterType, "None");
            widget = ctor.Invoke(new object[] { page, shape, text, legacy, true });
        }
        widget.GetType().GetProperty("Text").SetValue(widget, text);
        widget.GetType().GetMethod("l0or").Invoke(widget, new object[] { loc, false, true });
        widget.GetType().GetMethod("L0ob").Invoke(widget, new object[] { size, false, true });
        // 直接加入 DiagramObjects（该页面无根 Layer），再触发图表失效
        var diagram = page.GetType().GetProperty("Diagram").GetValue(page);
        var coll = diagram.GetType().GetProperty("DiagramObjects").GetValue(diagram);
        var addMethod = coll.GetType().GetMethod("Add", new Type[] { T("s0Vz.C0V2") });
        addMethod.Invoke(coll, new object[] { widget });
        diagram.GetType().GetMethod("Q0Ac").Invoke(diagram, null);
        var countP = coll.GetType().GetProperty("Count");
        Console.WriteLine("ADDED " + widget.GetType().FullName + " text=" + text + " at " + loc + " size " + size + " count=" + countP.GetValue(coll));

        var storage = mgrT.GetMethod("N0B9").Invoke(mgr, null);
        Console.WriteLine("STORAGE Location=" + storage.GetType().GetProperty("Location").GetValue(storage));
        var maIq = storage.GetType().GetMethod("MaIq", BindingFlags.NonPublic | BindingFlags.Instance);
        if (maIq != null)
        {
            var storageDir = maIq.Invoke(storage, null)?.ToString();
            Console.WriteLine("STORAGE Dir=" + storageDir);
            if (storageDir != null && Directory.Exists(storageDir))
                foreach (var fi in new DirectoryInfo(storageDir).GetFiles("*", SearchOption.AllDirectories))
                    Console.WriteLine("  FILE " + fi.FullName.Substring(storageDir.Length) + " " + fi.Length);
        }
        // 把整页序列化到流，并更新 laKD 的原始流缓存
        var pageStream = T("jaqN.taqY").GetMethod("IaqR").Invoke(null, new object[] { page, false }) as Stream;
        Console.WriteLine("PAGE STREAM len=" + (pageStream?.Length ?? -1));
        if (pageStream != null)
        {
            var bytes = new byte[pageStream.Length];
            pageStream.Position = 0; pageStream.Read(bytes, 0, bytes.Length);
            var txt = System.Text.Encoding.UTF8.GetString(bytes);
            Console.WriteLine("STREAM HAS TEXT=" + txt.Contains(text) + " utf16=" + (System.Text.Encoding.Unicode.GetString(bytes).Contains(text)));
        }
        var meta = page.GetType().GetMethod("Eavp").Invoke(page, null);
        meta.GetType().GetMethod("xayE").Invoke(meta, new object[] { pageStream });
        // 直接用完整页面序列化流覆盖存储中的页面包
        var dest = storage.GetType().GetMethod("Create", new Type[] { T("zayC.vayw") }).Invoke(storage, new object[] { page }) as Stream;
        if (dest != null && pageStream != null)
        {
            pageStream.Position = 0;
            using (var gz = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(dest))
            {
                pageStream.CopyTo(gz);
            }
            dest.Flush();
            Console.WriteLine("WROTE GZIP PAGE STREAM to storage, dest len=" + dest.Length);
        }
        var data = storage.GetType().GetMethod("maI5").Invoke(storage, null);
        foreach (var item in (IEnumerable)data.GetType().GetProperty("Items").GetValue(data))
        {
            try
            {
                var it = item.GetType();
                Console.WriteLine("ITEM key=" + it.GetProperty("Key").GetValue(item) + " pkg=" + it.GetProperty("PackageId").GetValue(item) + " len=" + it.GetProperty("Length").GetValue(item));
            }
            catch { }
        }

        // 保存到新路径
        mgrT.GetProperty("FileName").SetValue(mgr, output);
        mgrT.GetMethod("Save").Invoke(mgr, null);
        try { ((IDisposable)mgr).Dispose(); } catch { }
        Console.WriteLine("SAVED exists=" + File.Exists(output) + " size=" + (File.Exists(output) ? new FileInfo(output).Length : 0));
    }

    static void PocWidgets(string path, string pageName)
    {
        InitGlobals();
        var mgrT = T("m0Sq.A0St");
        var mgr = Activator.CreateInstance(mgrT);
        var access = Enum.Parse(T("Axure.IO.AccessMode"), "ReadWrite");
        object[] parameters = { path, null, true, access, true };
        if (!(bool)mgrT.GetMethod("Load").Invoke(mgr, parameters))
        {
            Console.WriteLine("LOAD FAILED " + parameters[1]); return;
        }
        var doc = mgrT.GetMethod("F0Ba").Invoke(mgr, null);
        object page = FindPage(mgr, doc, pageName);
        if (page == null) { Console.WriteLine("PAGE NOT FOUND " + pageName); return; }
        Console.WriteLine("PAGE " + pageName + " type=" + page.GetType().FullName);
        var objs = page.GetType().GetProperty("DiagramObjects").GetValue(page) as IEnumerable;
        int i = 0;
        if (objs != null) foreach (var o in objs)
        {
            Console.WriteLine("WIDGET " + i + " type=" + o.GetType().FullName);
            foreach (var pr in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (pr.Name == "Name" || pr.Name == "Text" || pr.Name == "Location" || pr.Name == "Size" || pr.Name == "DisplayName")
                {
                    try { Console.WriteLine("  " + pr.Name + "=" + pr.GetValue(o)); }
                    catch (Exception ex) { Console.WriteLine("  " + pr.Name + " ERR " + ex.Message); }
                }
            }
            i++;
        }
        Console.WriteLine("WIDGETCOUNT " + i);
    }

    static object FindPage(object mgr, object doc, string pageName)
    {
        var sitemap = doc.GetType().GetProperty("Sitemap").GetValue(doc);
        if (sitemap == null) return null;
        var tree = sitemap.GetType().GetProperty("Tree").GetValue(sitemap);
        return FindPageNode(mgr, sitemap, tree, pageName);
    }

    static object FindPageNode(object mgr, object sitemap, object node, string pageName)
    {
        if (node == null) return null;
        object raw = null;
        try { raw = sitemap.GetType().GetMethod("s0Hx").Invoke(sitemap, new object[] { node }); } catch { }
        if (raw is Guid id)
        {
            var live = mgr.GetType().GetMethod("k0Xc").Invoke(mgr, new object[] { id, true, null, Guid.Empty, null });
            if (live != null)
            {
                var nameP = live.GetType().GetProperty("Name");
                if (nameP != null && (nameP.GetValue(live)?.ToString() ?? "") == pageName) return live;
            }
        }
        var children = node.GetType().GetProperty("Children");
        if (children != null)
        {
            var list = children.GetValue(node) as IEnumerable;
            if (list != null) foreach (var c in list)
            {
                var found = FindPageNode(mgr, sitemap, c, pageName);
                if (found != null) return found;
            }
        }
        return null;
    }

    static void DumpPages(object mgr, object doc)
    {
        var mgrT = mgr.GetType();
        var pages = (IEnumerable)T("Q0IK.W0Iu").GetMethod("u0fY").Invoke(doc, null);
        int i = 0;
        foreach (var p in pages)
        {
            var pid = (Guid)p.GetType().GetProperty("PackageId").GetValue(p);
            var live = mgrT.GetMethod("k0Xc").Invoke(mgr, new object[] { pid, true, null, Guid.Empty, null });
            Console.WriteLine("PAGE " + i + " id=" + pid + " live=" + (live?.GetType().FullName ?? "null"));
            if (live != null)
            {
                foreach (var pr in live.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (pr.Name == "Name" || pr.Name == "Diagram" || pr.Name == "DiagramObjects" || pr.Name == "Size")
                    {
                        try { Console.WriteLine("  " + pr.Name + "=" + pr.GetValue(live)); }
                        catch (Exception ex) { Console.WriteLine("  " + pr.Name + " ERR " + ex.Message); }
                    }
                }
            }
            i++;
        }
        Console.WriteLine("PAGECOUNT " + i);
    }

    static void ListTypes(string filter)
    {
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
        Console.WriteLine("TYPECOUNT " + types.Length);
        foreach (var t in types)
        {
            string n = (t.FullName ?? "").ToLowerInvariant();
            if (filter.Length > 0 && !n.Contains(filter)) continue;
            if (filter.Length == 0 && !(n.Contains("document") || n.Contains("diagram") || n.Contains("widget") || n.Contains("axure."))) continue;
            Console.WriteLine("TYPE " + t.FullName + " : " + (t.BaseType?.FullName ?? ""));
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                Console.WriteLine("  M " + m);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                Console.WriteLine("  P " + p);
        }
    }
}
