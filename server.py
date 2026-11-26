"""axure-mcp server — 让 agent 直接控制本机 Axure RP 9（stdio MCP）。

架构：所有 UIA 操作派发到单一 STA 工作线程（COM 亲和），FastMCP 工具层只做参数转发。
"""
from __future__ import annotations
import queue
import threading
import time
import traceback
import json
import subprocess

# --- 专用 UIA worker 线程（COM 初始化一次，跨调用复用） ---------------------
_jobs: "queue.Queue[tuple]" = queue.Queue()

def _worker():
    import ctypes
    ctypes.oledll.ole32.CoInitialize(None)
    import uiautomation as auto
    auto.SetGlobalSearchTimeout(6)
    while True:
        fn, args, kwargs, box, done = _jobs.get()
        try:
            with auto.UIAutomationInitializerInThread():
                box["v"] = fn(*args, **kwargs)
        except Exception as e:
            box["e"] = f"{type(e).__name__}: {e}"
            box["tb"] = traceback.format_exc(limit=4)
        done.set()

def rpc(fn, *args, timeout: float = 120.0, **kwargs):
    box, done = {}, threading.Event()
    _jobs.put((fn, args, kwargs, box, done))
    if not done.wait(timeout):
        raise TimeoutError(f"axure 操作超时 {timeout}s: {fn.__name__}")
    if "e" in box:
        raise RuntimeError(f"axure 操作失败: {box['e']}")
    return box.get("v")

threading.Thread(target=_worker, daemon=True).start()

import axure_core as ax  # noqa: E402
from mcp.server.fastmcp import FastMCP  # noqa: E402

mcp = FastMCP("axure")

OUT_DIR = r"E:\myzcode\axure-mcp\shots"
CODE_BRIDGE = r"E:\myzcode\axure-mcp\bridge\bin\Debug\net48\axure-bridge.exe"
import os
os.makedirs(OUT_DIR, exist_ok=True)


def _code_call(cmd: str, timeout: float = 240, **payload) -> dict:
    """调用纯代码对象模型桥；不启动/聚焦 Axure GUI，不抢鼠标。"""
    req = {"cmd": cmd, **payload}
    try:
        r = subprocess.run(
            [CODE_BRIDGE], input=json.dumps(req, ensure_ascii=False),
            capture_output=True, text=True, encoding="utf-8", timeout=timeout,
        )
    except FileNotFoundError as e:
        raise RuntimeError(f"code bridge not found: {CODE_BRIDGE}; run dotnet build first") from e
    if r.returncode != 0:
        detail = (r.stderr or r.stdout or "bridge failed").strip()
        raise RuntimeError(detail)
    try:
        return json.loads(r.stdout)
    except json.JSONDecodeError as e:
        raise RuntimeError(f"invalid bridge output: {r.stdout[:500]}") from e


def _inst(pid=None, title_sub=None, hwnd=None):
    return ax.pick_instance(pid=pid, title_sub=title_sub, hwnd=hwnd)


# ------------------------------------------------------------------ 状态/生命周期
@mcp.tool()
def axure_status() -> dict:
    """列出运行中的 Axure RP 9 实例、各自标题、当前对话框与忙模态状态。"""
    def _go():
        out = []
        for i in ax.list_windows():
            out.append({"pid": i.pid, "hwnd": i.hwnd, "title": i.title,
                        "rect": list(i.rect),
                        "busy": bool(ax.busy_dialogs(i.pid)),
                        "dialogs": [d.title or "(无标题)" for d in ax.dialog_windows(i.pid)]})
        return {"instances": out, "exe": ax.AXURE_EXE}
    return rpc(_go)


@mcp.tool()
def axure_launch(path: str | None = None, timeout: float = 90) -> dict:
    """启动 Axure RP 9。path 给 .rp 文件则直接打开该工程（命令行通道，最可靠）。"""
    inst = rpc(ax.launch, path, timeout=timeout + 10)
    return {"pid": inst.pid, "hwnd": inst.hwnd, "title": inst.title,
            "rect": list(inst.rect)}


@mcp.tool()
def axure_open(path: str, mode: str = "instance", on_dirty: str = "save", pid: int | None = None) -> dict:
    """打开 .rp 工程。mode=instance：命令行直开新实例（推荐，可靠）；
    mode=dialog：在当前实例走 文件>打开（best-effort），on_dirty=save|discard|cancel。"""
    if mode == "dialog":
        r = rpc(ax.open_document, pid, path, on_dirty, timeout=180)
        return r
    inst = rpc(ax.launch, path, timeout=150)
    return {"pid": inst.pid, "hwnd": inst.hwnd, "title": inst.title,
            "rect": list(inst.rect)}


@mcp.tool()
def axure_save(path: str | None = None, confirm: bool = False,
               pid: int | None = None) -> dict:
    """保存当前工程。不给 path 走 文件>保存；给 path 走 另存为（需要操作保存对话框，
    对话框可被自动化时成功；confirm=true 才允许覆盖已有文件）。"""
    def _go():
        inst = _inst(pid)
        # 预清理遗留模态
        ax.clear_blocking_modals(inst.pid)
        if not path:
            ax.wait_idle(inst.pid, 60)
            r = ax.run_menu(inst, ["文件", "保存"])
            return {"action": "save", "menu": r}
        exists = os.path.exists(path)
        if exists and not confirm:
            raise ax.AxureError("目标已存在，需 confirm=true")
        r = ax.run_menu(inst, ["文件", "另存为"])
        info = ax.file_dialog_save(inst.pid, path, "另存为",
                                   confirm_overwrite=confirm)
        return {"action": "save_as", "menu": r, **info}
    return rpc(_go, timeout=150)


# ------------------------------------------------------------------ 通用命令通道
@mcp.tool()
def axure_ui_command(menu_path: list[str], pid: int | None = None) -> dict:
    """执行任意菜单命令。menu_path 逐级匹配，例如 ["发布","生成 HTML"] 或
    ["视图","重置视图"]。支持子串与跨级 token。"""
    def _go():
        inst = _inst(pid)
        try:
            r = ax.run_menu(inst, menu_path)
        except Exception:
            ax.dismiss_alerts(inst.pid)
            ax.clear_blocking_modals(inst.pid)
            r = ax.run_menu(inst, menu_path)  # 清障后重试一次
        time.sleep(0.4)
        return {"menu": menu_path, "result": r,
                "dialogs": [d.title or "(无标题)" for d in ax.dialog_windows(inst.pid)]}
    return rpc(_go, timeout=60)


@mcp.tool()
def axure_menu_tree(top: str | None = None, pid: int | None = None) -> dict:
    """枚举菜单树（agent 的“能力地图”）。top 给顶级菜单名则只展开该支。"""
    def _go():
        inst = _inst(pid)
        return {"menus": ax.menu_tree(inst, top)}
    return rpc(_go, timeout=150)


@mcp.tool()
def axure_dismiss_nags(pid: int | None = None) -> dict:
    """清理骚扰/恢复/告警弹窗（IDM corrupt、Warning、Alert 确定）。返回处置标题。"""
    def _go():
        pids = [pid] if pid else [i.pid for i in ax.list_windows()]
        out = []
        for p in pids:
            out += ax.dismiss_alerts(p)
            out += [t for t in ax.clear_blocking_modals(p) if t in ("Warning",) or "IDM" in t]
        return {"dismissed": out}
    return rpc(_go, timeout=45)


# ------------------------------------------------------------------ 对话框
@mcp.tool()
def axure_dialog_dump(screenshot: bool = True, pid: int | None = None) -> dict:
    """列出当前对话框：标题/矩形/UIA 可见控件（若有）+ 每个对话框的截图路径。"""
    def _go():
        inst = _inst(pid)
        return {"dialogs": ax.dialog_dump(inst, screenshot_dir=OUT_DIR if screenshot else None)}
    return rpc(_go, timeout=60)


@mcp.tool()
def axure_dialog_respond(title_sub: str, button_text: str | None = None,
                         edit_value: str | None = None, keys: str | None = None,
                         pid: int | None = None) -> dict:
    """操作对话框：按名点按钮 / 向首个输入框填值（剪贴板粘贴，支持中文） / 发按键。"""
    def _go():
        inst = _inst(pid)
        ok = ax.dialog_respond(inst, title_sub, button_text=button_text,
                               edit_value=edit_value, keys_after=keys)
        return {"ok": ok}
    return rpc(_go, timeout=60)


# ------------------------------------------------------------------ 画布输入
@mcp.tool()
def axure_canvas(x: float, y: float, action: str = "click",
                 button: str = "left", relative_to: str = "window",
                 pid: int | None = None, hwnd: int | None = None) -> dict:
    """在 Axure 窗口上操作鼠标。x,y 为屏幕绝对坐标；relative_to=window 时相对窗口左上角。
    action: click | double | right | move | down | up。"""
    def _go():
        inst = _inst(pid, hwnd=hwnd)
        if relative_to == "window":
            l, t, r, b = inst.rect
            x_abs, y_abs = l + x, t + y
        else:
            x_abs, y_abs = x, y
        if action == "double":
            ax.click(inst, x_abs, y_abs, button, double=True)
        elif action == "right":
            ax.click(inst, x_abs, y_abs, "right")
        elif action == "move":
            ax.activate(inst); import ctypes; ctypes.windll.user32.SetCursorPos(int(x_abs), int(y_abs))
        elif action == "down":
            ax.click(inst, x_abs, y_abs, button, hold=True)
        elif action == "up":
            ax.release(inst, x_abs, y_abs, button)
        else:
            ax.click(inst, x_abs, y_abs, button)
        return {"did": action, "at": [x_abs, y_abs]}
    return rpc(_go, timeout=30)


@mcp.tool()
def axure_canvas_drag(x1: float, y1: float, x2: float, y2: float,
                      pid: int | None = None, hwnd: int | None = None) -> dict:
    """窗口内拖拽（相对坐标）。用于画布拉元件/缩放选区。"""
    def _go():
        inst = _inst(pid, hwnd=hwnd)
        l, t, _, _ = inst.rect
        ax.drag(inst, l + x1, t + y1, l + x2, t + y2)
        return {"did": "drag", "from": [x1, y1], "to": [x2, y2]}
    return rpc(_go, timeout=30)


@mcp.tool()
def axure_keys(keys: str, pid: int | None = None) -> dict:
    """向 Axure 前台窗口发按键序列（uiautomation SendKeys 语法：^S {Enter} {ESC}）。"""
    def _go():
        inst = _inst(pid)
        ax.send_keys(inst, keys)
        return {"sent": keys}
    return rpc(_go, timeout=30)


@mcp.tool()
def axure_type(text: str, replace: bool = False, enter: bool = False,
               pid: int | None = None) -> dict:
    """输入文本（剪贴板粘贴路径对中文不稳，走 SendKeys 逐字）。replace=先全选替换。"""
    def _go():
        inst = _inst(pid)
        ax.type_text(inst, text, replace=replace, enter=enter)
        return {"typed_len": len(text)}
    return rpc(_go, timeout=30)


# ------------------------------------------------------------------ 截图
@mcp.tool()
def axure_screenshot(name: str | None = None, region: list[int] | None = None,
                     pid: int | None = None, hwnd: int | None = None) -> dict:
    """截图。默认整窗口；region=[l,t,r,b]（相对窗口像素）截局部。
    注意：若截图全白，多为 MuMu/GameViewer 虚拟显卡劫持 DComp——按 README 设 GpuPreference=2 修复。"""
    def _go():
        inst = _inst(pid, hwnd=hwnd, title_sub=None) if (pid or hwnd) else _inst()
        safe = (name or time.strftime("shot_%H%M%S"))
        p = os.path.join(OUT_DIR, safe + ".png")
        reg = tuple(region) if region else None
        try:
            ax.activate(inst)   # 拉到前台，避免被其它窗口遮挡屏幕抓帧
            time.sleep(0.7)
        except Exception:
            pass
        ax.screenshot(inst, p, region=reg)
        return {"file": p, "rect": list(inst.rect), "region": list(reg) if reg else None}
    return rpc(_go, timeout=40)


# ------------------------------------------------------------------ 导出/读设计
@mcp.tool()
def axure_export_html(pid: int | None = None, timeout: float = 150) -> dict:
    """发布>生成 HTML（使用上次记忆的输出目录）：菜单→对话框→Enter→等空闲。
    用 axure_read_export(目录) 解析结果页面列表。"""
    def _go():
        inst = _inst(pid)
        ax.clear_blocking_modals(inst.pid)
        r = ax.run_menu(inst, ["发布", "生成 HTML"])
        t0 = time.time()
        pub = None
        while time.time() - t0 < 20:
            time.sleep(0.7)
            pubs = [d for d in ax.dialog_windows(inst.pid) if "发布" in d.title]
            if pubs:
                pub = pubs[0]
                break
        if pub is None:
            return {"menu": r, "note": "发布对话框未出现"}
        time.sleep(1.2)
        import ctypes
        u = ctypes.windll.user32
        u.SetForegroundWindow(pub.hwnd)
        time.sleep(0.4)
        import uiautomation as auto
        auto.SetGlobalSearchTimeout(3)
        auto.SendKeys("{Enter}")
        entered = True
        t0 = time.time()
        seen_progress = False
        while time.time() - t0 < timeout:
            time.sleep(1.2)
            busy = ax.busy_dialogs(inst.pid)
            names = [d.title for d in busy]
            if any(n == "" or "进度" in n for n in names):
                seen_progress = True
            still_pub = [d for d in busy if "发布" in d.title]
            if still_pub and not seen_progress and time.time() - t0 > 8:
                # Enter 没吃到：再试一次
                u.SetForegroundWindow(still_pub[0].hwnd)
                time.sleep(0.4)
                auto.SendKeys("{Enter}")
            if not busy:
                return {"menu": r, "generated": True, "saw_progress": seen_progress,
                        "waited_s": round(time.time() - t0, 1)}
        return {"menu": r, "generated": False, "still_busy": [d.title or "(进度)" for d in ax.busy_dialogs(inst.pid)]}
    return rpc(_go, timeout=timeout + 40)


@mcp.tool()
def axure_read_export(html_dir: str) -> dict:
    """解析已生成的 HTML 导出目录 → 页面列表/入口/关键文件。"""
    return rpc(ax.read_export_pages, html_dir, timeout=20)


# ------------------------------------------------------------------ 纯代码对象模型（推荐）
@mcp.tool()
def axure_code_call(cmd: str, payload: dict | None = None, timeout: float = 240) -> dict:
    """通用纯代码桥：直接调用 Axure 内部对象模型，不启动 GUI、不抢鼠标。
    cmd 可用：new-project/list-pages/add-page/rename-page/list-widgets/add-widget/
    update-widget/delete-widget/generate-html。payload 为对应 JSON 参数。"""
    return _code_call(cmd, timeout=timeout, **(payload or {}))


@mcp.tool()
def axure_code_batch(ops: list[dict], timeout: float = 600) -> dict:
    """单进程批量执行多个纯代码命令，避免重复启动 Axure 程序集。"""
    return _code_call("batch", timeout=timeout, ops=ops)


@mcp.tool()
def axure_code_describe_type(name: str, max_members: int = 250) -> dict:
    """反射查看 Axure 内部类型的构造器、属性和方法，便于扩展 API。"""
    return _code_call("describe-type", name=name, max=max_members)


@mcp.tool()
def axure_code_find_types(query: str, max_types: int = 200) -> dict:
    """按名称搜索 Axure 内部 CLR 类型，辅助全功能 API 扩展。"""
    return _code_call("find-types", query=query, max=max_types)


@mcp.tool()
def axure_code_new_project(path: str, name: str = "Untitled", overwrite: bool = False) -> dict:
    """纯代码新建 .rp 工程。默认拒绝覆盖已有文件。"""
    if os.path.exists(path) and not overwrite:
        raise RuntimeError("目标已存在，需 overwrite=true")
    return _code_call("new-project", path=path, name=name)


@mcp.tool()
def axure_code_list_pages(path: str) -> dict:
    """纯代码读取工程页面树。"""
    return _code_call("list-pages", path=path)


@mcp.tool()
def axure_code_project_info(path: str) -> dict:
    """读取工程摘要：文件大小、页数、元件总数、文档类型。"""
    return _code_call("project-info", path=path)


@mcp.tool()
def axure_code_list_page_tree(path: str) -> dict:
    """读取完整页面树，包含文件夹层级。"""
    return _code_call("list-page-tree", path=path)


@mcp.tool()
def axure_code_add_folder(path: str, name: str, parent_folder: str | None = None,
                          index: int = 0) -> dict:
    """添加页面文件夹。"""
    payload = {"path": path, "name": name, "parentFolder": parent_folder, "index": index}
    return _code_call("add-folder", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_rename_folder(path: str, folder: str, name: str) -> dict:
    """重命名页面文件夹。"""
    return _code_call("rename-folder", path=path, folder=folder, name=name)


@mcp.tool()
def axure_code_delete_folder(path: str, folder: str) -> dict:
    """删除文件夹及其中的页面。"""
    return _code_call("delete-folder", path=path, folder=folder)


@mcp.tool()
def axure_code_move_page(path: str, page: str, parent_folder: str | None = None,
                         index: int = 0) -> dict:
    """移动页面到指定文件夹和位置。"""
    payload = {"path": path, "page": page, "parentFolder": parent_folder, "index": index}
    return _code_call("move-page", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_add_page(path: str, name: str, parent_folder: str | None = None,
                        index: int = 0) -> dict:
    """纯代码添加页面，可指定父文件夹和位置。"""
    payload = {"path": path, "name": name, "parentFolder": parent_folder, "index": index}
    return _code_call("add-page", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_rename_page(path: str, page: str, name: str) -> dict:
    """纯代码重命名页面。"""
    return _code_call("rename-page", path=path, page=page, name=name)


@mcp.tool()
def axure_code_delete_page(path: str, page: str) -> dict:
    """纯代码彻底删除页面（页面树摘除 + 页面包物理删除 + 保存）。"""
    return _code_call("delete-page", path=path, page=page)


@mcp.tool()
def axure_code_list_widgets(path: str, page: str) -> dict:
    """纯代码列出页面元件。"""
    return _code_call("list-widgets", path=path, page=page)


@mcp.tool()
def axure_code_get_widget(path: str, page: str, index: int) -> dict:
    """读取元件完整模型：基础信息、样式、交互树（自省用）。"""
    return _code_call("get-widget", path=path, page=page, index=index)


@mcp.tool()
def axure_code_dump_page(path: str, page: str) -> dict:
    """递归读取页面对象树，用于高级对象逆向与验证。"""
    return _code_call("dump-page", path=path, page=page)


@mcp.tool()
def axure_code_list_widget_tree(path: str, page: str, depth: int = 6) -> dict:
    """递归列出页面元件树，包含动态面板状态、中继器模板、表格单元格。"""
    return _code_call("list-widget-tree", path=path, page=page, depth=depth)


@mcp.tool()
def axure_code_get_widget_path(path: str, page: str, widget_path: str) -> dict:
    """按对象树路径读取元件，如 /9/state:1/0。"""
    return _code_call("get-widget-path", path=path, page=page, widgetPath=widget_path)


@mcp.tool()
def axure_code_update_widget_path(path: str, page: str, widget_path: str,
                                  text: str | None = None, name: str | None = None,
                                  x: float | None = None, y: float | None = None,
                                  w: float | None = None, h: float | None = None,
                                  style: dict | None = None) -> dict:
    """按对象树路径修改元件。"""
    payload = {"path": path, "page": page, "widgetPath": widget_path, "style": style}
    for k, v in (("name", name), ("text", text), ("x", x), ("y", y), ("w", w), ("h", h)):
        if v is not None:
            payload[k] = v
    return _code_call("update-widget-path", **payload)


@mcp.tool()
def axure_code_delete_widget_path(path: str, page: str, widget_path: str) -> dict:
    """按对象树路径删除元件。"""
    return _code_call("delete-widget-path", path=path, page=page, widgetPath=widget_path)


@mcp.tool()
def axure_code_add_widget(path: str, page: str, type: str = "rectangle", text: str = "",
                          name: str | None = None, x: float = 100, y: float = 100,
                          w: float = 200, h: float = 60, style: dict | None = None,
                          options: list | None = None, rows: int | None = None,
                          columns: int | None = None, cells: list | None = None,
                          data: dict | None = None, image_path: str | None = None,
                          state_name: str | None = None, selected: bool | None = None,
                          radio_group: str | None = None, placeholder: str | None = None) -> dict:
    """纯代码添加基础/高级元件；高级元件支持 options/rows/columns/cells/data/imagePath/stateName 等参数。"""
    payload = {"path": path, "page": page, "type": type, "text": text,
               "x": x, "y": y, "w": w, "h": h, "style": style}
    for key, value in {
        "name": name, "options": options, "rows": rows, "columns": columns,
        "cells": cells, "data": data, "imagePath": image_path,
        "stateName": state_name, "selected": selected, "radioGroup": radio_group,
        "placeholder": placeholder,
    }.items():
        if value is not None:
            payload[key] = value
    return _code_call("add-widget", **payload)


@mcp.tool()
def axure_code_update_widget(path: str, page: str, index: int, text: str | None = None,
                             name: str | None = None, x: float | None = None,
                             y: float | None = None, w: float | None = None,
                             h: float | None = None, style: dict | None = None) -> dict:
    """纯代码修改元件名称、文本、位置、尺寸或样式。"""
    payload = {"path": path, "page": page, "index": index, "style": style}
    for k, v in (("name", name), ("text", text), ("x", x), ("y", y), ("w", w), ("h", h)):
        if v is not None:
            payload[k] = v
    return _code_call("update-widget", **payload)


@mcp.tool()
def axure_code_set_widget_property(path: str, page: str, index: int,
                                   property: str, value: object) -> dict:
    """通用设置元件可写属性，适用于已逆向但尚未单独封装的基础属性。"""
    return _code_call("set-widget-property", path=path, page=page, index=index,
                      property=property, value=value)


@mcp.tool()
def axure_code_set_style_property(path: str, page: str, index: int,
                                  pname: str, value: object) -> dict:
    """按 Axure PName 通用设置样式属性，如 ROTATION/PADDING_LEFT/OPACITY。"""
    return _code_call("set-style-property", path=path, page=page, index=index,
                      pname=pname, value=value)


@mcp.tool()
def axure_code_set_page_property(path: str, page: str,
                                 property: str, value: object) -> dict:
    """通用设置页面可写属性。"""
    return _code_call("set-page-property", path=path, page=page,
                      property=property, value=value)


@mcp.tool()
def axure_code_delete_widget(path: str, page: str, index: int) -> dict:
    """纯代码删除页面元件。"""
    return _code_call("delete-widget", path=path, page=page, index=index)


@mcp.tool()
def axure_code_add_panel_state(path: str, page: str, index: int, name: str) -> dict:
    """给动态面板添加状态。"""
    return _code_call("add-panel-state", path=path, page=page, index=index, name=name)


@mcp.tool()
def axure_code_add_panel_widget(path: str, page: str, index: int, state_index: int,
                                type: str = "rectangle", text: str = "",
                                x: float = 0, y: float = 0, w: float = 200, h: float = 60,
                                style: dict | None = None) -> dict:
    """向动态面板指定状态添加元件。"""
    return _code_call("add-panel-widget", path=path, page=page, index=index,
                      stateIndex=state_index, type=type, text=text, x=x, y=y, w=w, h=h, style=style)


@mcp.tool()
def axure_code_set_panel_state(path: str, page: str, index: int, state_index: int) -> dict:
    """设置动态面板当前状态。"""
    return _code_call("set-panel-state", path=path, page=page, index=index, stateIndex=state_index)


@mcp.tool()
def axure_code_list_panel_states(path: str, page: str, index: int) -> dict:
    """读取动态面板状态列表。"""
    return _code_call("list-panel-states", path=path, page=page, index=index)


@mcp.tool()
def axure_code_repeater_add_column(path: str, page: str, index: int,
                                   name: str | None = None, column_index: int | None = None) -> dict:
    """给中继器数据集添加列。"""
    payload = {"path": path, "page": page, "index": index, "name": name, "columnIndex": column_index}
    return _code_call("repeater-add-column", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_repeater_add_row(path: str, page: str, index: int,
                               row_index: int | None = None) -> dict:
    """给中继器数据集添加行。"""
    payload = {"path": path, "page": page, "index": index, "rowIndex": row_index}
    return _code_call("repeater-add-row", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_repeater_set_cell(path: str, page: str, index: int,
                                 row: int, column: int, value: object) -> dict:
    """设置中继器数据集单元格。"""
    return _code_call("repeater-set-cell", path=path, page=page, index=index,
                      row=row, column=column, value=value)


@mcp.tool()
def axure_code_repeater_add_item(path: str, page: str, index: int,
                                 type: str = "rectangle", text: str = "",
                                 x: float = 0, y: float = 0, w: float = 200, h: float = 40,
                                 style: dict | None = None) -> dict:
    """向中继器项目模板添加元件。"""
    return _code_call("repeater-add-item", path=path, page=page, index=index,
                      type=type, text=text, x=x, y=y, w=w, h=h, style=style)


@mcp.tool()
def axure_code_list_repeater_data(path: str, page: str, index: int) -> dict:
    """读取中继器数据集。"""
    return _code_call("list-repeater-data", path=path, page=page, index=index)


@mcp.tool()
def axure_code_table_set_cell(path: str, page: str, index: int,
                              row: int, column: int, text: str) -> dict:
    """设置表格单元格文本。"""
    return _code_call("table-set-cell", path=path, page=page, index=index,
                      row=row, column=column, text=text)


@mcp.tool()
def axure_code_table_set_size(path: str, page: str, index: int,
                              rows: int | None = None, columns: int | None = None) -> dict:
    """调整表格行列数。"""
    payload = {"path": path, "page": page, "index": index, "rows": rows, "columns": columns}
    return _code_call("table-set-size", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_add_list_option(path: str, page: str, index: int,
                               text: str, selected: bool = False) -> dict:
    """给下拉框/列表框添加选项。"""
    return _code_call("add-list-option", path=path, page=page, index=index, text=text, selected=selected)


@mcp.tool()
def axure_code_list_options(path: str, page: str, index: int) -> dict:
    """读取下拉框/列表框选项。"""
    return _code_call("list-options", path=path, page=page, index=index)


@mcp.tool()
def axure_code_add_link(path: str, page: str, index: int, target_page: str,
                        event: str = "OnClick") -> dict:
    """纯代码给元件添加交互：当前窗口单击打开目标页面。"""
    return _code_call("add-link", path=path, page=page, index=index,
                      targetPage=target_page, event=event)


@mcp.tool()
def axure_code_list_variables(path: str) -> dict:
    """读取全局变量列表。"""
    return _code_call("list-variables", path=path)


@mcp.tool()
def axure_code_get_variable(path: str, name: str) -> dict:
    """读取全局变量。"""
    return _code_call("get-variable", path=path, name=name)


@mcp.tool()
def axure_code_add_variable(path: str, name: str, value: str = "") -> dict:
    """新增全局变量并设置默认值。"""
    return _code_call("add-variable", path=path, name=name, value=value)


@mcp.tool()
def axure_code_update_variable(path: str, name: str, new_name: str | None = None,
                               value: str | None = None) -> dict:
    """重命名或修改全局变量值。"""
    payload = {"path": path, "name": name, "newName": new_name, "value": value}
    return _code_call("update-variable", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_delete_variable(path: str, name: str) -> dict:
    """删除全局变量。"""
    return _code_call("delete-variable", path=path, name=name)


@mcp.tool()
def axure_code_evaluate_expression(path: str, text: str) -> dict:
    """按 Axure 变量插值规则求值文本表达式。"""
    return _code_call("evaluate-expression", path=path, text=text)


@mcp.tool()
def axure_code_add_page_load_set_variable(path: str, page: str, name: str, value: str) -> dict:
    """添加页面 OnLoad 时设置全局变量的动作。"""
    return _code_call("add-page-load-set-variable", path=path, page=page, name=name, value=value)


@mcp.tool()
def axure_code_list_masters(path: str) -> dict:
    """读取母版列表。"""
    return _code_call("list-masters", path=path)


@mcp.tool()
def axure_code_add_master(path: str, name: str, index: int = 0) -> dict:
    """新建母版。"""
    return _code_call("add-master", path=path, name=name, index=index)


@mcp.tool()
def axure_code_rename_master(path: str, master: str, name: str) -> dict:
    """重命名母版。"""
    return _code_call("rename-master", path=path, master=master, name=name)


@mcp.tool()
def axure_code_delete_master(path: str, master: str) -> dict:
    """删除母版。"""
    return _code_call("delete-master", path=path, master=master)


@mcp.tool()
def axure_code_add_master_widget(path: str, master: str, type: str = "rectangle",
                                 text: str = "", x: float = 0, y: float = 0,
                                 w: float = 200, h: float = 60,
                                 style: dict | None = None) -> dict:
    """向母版添加元件。"""
    return _code_call("add-master-widget", path=path, master=master, type=type, text=text,
                      x=x, y=y, w=w, h=h, style=style)


@mcp.tool()
def axure_code_apply_master(path: str, page: str, master: str,
                            x: float = 0, y: float = 0, w: float = 200,
                            h: float = 100, background: bool = False) -> dict:
    """将母版应用到页面。"""
    return _code_call("apply-master", path=path, page=page, master=master,
                      x=x, y=y, w=w, h=h, background=background)


@mcp.tool()
def axure_code_remove_master(path: str, page: str, master: str) -> dict:
    """从页面移除母版实例。"""
    return _code_call("remove-master", path=path, page=page, master=master)


@mcp.tool()
def axure_code_list_adaptive_views(path: str, page: str) -> dict:
    """读取页面自适应视图。"""
    return _code_call("list-adaptive-views", path=path, page=page)


@mcp.tool()
def axure_code_add_adaptive_view(path: str, page: str, name: str,
                                 width: float = 375, height: float = 812,
                                 default_width: float = 1200,
                                 default_height: float = 800,
                                 parent_view: str | None = None) -> dict:
    """为页面添加自适应视图。"""
    payload = {"path": path, "page": page, "name": name, "width": width, "height": height,
               "defaultWidth": default_width, "defaultHeight": default_height, "parentView": parent_view}
    return _code_call("add-adaptive-view", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_set_adaptive_view_size(path: str, page: str, view: str,
                                      width: float, height: float) -> dict:
    """设置自适应视图尺寸。"""
    return _code_call("set-adaptive-view-size", path=path, page=page, view=view, width=width, height=height)


@mcp.tool()
def axure_code_set_page_size(path: str, page: str, width: float, height: float) -> dict:
    """设置页面尺寸。"""
    return _code_call("set-page-size", path=path, page=page, width=width, height=height)


@mcp.tool()
def axure_code_set_page_style(path: str, page: str, pname: str, value: object) -> dict:
    """设置页面样式属性，如 FILL/PAGE_ALIGNMENT。"""
    return _code_call("set-page-style", path=path, page=page, pname=pname, value=value)


@mcp.tool()
def axure_code_list_named_styles(path: str, kind: str = "widget") -> dict:
    """列出命名样式；kind=widget|page。"""
    return _code_call("list-named-styles", path=path, kind=kind)


@mcp.tool()
def axure_code_get_named_style(path: str, name: str, kind: str = "widget") -> dict:
    """读取命名样式完整模型。"""
    return _code_call("get-named-style", path=path, name=name, kind=kind)


@mcp.tool()
def axure_code_add_named_style(path: str, name: str, kind: str = "widget",
                               style: dict | None = None) -> dict:
    """新建命名样式。"""
    return _code_call("add-named-style", path=path, name=name, kind=kind, style=style)


@mcp.tool()
def axure_code_apply_named_style(path: str, page: str, name: str,
                                 kind: str = "widget", index: int | None = None) -> dict:
    """应用命名样式到元件或页面。"""
    payload = {"path": path, "page": page, "name": name, "kind": kind, "index": index}
    return _code_call("apply-named-style", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_delete_named_style(path: str, name: str, kind: str = "widget") -> dict:
    """删除命名样式。"""
    return _code_call("delete-named-style", path=path, name=name, kind=kind)


@mcp.tool()
def axure_code_add_action(path: str, page: str, index: int, action: str,
                          event: str = "OnClick", target_index: int | None = None,
                          target_path: str | None = None, target_page: str | None = None,
                          url: str | None = None, new_window: bool = False,
                          text: str | None = None, value: object = None,
                          variable: str | None = None, wait_ms: int | None = None, duration: float | None = None,
                          direction: str | None = None, target_event: str | None = None,
                          x: str | None = None, y: str | None = None,
                          w: str | None = None, h: str | None = None,
                          behavior: str | None = None, anchor: str | None = None,
                          condition: dict | None = None) -> dict:
    """给元件添加复杂交互动作：show/hide/toggle/wait/set-text/set-value/set-variable/open-link/
    scroll/fire-event/enable/disable/move/resize。condition 支持文本/选中/可见/禁用条件与 and/or 嵌套。"""
    payload = {
        "path": path, "page": page, "index": index, "action": action, "event": event,
        "targetIndex": target_index, "targetPath": target_path, "targetPage": target_page,
        "url": url, "newWindow": new_window, "text": text, "value": value,
        "variable": variable, "waitMs": wait_ms, "duration": duration, "direction": direction,
        "targetEvent": target_event, "x": x, "y": y, "w": w, "h": h,
        "behavior": behavior, "anchor": anchor, "condition": condition,
    }
    return _code_call("add-action", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_clear_interactions(path: str, page: str, index: int | None = None) -> dict:
    """清空元件或页面的全部交互。"""
    payload = {"path": path, "page": page, "index": index}
    return _code_call("clear-interactions", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_export_word(path: str, out_path: str | None = None) -> dict:
    """纯代码导出 Word 规格说明书。"""
    payload = {"path": path, "outPath": out_path}
    return _code_call("export-word", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_export_page_image(path: str, page: str, out_path: str,
                                 format: str = "png") -> dict:
    """纯代码离屏渲染当前页图片。"""
    return _code_call("export-page-image", path=path, page=page, outPath=out_path, format=format)


@mcp.tool()
def axure_code_export_all_images(path: str, out_dir: str | None = None,
                                 format: str = "png") -> dict:
    """纯代码渲染所有页面图片。"""
    payload = {"path": path, "outDir": out_dir, "format": format}
    return _code_call("export-all-images", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_optimize_image(input_path: str, output_path: str | None = None) -> dict:
    """调用 Axure 图片优化器压缩图片。"""
    payload = {"inputPath": input_path, "outputPath": output_path}
    return _code_call("optimize-image", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_export_csv_report(path: str, out_path: str | None = None) -> dict:
    """导出 CSV 规格报告（页面/母版清单）。"""
    payload = {"path": path, "outPath": out_path}
    return _code_call("export-csv-report", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_export_pdf(path: str, out_path: str | None = None,
                          page: str | None = None) -> dict:
    """纯代码导出图像型 PDF；page 为空时导出全部页面。"""
    payload = {"path": path, "outPath": out_path, "page": page}
    return _code_call("export-pdf", **{k: v for k, v in payload.items() if v is not None})


@mcp.tool()
def axure_code_generate_html(path: str, out_dir: str | None = None, timeout: float = 300) -> dict:
    """纯代码生成可运行的 Axure HTML 原型，返回 index.html。"""
    payload = {"path": path}
    if out_dir:
        payload["outDir"] = out_dir
    return _code_call("generate-html", timeout=timeout, **payload)


def _clip(text: str):
    ax._clip_text(text)


def _shot_dialogs(inst) -> list[str]:
    ps = []
    for d in ax.dialog_windows(inst.pid):
        p = os.path.join(OUT_DIR, f"dlg_{int(time.time())}.png")
        try:
            ax.screenshot(d, p)
            ps.append(p)
        except Exception:
            pass
    return ps


if __name__ == "__main__":
    mcp.run()
