using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.GZip;

// 纯代码 Axure RP9 对象模型桥：所有操作后台完成，零 GUI / 零鼠标。
// 用法: axure-bridge.exe <json>   (JSON 从命令行或 stdin 读取)，stdout 输出 JSON。
partial class CodeBridge
{
    static Assembly asm;
    static string dir = @"D:\Program Files (x86)\Axure\Axure RP 9";

    [STAThread]
    static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string name = new AssemblyName(e.Name).Name + ".dll";
            string p = Path.Combine(dir, name);
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try
        {
            string json = args.Length > 0 ? args[0] : Console.In.ReadToEnd();
            var req = JObject.Parse(json);
            string cmd = req.Value<string>("cmd") ?? "";
            asm = Assembly.LoadFrom(Path.Combine(dir, "AxureRP9.exe"));
            InitGlobals();
            object result = Dispatch(cmd, req);
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonConvert.SerializeObject(new { ok = false, error = ex.ToString() }));
            return 1;
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

    static object Dispatch(string cmd, JObject req)
    {
        switch (cmd)
        {
            case "batch": return Batch(req);
            case "describe-type": return DescribeType(req);
            case "find-types": return FindTypes(req);
            case "new-project": return NewProject(req);
            case "list-pages": return ListPages(req);
            case "list-page-tree": return ListPageTree(req);
            case "add-folder": return AddFolder(req);
            case "rename-folder": return RenameFolder(req);
            case "delete-folder": return DeleteFolder(req);
            case "move-page": return MovePage(req);
            case "list-masters": return ListMasters(req);
            case "add-master": return AddMaster(req);
            case "rename-master": return RenameMaster(req);
            case "delete-master": return DeleteMaster(req);
            case "add-master-widget": return AddMasterWidget(req);
            case "apply-master": return ApplyMaster(req);
            case "remove-master": return RemoveMaster(req);
            case "list-adaptive-views": return ListAdaptiveViews(req);
            case "add-adaptive-view": return AddAdaptiveView(req);
            case "set-adaptive-view-size": return SetAdaptiveViewSize(req);
            case "set-page-size": return SetPageSize(req);
            case "set-page-style": return SetPageStyle(req);
            case "list-named-styles": return ListNamedStyles(req);
            case "add-named-style": return AddNamedStyle(req);
            case "apply-named-style": return ApplyNamedStyle(req);
            case "get-named-style": return GetNamedStyle(req);
            case "delete-named-style": return DeleteNamedStyle(req);
            case "project-info": return ProjectInfo(req);
            case "add-page": return AddPage(req);
            case "rename-page": return RenamePage(req);
            case "delete-page": return DeletePage(req);
            case "list-widgets": return ListWidgets(req);
            case "get-widget": return GetWidget(req);
            case "dump-page": return DumpPage(req);
            case "list-widget-tree": return ListWidgetTree(req);
            case "get-widget-path": return GetWidgetPath(req);
            case "update-widget-path": return UpdateWidgetPath(req);
            case "delete-widget-path": return DeleteWidgetPath(req);
            case "add-widget": return AddWidget(req);
            case "add-panel-state": return AddPanelState(req);
            case "add-panel-widget": return AddPanelWidget(req);
            case "set-panel-state": return SetPanelState(req);
            case "list-panel-states": return ListPanelStates(req);
            case "repeater-add-column": return RepeaterAddColumn(req);
            case "repeater-add-row": return RepeaterAddRow(req);
            case "repeater-set-cell": return RepeaterSetCell(req);
            case "repeater-add-item": return RepeaterAddItem(req);
            case "list-repeater-data": return ListRepeaterData(req);
            case "table-set-cell": return TableSetCell(req);
            case "table-set-size": return TableSetSize(req);
            case "add-list-option": return AddListOption(req);
            case "list-options": return ListOptions(req);
            case "update-widget": return UpdateWidget(req);
            case "set-widget-property": return SetWidgetProperty(req);
            case "set-style-property": return SetStyleProperty(req);
            case "set-page-property": return SetPageProperty(req);
            case "delete-widget": return DeleteWidget(req);
            case "add-link": return AddLink(req);
            case "add-action": return AddInteractionAction(req);
            case "clear-interactions": return ClearInteractions(req);
            case "list-variables": return ListVariables(req);
            case "get-variable": return GetVariable(req);
            case "add-variable": return AddVariable(req);
            case "update-variable": return UpdateVariable(req);
            case "delete-variable": return DeleteVariable(req);
            case "evaluate-expression": return EvaluateExpression(req);
            case "add-page-load-set-variable": return AddPageLoadSetVariable(req);
            case "generate-html": return GenerateHtml(req);
            case "export-word": return ExportWord(req);
            case "export-page-image": return ExportPageImage(req);
            case "export-all-images": return ExportAllImages(req);
            case "optimize-image": return OptimizeImageFile(req);
            case "export-csv-report": return ExportCsvReport(req);
            case "export-pdf": return ExportPdf(req);
            default: throw new ArgumentException("unknown cmd: " + cmd);
        }
    }

    static object Batch(JObject req)
    {
        if (!(req["ops"] is JArray ops)) throw new ArgumentException("batch requires ops array");
        var results = new JArray();
        for (int i = 0; i < ops.Count; i++)
        {
            if (!(ops[i] is JObject op)) throw new ArgumentException($"batch op {i} must be object");
            string cmd = op.Value<string>("cmd") ?? "";
            try { results.Add(JToken.FromObject(Dispatch(cmd, op))); }
            catch (Exception ex) { throw new InvalidOperationException($"batch op {i} ({cmd}) failed: {ex.Message}", ex); }
        }
        return new { ok = true, count = results.Count, results };
    }

    static object DescribeType(JObject req)
    {
        string name = req.Value<string>("name");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required");
        var t = T(name);
        int max = req.Value<int?>("max") ?? 250;
        var constructors = new JArray();
        foreach (var c in t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Take(max))
            constructors.Add(Signature(c));
        var properties = new JArray();
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Take(max))
            properties.Add(new JObject { ["name"] = p.Name, ["type"] = p.PropertyType.FullName,
                ["canRead"] = p.CanRead, ["canWrite"] = p.CanWrite });
        var methods = new JArray();
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                           .Where(x => !x.IsSpecialName).Take(max))
            methods.Add(Signature(m));
        return new { ok = true, name = t.FullName, baseType = t.BaseType?.FullName,
            constructors, properties, methods };
    }

    static JObject Signature(MethodBase m)
    {
        var ps = m.GetParameters();
        return new JObject
        {
            ["name"] = m.Name,
            ["returnType"] = (m is MethodInfo mi ? mi.ReturnType.FullName : null),
            ["parameters"] = new JArray(ps.Select(p => new JObject
            {
                ["name"] = p.Name, ["type"] = p.ParameterType.FullName,
                ["optional"] = p.IsOptional,
                ["default"] = p.HasDefaultValue && p.DefaultValue != null ? JToken.FromObject(p.DefaultValue) : JValue.CreateNull()
            })),
            ["static"] = m.IsStatic, ["public"] = m.IsPublic
        };
    }

    static object FindTypes(JObject req)
    {
        string query = req.Value<string>("query") ?? "";
        int max = req.Value<int?>("max") ?? 200;
        Type[] allTypes;
        try { allTypes = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { allTypes = ex.Types.Where(t => t != null).ToArray(); }
        var matches = allTypes
            .Where(t => t.FullName != null && t.FullName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(t => t.FullName)
            .Take(max)
            .Select(t => (JObject)new JObject
            {
                ["name"] = t.FullName,
                ["baseType"] = t.BaseType?.FullName,
                ["isEnum"] = t.IsEnum,
                ["isInterface"] = t.IsInterface
            });
        return new { ok = true, query, count = matches.Count(), types = new JArray(matches) };
    }

    static object NewProject(JObject req)
    {
        string path = req.Value<string>("path");
        string name = req.Value<string>("name") ?? "Untitled";
        var mgrT = T("m0Sq.A0St");
        var mgr = Activator.CreateInstance(mgrT);
        var standard = Enum.Parse(T("Axure.Platform.ObjectPersistanceContext.DesignDocumentType"), "Standard");
        T("Q0IK.W0Iu").GetMethod("b0Iv", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { mgr, standard, name });
        SaveManager(mgr, path);
        return new { ok = true, path, name, size = new FileInfo(path).Length };
    }

    static object ListPages(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var pages = new JArray();
            foreach (var p in EnumeratePages(ctx))
                pages.Add(PageInfo(p));
            return new { ok = true, path = ctx.Path, pages };
        }
    }

    static object ProjectInfo(JObject req)
    {
        string path = req.Value<string>("path");
        using (var ctx = LoadContext(path))
        {
            var pages = EnumeratePages(ctx).ToList();
            int widgets = pages.Sum(p => GetWidgets(p).Count());
            var fi = new FileInfo(path);
            return new { ok = true, path, bytes = fi.Length, lastWriteTime = fi.LastWriteTime.ToString("o"),
                pages = pages.Count, widgets, documentType = ctx.Doc.GetType().FullName };
        }
    }

    static object AddPage(JObject req)
    {
        string path = req.Value<string>("path");
        string name = req.Value<string>("name") ?? "New Page";
        using (var ctx = LoadContext(path))
        {
            var pageId = Guid.NewGuid();
            var doc = ctx.Doc;
            var cache = doc.GetType().GetMethod("NIkS").Invoke(doc, null);
            var page = Activator.CreateInstance(T("W0u2.m0uQ"), new object[] { doc, cache });
            page.GetType().GetMethod("UayB").Invoke(page, new object[] { pageId });
            var meta = Activator.CreateInstance(T("paEo.WaEV"), new object[] { doc });
            meta.GetType().GetProperty("PackageId").SetValue(meta, pageId);
            meta.GetType().GetMethod("Day7").Invoke(meta, new object[] { "Axure:Page" });
            meta.GetType().GetMethod("oayM").Invoke(meta, new object[] { name });
            doc.GetType().GetMethod("lalu").Invoke(doc, new object[] { meta });
            page.GetType().GetProperty("Name").SetValue(page, name);
            var sitemap = doc.GetType().GetProperty("Sitemap").GetValue(doc);
            var tree = sitemap.GetType().GetProperty("Tree").GetValue(sitemap);
            object parent = tree;
            if (!string.IsNullOrEmpty(req.Value<string>("parentFolder")))
                parent = FindFolderNode(ctx, req.Value<string>("parentFolder"));
            int index = req.Value<int?>("index") ?? 0;
            sitemap.GetType().GetMethod("I0Hg").Invoke(sitemap, new object[] { null, parent, index, pageId });
            ctx.ManagerType.GetMethod("c0XR").Invoke(ctx.Manager, new object[] { page, null });
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, path, page = name, id = pageId };
        }
    }

    static object RenamePage(JObject req)
    {
        string path = req.Value<string>("path");
        string oldName = req.Value<string>("page");
        string newName = req.Value<string>("name");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, oldName);
            if (page == null) throw new ArgumentException("page not found: " + oldName);
            page.GetType().GetProperty("Name").SetValue(page, newName);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = newName };
        }
    }

    static object DeletePage(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found: " + pageName);
            var id = (Guid)page.GetType().GetProperty("PackageId").GetValue(page);
            var doc = ctx.Doc;
            var sitemap = doc.GetType().GetProperty("Sitemap").GetValue(doc);
            var node = sitemap.GetType().GetMethod("R0HZ", new Type[] { typeof(Guid) }).Invoke(sitemap, new object[] { id });
            doc.GetType().GetMethod("sald", new Type[] { typeof(Guid) }).Invoke(doc, new object[] { id });
            if (node != null)
                sitemap.GetType().GetMethod("l0HS").Invoke(sitemap, new object[] { node });
            var storage = ctx.ManagerType.GetMethod("N0B9").Invoke(ctx.Manager, null);
            storage.GetType().GetMethod("Delete", new Type[] { typeof(Guid), typeof(Guid?) }).Invoke(storage, new object[] { id, null });
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, id };
        }
    }

    static object ListWidgets(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var widgets = new JArray();
            int i = 0;
            foreach (var w in GetWidgets(page))
            {
                widgets.Add(WidgetInfo(w, i));
                i++;
            }
            return new { ok = true, page = req.Value<string>("page"), widgets, count = widgets.Count };
        }
    }

    static object GetWidget(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        int index = req.Value<int>("index");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var widgets = GetWidgets(page).ToList();
            if (index < 0 || index >= widgets.Count) throw new ArgumentException("widget index out of range");
            var widget = widgets[index];
            var info = WidgetInfo(widget, index);
            object style = null, ix = null;
            try { style = widget.GetType().GetProperty("MyStyle").GetValue(widget); } catch { }
            try { ix = widget.GetType().GetMethod("sf04")?.Invoke(widget, null); } catch { }
            info["style"] = SafeDump(style, 3, new HashSet<object>());
            object effective = null;
            try { effective = FindMethod(widget.GetType(), "U0cP").Invoke(widget, null); } catch { }
            info["effectiveStyle"] = SafeDump(effective, 3, new HashSet<object>());
            info["interactions"] = SafeDump(ix, 5, new HashSet<object>());
            return new { ok = true, page = pageName, widget = info };
        }
    }

    static object DumpPage(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            return new { ok = true, page = pageName, model = SafeDump(page, 4, new HashSet<object>()) };
        }
    }

    static JToken SafeDump(object o, int depth, HashSet<object> visited)
    {
        if (o == null) return JValue.CreateNull();
        Type t = o.GetType();
        if (o is string s) return new JValue(s.Length > 1000 ? s.Substring(0, 1000) + "..." : s);
        if (o is bool || o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint ||
            o is long || o is ulong || o is float || o is double || o is decimal)
            return JValue.FromObject(o);
        if (o is Guid g) return new JValue(g.ToString());
        if (o is DateTime dt) return new JValue(dt.ToString("o"));
        if (t.IsEnum) return new JValue(Enum.GetName(t, o) ?? o.ToString());
        if (depth <= 0) return new JObject { ["_type"] = t.FullName, ["_truncated"] = true };
        if (!visited.Add(o)) return new JObject { ["_type"] = t.FullName, ["_cycle"] = true };

        JToken result;
        if (o is IDictionary dict)
        {
            var jo = new JObject();
            int n = 0;
            foreach (DictionaryEntry e in dict)
            {
                if (n++ >= 100) break;
                jo[e.Key?.ToString() ?? "null"] = SafeDump(e.Value, depth - 1, visited);
            }
            result = jo;
        }
        else if (o is IEnumerable seq && !(o is byte[]))
        {
            var ja = new JArray();
            int n = 0;
            foreach (var item in seq)
            {
                if (n++ >= 100) break;
                ja.Add(SafeDump(item, depth - 1, visited));
            }
            result = ja;
        }
        else
        {
            var jo = new JObject { ["_type"] = t.FullName };
            int n = 0;
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length != 0 || !p.CanRead) continue;
                if (n++ >= 80) break;
                try { jo[p.Name] = SafeDump(p.GetValue(o), depth - 1, visited); }
                catch { jo[p.Name] = JValue.CreateNull(); }
            }
            result = jo;
        }
        visited.Remove(o);
        return result;
    }

    static object AddWidget(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        string type = (req.Value<string>("type") ?? "rectangle").ToLowerInvariant();
        string text = req.Value<string>("text") ?? "";
        string widgetName = req.Value<string>("name");
        double x = req.Value<double?>("x") ?? 100;
        double y = req.Value<double?>("y") ?? 100;
        double w = req.Value<double?>("w") ?? 200;
        double h = req.Value<double?>("h") ?? 60;
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found: " + pageName);
            var widget = IsAdvancedWidget(type)
                ? CreateAdvancedWidget(page, type, text, x, y, w, h, req)
                : CreateWidget(page, type, text, x, y, w, h);
            if (!string.IsNullOrEmpty(widgetName)) widget.GetType().GetProperty("Name").SetValue(widget, widgetName);
            if (req["style"] is JObject style) ApplyStyle(widget, style);
            AddToDiagram(page, widget);
            if (type == "dynamic-panel" && req["stateName"] != null)
            {
                object state = GetProperty(widget, "CurrentPanelState");
                if (state != null) SetProperty(state, "Name", req.Value<string>("stateName"));
            }
            TouchPage(page);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, type, text, x, y, w, h };
        }
    }

    static object UpdateWidget(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        int index = req.Value<int>("index");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var widgets = GetWidgets(page).ToList();
            if (index < 0 || index >= widgets.Count) throw new ArgumentException("widget index out of range");
            var widget = widgets[index];
            if (req["name"] != null) widget.GetType().GetProperty("Name").SetValue(widget, req.Value<string>("name"));
            if (req["text"] != null) widget.GetType().GetProperty("Text").SetValue(widget, req.Value<string>("text"));
            if (req["x"] != null || req["y"] != null)
            {
                double x = req.Value<double?>("x") ?? GetLoc(widget, "X");
                double y = req.Value<double?>("y") ?? GetLoc(widget, "Y");
                var locType = widget.GetType().GetProperty("Location").PropertyType;
                var loc = Activator.CreateInstance(locType, new object[] { x, y });
                widget.GetType().GetMethod("l0or").Invoke(widget, new object[] { loc, false, true });
            }
            if (req["w"] != null || req["h"] != null)
            {
                double w = req.Value<double?>("w") ?? GetSize(widget, "Width");
                double h = req.Value<double?>("h") ?? GetSize(widget, "Height");
                var sizeType = widget.GetType().GetProperty("Size").PropertyType;
                var size = Activator.CreateInstance(sizeType, new object[] { w, h });
                widget.GetType().GetMethod("L0ob").Invoke(widget, new object[] { size, false, true });
            }
            if (req["style"] is JObject style) ApplyStyle(widget, style);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, index };
        }
    }

    static object SetWidgetProperty(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        int index = req.Value<int>("index");
        string propertyName = req.Value<string>("property");
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("property required");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var widgets = GetWidgets(page).ToList();
            if (index < 0 || index >= widgets.Count) throw new ArgumentException("widget index out of range");
            var widget = widgets[index];
            var prop = FindProperty(widget.GetType(), propertyName);
            if (!prop.CanWrite) throw new InvalidOperationException($"{prop.DeclaringType.FullName}.{prop.Name} is read-only");
            object value = ConvertJsonProperty(req["value"], prop.PropertyType);
            prop.SetValue(widget, value);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, index, property = propertyName, value };
        }
    }

    static object SetStyleProperty(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        int index = req.Value<int>("index");
        string pname = req.Value<string>("pname");
        if (string.IsNullOrEmpty(pname)) throw new ArgumentException("pname required");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var widget = GetWidgets(page).ElementAt(index);
            var wt = widget.GetType();
            object style = wt.GetProperty("MyStyle").GetValue(widget);
            if (style == null)
            {
                wt.GetMethod("p0U4", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(widget, new object[] { null });
                style = wt.GetProperty("MyStyle").GetValue(widget);
            }
            var name = Enum.Parse(T("Axure.Constants.PName"), pname, true);
            var prop = style.GetType().GetProperties().FirstOrDefault(p =>
                p.Name.Replace("_", "").Equals(pname.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
            object value;
            if (prop != null && prop.PropertyType.IsEnum && prop.PropertyType.Name == "BoolOverride" && req["value"] is JValue b && b.Type == JTokenType.Boolean)
                value = Enum.Parse(prop.PropertyType, b.Value<bool>() ? "TRUE" : "FALSE");
            else if (prop != null && (pname == "FILL" || pname == "FORE_GROUND_FILL" || pname == "BORDER_FILL" || pname == "ITEM_BACK_COLOR" || pname == "ITEM_ALT_COLOR"))
                value = MakeFill(req.Value<string>("value"));
            else if (prop != null)
                value = ConvertJsonProperty(req["value"], prop.PropertyType);
            else
                value = req["value"] is JValue v ? v.Value : req["value"];
            SetStyle(style, pname, value);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, index, pname, value = value?.ToString() };
        }
    }

    static object SetPageProperty(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        string propertyName = req.Value<string>("property");
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("property required");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var prop = FindProperty(page.GetType(), propertyName);
            if (!prop.CanWrite) throw new InvalidOperationException($"{prop.DeclaringType.FullName}.{prop.Name} is read-only");
            object value = ConvertJsonProperty(req["value"], prop.PropertyType);
            prop.SetValue(page, value);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, property = propertyName, value };
        }
    }

    static object ConvertJsonProperty(JToken token, Type target)
    {
        if (token == null || token.Type == JTokenType.Null) return null;
        if (target.IsEnum && token.Type == JTokenType.String)
            return Enum.Parse(target, token.Value<string>(), true);
        if (target == typeof(Guid) && token.Type == JTokenType.String)
            return Guid.Parse(token.Value<string>());
        return token.ToObject(target);
    }

    static object DeleteWidget(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        int index = req.Value<int>("index");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var widgets = GetWidgets(page).ToList();
            if (index < 0 || index >= widgets.Count) throw new ArgumentException("widget index out of range");
            var diagram = page.GetType().GetProperty("Diagram").GetValue(page);
            var remove = diagram.GetType().GetMethod("Remove", new Type[] { T("s0Vz.C0V2") });
            if (remove != null) remove.Invoke(diagram, new object[] { widgets[index] });
            else
            {
                var coll = page.GetType().GetProperty("DiagramObjects").GetValue(page);
                coll.GetType().GetMethod("Remove", new Type[] { T("s0Vz.C0V2") }).Invoke(coll, new object[] { widgets[index] });
            }
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, deleted = index };
        }
    }

    static object AddLink(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        string targetName = req.Value<string>("targetPage");
        int index = req.Value<int>("index");
        string eventName = req.Value<string>("event") ?? "OnClick";
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            var target = FindPage(ctx, targetName);
            if (page == null) throw new ArgumentException("page not found: " + pageName);
            if (target == null) throw new ArgumentException("target page not found: " + targetName);
            var widgets = GetWidgets(page).ToList();
            if (index < 0 || index >= widgets.Count) throw new ArgumentException("widget index out of range");
            var widget = widgets[index];
            var wt = widget.GetType();
            object ix = wt.GetMethod("sf04").Invoke(widget, null);
            if (ix == null)
            {
                object owner = wt.GetMethod("Maqe").Invoke(widget, null);
                ix = Activator.CreateInstance(T("j0be.K0bF"), new object[] { owner, widget });
                wt.GetMethod("p0FN").Invoke(widget, new object[] { ix, false, false });
            }
            var ixType = ix.GetType();
            var eventType = Enum.Parse(T("Axure.Constants.IxEventType"), eventName, true);
            var eventNode = ixType.GetMethod("N0bq").Invoke(ix, new object[] { eventType });
            object ixOwner = ixType.GetMethod("Maqe").Invoke(ix, null);
            var kase = Activator.CreateInstance(T("Axure.DocumentModel.Interaction.IxCase"), new object[] { ixOwner });
            kase.GetType().GetProperty("IxMap").SetValue(kase, ix);
            var nodeType = T("a0dU.H0do");
            int append = Convert.ToInt32(nodeType.GetMethod("X0dn").Invoke(eventNode, null));
            var caseNode = ixType.GetMethod("I0Hg", new Type[] { nodeType, nodeType, typeof(int), typeof(object) })
                .Invoke(ix, new object[] { null, eventNode, append, kase });
            var linkType = Enum.Parse(T("Axure.DocumentModel.Interaction.LinkType"), "Current");
            var openLink = Activator.CreateInstance(T("Axure.DocumentModel.Interaction.IxLinkWindowAction"), new object[] { ixOwner, linkType });
            var ixTarget = openLink.GetType().GetProperty("Target").GetValue(openLink);
            ixTarget.GetType().GetProperty("TargetType").SetValue(ixTarget, Enum.Parse(T("Axure.DocumentModel.Interaction.IxTargetType"), "Page"));
            ixTarget.GetType().GetProperty("TargetPagePackageId").SetValue(ixTarget, target.GetType().GetProperty("PackageId").GetValue(target));
            ixType.GetMethod("I0Hg", new Type[] { nodeType, nodeType, typeof(int), typeof(object) })
                .Invoke(ix, new object[] { null, caseNode, 0, openLink });
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = pageName, index, targetPage = targetName, @event = eventName };
        }
    }

    static object GenerateHtml(JObject req)
    {
        string path = req.Value<string>("path");
        string outDir = req.Value<string>("outDir") ?? Path.Combine(Path.GetDirectoryName(path), "html");
        Directory.CreateDirectory(outDir);
        string htmlPath = Path.Combine(outDir, "index.html");
        string tempPath = Path.Combine(Path.GetTempPath(), "axure-html-" + Guid.NewGuid().ToString("N") + ".rp");
        File.Copy(path, tempPath);
        try
        {
            using (var ctx = LoadContext(tempPath, true))
            {
                var config = Activator.CreateInstance(T("cRNf.mRNI"), new object[] { ctx.Doc });
                config.GetType().GetMethod("eRNE").Invoke(config, new object[] { outDir });
                config.GetType().GetMethod("URNv").Invoke(config, new object[] { false });
                var generator = Activator.CreateInstance(T("x9xW.n9xc"), new object[] { config });
            bool ok = true;
            var result = generator.GetType().GetMethod("Generate").Invoke(generator, new object[] { ctx.Manager, null, null, null });
            string playerJs = Path.Combine(outDir, "resources", "scripts", "player", "axplayer.js");
            bool patched = false;
            if (File.Exists(playerJs))
            {
                string js = File.ReadAllText(playerJs);
                string patchedJs = js
                    .Replace("if (CHROME_5_LOCAL && !$('body').attr('pluginDetected'))", "if (false)")
                    .Replace("if (FIREFOX && BROWSER_VERSION >= 68 && document.location.href.indexOf('file://') >= 0)", "if (false)");
                if (patchedJs != js)
                {
                    File.WriteAllText(playerJs, patchedJs);
                    patched = true;
                }
            }
            return new { ok, path = htmlPath, exists = File.Exists(htmlPath), size = File.Exists(htmlPath) ? new FileInfo(htmlPath).Length : 0, result = result?.GetType().FullName, extensionPromptPatched = patched };
            }
        }
        finally { try { File.Delete(tempPath); } catch { } }
    }

    // ------------------------------------------------------------ 上下文与序列化
    class Ctx : IDisposable
    {
        public string Path;
        public object Manager;
        public Type ManagerType;
        public object Doc;
        public void Dispose() { try { ((IDisposable)Manager).Dispose(); } catch { } }
    }

    static Ctx LoadContext(string path, bool readWrite = true)
    {
        var mgrT = T("m0Sq.A0St");
        var mgr = Activator.CreateInstance(mgrT);
        var access = Enum.Parse(T("Axure.IO.AccessMode"), readWrite ? "ReadWrite" : "Read");
        object[] parameters = { path, null, true, access, true };
        if (!(bool)mgrT.GetMethod("Load").Invoke(mgr, parameters))
            throw new IOException("load failed: " + parameters[1]);
        var doc = mgrT.GetMethod("F0Ba").Invoke(mgr, null);
        return new Ctx { Path = path, Manager = mgr, ManagerType = mgrT, Doc = doc };
    }

    static void SaveManager(object mgr, string path)
    {
        var mgrT = mgr.GetType();
        mgrT.GetProperty("FileName").SetValue(mgr, path);
        var storage = mgrT.GetMethod("N0B9").Invoke(mgr, null);
        storage.GetType().GetProperty("Location").SetValue(storage, path);
        mgrT.GetMethod("Save").Invoke(mgr, null);
        try { ((IDisposable)mgr).Dispose(); } catch { }
    }

    static void WritePageStream(Ctx ctx, object page)
    {
        var pageStream = T("jaqN.taqY").GetMethod("IaqR").Invoke(null, new object[] { page, true }) as Stream;
        if (pageStream == null) throw new IOException("page serialization failed");
        var storage = ctx.ManagerType.GetMethod("N0B9").Invoke(ctx.Manager, null);
        var dest = storage.GetType().GetMethod("Create", new Type[] { T("zayC.vayw") }).Invoke(storage, new object[] { page }) as Stream;
        using (var gz = new GZipOutputStream(dest))
        {
            pageStream.Position = 0;
            pageStream.CopyTo(gz);
        }
        dest.Flush();
    }

    // ------------------------------------------------------------ 页面/元件
    static object FindPage(Ctx ctx, string name)
    {
        var sitemap = ctx.Doc.GetType().GetProperty("Sitemap").GetValue(ctx.Doc);
        if (sitemap == null) return null;
        var tree = sitemap.GetType().GetProperty("Tree").GetValue(sitemap);
        return FindPageNode(ctx, sitemap, tree, name);
    }

    static object FindPageNode(Ctx ctx, object sitemap, object node, string name)
    {
        if (node == null) return null;
        object raw = null;
        try { raw = sitemap.GetType().GetMethod("s0Hx").Invoke(sitemap, new object[] { node }); } catch { }
        if (raw is Guid id)
        {
            var live = ctx.ManagerType.GetMethod("k0Xc").Invoke(ctx.Manager, new object[] { id, true, null, Guid.Empty, null });
            if (live != null)
            {
                var nameP = live.GetType().GetProperty("Name");
                if (nameP != null && (nameP.GetValue(live)?.ToString() ?? "") == name) return live;
            }
        }
        var children = node.GetType().GetProperty("Children");
        if (children != null)
        {
            var list = children.GetValue(node) as IEnumerable;
            if (list != null) foreach (var c in list)
            {
                var found = FindPageNode(ctx, sitemap, c, name);
                if (found != null) return found;
            }
        }
        return null;
    }

    static IEnumerable<object> EnumeratePages(Ctx ctx)
    {
        var sitemap = ctx.Doc.GetType().GetProperty("Sitemap").GetValue(ctx.Doc);
        if (sitemap == null) yield break;
        var tree = sitemap.GetType().GetProperty("Tree").GetValue(sitemap);
        foreach (var p in EnumeratePageNodes(ctx, sitemap, tree)) yield return p;
    }

    static IEnumerable<object> EnumeratePageNodes(Ctx ctx, object sitemap, object node)
    {
        if (node == null) yield break;
        object raw = null;
        try { raw = sitemap.GetType().GetMethod("s0Hx").Invoke(sitemap, new object[] { node }); } catch { }
        if (raw is Guid id)
        {
            var live = ctx.ManagerType.GetMethod("k0Xc").Invoke(ctx.Manager, new object[] { id, true, null, Guid.Empty, null });
            if (live != null) yield return live;
        }
        var children = node.GetType().GetProperty("Children");
        if (children != null)
        {
            var list = children.GetValue(node) as IEnumerable;
            if (list != null) foreach (var c in list)
                foreach (var p in EnumeratePageNodes(ctx, sitemap, c)) yield return p;
        }
    }

    static JObject PageInfo(object page)
    {
        var name = page.GetType().GetProperty("Name").GetValue(page)?.ToString() ?? "";
        var id = page.GetType().GetProperty("PackageId").GetValue(page);
        int count = GetWidgets(page).Count();
        return new JObject { ["name"] = name, ["id"] = id.ToString(), ["widgets"] = count, ["type"] = page.GetType().FullName };
    }

    static IEnumerable<object> GetWidgets(object page)
    {
        var coll = page.GetType().GetProperty("DiagramObjects").GetValue(page) as IEnumerable;
        if (coll != null) foreach (var w in coll) yield return w;
    }

    static JObject WidgetInfo(object w, int index)
    {
        var t = w.GetType();
        var loc = t.GetProperty("Location").GetValue(w);
        var size = t.GetProperty("Size").GetValue(w);
        string text = "";
        try { text = t.GetProperty("Text").GetValue(w)?.ToString() ?? ""; } catch { }
        return new JObject
        {
            ["index"] = index,
            ["id"] = SafeGet(w, "PackageId")?.ToString() ?? "",
            ["type"] = t.FullName,
            ["name"] = t.GetProperty("Name").GetValue(w)?.ToString() ?? "",
            ["text"] = text,
            ["x"] = GetProp(loc, "X"), ["y"] = GetProp(loc, "Y"),
            ["w"] = GetProp(size, "Width"), ["h"] = GetProp(size, "Height"),
            ["style"] = StyleSummary(w)
        };
    }

    static JObject StyleSummary(object widget)
    {
        var result = new JObject();
        try
        {
            object style = null;
            try { style = FindMethod(widget.GetType(), "U0cP").Invoke(widget, null); } catch { }
            if (style == null) style = widget.GetType().GetProperty("MyStyle").GetValue(widget);
            if (style == null) return result;
            result["fontName"] = style.GetType().GetProperty("FontName")?.GetValue(style)?.ToString();
            result["fontSize"] = GetProp(style, "FontSize");
            result["bold"] = style.GetType().GetProperty("Bold")?.GetValue(style)?.ToString();
            result["italic"] = style.GetType().GetProperty("Italic")?.GetValue(style)?.ToString();
            result["underline"] = new JValue(Convert.ToBoolean(style.GetType().GetProperty("Underline")?.GetValue(style) ?? false));
            result["fill"] = FillHex(style, "Fill");
            result["textColor"] = FillHex(style, "ForeGroundFill");
            result["borderColor"] = FillHex(style, "BorderFill");
            result["borderWidth"] = GetProp(style, "BorderWidth");
            result["cornerRadius"] = GetProp(style, "CornerRadius");
            result["opacity"] = GetProp(style, "Opacity");
            result["rotation"] = GetProp(style, "Rotation");
        }
        catch { }
        return result;
    }

    static string FillHex(object style, string propertyName)
    {
        try
        {
            object fill = style.GetType().GetProperty(propertyName)?.GetValue(style);
            if (fill == null) return null;
            object color = fill.GetType().GetProperty("Color")?.GetValue(fill);
            if (color == null) return null;
            int a = Convert.ToInt32(GetProp(color, "A"));
            int r = Convert.ToInt32(GetProp(color, "R"));
            int g = Convert.ToInt32(GetProp(color, "G"));
            int b = Convert.ToInt32(GetProp(color, "B"));
            return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
        }
        catch { return null; }
    }

    static object CreateWidget(object page, string type, string text, double x, double y, double w, double h)
    {
        object widget;
        if (type == "button")
            widget = Activator.CreateInstance(T("Axure.DocumentModel.Objects.Button"), new object[] { page });
        else if (type == "input")
            widget = Activator.CreateInstance(T("Axure.DocumentModel.Objects.TextBox"), new object[] { page });
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
        var locType = widget.GetType().GetProperty("Location").PropertyType;
        var sizeType = widget.GetType().GetProperty("Size").PropertyType;
        var loc = Activator.CreateInstance(locType, new object[] { x, y });
        var size = Activator.CreateInstance(sizeType, new object[] { w, h });
        widget.GetType().GetMethod("l0or").Invoke(widget, new object[] { loc, false, true });
        widget.GetType().GetMethod("L0ob").Invoke(widget, new object[] { size, false, true });
        return widget;
    }

    static void ApplyStyle(object widget, JObject spec)
    {
        var widgetType = widget.GetType();
        object style = widgetType.GetProperty("MyStyle").GetValue(widget);
        if (style == null)
        {
            widgetType.GetMethod("p0U4", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(widget, new object[] { null });
            style = widgetType.GetProperty("MyStyle").GetValue(widget);
        }
        if (style == null) throw new InvalidOperationException("cannot create widget style");

        if (spec["fill"] != null) SetStyle(style, "FILL", MakeFill(spec.Value<string>("fill")));
        if (spec["textColor"] != null) SetStyle(style, "FORE_GROUND_FILL", MakeFill(spec.Value<string>("textColor")));
        if (spec["borderColor"] != null) SetStyle(style, "BORDER_FILL", MakeFill(spec.Value<string>("borderColor")));
        if (spec["fontSize"] != null) SetStyle(style, "FONT_SIZE", spec.Value<double>("fontSize"));
        if (spec["fontName"] != null) SetStyle(style, "FONT_NAME", spec.Value<string>("fontName"));
        if (spec["bold"] != null) SetStyle(style, "BOLD", Enum.Parse(T("Axure.Constants.BoolOverride"), spec.Value<bool>("bold") ? "TRUE" : "FALSE"));
        if (spec["italic"] != null) SetStyle(style, "ITALIC", Enum.Parse(T("Axure.Constants.BoolOverride"), spec.Value<bool>("italic") ? "TRUE" : "FALSE"));
        if (spec["underline"] != null) SetStyle(style, "UNDERLINE", spec.Value<bool>("underline"));
        if (spec["borderWidth"] != null) SetStyle(style, "BORDER_WIDTH", spec.Value<double>("borderWidth"));
        if (spec["linePattern"] != null)
            SetStyle(style, "LINE_PATTERN", Enum.Parse(T("Axure.Platform.LineStyle"), spec.Value<string>("linePattern"), true));
        if (spec["cornerRadius"] != null) SetStyle(style, "CORNER_RADIUS", spec.Value<double>("cornerRadius"));
        if (spec["opacity"] != null) SetStyle(style, "OPACITY", spec.Value<double>("opacity"));
    }

    static void SetStyle(object style, string pname, object value)
    {
        var enumValue = Enum.Parse(T("Axure.Constants.PName"), pname);
        style.GetType().GetMethod("oanl").Invoke(style, new object[] { enumValue, value, false });
    }

    static object MakeFill(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("empty color");
        hex = hex.Trim().TrimStart('#');
        int a, r, g, b;
        if (hex.Length == 6)
        {
            a = 255;
            r = Convert.ToInt32(hex.Substring(0, 2), 16);
            g = Convert.ToInt32(hex.Substring(2, 2), 16);
            b = Convert.ToInt32(hex.Substring(4, 2), 16);
        }
        else if (hex.Length == 8)
        {
            a = Convert.ToInt32(hex.Substring(0, 2), 16);
            r = Convert.ToInt32(hex.Substring(2, 2), 16);
            g = Convert.ToInt32(hex.Substring(4, 2), 16);
            b = Convert.ToInt32(hex.Substring(6, 2), 16);
        }
        else throw new ArgumentException("color must be #RRGGBB or #AARRGGBB: " + hex);

        var colorType = T("u6n7.D6nO");
        object color = a == 255
            ? colorType.GetMethod("N6nA", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { r, g, b })
            : colorType.GetMethod("B6nM", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { a, r, g, b });
        return Activator.CreateInstance(T("A6Hb.D6Hl"), new object[] { color });
    }

    static object SafeGet(object o, string name)
    {
        try { return FindProperty(o.GetType(), name).GetValue(o); } catch { return null; }
    }

    static double GetProp(object o, string name)
    {
        try { return Convert.ToDouble(o.GetType().GetProperty(name).GetValue(o)); } catch { return 0; }
    }
    static double GetLoc(object w, string name) => GetProp(w.GetType().GetProperty("Location").GetValue(w), name);
    static double GetSize(object w, string name) => GetProp(w.GetType().GetProperty("Size").GetValue(w), name);
}
