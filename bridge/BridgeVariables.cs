using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object GetGlobalVariable(object doc, string name, bool create = false, string defaultValue = "")
    {
        object set = GetProperty(doc, "GlobalVariableSet");
        object variable = FindMethod(set.GetType(), "qaWL", new Type[] { typeof(string), typeof(bool) })
            .Invoke(set, new object[] { name, false });
        if (variable == null && create)
            variable = FindMethod(set.GetType(), "OaWw", new Type[] { typeof(string), typeof(string) })
                .Invoke(set, new object[] { name, defaultValue });
        return variable;
    }

    static object ListVariables(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object set = GetProperty(ctx.Doc, "GlobalVariableSet");
            var variables = new JArray();
            foreach (var variable in (IEnumerable)FindMethod(set.GetType(), "VaWQ").Invoke(set, null))
            {
                variables.Add(new JObject
                {
                    ["name"] = GetProperty(variable, "Name")?.ToString(),
                    ["value"] = FindMethod(variable.GetType(), "SaWv").Invoke(variable, null)?.ToString(),
                    ["id"] = FindMethod(variable.GetType(), "CfpW").Invoke(variable, null)?.ToString()
                });
            }
            return new { ok = true, path = req.Value<string>("path"), variables, count = variables.Count };
        }
    }

    static object GetVariable(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object variable = GetGlobalVariable(ctx.Doc, req.Value<string>("name"));
            if (variable == null) throw new ArgumentException("variable not found: " + req.Value<string>("name"));
            return new { ok = true, name = GetProperty(variable, "Name"),
                value = FindMethod(variable.GetType(), "SaWv").Invoke(variable, null),
                id = FindMethod(variable.GetType(), "CfpW").Invoke(variable, null) };
        }
    }

    static object AddVariable(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            string name = req.Value<string>("name");
            string value = req.Value<string>("value") ?? "";
            object variable = GetGlobalVariable(ctx.Doc, name, true, value);
            if (variable == null) throw new InvalidOperationException("cannot create variable");
            FindMethod(variable.GetType(), "laWt").Invoke(variable, new object[] { value });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, name, value };
        }
    }

    static object UpdateVariable(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object variable = GetGlobalVariable(ctx.Doc, req.Value<string>("name"));
            if (variable == null) throw new ArgumentException("variable not found");
            if (req["newName"] != null) SetProperty(variable, "Name", req.Value<string>("newName"));
            if (req["value"] != null) FindMethod(variable.GetType(), "laWt").Invoke(variable, new object[] { req.Value<string>("value") });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, name = GetProperty(variable, "Name"),
                value = FindMethod(variable.GetType(), "SaWv").Invoke(variable, null) };
        }
    }

    static object DeleteVariable(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            object variable = GetGlobalVariable(ctx.Doc, req.Value<string>("name"));
            if (variable == null) throw new ArgumentException("variable not found");
            object set = GetProperty(ctx.Doc, "GlobalVariableSet");
            FindMethod(set.GetType(), "VaWC").Invoke(set, new object[] { variable });
            SaveManager(ctx.Manager, req.Value<string>("path"));
            return new { ok = true, deleted = req.Value<string>("name") };
        }
    }

    static object EvaluateExpression(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            string text = req.Value<string>("text") ?? "";
            string value = Regex.Replace(text, @"\[\[([^\]]+)\]\]", m =>
            {
                object variable = GetGlobalVariable(ctx.Doc, m.Groups[1].Value.Trim());
                return variable == null ? m.Value : FindMethod(variable.GetType(), "SaWv").Invoke(variable, null)?.ToString() ?? "";
            });
            return new { ok = true, text, value };
        }
    }

    static object AddPageLoadSetVariable(JObject req)
    {
        string path = req.Value<string>("path");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            string name = req.Value<string>("name");
            string value = req.Value<string>("value") ?? "";
            object variable = GetGlobalVariable(ctx.Doc, name, true, "");
            if (variable == null) throw new InvalidOperationException("cannot create variable");
            object owner = FindMethod(page.GetType(), "Maqe").Invoke(page, null);
            object ix = GetInteractionMap(page);
            object variableLiteral = New("Axure.DocumentModel.Expressions.GlobalVariableLiteral", owner, variable);
            object action = MakeExprAction(owner, "SetGlobalVariableValue", variableLiteral, MakeStringLiteral(owner, value));
            object eventType = Enum.Parse(T("Axure.Constants.IxEventType"), "OnLoad");
            AttachActionToEvent(ix, owner, eventType, null, action);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = req.Value<string>("page"), name, value, @event = "OnLoad" };
        }
    }
}
