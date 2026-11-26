using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object SitemapNodeValue(object sitemap, object node)
        => FindMethod(sitemap.GetType(), "s0Hx").Invoke(sitemap, new object[] { node });

    static object FindFolderNode(Ctx ctx, string nameOrId)
    {
        var sitemap = GetProperty(ctx.Doc, "Sitemap");
        object root = GetProperty(sitemap, "Tree");
        return FindFolderNode(ctx, sitemap, root, nameOrId);
    }

    static object FindFolderNode(Ctx ctx, object sitemap, object node, string nameOrId)
    {
        if (node == null) return null;
        object value = SitemapNodeValue(sitemap, node);
        if (value != null && value.GetType().FullName == "j03B.p03X")
        {
            string name = GetProperty(value, "Name")?.ToString();
            string id = GetProperty(value, "FolderId")?.ToString();
            if (string.Equals(name, nameOrId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, nameOrId, StringComparison.OrdinalIgnoreCase)) return node;
        }
        foreach (var child in (IEnumerable)GetProperty(node, "Children"))
        {
            object found = FindFolderNode(ctx, sitemap, child, nameOrId);
            if (found != null) return found;
        }
        return null;
    }

    static string SitemapNodeName(Ctx ctx, object sitemap, object node)
    {
        object value = SitemapNodeValue(sitemap, node);
        if (value == null) return "";
        if (value is Guid id)
        {
            try
            {
                object page = FindMethod(ctx.ManagerType, "k0Xc").Invoke(ctx.Manager, new object[] { id, true, null, Guid.Empty, null });
                return GetProperty(page, "Name")?.ToString() ?? "";
            }
            catch { return id.ToString(); }
        }
        return GetProperty(value, "Name")?.ToString() ?? "";
    }

    static JArray DumpSitemap(Ctx ctx, object sitemap, object node)
    {
        var result = new JArray();
        foreach (var child in (IEnumerable)GetProperty(node, "Children"))
        {
            object value = SitemapNodeValue(sitemap, child);
            bool folder = value != null && value.GetType().FullName == "j03B.p03X";
            string id = folder ? GetProperty(value, "FolderId")?.ToString() : value?.ToString();
            result.Add(new JObject
            {
                ["type"] = folder ? "Folder" : "Page",
                ["id"] = id,
                ["name"] = SitemapNodeName(ctx, sitemap, child),
                ["children"] = folder ? DumpSitemap(ctx, sitemap, child) : new JArray()
            });
        }
        return result;
    }

    static object ListPageTree(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object sitemap = GetProperty(ctx.Doc, "Sitemap");
            object root = GetProperty(sitemap, "Tree");
            var tree = DumpSitemap(ctx, sitemap, root);
            return new { ok = true, path = req.Value<string>("path"), tree };
        }
    }

    static object AddFolder(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var doc = ctx.Doc;
            object sitemap = GetProperty(doc, "Sitemap");
            object parent = GetProperty(sitemap, "Tree");
            if (!string.IsNullOrEmpty(req.Value<string>("parentFolder")))
                parent = FindFolderNode(ctx, req.Value<string>("parentFolder"));
            string name = req.Value<string>("name") ?? "New Folder";
            object folder = New("j03B.p03X", doc, name);
            int index = req.Value<int?>("index") ?? 0;
            object node = FindMethod(sitemap.GetType(), "I0Hg").Invoke(sitemap, new object[] { null, parent, index, folder });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, folder = name, id = GetProperty(folder, "FolderId") };
        }
    }

    static object RenameFolder(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object node = FindFolderNode(ctx, req.Value<string>("folder"));
            if (node == null) throw new ArgumentException("folder not found");
            object sitemap = GetProperty(ctx.Doc, "Sitemap");
            string name = req.Value<string>("name") ?? "Renamed Folder";
            FindMethod(sitemap.GetType(), "Gfpj").Invoke(sitemap, new object[] { node, name });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, folder = name };
        }
    }

    static object DeleteFolder(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object node = FindFolderNode(ctx, req.Value<string>("folder"));
            if (node == null) throw new ArgumentException("folder not found");
            var doc = ctx.Doc;
            object sitemap = GetProperty(doc, "Sitemap");
            var pageIds = new List<Guid>();
            CollectPageIds(sitemap, node, pageIds);
            FindMethod(sitemap.GetType(), "l0HS").Invoke(sitemap, new object[] { node });
            object storage = FindMethod(ctx.ManagerType, "N0B9").Invoke(ctx.Manager, null);
            foreach (var id in pageIds)
            {
                FindMethod(doc.GetType(), "sald", new Type[] { typeof(Guid) }).Invoke(doc, new object[] { id });
                FindMethod(storage.GetType(), "Delete", new Type[] { typeof(Guid), typeof(Guid?) })
                    .Invoke(storage, new object[] { id, null });
            }
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, folder = req.Value<string>("folder"), deletedPages = pageIds.Count };
        }
    }

    static void CollectPageIds(object sitemap, object node, List<Guid> ids)
    {
        foreach (var child in (IEnumerable)GetProperty(node, "Children"))
        {
            object value = SitemapNodeValue(sitemap, child);
            if (value is Guid id) ids.Add(id);
            else CollectPageIds(sitemap, child, ids);
        }
    }

    static object ClonePage(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var source = FindPage(ctx, req.Value<string>("page"));
            if (source == null) throw new ArgumentException("page not found");
            var doc = ctx.Doc;
            var copyMethod = FindMethod(source.GetType(), "Copy", new Type[] { T("zayC.vayw"), typeof(Dictionary<Guid, Guid>).MakeByRefType() });
            object[] copyArgs = { doc, null };
            object clone = copyMethod.Invoke(source, copyArgs);
            if (clone == null) throw new InvalidOperationException("page copy failed");
            object id = GetProperty(clone, "PackageId");
            string name = req.Value<string>("name") ?? (GetProperty(source, "Name") + " Copy");
            object sourceMeta = FindMethod(source.GetType(), "Eavp").Invoke(source, null);
            object meta = FindMethod(sourceMeta.GetType(), "Copy", new Type[] { T("zayC.vayw") }).Invoke(sourceMeta, new object[] { doc });
            SetProperty(meta, "PackageId", id);
            FindMethod(meta.GetType(), "oayM").Invoke(meta, new object[] { name });
            FindMethod(doc.GetType(), "lalu").Invoke(doc, new object[] { meta });
            SetProperty(clone, "Name", name);
            object sitemap = GetProperty(doc, "Sitemap");
            object parent = GetProperty(sitemap, "Tree");
            if (!string.IsNullOrEmpty(req.Value<string>("parentFolder")))
                parent = FindFolderNode(ctx, req.Value<string>("parentFolder"));
            FindMethod(sitemap.GetType(), "I0Hg").Invoke(sitemap, new object[] { null, parent, req.Value<int?>("index") ?? 0, id });
            FindMethod(ctx.ManagerType, "c0XR").Invoke(ctx.Manager, new object[] { clone, null });
            WritePageStream(ctx, clone);
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, source = req.Value<string>("page"), page = name, id };
        }
    }

    static object MovePage(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object sitemap = GetProperty(ctx.Doc, "Sitemap");
            object id = GetProperty(page, "PackageId");
            object node = FindMethod(sitemap.GetType(), "R0HZ", new Type[] { typeof(Guid) }).Invoke(sitemap, new object[] { id });
            object parent = GetProperty(sitemap, "Tree");
            if (!string.IsNullOrEmpty(req.Value<string>("parentFolder")))
                parent = FindFolderNode(ctx, req.Value<string>("parentFolder"));
            int index = req.Value<int?>("index") ?? 0;
            FindMethod(sitemap.GetType(), "l0HS").Invoke(sitemap, new object[] { node });
            FindMethod(sitemap.GetType(), "y0H8").Invoke(sitemap, new object[] { node, parent, index });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, page = req.Value<string>("page"), index };
        }
    }
}
