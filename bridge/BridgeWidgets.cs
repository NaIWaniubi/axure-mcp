using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static bool IsAdvancedWidget(string type)
    {
        switch (type)
        {
            case "textarea":
            case "combobox":
            case "listbox":
            case "checkbox":
            case "radio":
            case "image":
            case "table":
            case "dynamic-panel":
            case "repeater":
            case "ellipse":
            case "hline":
            case "vline":
                return true;
            default:
                return false;
        }
    }

    static object CreateAdvancedWidget(object owner, string type, string text, double x, double y, double w, double h, JObject req)
    {
        object widget;
        switch (type)
        {
            case "textarea": widget = New("Axure.DocumentModel.Objects.TextArea", owner); break;
            case "combobox": widget = New("Axure.DocumentModel.Objects.ComboBox", owner); break;
            case "listbox": widget = New("Axure.DocumentModel.Objects.ListBox", owner); break;
            case "checkbox": widget = New("Axure.DocumentModel.Objects.Checkbox", owner); break;
            case "radio": widget = New("Axure.DocumentModel.Objects.RadioButton", owner); break;
            case "image": widget = New("Axure.DocumentModel.Objects.ImageBox", owner); break;
            case "table": widget = New("Axure.DocumentModel.Objects.Table", owner); break;
            case "dynamic-panel": widget = New("Axure.DocumentModel.Objects.DynamicPanel", owner); break;
            case "repeater": widget = New("Axure.DocumentModel.Objects.Repeater", owner); break;
            case "ellipse": widget = CreateVectorShape(owner, "Ellipse", text); break;
            case "hline": widget = CreateVectorShape(owner, "HorizontalLine", text); break;
            case "vline": widget = CreateVectorShape(owner, "VerticalLine", text); break;
            default: throw new ArgumentException("unsupported advanced widget: " + type);
        }

        TrySetText(widget, text);
        PlaceWidget(widget, x, y, w, h);

        switch (type)
        {
            case "textarea":
                SetJsonIf(req, "placeholder", widget, "PlaceholderText");
                SetJsonIf(req, "readOnly", widget, "ReadOnly");
                SetJsonIf(req, "hideHintOnFocus", widget, "HideHintOnFocus");
                SetJsonIf(req, "inputType", widget, "Type");
                break;
            case "combobox":
            case "listbox":
                SetJsonIf(req, "multiple", widget, "Multiple");
                if (req["options"] is JArray options)
                {
                    object optionOwner = Invoke(widget, "Maqe");
                    var list = (IList)GetProperty(widget, "Options");
                    foreach (var item in options)
                    {
                        string value = item is JValue v ? v.Value?.ToString() ?? "" : item.Value<string>("text") ?? item.Value<string>("value") ?? "";
                        bool selected = item is JObject jo && jo["selected"] != null && jo.Value<bool>("selected");
                        list.Add(New("EaDD.daDq", optionOwner, value, selected));
                    }
                    Invoke(widget, "MRc7");
                }
                break;
            case "checkbox":
            case "radio":
                SetJsonIf(req, "selected", widget, "Selected");
                SetJsonIf(req, "alignRight", widget, "AlignRight");
                if (type == "radio") SetJsonIf(req, "radioGroup", widget, "RadioGroup");
                break;
            case "image":
            {
                string imagePath = req.Value<string>("imagePath");
                if (string.IsNullOrWhiteSpace(imagePath)) throw new ArgumentException("imagePath required for image widget");
                if (!File.Exists(imagePath)) throw new FileNotFoundException(imagePath);
                var stream = new MemoryStream(File.ReadAllBytes(imagePath));
                var image = Activator.CreateInstance(T("gar0.gar9"),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[] { stream, Path.GetExtension(imagePath), null, true }, null);
                SetProperty(widget, "Image", image);
                SetJsonIf(req, "fitToImage", widget, "FitToImage");
                PlaceWidget(widget, x, y, w, h);
                break;
            }
            case "table":
                SetProperty(widget, "Rows", req.Value<int?>("rows") ?? 2);
                SetProperty(widget, "Columns", req.Value<int?>("columns") ?? 2);
                if (req["cells"] is JArray cells)
                {
                    for (int r = 0; r < cells.Count; r++)
                    {
                        if (!(cells[r] is JArray row)) continue;
                        for (int c = 0; c < row.Count; c++)
                        {
                            object cell = Invoke(widget, "QRES", r, c);
                            TrySetText(cell, row[c].Value<string>() ?? "");
                        }
                    }
                }
                Invoke(widget, "vRsa");
                Invoke(widget, "bRsp");
                PlaceWidget(widget, x, y, w, h);
                break;
            case "dynamic-panel":
            {
                object state = GetProperty(widget, "CurrentPanelState");
                if (state != null) SetProperty(state, "Name", req.Value<string>("stateName") ?? "State1");
                SetJsonIf(req, "fitToContent", widget, "FitToContent");
                break;
            }
            case "repeater":
                SetJsonIf(req, "fitToContent", widget, "FitToContent");
                if (req["data"] is JObject data) ApplyRepeaterData(widget, data);
                break;
        }
        return widget;
    }

    static object CreateVectorShape(object owner, string shapeName, string text)
    {
        var vsType = T("Axure.DocumentModel.Objects.VectorShape");
        var ctor = vsType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var ps = ctor.GetParameters();
        var shape = Enum.Parse(ps[1].ParameterType, shapeName);
        var legacy = Enum.Parse(ps[3].ParameterType, "None");
        return ctor.Invoke(new object[] { owner, shape, text, legacy, true });
    }

    static void TrySetText(object target, string text)
    {
        try
        {
            var prop = FindProperty(target.GetType(), "Text");
            if (prop.CanWrite) prop.SetValue(target, text);
        }
        catch { } // Not every object-model type supports this optional property.
    }

    static void PlaceWidget(object widget, double x, double y, double w, double h)
    {
        var locType = FindProperty(widget.GetType(), "Location").PropertyType;
        var sizeType = FindProperty(widget.GetType(), "Size").PropertyType;
        var loc = Activator.CreateInstance(locType, x, y);
        var size = Activator.CreateInstance(sizeType, w, h);
        FindMethod(widget.GetType(), "L0ob").Invoke(widget, new object[] { size, false, true });
        FindMethod(widget.GetType(), "l0or").Invoke(widget, new object[] { loc, false, true });
    }

    static void AddToDiagram(object diagram, object widget)
    {
        var coll = GetProperty(diagram, "DiagramObjects");
        FindMethod(coll.GetType(), "Add", new Type[] { T("s0Vz.C0V2") }).Invoke(coll, new object[] { widget });
    }

    static void TouchPage(object page)
    {
        object diagram = GetProperty(page, "Diagram");
        FindMethod(diagram.GetType(), "Q0Ac").Invoke(diagram, null);
    }

    static void SavePage(Ctx ctx, object page)
    {
        TouchPage(page);
        WritePageStream(ctx, page);
        SaveManager(ctx.Manager, ctx.Path);
    }

    static object RootWidget(object page, int index)
    {
        var widgets = GetWidgets(page).ToList();
        if (index < 0 || index >= widgets.Count) throw new ArgumentException("widget index out of range");
        return widgets[index];
    }

    static void EnsureWidgetType(object widget, string typeName)
    {
        if (widget.GetType().FullName != typeName)
            throw new InvalidOperationException($"expected {typeName}, got {widget.GetType().FullName}");
    }

    static void SetJsonIf(JObject req, string key, object target, string property)
    {
        if (req[key] == null) return;
        var prop = FindProperty(target.GetType(), property);
        if (!prop.CanWrite) return;
        prop.SetValue(target, ConvertJsonProperty(req[key], prop.PropertyType));
    }

    static object AddPanelState(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        int index = req.Value<int>("index");
        string name = req.Value<string>("name") ?? "NewState";
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var panel = RootWidget(page, index);
            EnsureWidgetType(panel, "Axure.DocumentModel.Objects.DynamicPanel");
            var pairType = T("Y9o8.z9oC`2").MakeGenericType(typeof(string), T("E0xu.v0xd"));
            var pairCtor = pairType.GetConstructor(new Type[] { typeof(string), T("E0xu.v0xd") });
            var listType = typeof(List<>).MakeGenericType(pairType);
            var plan = (IList)Activator.CreateInstance(listType);
            foreach (var state in (IEnumerable)GetProperty(panel, "PanelStates"))
                plan.Add(pairCtor.Invoke(new object[] { GetProperty(state, "Name"), state }));
            plan.Add(pairCtor.Invoke(new object[] { name, null }));
            FindMethod(panel.GetType(), "dRU2").Invoke(panel, new object[] { plan, null, false });
            var states = ((IEnumerable)GetProperty(panel, "PanelStates")).Cast<object>().ToList();
            if (states.Count > 0) SetProperty(states[states.Count - 1], "Name", name);
            SavePage(ctx, page);
            return new { ok = true, page = pageName, index, state = name,
                count = Convert.ToInt32(GetProperty(panel, "NumberOfPanelStates")) };
        }
    }

    static object ListPanelStates(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var panel = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(panel, "Axure.DocumentModel.Objects.DynamicPanel");
            var states = new JArray();
            int i = 0;
            foreach (var state in (IEnumerable)GetProperty(panel, "PanelStates"))
                states.Add(new JObject { ["index"] = i++, ["name"] = GetProperty(state, "Name")?.ToString(),
                    ["id"] = SafeGet(state, "PackageId")?.ToString() });
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                current = GetProperty(panel, "CurrentPanelIndex"), states };
        }
    }

    static object SetPanelState(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var panel = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(panel, "Axure.DocumentModel.Objects.DynamicPanel");
            SetProperty(panel, "CurrentPanelIndex", req.Value<int>("stateIndex"));
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                stateIndex = GetProperty(panel, "CurrentPanelIndex") };
        }
    }

    static object AddPanelWidget(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var panel = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(panel, "Axure.DocumentModel.Objects.DynamicPanel");
            int stateIndex = req.Value<int>("stateIndex");
            object diagram = Invoke(panel, "URUT", stateIndex);
            string type = (req.Value<string>("type") ?? "rectangle").ToLowerInvariant();
            string text = req.Value<string>("text") ?? "";
            double x = req.Value<double?>("x") ?? 0, y = req.Value<double?>("y") ?? 0;
            double w = req.Value<double?>("w") ?? 200, h = req.Value<double?>("h") ?? 60;
            object widget = IsAdvancedWidget(type)
                ? CreateAdvancedWidget(page, type, text, x, y, w, h, req)
                : CreateWidget(page, type, text, x, y, w, h);
            if (req["name"] != null) TrySetName(widget, req.Value<string>("name"));
            if (req["style"] is JObject style) ApplyStyle(widget, style);
            AddToDiagram(diagram, widget);
            SavePage(ctx, page);
            return new { ok = true, page = pageName, panel = req.Value<int>("index"), stateIndex, type, text };
        }
    }

    static void TrySetName(object target, string name)
    {
        try { var p = FindProperty(target.GetType(), "Name"); if (p.CanWrite) p.SetValue(target, name); }
        catch { } // Not every object-model type supports this optional property.
    }

    static object RepeaterAddColumn(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var repeater = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(repeater, "Axure.DocumentModel.Objects.Repeater");
            object data = GetProperty(repeater, "Data");
            int columnIndex = req.Value<int?>("columnIndex") ?? ((ICollection)GetProperty(data, "Columns")).Count;
            Invoke(data, "t0Ps", columnIndex, true);
            if (req["name"] != null) Invoke(data, "T0Pg", columnIndex, req.Value<string>("name"), true);
            Invoke(repeater, "IRrd");
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"), columnIndex };
        }
    }

    static object RepeaterAddRow(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var repeater = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(repeater, "Axure.DocumentModel.Objects.Repeater");
            object data = GetProperty(repeater, "Data");
            int rowIndex = req.Value<int?>("rowIndex") ?? ((ICollection)GetProperty(data, "Rows")).Count;
            if (req["rowIndex"] != null) Invoke(data, "M0Pn", rowIndex, true);
            else Invoke(data, "J0PH", true);
            Invoke(repeater, "IRrd");
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"), rowIndex };
        }
    }

    static object RepeaterSetCell(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var repeater = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(repeater, "Axure.DocumentModel.Objects.Repeater");
            object data = GetProperty(repeater, "Data");
            object raw = req["value"] is JValue v ? v.Value : req["value"];
            object wrapped = FindMethod(data.GetType(), "W0P5").Invoke(null, new object[] { raw });
            Invoke(data, "q0P8", req.Value<int>("row"), req.Value<int>("column"), wrapped, true, true);
            Invoke(repeater, "IRrd");
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                row = req.Value<int>("row"), column = req.Value<int>("column"), value = raw };
        }
    }

    static object RepeaterAddItem(JObject req)
    {
        string path = req.Value<string>("path");
        string pageName = req.Value<string>("page");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, pageName);
            if (page == null) throw new ArgumentException("page not found");
            var repeater = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(repeater, "Axure.DocumentModel.Objects.Repeater");
            object diagram = GetProperty(repeater, "BaseDiagram");
            string type = (req.Value<string>("type") ?? "rectangle").ToLowerInvariant();
            string text = req.Value<string>("text") ?? "";
            double x = req.Value<double?>("x") ?? 0, y = req.Value<double?>("y") ?? 0;
            double w = req.Value<double?>("w") ?? 200, h = req.Value<double?>("h") ?? 40;
            object widget = IsAdvancedWidget(type)
                ? CreateAdvancedWidget(page, type, text, x, y, w, h, req)
                : CreateWidget(page, type, text, x, y, w, h);
            if (req["name"] != null) TrySetName(widget, req.Value<string>("name"));
            if (req["style"] is JObject style) ApplyStyle(widget, style);
            AddToDiagram(diagram, widget);
            Invoke(repeater, "IRrd");
            SavePage(ctx, page);
            return new { ok = true, page = pageName, index = req.Value<int>("index"), type, text };
        }
    }

    static object ListRepeaterData(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var repeater = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(repeater, "Axure.DocumentModel.Objects.Repeater");
            object data = GetProperty(repeater, "Data");
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                columns = ((ICollection)GetProperty(data, "Columns")).Count,
                rows = ((ICollection)GetProperty(data, "Rows")).Count,
                data = SafeDump(data, 4, new HashSet<object>()) };
        }
    }

    static object TableSetCell(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var table = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(table, "Axure.DocumentModel.Objects.Table");
            object cell = Invoke(table, "QRES", req.Value<int>("row"), req.Value<int>("column"));
            TrySetText(cell, req.Value<string>("text") ?? "");
            Invoke(table, "bRsp");
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                row = req.Value<int>("row"), column = req.Value<int>("column") };
        }
    }

    static object TableSetSize(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var table = RootWidget(page, req.Value<int>("index"));
            EnsureWidgetType(table, "Axure.DocumentModel.Objects.Table");
            if (req["rows"] != null) SetProperty(table, "Rows", req.Value<int>("rows"));
            if (req["columns"] != null) SetProperty(table, "Columns", req.Value<int>("columns"));
            Invoke(table, "vRsa");
            Invoke(table, "bRsp");
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                rows = GetProperty(table, "Rows"), columns = GetProperty(table, "Columns") };
        }
    }

    static object AddListOption(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var widget = RootWidget(page, req.Value<int>("index"));
            object owner = Invoke(widget, "Maqe");
            var option = New("EaDD.daDq", owner, req.Value<string>("text") ?? "", req.Value<bool?>("selected") ?? false);
            ((IList)GetProperty(widget, "Options")).Add(option);
            Invoke(widget, "MRc7");
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                text = req.Value<string>("text") };
        }
    }

    static object ListOptions(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var widget = RootWidget(page, req.Value<int>("index"));
            var options = new JArray();
            int i = 0;
            foreach (var option in (IEnumerable)GetProperty(widget, "Options"))
                options.Add(new JObject { ["index"] = i++, ["text"] = Invoke(option, "xaDZ")?.ToString(),
                    ["selected"] = (SafeGet(option, "Selected") is bool b ? new JValue(b) : JValue.CreateNull()) });
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"), options };
        }
    }

    static void ApplyRepeaterData(object repeater, JObject spec)
    {
        object data = GetProperty(repeater, "Data");
        if (spec["columns"] is JArray columns)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                string name = columns[i].Value<string>() ?? $"Column{i + 1}";
                if (i == 0) Invoke(data, "T0Pg", 0, name, true);
                else
                {
                    Invoke(data, "t0Ps", i, true);
                    Invoke(data, "T0Pg", i, name, true);
                }
            }
        }
        if (spec["rows"] is JArray rows)
        {
            foreach (var row in rows)
            {
                int rowIndex = ((ICollection)GetProperty(data, "Rows")).Count;
                Invoke(data, "J0PH", true);
                if (row is JArray values)
                {
                    for (int c = 0; c < values.Count; c++)
                    {
                        object raw = values[c] is JValue v ? v.Value : values[c];
                        object wrapped = FindMethod(data.GetType(), "W0P5").Invoke(null, new object[] { raw });
                        Invoke(data, "q0P8", rowIndex, c, wrapped, true, true);
                    }
                }
            }
        }
        Invoke(repeater, "IRrd");
    }
}
