using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object ListAdaptiveViews(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object set = SafeGet(page, "AdaptiveViewSet");
            var views = new JArray();
            if (set != null)
            {
                object defaultView = GetProperty(set, "DefaultAdaptiveView");
                views.Add(ViewInfo(defaultView, true));
                foreach (var view in (IEnumerable)GetProperty(set, "AdaptiveViews"))
                    views.Add(ViewInfo(view, false));
            }
            return new { ok = true, page = req.Value<string>("page"),
                setId = SafeGet(page, "AdaptiveViewSetId"), views };
        }
    }

    static JObject ViewInfo(object view, bool isDefault)
    {
        object size = SafeGet(view, "Size");
        return new JObject
        {
            ["id"] = SafeGet(view, "PackageId")?.ToString() ?? SafeGet(view, "Id")?.ToString(),
            ["name"] = SafeGet(view, "Name")?.ToString(),
            ["width"] = size == null ? 0 : GetProp(size, "Width"),
            ["height"] = size == null ? 0 : GetProp(size, "Height"),
            ["default"] = isDefault
        };
    }

    static object AddAdaptiveView(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var doc = ctx.Doc;
            object set = SafeGet(page, "AdaptiveViewSet");
            if (set == null)
            {
                set = New("WaFy.AaFK", doc);
                object defaultView = GetProperty(set, "DefaultAdaptiveView");
                SetProperty(defaultView, "Name", "Desktop");
                double defaultWidth = req.Value<double?>("defaultWidth") ?? 1200;
                double defaultHeight = req.Value<double?>("defaultHeight") ?? 800;
                SetProperty(defaultView, "Size", New("F6Bi.B6BA", defaultWidth, defaultHeight));
                FindMethod(doc.GetType(), "lalW").Invoke(doc, new object[] { set });
                FindMethod(page.GetType(), "b0Kz").Invoke(page, new object[] { GetProperty(set, "Id") });
                FindMethod(page.GetType(), "e0y1").Invoke(page, new object[] { New("F6Bi.B6BA", defaultWidth, defaultHeight) });
            }
            object view = New("Xae9.Daep", doc, set);
            SetProperty(view, "Name", req.Value<string>("name") ?? "New View");
            SetProperty(view, "Size", New("F6Bi.B6BA", req.Value<double?>("width") ?? 375, req.Value<double?>("height") ?? 812));
            if (!string.IsNullOrEmpty(req.Value<string>("parentView")))
            {
                object parent = FindAdaptiveView(set, req.Value<string>("parentView"));
                if (parent != null) FindMethod(view.GetType(), "eaex").Invoke(view, new object[] { parent });
            }
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(T("Xae9.Daep")));
            foreach (var old in (IEnumerable)GetProperty(set, "AdaptiveViews")) list.Add(old);
            list.Add(view);
            SetProperty(set, "AdaptiveViews", list);
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), view = GetProperty(view, "Name"),
                id = SafeGet(view, "PackageId")?.ToString() ?? SafeGet(view, "Id")?.ToString() };
        }
    }

    static object FindAdaptiveView(object set, string nameOrId)
    {
        foreach (var view in new[] { GetProperty(set, "DefaultAdaptiveView") }.Concat(((IEnumerable)GetProperty(set, "AdaptiveViews")).Cast<object>()))
        {
            string name = SafeGet(view, "Name")?.ToString();
            string id = SafeGet(view, "PackageId")?.ToString() ?? SafeGet(view, "Id")?.ToString();
            if (string.Equals(name, nameOrId, StringComparison.OrdinalIgnoreCase) || string.Equals(id, nameOrId, StringComparison.OrdinalIgnoreCase))
                return view;
        }
        return null;
    }

    static object SetAdaptiveViewSize(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object set = SafeGet(page, "AdaptiveViewSet");
            if (set == null) throw new ArgumentException("page has no adaptive view set");
            object view = FindAdaptiveView(set, req.Value<string>("view"));
            if (view == null) throw new ArgumentException("view not found");
            object size = New("F6Bi.B6BA", req.Value<double>("width"), req.Value<double>("height"));
            SetProperty(view, "Size", size);
            object id = SafeGet(view, "PackageId") ?? SafeGet(view, "Id");
            if (Convert.ToBoolean(SafeGet(view, "Default") ?? false))
                FindMethod(page.GetType(), "e0y1").Invoke(page, new object[] { size });
            else
            {
                object style = FindMethod(page.GetType(), "AdaptiveStyle", new Type[] { typeof(Guid) }).Invoke(page, new object[] { id });
                FindMethod(style.GetType(), "oanl").Invoke(style, new object[] { Enum.Parse(T("Axure.Constants.PName"), "SIZE"), size, false });
            }
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), view = GetProperty(view, "Name"),
                width = req.Value<double>("width"), height = req.Value<double>("height") };
        }
    }

    static object SetPageSize(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object size = New("F6Bi.B6BA", req.Value<double>("width"), req.Value<double>("height"));
            FindMethod(page.GetType(), "e0y1").Invoke(page, new object[] { size });
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), width = req.Value<double>("width"), height = req.Value<double>("height") };
        }
    }

    static object SetPageStyle(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object style = GetProperty(page, "MyStyle");
            string pname = req.Value<string>("pname");
            object value;
            if (pname == "FILL") value = MakeFill(req.Value<string>("value"));
            else if (pname == "PAGE_ALIGNMENT") value = Enum.Parse(T("Axure.Platform.LayoutAlignment"), req.Value<string>("value"), true);
            else
            {
                var prop = style.GetType().GetProperties().FirstOrDefault(p => p.Name.Replace("_", "").Equals(pname.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
                value = prop != null ? ConvertJsonProperty(req["value"], prop.PropertyType) : req["value"] is JValue v ? v.Value : req["value"];
            }
            FindMethod(style.GetType(), "oanl").Invoke(style, new object[] { Enum.Parse(T("Axure.Constants.PName"), pname, true), value, false });
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), pname, value = value?.ToString() };
        }
    }
}
