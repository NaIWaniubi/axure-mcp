using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object StyleSheet(Ctx ctx, string kind)
        => GetProperty(ctx.Doc, string.Equals(kind, "page", StringComparison.OrdinalIgnoreCase) ? "PageStyleSheet" : "StyleSheet");

    static IList NamedStyleList(object sheet)
        => (IList)sheet.GetType().GetField("ya24", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sheet);

    static object ListNamedStyles(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            string kind = req.Value<string>("kind") ?? "widget";
            object sheet = StyleSheet(ctx, kind);
            var styles = new JArray();
            foreach (var style in NamedStyleList(sheet))
                styles.Add(new JObject { ["name"] = SafeGet(style, "Name")?.ToString(),
                    ["type"] = style.GetType().FullName });
            return new { ok = true, kind, styles, count = styles.Count };
        }
    }

    static object AddNamedStyle(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            string kind = req.Value<string>("kind") ?? "widget";
            object sheet = StyleSheet(ctx, kind);
            string name = req.Value<string>("name") ?? "NewStyle";
            object style = FindMethod(sheet.GetType(), "XfAf").Invoke(sheet, new object[] { null });
            SetProperty(style, "Name", name);
            if (req["style"] is JObject spec) ApplyStyleSpec(style, spec);
            if (!string.Equals(kind, "page", StringComparison.OrdinalIgnoreCase))
                FindMethod(T("WaBK.LaBu"), "GaBX").Invoke(null, new object[] { style, null });
            NamedStyleList(sheet).Add(style);
            FindMethod(sheet.GetType(), "Ta29").Invoke(sheet, new object[] { EventArgs.Empty });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, kind, name };
        }
    }

    static void ApplyStyleSpec(object style, JObject spec)
    {
        if (spec["fill"] != null) SetStyle(style, "FILL", MakeFill(spec.Value<string>("fill")));
        if (spec["textColor"] != null) SetStyle(style, "FORE_GROUND_FILL", MakeFill(spec.Value<string>("textColor")));
        if (spec["borderColor"] != null) SetStyle(style, "BORDER_FILL", MakeFill(spec.Value<string>("borderColor")));
        if (spec["fontSize"] != null) SetStyle(style, "FONT_SIZE", spec.Value<double>("fontSize"));
        if (spec["fontName"] != null) SetStyle(style, "FONT_NAME", spec.Value<string>("fontName"));
        if (spec["bold"] != null) SetStyle(style, "BOLD", Enum.Parse(T("Axure.Constants.BoolOverride"), spec.Value<bool>("bold") ? "TRUE" : "FALSE"));
        if (spec["italic"] != null) SetStyle(style, "ITALIC", Enum.Parse(T("Axure.Constants.BoolOverride"), spec.Value<bool>("italic") ? "TRUE" : "FALSE"));
        if (spec["underline"] != null) SetStyle(style, "UNDERLINE", spec.Value<bool>("underline"));
        if (spec["borderWidth"] != null) SetStyle(style, "BORDER_WIDTH", spec.Value<double>("borderWidth"));
        if (spec["linePattern"] != null) SetStyle(style, "LINE_PATTERN", Enum.Parse(T("Axure.Platform.LineStyle"), spec.Value<string>("linePattern"), true));
        if (spec["cornerRadius"] != null) SetStyle(style, "CORNER_RADIUS", spec.Value<double>("cornerRadius"));
        if (spec["opacity"] != null) SetStyle(style, "OPACITY", spec.Value<double>("opacity"));
    }

    static object ApplyNamedStyle(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            string kind = req.Value<string>("kind") ?? "widget";
            object sheet = StyleSheet(ctx, kind);
            object named = FindMethod(sheet.GetType(), "ya2h").Invoke(sheet, new object[] { req.Value<string>("name") });
            if (named == null) throw new ArgumentException("named style not found");
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object namedId = GetProperty(named, "Id");
            if (string.Equals(kind, "page", StringComparison.OrdinalIgnoreCase))
            {
                FindMethod(page.GetType(), "g0KZ").Invoke(page, new object[] { named });
                SetStyle(GetProperty(page, "MyStyle"), "BASE_STYLE", namedId);
            }
            else
            {
                object widget = RootWidget(page, req.Value<int>("index"));
                FindMethod(widget.GetType(), "L0Uo").Invoke(widget, new object[] { named });
                SetStyle(GetProperty(widget, "MyStyle"), "BASE_STYLE", namedId);
            }
            SavePage(ctx, page);
            return new { ok = true, kind, page = req.Value<string>("page"), name = req.Value<string>("name") };
        }
    }

    static object GetNamedStyle(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            string kind = req.Value<string>("kind") ?? "widget";
            object sheet = StyleSheet(ctx, kind);
            object named = FindMethod(sheet.GetType(), "ya2h").Invoke(sheet, new object[] { req.Value<string>("name") });
            if (named == null) throw new ArgumentException("named style not found");
            return new { ok = true, kind, name = GetProperty(named, "Name"), style = SafeDump(named, 3, new HashSet<object>()) };
        }
    }

    static object DeleteNamedStyle(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            string kind = req.Value<string>("kind") ?? "widget";
            object sheet = StyleSheet(ctx, kind);
            object named = FindMethod(sheet.GetType(), "ya2h").Invoke(sheet, new object[] { req.Value<string>("name") });
            if (named == null) throw new ArgumentException("named style not found");
            NamedStyleList(sheet).Remove(named);
            FindMethod(sheet.GetType(), "Ta29").Invoke(sheet, new object[] { EventArgs.Empty });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, kind, deleted = req.Value<string>("name") };
        }
    }
}
