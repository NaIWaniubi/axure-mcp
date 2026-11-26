using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object ListWidgetTree(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            int depth = req.Value<int?>("depth") ?? 6;
            var tree = DumpWidgetTree(page, "", depth, new HashSet<object>());
            return new { ok = true, page = req.Value<string>("page"), widgets = tree, count = tree.Count };
        }
    }

    static (object widget, object container) ResolveWidgetPath(object page, string path)
    {
        var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) throw new ArgumentException("empty widget path");
        object container = page;
        object widget = null;
        int pos = 0;
        while (pos < segments.Length)
        {
            if (!int.TryParse(segments[pos++], out int index)) throw new ArgumentException("invalid widget path: " + path);
            var items = ((IEnumerable)GetProperty(container, "DiagramObjects")).Cast<object>().ToList();
            if (index < 0 || index >= items.Count) throw new ArgumentException("widget path index out of range: " + path);
            widget = items[index];
            if (pos >= segments.Length) break;
            string next = segments[pos++];
            if (next.StartsWith("state:"))
            {
                int stateIndex = int.Parse(next.Substring(6));
                var state = ((IEnumerable)GetProperty(widget, "PanelStates")).Cast<object>().ElementAt(stateIndex);
                container = GetProperty(state, "Diagram");
            }
            else if (next == "item") container = GetProperty(widget, "BaseDiagram");
            else if (next == "cells") container = widget;
            else throw new ArgumentException("unknown widget path segment: " + next);
        }
        return (widget, container);
    }

    static object GetWidgetPath(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var resolved = ResolveWidgetPath(page, req.Value<string>("widgetPath"));
            var info = WidgetInfo(resolved.widget, 0);
            info["path"] = req.Value<string>("widgetPath");
            object ix = null;
            try { ix = FindMethod(resolved.widget.GetType(), "sf04").Invoke(resolved.widget, null); } catch { }
            info["style"] = SafeDump(SafeGet(resolved.widget, "MyStyle"), 3, new HashSet<object>());
            info["interactions"] = SafeDump(ix, 5, new HashSet<object>());
            return new { ok = true, page = req.Value<string>("page"), widget = info };
        }
    }

    static object UpdateWidgetPath(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var resolved = ResolveWidgetPath(page, req.Value<string>("widgetPath"));
            var widget = resolved.widget;
            if (req["name"] != null) TrySetName(widget, req.Value<string>("name"));
            if (req["text"] != null) TrySetText(widget, req.Value<string>("text"));
            if (req["x"] != null || req["y"] != null)
            {
                double x = req.Value<double?>("x") ?? GetLoc(widget, "X");
                double y = req.Value<double?>("y") ?? GetLoc(widget, "Y");
                var locType = FindProperty(widget.GetType(), "Location").PropertyType;
                FindMethod(widget.GetType(), "l0or").Invoke(widget, new object[] { Activator.CreateInstance(locType, x, y), false, true });
            }
            if (req["w"] != null || req["h"] != null)
            {
                double w = req.Value<double?>("w") ?? GetSize(widget, "Width");
                double h = req.Value<double?>("h") ?? GetSize(widget, "Height");
                var sizeType = FindProperty(widget.GetType(), "Size").PropertyType;
                FindMethod(widget.GetType(), "L0ob").Invoke(widget, new object[] { Activator.CreateInstance(sizeType, w, h), false, true });
            }
            if (req["style"] is JObject style) ApplyStyle(widget, style);
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), widgetPath = req.Value<string>("widgetPath") };
        }
    }

    static object DeleteWidgetPath(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var resolved = ResolveWidgetPath(page, req.Value<string>("widgetPath"));
            var coll = GetProperty(resolved.container, "DiagramObjects");
            FindMethod(coll.GetType(), "Remove", new Type[] { T("s0Vz.C0V2") }).Invoke(coll, new object[] { resolved.widget });
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), widgetPath = req.Value<string>("widgetPath") };
        }
    }

    static JArray DumpWidgetTree(object container, string path, int depth, HashSet<object> visited)
    {
        var result = new JArray();
        object coll;
        try { coll = GetProperty(container, "DiagramObjects"); }
        catch { return result; }
        if (!(coll is IEnumerable items)) return result;
        int index = 0;
        foreach (var widget in items)
        {
            if (widget == null) continue;
            string widgetPath = path + "/" + index;
            var node = WidgetInfo(widget, index);
            node["path"] = widgetPath;
            var children = new JArray();
            if (depth > 0 && visited.Add(widget))
            {
                string typeName = widget.GetType().FullName;
                if (typeName == "Axure.DocumentModel.Objects.DynamicPanel")
                {
                    int stateIndex = 0;
                    foreach (var state in (IEnumerable)GetProperty(widget, "PanelStates"))
                    {
                        object diagram = GetProperty(state, "Diagram");
                        children.Add(new JObject
                        {
                            ["type"] = "PanelState",
                            ["index"] = stateIndex,
                            ["name"] = GetProperty(state, "Name")?.ToString(),
                            ["id"] = SafeGet(state, "PackageId")?.ToString(),
                            ["children"] = DumpWidgetTree(diagram, widgetPath + "/state:" + stateIndex, depth - 1, visited)
                        });
                        stateIndex++;
                    }
                }
                else if (typeName == "Axure.DocumentModel.Objects.Repeater")
                {
                    object baseDiagram = GetProperty(widget, "BaseDiagram");
                    children = DumpWidgetTree(baseDiagram, widgetPath + "/item", depth - 1, visited);
                }
                else if (typeName == "Axure.DocumentModel.Objects.Table")
                {
                    children = DumpWidgetTree(widget, widgetPath + "/cells", depth - 1, visited);
                }
                visited.Remove(widget);
            }
            if (children.Count > 0) node["children"] = children;
            result.Add(node);
            index++;
        }
        return result;
    }
}
