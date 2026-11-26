using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

partial class CodeBridge
{
    static object GetInteractionMap(object widget)
    {
        var wt = widget.GetType();
        object ix = FindMethod(wt, "sf04").Invoke(widget, null);
        if (ix == null)
        {
            object owner = FindMethod(wt, "Maqe").Invoke(widget, null);
            ix = New("j0be.K0bF", owner, widget);
            FindMethod(wt, "p0FN").Invoke(widget, new object[] { ix, false, false });
        }
        return ix;
    }

    static object MakeWidgetPath(object owner, object widget)
        => New("M0lV.R0lj", owner, null, null, FindMethod(widget.GetType(), "HaqA").Invoke(widget, null));

    static object MakeStringLiteral(object owner, string value)
        => New("Axure.DocumentModel.Expressions.StringLiteral", owner, value, null, null, null);

    static object MakeLiteral(object owner, JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return MakeStringLiteral(owner, "");
        if (token.Type == JTokenType.Boolean)
            return New("Axure.DocumentModel.Expressions.BooleanLiteral", owner, token.Value<bool>());
        return MakeStringLiteral(owner, token.ToString());
    }

    static object MakeFCall(object owner, string functionName, params object[] args)
    {
        var exprType = T("Axure.DocumentModel.Expressions.Expr");
        var exprArray = Array.CreateInstance(exprType, args.Length);
        for (int i = 0; i < args.Length; i++) exprArray.SetValue(args[i], i);
        return New("Axure.DocumentModel.Expressions.FCall", owner, functionName, exprArray);
    }

    static object MakeCondition(Ctx ctx, object owner, object page, JObject spec)
    {
        if (spec["conditions"] is JArray conditions && (spec.Value<string>("op") == "and" || spec.Value<string>("op") == "or"))
        {
            object current = null;
            foreach (var item in conditions)
            {
                object next = MakeCondition(ctx, owner, page, (JObject)item);
                if (current == null) current = next;
                else current = New("Axure.DocumentModel.Expressions.BinaryExpr", owner,
                    Enum.Parse(T("Axure.Platform.LogicalComparison"), spec.Value<string>("op") == "and" ? "And" : "Or"),
                    current, next);
            }
            return current;
        }

        if ((spec.Value<string>("kind") ?? "") == "global")
        {
            object variable = GetGlobalVariable(ctx.Doc, spec.Value<string>("variable"));
            if (variable == null) throw new ArgumentException("global variable not found");
            object gLeft = New("Axure.DocumentModel.Expressions.GlobalVariableLiteral", owner, variable);
            object gRight = MakeLiteral(owner, spec["value"]);
            string gComparison = (spec.Value<string>("op") ?? "equals") switch
            {
                "not_equals" => "NotEquals",
                "greater" => "GreaterThan",
                "less" => "LessThan",
                "greater_equal" => "GreaterThanEqualTo",
                "less_equal" => "LessThanEqualTo",
                _ => "Equals"
            };
            return New("Axure.DocumentModel.Expressions.BinaryExpr", owner,
                Enum.Parse(T("Axure.Platform.LogicalComparison"), gComparison), gLeft, gRight);
        }

        object target = null;
        if (spec["targetPath"] != null)
            target = ResolveWidgetPath(page, spec.Value<string>("targetPath")).widget;
        else if (spec["targetIndex"] != null)
            target = RootWidget(page, spec.Value<int>("targetIndex"));
        else throw new ArgumentException("condition requires targetIndex or targetPath");

        string kind = spec.Value<string>("kind") ?? "text";
        string function = kind switch
        {
            "selected" => "GetCheckState",
            "visible" => "GetWidgetVisibility",
            "disabled" => "GetDisabledState",
            _ => "GetWidgetText"
        };
        object path = MakeWidgetPath(owner, target);
        object pathExpr = New("Axure.DocumentModel.Expressions.DiagramObjectPathLiteral", owner, path);
        object left = MakeFCall(owner, function, pathExpr);
        object right = MakeLiteral(owner, spec["value"]);
        string op = spec.Value<string>("op") ?? "equals";
        string comparison = op switch
        {
            "not_equals" => "NotEquals",
            "greater" => "GreaterThan",
            "less" => "LessThan",
            "greater_equal" => "GreaterThanEqualTo",
            "less_equal" => "LessThanEqualTo",
            _ => "Equals"
        };
        return New("Axure.DocumentModel.Expressions.BinaryExpr", owner,
            Enum.Parse(T("Axure.Platform.LogicalComparison"), comparison), left, right);
    }

    static object MakeExprAction(object owner, string functionName, params object[] args)
    {
        object block = New("Axure.DocumentModel.Expressions.Block", owner);
        object call = MakeFCall(owner, functionName, args);
        FindMethod(block.GetType(), "kRTQ").Invoke(block, new object[] { call });
        object action = New("Axure.DocumentModel.Interaction.IxActionSetFunction", owner, functionName);
        SetProperty(action, "Action", block);
        return action;
    }

    static object NewDurationAction(string typeName, object owner)
    {
        var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(double));
        object durations = Activator.CreateInstance(dictType);
        var ctor = T(typeName).GetConstructor(new Type[] { T("zayC.vayw"), dictType });
        return ctor.Invoke(new object[] { owner, durations });
    }

    static object CreateInteractionAction(Ctx ctx, object owner, object page, object source, object ix, JObject req)
    {
        string actionName = (req.Value<string>("action") ?? "").ToLowerInvariant();
        object target = source;
        if (req["targetPath"] != null)
            target = ResolveWidgetPath(page, req.Value<string>("targetPath")).widget;
        else if (req["targetIndex"] != null)
            target = RootWidget(page, req.Value<int>("targetIndex"));

        switch (actionName)
        {
            case "open-link":
            {
                bool newWindow = req.Value<bool?>("newWindow") ?? false;
                var linkType = Enum.Parse(T("Axure.DocumentModel.Interaction.LinkType"), newWindow ? "New" : "Current");
                object action = New("Axure.DocumentModel.Interaction.IxLinkWindowAction", owner, linkType);
                object ixTarget = GetProperty(action, "Target");
                if (req["url"] != null)
                {
                    SetProperty(ixTarget, "TargetType", Enum.Parse(T("Axure.DocumentModel.Interaction.IxTargetType"), "WebUrl"));
                    SetProperty(ixTarget, "Hyperlink", req.Value<string>("url"));
                }
                else
                {
                    object targetPage = FindPage(ctx, req.Value<string>("targetPage"));
                    if (targetPage == null) throw new ArgumentException("targetPage not found");
                    SetProperty(ixTarget, "TargetType", Enum.Parse(T("Axure.DocumentModel.Interaction.IxTargetType"), "Page"));
                    SetProperty(ixTarget, "TargetPagePackageId", GetProperty(targetPage, "PackageId"));
                }
                return action;
            }
            case "show":
            case "hide":
            case "toggle":
            {
                object action = New("Axure.DocumentModel.Interaction.IxFadeWidgetAction", owner,
                    Enum.Parse(T("Axure.DocumentModel.Interaction.FadeActionType"),
                        actionName == "show" ? "Show" : actionName == "hide" ? "Hide" : "Toggle"), null);
                object path = MakeWidgetPath(owner, target);
                FindMethod(action.GetType(), "efOh").Invoke(action, new object[] { path });
                if (req["duration"] != null)
                {
                    double d = req.Value<double>("duration");
                    FindMethod(action.GetType(), "jRH2").Invoke(action, new object[] { path, d });
                    FindMethod(action.GetType(), "TRnY").Invoke(action, new object[] { path, d });
                }
                return action;
            }
            case "wait":
            {
                object action = New("Axure.DocumentModel.Interaction.IxWaitAction", owner);
                SetProperty(action, "WaitTime", req.Value<int?>("waitMs") ?? 1000);
                return action;
            }
            case "set-text":
            case "set-value":
            {
                object path = MakeWidgetPath(owner, target);
                object pathExpr = New("Axure.DocumentModel.Expressions.DiagramObjectPathLiteral", owner, path);
                string function = actionName == "set-text" ? "SetWidgetRichText" : "SetWidgetFormText";
                return MakeExprAction(owner, function, pathExpr,
                    MakeStringLiteral(owner, req.Value<string>("text") ?? req.Value<string>("value") ?? ""));
            }
            case "set-variable":
            {
                object variable = GetGlobalVariable(ctx.Doc, req.Value<string>("variable"), true, "");
                if (variable == null) throw new ArgumentException("variable required");
                object variableLiteral = New("Axure.DocumentModel.Expressions.GlobalVariableLiteral", owner, variable);
                return MakeExprAction(owner, "SetGlobalVariableValue", variableLiteral,
                    MakeStringLiteral(owner, req.Value<string>("value") ?? ""));
            }
            case "scroll":
            {
                object action = New("Axure.DocumentModel.Interaction.IxScrollToWidgetAction", owner);
                object path = MakeWidgetPath(owner, target);
                SetProperty(action, "TargetWidgetPath", path);
                SetProperty(action, "Direction", Enum.Parse(T("Axure.Platform.Orientation"), req.Value<string>("direction") ?? "Vertical", true));
                SetProperty(action, "Duration", req.Value<double?>("duration") ?? 500.0);
                return action;
            }
            case "fire-event":
            {
                object action = New("Axure.DocumentModel.Interaction.IxFireEventsAction", owner, false);
                object path = MakeWidgetPath(owner, target);
                FindMethod(action.GetType(), "efOh").Invoke(action, new object[] { path });
                FindMethod(action.GetType(), "TRnQ").Invoke(action, new object[] { path,
                    Enum.Parse(T("Axure.Constants.IxEventType"), req.Value<string>("targetEvent") ?? "OnClick", true) });
                return action;
            }
            case "enable":
            case "disable":
            {
                object action = New("Axure.DocumentModel.Interaction.IxEnableDisableWidgetsAction", owner, actionName == "enable");
                object path = MakeWidgetPath(owner, target);
                FindMethod(action.GetType(), "efOh").Invoke(action, new object[] { path });
                return action;
            }
            case "move":
            {
                object action = NewDurationAction("Axure.DocumentModel.Interaction.IxMoveWidgetAction", owner);
                TrySetProperty(action, "IxMap", ix);
                object path = MakeWidgetPath(owner, target);
                FindMethod(action.GetType(), "efOh").Invoke(action, new object[] { path });
                FindMethod(action.GetType(), "CRHV").Invoke(action, new object[] { path, req.Value<string>("x") ?? "0", null });
                FindMethod(action.GetType(), "tRHo").Invoke(action, new object[] { path, req.Value<string>("y") ?? "0", null });
                FindMethod(action.GetType(), "fRHU").Invoke(action, new object[] { path,
                    Enum.Parse(T("Axure.DocumentModel.Expressions.MoveBehavior"), req.Value<string>("behavior") ?? "Location", true) });
                return action;
            }
            case "resize":
            {
                object action = NewDurationAction("Axure.DocumentModel.Interaction.IxSetWidgetSizeAction", owner);
                TrySetProperty(action, "IxMap", ix);
                object path = MakeWidgetPath(owner, target);
                FindMethod(action.GetType(), "efOh").Invoke(action, new object[] { path });
                FindMethod(action.GetType(), "TRtw").Invoke(action, new object[] { path, req.Value<string>("w") ?? "200", null });
                FindMethod(action.GetType(), "PRtC").Invoke(action, new object[] { path, req.Value<string>("h") ?? "60", null });
                FindMethod(action.GetType(), "oRtk").Invoke(action, new object[] { path, req.Value<string>("anchor") ?? "center" });
                return action;
            }
            default:
                throw new ArgumentException("unsupported interaction action: " + actionName);
        }
    }

    static object CaseValue(object caseNode)
    {
        try { return FindMethod(caseNode.GetType(), "Rihu").Invoke(caseNode, null); }
        catch { return null; }
    }

    static object AttachActionToEvent(object ix, object owner, object eventType, object condition, object action, bool appendToLastCase = false)
    {
        TrySetProperty(action, "IxMap", ix);
        object eventNode = FindMethod(ix.GetType(), "N0bq").Invoke(ix, new object[] { eventType });
        var nodeType = T("a0dU.H0do");
        object caseNode = null;
        if (appendToLastCase)
        {
            foreach (var child in ((IEnumerable)GetProperty(eventNode, "Children")).Cast<object>())
                if (IsCaseNode(child)) caseNode = child;
        }
        if (caseNode == null)
        {
            object kase = New("Axure.DocumentModel.Interaction.IxCase", owner);
            SetProperty(kase, "IxMap", ix);
            if (condition != null) SetProperty(kase, "Condition", condition);
            int append = Convert.ToInt32(FindMethod(nodeType, "X0dn").Invoke(eventNode, null));
            caseNode = FindMethod(ix.GetType(), "I0Hg", new Type[] { nodeType, nodeType, typeof(int), typeof(object) })
                .Invoke(ix, new object[] { null, eventNode, append, kase });
        }
        int actionIndex = Convert.ToInt32(FindMethod(nodeType, "X0dn").Invoke(caseNode, null));
        FindMethod(ix.GetType(), "I0Hg", new Type[] { nodeType, nodeType, typeof(int), typeof(object) })
            .Invoke(ix, new object[] { null, caseNode, actionIndex, action });
        return caseNode;
    }

    static bool IsCaseNode(object node)
    {
        object value = CaseValue(node);
        return value != null && value.GetType().FullName == "Axure.DocumentModel.Interaction.IxCase";
    }

    static object AddInteractionAction(JObject req)
    {
        string path = req.Value<string>("path");
        using (var ctx = LoadContext(path))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            var source = RootWidget(page, req.Value<int>("index"));
            object owner = FindMethod(source.GetType(), "Maqe").Invoke(source, null);
            object ix = GetInteractionMap(source);
            object condition = req["condition"] is JObject c ? MakeCondition(ctx, owner, page, c) : null;
            object action = CreateInteractionAction(ctx, owner, page, source, ix, req);
            object eventType = Enum.Parse(T("Axure.Constants.IxEventType"), req.Value<string>("event") ?? "OnClick", true);
            AttachActionToEvent(ix, owner, eventType, condition, action, req.Value<bool?>("appendToLastCase") ?? false);
            WritePageStream(ctx, page);
            SaveManager(ctx.Manager, path);
            return new { ok = true, page = req.Value<string>("page"), index = req.Value<int>("index"),
                action = req.Value<string>("action"), @event = req.Value<string>("event") ?? "OnClick" };
        }
    }

    static object ClearInteractions(JObject req)
    {
        using (var ctx = LoadContext(req.Value<string>("path")))
        {
            var page = FindPage(ctx, req.Value<string>("page"));
            if (page == null) throw new ArgumentException("page not found");
            object target = req["index"] != null ? RootWidget(page, req.Value<int>("index")) : page;
            object ix = FindMethod(target.GetType(), "sf04").Invoke(target, null);
            if (ix != null)
            {
                object owner = FindMethod(target.GetType(), "Maqe").Invoke(target, null);
                object empty = New("j0be.K0bF", owner, target);
                FindMethod(target.GetType(), "p0FN").Invoke(target, new object[] { empty, false, false });
            }
            SavePage(ctx, page);
            return new { ok = true, page = req.Value<string>("page"), index = req["index"] != null ? req.Value<int>("index") : -1 };
        }
    }

    static void TrySetProperty(object target, string name, object value)
    {
        try { SetProperty(target, name, value); } catch { }
    }
}
