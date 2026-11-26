using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object MasterMap(Ctx ctx) => FindMethod(ctx.Doc.GetType(), "P0fp").Invoke(ctx.Doc, null);

    static object FindMaster(Ctx ctx, string nameOrId)
    {
        object map = MasterMap(ctx);
        object node = FindMasterNode(ctx, map, GetProperty(map, "Tree"), nameOrId);
        if (node == null) return null;
        object id = SitemapNodeValue(map, node);
        return FindMethod(ctx.ManagerType, "k0Xc").Invoke(ctx.Manager, new object[] { id, true, null, Guid.Empty, null });
    }

    static object FindMasterNode(Ctx ctx, object map, object node, string nameOrId)
    {
        if (node == null) return null;
        object value = SitemapNodeValue(map, node);
        if (value is Guid id)
        {
            try
            {
                object master = FindMethod(ctx.ManagerType, "k0Xc").Invoke(ctx.Manager, new object[] { id, true, null, Guid.Empty, null });
                string name = GetProperty(master, "Name")?.ToString();
                if (string.Equals(name, nameOrId, StringComparison.OrdinalIgnoreCase) || string.Equals(id.ToString(), nameOrId, StringComparison.OrdinalIgnoreCase))
                    return node;
            }
            catch { }
        }
        foreach (var child in (IEnumerable)GetProperty(node, "Children"))
        {
            object found = FindMasterNode(ctx, map, child, nameOrId);
            if (found != null) return found;
        }
        return null;
    }

    static object ListMasters(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object map = MasterMap(ctx);
            var masters = new JArray();
            foreach (var node in EnumerateMasterNodes(ctx, map, GetProperty(map, "Tree")))
            {
                object id = SitemapNodeValue(map, node);
                object master = FindMethod(ctx.ManagerType, "k0Xc").Invoke(ctx.Manager, new object[] { id, true, null, Guid.Empty, null });
                masters.Add(new JObject { ["id"] = id.ToString(), ["name"] = GetProperty(master, "Name")?.ToString(),
                    ["widgets"] = GetWidgets(master).Count(), ["type"] = master.GetType().FullName });
            }
            return new { ok = true, path = req.Value<string>("path"), masters, count = masters.Count };
        }
    }

    static IEnumerable<object> EnumerateMasterNodes(Ctx ctx, object map, object node)
    {
        foreach (var child in (IEnumerable)GetProperty(node, "Children"))
        {
            object value = SitemapNodeValue(map, child);
            if (value is Guid) yield return child;
            foreach (var nested in EnumerateMasterNodes(ctx, map, child)) yield return nested;
        }
    }

    static object AddMaster(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var doc = ctx.Doc;
            object cache = FindMethod(doc.GetType(), "NIkS").Invoke(doc, null);
            object master = New("R03D.C03q", doc, cache);
            var id = Guid.NewGuid();
            FindMethod(master.GetType(), "UayB").Invoke(master, new object[] { id });
            string name = req.Value<string>("name") ?? "New Master";
            object meta = FindMethod(doc.GetType(), "falc").Invoke(doc, new object[] { id, GetProperty(master, "TypeName"), name });
            FindMethod(meta.GetType(), "TaEc").Invoke(meta, new object[] { Enum.Parse(T("Axure.Platform.ObjectPersistanceContext.MasterMode"), "Normal") });
            SetProperty(master, "Name", name);
            object map = MasterMap(ctx);
            FindMethod(map.GetType(), "I0Hg").Invoke(map, new object[] { null, GetProperty(map, "Tree"), req.Value<int?>("index") ?? 0, id });
            FindMethod(ctx.ManagerType, "c0XR").Invoke(ctx.Manager, new object[] { master, null });
            WritePageStream(ctx, master);
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, master = name, id };
        }
    }

    static object RenameMaster(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object master = FindMaster(ctx, req.Value<string>("master"));
            if (master == null) throw new ArgumentException("master not found");
            object id = GetProperty(master, "PackageId");
            string name = req.Value<string>("name") ?? "Renamed Master";
            SetProperty(master, "Name", name);
            object meta = FindMethod(ctx.Doc.GetType(), "Vall").Invoke(ctx.Doc, new object[] { id });
            FindMethod(meta.GetType(), "oayM").Invoke(meta, new object[] { name });
            SavePage(ctx, master);
            return new { ok = true, master = name };
        }
    }

    static object DeleteMaster(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object map = MasterMap(ctx);
            object node = FindMasterNode(ctx, map, GetProperty(map, "Tree"), req.Value<string>("master"));
            if (node == null) throw new ArgumentException("master not found");
            object id = SitemapNodeValue(map, node);
            FindMethod(map.GetType(), "l0HS").Invoke(map, new object[] { node });
            FindMethod(ctx.Doc.GetType(), "sald", new Type[] { typeof(Guid) }).Invoke(ctx.Doc, new object[] { id });
            object storage = FindMethod(ctx.ManagerType, "N0B9").Invoke(ctx.Manager, null);
            FindMethod(storage.GetType(), "Delete", new Type[] { typeof(Guid), typeof(Guid?) }).Invoke(storage, new object[] { id, null });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, deleted = req.Value<string>("master"), id };
        }
    }

    static object AddMasterWidget(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object master = FindMaster(ctx, req.Value<string>("master"));
            if (master == null) throw new ArgumentException("master not found");
            string type = (req.Value<string>("type") ?? "rectangle").ToLowerInvariant();
            string text = req.Value<string>("text") ?? "";
            double x = req.Value<double?>("x") ?? 0, y = req.Value<double?>("y") ?? 0;
            double w = req.Value<double?>("w") ?? 200, h = req.Value<double?>("h") ?? 60;
            object widget = IsAdvancedWidget(type) ? CreateAdvancedWidget(master, type, text, x, y, w, h, req) : CreateWidget(master, type, text, x, y, w, h);
            if (req["name"] != null) TrySetName(widget, req.Value<string>("name"));
            if (req["style"] is JObject style) ApplyStyle(widget, style);
            AddToDiagram(GetProperty(master, "Diagram"), widget);
            SavePage(ctx, master);
            return new { ok = true, master = req.Value<string>("master"), type, text };
        }
    }

    static object ApplyMaster(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            object master = FindMaster(ctx, req.Value<string>("master"));
            if (page == null || master == null) throw new ArgumentException("page or master not found");
            object owner = FindMethod(page.GetType(), "Maqe").Invoke(page, null);
            object reference = New("Axure.DocumentModel.Objects.ReferenceDiagramObject", owner, master);
            SetProperty(reference, "CacheService", FindMethod(ctx.Doc.GetType(), "NIkS").Invoke(ctx.Doc, null));
            if (req.Value<bool?>("background") ?? false) SetProperty(reference, "IsBackground", true);
            else PlaceWidget(reference, req.Value<double?>("x") ?? 0, req.Value<double?>("y") ?? 0,
                req.Value<double?>("w") ?? 200, req.Value<double?>("h") ?? 100);
            AddToDiagram(GetProperty(page, "Diagram"), reference);
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), master = req.Value<string>("master") };
        }
    }

    static object RemoveMaster(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            object master = FindMaster(ctx, req.Value<string>("master"));
            if (page == null || master == null) throw new ArgumentException("page or master not found");
            object masterId = GetProperty(master, "PackageId");
            object diagram = GetProperty(page, "Diagram");
            var generic = FindMethod(diagram.GetType(), "J0fw").MakeGenericMethod(T("Axure.DocumentModel.Objects.ReferenceDiagramObject"));
            var refs = ((IEnumerable)generic.Invoke(diagram, new object[] { true, false, true })).Cast<object>()
                .Where(r => object.Equals(GetProperty(r, "ReferredPackageId"), masterId)).ToList();
            foreach (var reference in refs)
            {
                object ownerDiagram = FindMethod(reference.GetType(), "v0e3").Invoke(reference, null);
                FindMethod(ownerDiagram.GetType(), "Remove").Invoke(ownerDiagram, new object[] { reference });
            }
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), master = req.Value<string>("master"), removed = refs.Count };
        }
    }
}
