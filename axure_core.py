"""axure_core.py — Axure RP 9 GUI 控制原语层（逆向侦察成果的工程化）。

控制面（按可靠性排序）：
1. WinForms 菜单条：UIA 完整暴露，Invoke 或 展开+点击 可执行任意命令
2. 命令行开文件：AxureRP9.exe <path.rp> 直接打开（多实例安全）
3. 标准 Win32 对话框（打开/保存类）：UIA 可枚举；自绘对话框退化为 截图+坐标
4. 画布/面板自绘区：坐标鼠标 + SendKeys + 截图（视觉回读）
5. 设计内容读取：经 HTML 导出后解析（pages_summary）
"""
from __future__ import annotations
import ctypes as C
import ctypes.wintypes as W
import os
import re
import subprocess
import time
from dataclasses import dataclass, field

import uiautomation as auto

auto.SetGlobalSearchTimeout(5)

USER32 = C.windll.user32
# 显式 argtypes：拖拽坐标来自 MCP 是 float，不设 argtypes 会崩（"Don''t know how to convert"）
USER32.SetCursorPos.argtypes = [C.c_int, C.c_int]; USER32.SetCursorPos.restype = W.BOOL
USER32.ShowWindow.argtypes = [W.HWND, C.c_int]
USER32.SwitchToThisWindow.argtypes = [W.HWND, W.BOOL]
USER32.SetWindowPos.argtypes = [W.HWND, W.HWND, C.c_int, C.c_int, C.c_int, C.c_int, W.UINT]
USER32.PostMessageW.argtypes = [W.HWND, W.UINT, W.WPARAM, W.LPARAM]
C.windll.user32.mouse_event.argtypes = [W.DWORD, W.DWORD, W.DWORD, W.DWORD, C.c_size_t]
C.windll.user32.mouse_event.restype = None
USER32.SendMessageTimeoutW.restype = C.c_long
AXURE_EXE = r"D:\Program Files (x86)\Axure\Axure RP 9\AxureRP9.exe"
AXURE_DIR = os.path.dirname(AXURE_EXE)
MAIN_CLASS_MARK = "WindowsForms10.Window.8."  # 主窗体类前缀；808 为浮动子窗


class AxureError(RuntimeError):
    pass


# ---------------------------------------------------------------- 低级工具
def _wrap(hwnd: int):
    """在当前线程直接包装 UIA 元素（元素不可跨线程复用；调用方须已 CoInitialize）。"""
    for _ in range(2):
        try:
            c = auto.ControlFromHandle(hwnd)
            if c is not None:
                return c
        except Exception:
            pass
        time.sleep(0.15)
    return None


def _norm(s: str) -> str:
    """菜单名宽松化：去助记符括注、去空格、小写。'文件(F)' -> '文件'。"""
    s = re.sub(r"[（(][A-Za-zÀ-ÿ][）)]", "", s or "")
    return re.sub(r"\s+", "", s).lower()


def _pid_of(hwnd: int) -> int:
    p = W.DWORD()
    USER32.GetWindowThreadProcessId(hwnd, C.byref(p))
    return p.value


def _title_of(hwnd: int) -> str:
    n = USER32.GetWindowTextLengthW(hwnd) + 1
    b = C.create_unicode_buffer(n)
    USER32.GetWindowTextW(hwnd, b, n)
    return b.value


def _class_of(hwnd: int) -> str:
    b = C.create_unicode_buffer(256)
    USER32.GetClassNameW(hwnd, b, 256)
    return b.value


def _rect_of(hwnd: int):
    r = W.RECT()
    USER32.GetWindowRect(hwnd, C.byref(r))
    return (r.left, r.top, r.right, r.bottom)


def _enum_windows() -> list[int]:
    out: list[int] = []
    proto = C.WINFUNCTYPE(C.c_bool, W.HWND, W.LPARAM)
    def cb(h, _l):
        out.append(h)
        return True
    USER32.EnumWindows(proto(cb), 0)
    return out


# ---------------------------------------------------------------- 实例/窗口
@dataclass
class Instance:
    pid: int
    hwnd: int
    title: str
    rect: tuple
    def as_control(self) -> auto.WindowControl:
        c = _wrap(self.hwnd)
        if c is None:
            raise AxureError(f"窗口句柄 {self.hwnd} 已失效")
        return c

    def try_control(self):
        try:
            return self.as_control()
        except Exception:
            return None


def axure_pids() -> list[int]:
    return sorted({i.pid for i in list_windows()})


def list_windows(pid: int | None = None, visible_only: bool = True) -> list[Instance]:
    res = []
    for h in _enum_windows():
        if visible_only and not USER32.IsWindowVisible(h):
            continue
        p = _pid_of(h)
        if not p:
            continue
        if pid is not None and p != pid:
            continue
        cls = _class_of(h)
        title = _title_of(h)
        if "Axure RP 9" not in title and "Axure" not in cls:
            continue
        if "Axure RP" in title:
            res.append(Instance(p, h, title, _rect_of(h)))
    return res


def dialog_windows(pid: int) -> list[Instance]:
    """同进程、可见、非主窗口的标题窗（对话框/浮动面板）。"""
    out = []
    for h in _enum_windows():
        if not USER32.IsWindowVisible(h) or _pid_of(h) != pid:
            continue
        cls = _class_of(h)
        title = _title_of(h)
        left, top, right, bottom = _rect_of(h)
        w, hh = right - left, bottom - top
        if w < 80 or hh < 50:
            continue
        if "Axure RP 9" in title and cls.startswith(MAIN_CLASS_MARK):
            continue  # 主窗
        if cls.startswith("WindowsForms10.Window.8.") or cls == "#32770":
            out.append(Instance(_pid_of(h), h, title, (left, top, right, bottom)))
    return out


def pick_instance(pid: int | None = None, title_sub: str | None = None,
                  hwnd: int | None = None) -> Instance:
    if hwnd:
        return Instance(_pid_of(hwnd), hwnd, _title_of(hwnd), _rect_of(hwnd))
    insts = list_windows(pid)
    if not insts:
        raise AxureError("未找到运行中的 Axure RP 9 窗口；先 axure_launch 或手动打开")
    if title_sub:
        for i in insts:
            if _norm(title_sub) in _norm(i.title):
                return i
        raise AxureError(f"无标题含 {title_sub!r} 的 Axure 实例")
    # 默认取前台窗口，否则取第一个
    fg = USER32.GetForegroundWindow()
    for i in insts:
        if i.hwnd == fg:
            return i
    return insts[0]


# ---------------------------------------------------------------- 启动
def launch(path: str | None = None, timeout: float = 90.0) -> Instance:
    """命令行直开工程；失败则用 文件>打开最近 兜底；path=None 开空白实例。"""
    base = list_windows()
    absp = os.path.abspath(path) if path else None
    # detached：Axure 不随 MCP server 子进程树被回收，会话结束后仍存活
    if hasattr(subprocess, "DETACHED_PROCESS"):
        subprocess.Popen([AXURE_EXE] + ([absp] if absp else []),
                         cwd=AXURE_DIR,
                         creationflags=getattr(subprocess, "DETACHED_PROCESS", 0x8),
                         close_fds=True)
    else:
        subprocess.Popen([AXURE_EXE] + ([absp] if absp else []), cwd=AXURE_DIR)
    t0 = time.time()
    while time.time() - t0 < timeout:
        time.sleep(1.5)
        cur = list_windows()
        new = [i for i in cur if i.hwnd not in {b.hwnd for b in base}]
        for i in new or cur:
            _sweep_nags(i.pid)
        stem = _norm(os.path.splitext(os.path.basename(path))[0]) if path else None
        for i in new or cur:
            if stem and stem in _norm(i.title):
                return i
        if not path and new:
            return new[-1]
    if path:
        # 兜底：对任意实例走 打开最近
        inst = pick_instance()
        r = run_menu(inst, ["文件", "打开最近", os.path.basename(path)])
        t0 = time.time()
        while time.time() - t0 < 40:
            time.sleep(1)
            stem2 = _norm(os.path.splitext(os.path.basename(path))[0])
            for i in list_windows():
                if stem2 in _norm(i.title):
                    return i
        raise AxureError(f"命令行与最近文件均未能打开 {path}")
    raise AxureError(f"启动 Axure {timeout}s 超时")


def _sweep_nags(pid: int):
    """清掉改版/第三方注入产生的骚扰弹窗（IDM corrupt、Warning 恢复提示）。"""
    for d in dialog_windows(pid):
        if d.title in ("Warning",) or "IDM" in d.title or "corrupt" in d.title.lower():
            try:
                _dismiss(d, prefer=("否", "No", "确定", "OK"))
            except Exception:
                pass


def sweep_nags_all() -> list[int]:
    cleared = []
    for i in list_windows():
        before = {d.hwnd for d in dialog_windows(i.pid)}
        _sweep_nags(i.pid)
        after = {d.hwnd for d in dialog_windows(i.pid)}
        cleared += list(before - after)
    return cleared


BUSY_DIALOG_TITLES = ("发布项目", "进度", "导出图片", "导出所有页面", "生成 Word")

def busy_dialogs(pid: int) -> list[Instance]:
    """命令执行中的模态（发布项目/进度窗——进度窗可能无标题）。"""
    out = []
    for d in dialog_windows(pid):
        l, t, r, b = d.rect
        w, h = r - l, b - t
        if d.title in BUSY_DIALOG_TITLES or (d.title == "" and 150 < w < 3000 and 80 < h < 2000):
            out.append(d)
    return out


def wait_idle(pid: int, timeout: float = 120.0, poll: float = 1.5) -> float:
    """等待无忙模态，返回耗时。"""
    t0 = time.time()
    while time.time() - t0 < timeout:
        if not busy_dialogs(pid):
            return time.time() - t0
        time.sleep(poll)
    raise AxureError(f"等待 Axure 空闲超时 {timeout}s")


def _handle_confirm_dialogs(pid: int, prefer_yes: bool) -> tuple | None:
    """Win32 标准确认对话框（覆盖提示等）：枚举真 Button 子窗、WM_GETTEXT 读名、BM_CLICK。
    不依赖 UIA（某些确认框会让 ViewWalker 抛 E_INVALIDARG）。"""
    BM_CLICK, WM_GETTEXT = 0x00F5, 0x000D
    for d in dialog_windows(pid):
        if d.title in ("另存为", "打开", "Warning", "发布项目", "进度") or not d.title:
            continue
        kids = _enum_children(d.hwnd)
        btns = [k for k in kids if k[1] == "Button"]
        if not btns:
            continue
        labels = {h: _get_text(h) for h, _c, _r in btns}
        want = ("是", "Yes") if prefer_yes else ("否", "No", "取消", "Cancel")
        for w in want:
            hit = next((h for h, t in labels.items() if w in t), None)
            if hit:
                USER32.SetForegroundWindow(d.hwnd)
                time.sleep(0.15)
                _post(hit, BM_CLICK)
                time.sleep(0.45)
                return (d.title, labels[hit])
        # DirectUI/多按钮确认框无文本：按位置策略。
        # 2 按钮: 是(左)/否(右)；3 按钮(Save 类): 保存/不保存/取消
        if all(not t for t in labels.values()) and len(btns) >= 2:
            order = sorted(btns, key=lambda k: k[2][0])
            if len(order) == 3:  # Save/Discard/Cancel
                mode = prefer_yes if isinstance(prefer_yes, str) else ("save" if prefer_yes else "cancel")
                pick = {"save": order[0], "discard": order[1], "cancel": order[2]}[mode]
            elif isinstance(prefer_yes, str):
                pick = order[0] if prefer_yes in ("save", "yes") else order[-1]
            else:
                pick = order[0] if prefer_yes else order[-1]
            USER32.SetForegroundWindow(d.hwnd)
            time.sleep(0.1)
            _post(pick[0], BM_CLICK)
            time.sleep(0.45)
            return (d.title, "bm" + ("Y" if prefer_yes else "N"))
    return None


def clear_blocking_modals(pid: int) -> list[str]:
    """预清理遗留模态：确认框必须点按钮；其余 Esc→WM_CLOSE；进度窗不动。"""
    WM_CLOSE = 0x0010
    cleared = []
    for attempt in range(4):
        ds = [d for d in dialog_windows(pid)
              if d.title not in ("进度", "发布项目") and d.title != ""
              and not d.title.startswith("导出")]
        # 进度/发布/导出 窗由各自流程管理；这里只清 确认/另存为/打开/Warning/IDM
        targets = [d for d in dialog_windows(pid)
                   if (d.title.startswith("确认") or d.title in ("另存为", "打开", "Warning")
                       or "IDM" in d.title or "Save As" in d.title)]
        if not targets:
            break
        progressed = False
        for d in targets:
            if not USER32.IsWindow(d.hwnd):
                continue
            if d.title.startswith("确认") or "Save As" in d.title or "替换" in d.title:
                r = _handle_confirm_dialogs(pid, prefer_yes=False)
                if r is None:
                    r = _handle_confirm_dialogs(pid, prefer_yes=True)
                if r:
                    cleared.append(d.title)
                    progressed = True
                continue
            USER32.SetForegroundWindow(d.hwnd)
            time.sleep(0.2)
            auto.SendKeys("{Esc}")
            time.sleep(0.4)
            if USER32.IsWindow(d.hwnd):
                USER32.PostMessageW(d.hwnd, WM_CLOSE, 0, 0)
                time.sleep(0.4)
            if not USER32.IsWindow(d.hwnd):
                cleared.append(d.title)
                progressed = True
        if not progressed:
            break
        time.sleep(0.4)
    if cleared:
        time.sleep(0.8)
    return cleared


def dismiss_alerts(pid: int) -> list[str]:
    """点掉原生 #32770 Alert/提示框的确定按钮。"""
    done = []
    for d in dialog_windows(pid):
        if d.title in ("Alert", "Warning") or "IDM" in d.title:
            ok = _dismiss(d, prefer=("确定", "OK", "是", "Yes", "否", "No"))
            if ok:
                done.append(d.title)
    return done


def _dismiss(inst: Instance, prefer=("确定", "OK", "是", "Yes")):
    if not USER32.IsWindow(inst.hwnd):
        return True
    USER32.SetForegroundWindow(inst.hwnd)
    time.sleep(0.3)
    ctrl = inst.try_control()
    if ctrl is None:
        return True
    for want in prefer:
        try:
            b = ctrl.ButtonControl(searchDepth=4, SubName=want)
            if b and b.Exists(0.6, 0.2):
                b.GetInvokePattern().Invoke()
                time.sleep(0.4)
                if not USER32.IsWindow(inst.hwnd):
                    return True
        except Exception:
            pass
    auto.SendKeys("{Esc}")
    time.sleep(0.4)
    return not USER32.IsWindow(inst.hwnd)


# ---------------------------------------------------------------- 菜单
class MenuNotFound(AxureError):
    pass


def _descendants(node, depth=0, maxd=6):
    try:
        children = node.GetChildren()
    except Exception:
        return
    for ch in children:
        if ch.ControlTypeName in ("MenuItemControl", "SeparatorControl"):
            yield ch
        yield from _descendants(ch, depth + 1, maxd)


def _find_menu_item(menubar: auto.Control, path: list[str]):
    """路径匹配三级：①逐段精确 ②逐段子串 ③顶级段命中后在子树里找包含全部剩余 token 的项。"""
    toks = [_norm(p) for p in path]

    def exact(node, depth):
        t = toks[depth]
        try:
            children = node.GetChildren()
        except Exception:
            return None
        for ch in children:
            try:
                if _norm(ch.Name) == t:
                    if depth == len(toks) - 1:
                        return ch
                    r = exact(ch, depth + 1)
                    if r is not None:
                        return r
            except Exception:
                continue
        return None

    def sub(node, depth):
        t = toks[depth]
        try:
            children = node.GetChildren()
        except Exception:
            return None
        for ch in children:
            try:
                n = _norm(ch.Name)
            except Exception:
                continue
            if t and (t == n or t in n):
                if depth == len(toks) - 1:
                    return ch
                r = sub(ch, depth + 1)
                if r is not None:
                    return r
        return None

    r = exact(menubar, 0) or sub(menubar, 0)
    if r is not None:
        return r
    # ③ 跨级：顶级段命中 + 子树 token 全包含
    top = sub(menubar, 0) if len(toks) > 1 else None
    if top is None:
        for ch in menubar.GetChildren():
            try:
                if _norm(ch.Name) == toks[0]:
                    top = ch
                    break
            except Exception:
                pass
    if top is not None:
        rest = toks[1:]
        best = None
        for d in _descendants(top):
            if d.ControlTypeName != "MenuItemControl":
                continue
            try:
                n = _norm(d.Name)
            except Exception:
                continue
            if n and all(t in n for t in rest):
                if best is None or len(n) < len(_norm(best.Name)):
                    best = d
        if best is not None:
            return best
    raise MenuNotFound(f"菜单项不存在: {' > '.join(path)}")


def run_menu(inst: Instance, path: list[str], settle: float = 1.5) -> str:
    """执行菜单命令。Invoke 放到后台线程（模态命令会阻塞 COM 调用直到对话框关闭）。
    返回 'invoked' / 'invoked(modal)' / 'clicked'。"""
    win = inst.as_control()
    mb = win.MenuBarControl()
    if not mb.Exists(3, 0.5):
        raise AxureError("菜单条不可用（窗口可能最小化或正在忙）")
    item = _find_menu_item(mb, path)
    before = {d.hwnd for d in dialog_windows(inst.pid)}
    import threading
    st = {"done": False, "err": None}
    def go():
        try:
            with auto.UIAutomationInitializerInThread():
                item.GetInvokePattern().Invoke()
            st["done"] = True
        except Exception as e:
            st["err"] = e
    t = threading.Thread(target=go, daemon=True)
    t.start()
    t.join(settle)
    if t.is_alive():  # 阻塞 = 模态已弹
        return "invoked(modal)"
    if st["done"]:
        return "invoked"
    if dialog_windows(inst.pid) and {d.hwnd for d in dialog_windows(inst.pid)} - before:
        return "invoked(modal)"
    # fallback：沿父链逐级展开后点击末端
    chain = [item]
    node = item.GetParentControl()
    while node is not None and node.ControlTypeName != "MenuBarControl":
        chain.append(node)
        node = node.GetParentControl()
    chain.reverse()
    USER32.SetForegroundWindow(inst.hwnd)
    time.sleep(0.2)
    for seg_node in chain[:-1]:
        try:
            seg_node.GetExpandCollapsePattern().Expand()
            time.sleep(0.35)
        except Exception:
            pass
    try:
        chain[-1].Click(simulateMove=False)
        return "clicked"
    except Exception as e:
        raise AxureError(f"菜单执行失败 {path}: invoke 与 click 均不可用（{e}）")


def _find_child(node: auto.Control, seg: str):
    t = _norm(seg)
    for ch in node.GetChildren():
        try:
            if _norm(ch.Name) == t or t in _norm(ch.Name):
                return ch
        except Exception:
            continue
    return None


def menu_tree(inst: Instance, top: str | None = None, max_depth: int = 3) -> list:
    """枚举菜单结构（top=顶级菜单名过滤）。"""
    mb = inst.as_control().MenuBarControl()
    def dump(node, d=0):
        acc = []
        try:
            children = node.GetChildren()
        except Exception:
            return acc
        for ch in children:
            if ch.ControlTypeName not in ("MenuItemControl", "MenuBarControl"):
                continue
            acc.append({"name": ch.Name, "children": dump(ch, d + 1) if d < max_depth - 1 else []})
        return acc
    if top:
        item = _find_menu_item(mb, [top])
        return [{"name": item.Name, "children": dump(item)}]
    return dump(mb)


# ---------------------------------------------------------------- 前台/输入
def restore(inst: Instance):
    """若最小化则恢复;刷新 rect。"""
    SW_RESTORE = 9
    IsIconic = USER32.IsIconic
    if IsIconic(inst.hwnd):
        USER32.ShowWindow(inst.hwnd, SW_RESTORE)
        time.sleep(0.4)
    r = W.RECT(); USER32.GetWindowRect(inst.hwnd, C.byref(r))
    inst.rect = (r.left, r.top, r.right, r.bottom)
    return inst.rect


def activate(inst: Instance):
    restore(inst)
    USER32.SwitchToThisWindow(inst.hwnd, True)
    time.sleep(0.25)
    return USER32.GetForegroundWindow() == inst.hwnd


def _mouse(x: int, y: int, button: str = "left", action: str = "click",
           n: int = 1):
    """单/双击按下抬起；down 仅按下、up 仅抬起。"""
    USER32.SetCursorPos(int(x), int(y))
    time.sleep(0.06)
    fl = {"left": 0x0002, "right": 0x0008, "middle": 0x0020}[button]
    upf = {"left": 0x0004, "right": 0x0010, "middle": 0x0040}[button]
    if action == "down":
        USER32.mouse_event(fl, 0, 0, 0, 0)
    elif action == "up":
        USER32.mouse_event(upf, 0, 0, 0, 0)
    else:
        for i in range(n):
            USER32.mouse_event(fl, 0, 0, 0, 0)
            USER32.mouse_event(upf, 0, 0, 0, 0)
            if n > 1:
                time.sleep(0.08)


def click(inst: Instance, x: int, y: int, button: str = "left",
          double: bool = False, hold: bool = False):
    activate(inst)
    if hold:
        _mouse(x, y, button, "down")
        return
    _mouse(x, y, button, "click", 2 if double else 1)


def release(inst: Instance, x: int, y: int, button: str = "left"):
    _mouse(x, y, button, "up")


def drag(inst: Instance, x1, y1, x2, y2,
         button: str = "left", steps: int = 20):
    activate(inst)
    x1, y1, x2, y2 = int(x1), int(y1), int(x2), int(y2)
    USER32.SetCursorPos(x1, y1)
    time.sleep(0.1)
    _mouse(x1, y1, button, "down")
    time.sleep(0.08)
    for i in range(1, steps + 1):
        USER32.SetCursorPos(int(x1 + (x2 - x1) * i / steps),
                            int(y1 + (y2 - y1) * i / steps))
        time.sleep(0.012)
    _mouse(x2, y2, button, "up")


def type_text(inst: Instance, text: str, replace: bool = False,
              enter: bool = False):
    activate(inst)
    if replace:
        auto.SendKeys("^{a}")
        time.sleep(0.1)
    auto.SendKeys(text, waitTime=0.05, interval=0.02)
    if enter:
        auto.SendKeys("{Enter}")


def send_keys(inst: Instance, keys: str):
    activate(inst)
    auto.SendKeys(keys)


# ================================================================ 后台通道（零抢焦点/零移鼠标）
# 用 PostMessage 直接向窗口投递输入消息，不调用 SetCursorPos / SetForegroundWindow。
# 坐标一律用「客户区坐标」。适用于 Axure 这类 WinForms 自绘窗口（消息循环会被正常预处理）。

_WM = dict(MOUSEMOVE=0x0200, LBUTTONDOWN=0x0201, LBUTTONUP=0x0202,
           RBUTTONDOWN=0x0204, RBUTTONUP=0x0205, LBUTTONDBLCLK=0x0203,
           KEYDOWN=0x0100, KEYUP=0x0101, CHAR=0x0102, SETFOCUS=0x0007)


def surface_visible(inst: Instance, topmost: bool = True) -> bool:
    """把窗口抬到最上层并标记可见，但 **不激活**（不抢焦点、不移动鼠标）。
    DComp 只有在窗口可见且未被完全遮挡时才提交新帧；这是后台截图能拿到内容的前提。"""
    SWP_NOSIZE = 0x0001; SWP_NOMOVE = 0x0002
    SWP_NOACTIVATE = 0x0010; SWP_SHOWWINDOW = 0x0040
    SW_SHOWNOACTIVATE = 4
    HWND_TOPMOST = -1; HWND_NOTOPMOST = -2
    if USER32.IsIconic(inst.hwnd):
        USER32.ShowWindow(inst.hwnd, SW_SHOWNOACTIVATE)  # 恢复但不激活
        time.sleep(0.4)
    insert = HWND_TOPMOST if topmost else HWND_NOTOPMOST
    ok = USER32.SetWindowPos(inst.hwnd, insert, 0, 0, 0, 0,
                             SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW)
    time.sleep(0.6)   # 留一帧给合成器提交
    return bool(ok)


def client_origin(inst: Instance) -> tuple[int, int]:
    """窗口客户区左上角的屏幕坐标（用于屏幕坐标→客户区坐标换算）。"""
    p = W.POINT(0, 0)
    USER32.ClientToScreen(inst.hwnd, C.byref(p))
    return (p.x, p.y)


def _screen_to_client(inst: Instance, x: float, y: float) -> tuple[int, int]:
    if hasattr(USER32.ClientToScreen, "argtypes") is False:
        pass
    ox, oy = client_origin(inst)
    return (int(x) - ox, int(y) - oy)


def _lparam(x: int, y: int) -> int:
    return ((int(y) & 0xFFFF) << 16) | (int(x) & 0xFFFF)


def bg_click(inst: Instance, x: float, y: float, button: str = "left",
             double: bool = False, screen_coords: bool = True) -> dict:
    """后台点击：PostMessage，不移动真实鼠标、不抢焦点。"""
    cx, cy = _screen_to_client(inst, x, y) if screen_coords else (int(x), int(y))
    lp = _lparam(cx, cy)
    USER32.PostMessageW(inst.hwnd, _WM["MOUSEMOVE"], 0, lp)
    down = _WM["LBUTTONDOWN"] if button == "left" else _WM["RBUTTONDOWN"]
    up = _WM["LBUTTONUP"] if button == "left" else _WM["RBUTTONUP"]
    mk = 0x0001 if button == "left" else 0x0002
    USER32.PostMessageW(inst.hwnd, down, mk, lp)
    USER32.PostMessageW(inst.hwnd, up, 0, lp)
    if double:
        time.sleep(0.05)
        USER32.PostMessageW(inst.hwnd, _WM["LBUTTONDBLCLK"], mk, lp)
        USER32.PostMessageW(inst.hwnd, _WM["LBUTTONUP"], 0, lp)
    return {"mode": "background", "client": [cx, cy], "button": button}


def bg_drag(inst: Instance, x1, y1, x2, y2, steps: int = 18,
            screen_coords: bool = True) -> dict:
    """后台拖拽：整条轨迹都由 PostMessage 派发。"""
    if screen_coords:
        c1 = _screen_to_client(inst, x1, y1)
        c2 = _screen_to_client(inst, x2, y2)
    else:
        c1, c2 = (int(x1), int(y1)), (int(x2), int(y2))
    lp = _lparam(*c1)
    USER32.PostMessageW(inst.hwnd, _WM["MOUSEMOVE"], 0, lp)
    USER32.PostMessageW(inst.hwnd, _WM["LBUTTONDOWN"], 0x0001, lp)
    for i in range(1, steps + 1):
        cx = int(c1[0] + (c2[0] - c1[0]) * i / steps)
        cy = int(c1[1] + (c2[1] - c1[1]) * i / steps)
        USER32.PostMessageW(inst.hwnd, _WM["MOUSEMOVE"], 0x0001, _lparam(cx, cy))
        time.sleep(0.012)
    USER32.PostMessageW(inst.hwnd, _WM["LBUTTONUP"], 0, _lparam(*c2))
    return {"mode": "background", "from": list(c1), "to": list(c2)}


def bg_type(inst: Instance, text: str) -> dict:
    """后台输入文本：WM_CHAR 逐字符投递，不抢焦点。"""
    n = 0
    for ch in text:
        USER32.PostMessageW(inst.hwnd, _WM["CHAR"], ord(ch), 1)
        n += 1
        time.sleep(0.008)
    return {"mode": "background", "chars": n}


def bg_keys(inst: Instance, vk: int, times: int = 1) -> dict:
    """后台按键（VK 码）：WM_KEYDOWN/UP。"""
    for _ in range(times):
        USER32.PostMessageW(inst.hwnd, _WM["KEYDOWN"], vk, 1)
        USER32.PostMessageW(inst.hwnd, _WM["KEYUP"], vk, 0xC0000001)
        time.sleep(0.04)
    return {"mode": "background", "vk": vk, "times": times}


# 常用 VK
VK = dict(TAB=0x09, ENTER=0x0D, ESC=0x1B, SPACE=0x20, BACK=0x08, DELETE=0x2E,
          LEFT=0x25, UP=0x26, RIGHT=0x27, DOWN=0x28, F2=0x71, HOME=0x24, END=0x23)


# ---------------------------------------------------------------- 截图
_gdi_ready = False
def _gdi_setup():
    global _gdi_ready
    if _gdi_ready:
        return
    g = C.windll.gdi32
    u = USER32
    u.GetWindowDC.restype = C.c_void_p; u.GetWindowDC.argtypes = [W.HWND]
    u.ReleaseDC.argtypes = [W.HWND, C.c_void_p]
    g.CreateCompatibleDC.restype = C.c_void_p; g.CreateCompatibleDC.argtypes = [C.c_void_p]
    g.CreateCompatibleBitmap.restype = C.c_void_p; g.CreateCompatibleBitmap.argtypes = [C.c_void_p, C.c_int, C.c_int]
    g.SelectObject.restype = C.c_void_p; g.SelectObject.argtypes = [C.c_void_p, C.c_void_p]
    g.GetDIBits.restype = C.c_int; g.GetDIBits.argtypes = [C.c_void_p, C.c_void_p, W.UINT, W.UINT, C.c_void_p, C.c_void_p, W.UINT]
    g.DeleteDC.argtypes = [C.c_void_p]
    u.PrintWindow.argtypes = [W.HWND, C.c_void_p, W.UINT]; u.PrintWindow.restype = W.BOOL
    _gdi_ready = True


def _printwindow(hwnd: int):
    """直接渲染窗口内容，抗遮挡（不受 topmost 窗口覆盖影响）。返回 PIL Image 或 None。"""
    _gdi_setup()
    r = W.RECT()
    if not USER32.GetWindowRect(hwnd, C.byref(r)):
        return None
    w, h = r.right - r.left, r.bottom - r.top
    if w < 16 or h < 16:
        return None
    from PIL import Image
    hdc = USER32.GetWindowDC(hwnd)
    g = C.windll.gdi32
    mem = g.CreateCompatibleDC(hdc)
    bmp = g.CreateCompatibleBitmap(hdc, w, h)
    g.SelectObject(mem, bmp)
    USER32.PrintWindow(hwnd, mem, 2)  # PW_RENDERFULLCONTENT

    class BIH(C.Structure):
        _fields_ = [("s", W.DWORD), ("w", C.c_long), ("h", C.c_long), ("p", W.WORD), ("b", W.WORD),
                    ("c", W.DWORD), ("i", W.DWORD), ("x", C.c_long), ("y", C.c_long), ("cu", W.DWORD), ("ci", W.DWORD)]
    bi = BIH(); bi.s = C.sizeof(bi); bi.w = w; bi.h = -h; bi.p = 1; bi.b = 32
    buf = C.create_string_buffer(w * h * 4)
    g.GetDIBits(mem, bmp, 0, h, buf, C.byref(bi), 0)
    img = Image.frombuffer("RGBA", (w, h), bytes(buf), "raw", "BGRA").convert("RGB")
    g.DeleteDC(mem); USER32.ReleaseDC(hwnd, hdc)
    return img


def _nonwhite_pct(img) -> float:
    import statistics
    small = img.resize((120, 70))
    px = list(small.getdata())
    nw = sum(1 for q in px if not (q[0] > 245 and q[1] > 245 and q[2] > 245))
    return 100.0 * nw / len(px)


def screenshot(inst: Instance, path: str, region: tuple | None = None) -> str:
    # 主路径：PrintWindow 抗遮挡渲染整窗，再按 region 裁切（region 为相对窗口坐标）
    try:
        img = _printwindow(inst.hwnd)
    except Exception:
        img = None
    if img is not None and _nonwhite_pct(img) > 1.0:
        if region:
            l, t, r, b = region
            l, t, rr, bb = min(l, r), min(t, b), max(l, r), max(t, b)
            img = img.crop((l, t, rr, bb))
        img.save(path)
        return path
    # 回退：屏幕抓取（窗口不可见/最小化时 PrintWindow 可能全白）
    l, t, r, b = region if region else inst.rect
    from PIL import ImageGrab
    ImageGrab.grab(bbox=(min(l, r), min(t, b), max(l, r), max(t, b)), all_screens=True).save(path)
    return path


# ---------------------------------------------------------------- 对话框通用
def dialog_dump(inst: Instance, title_sub: str | None = None,
                screenshot_dir: str | None = None):
    dlg = dialog_windows(inst.pid)
    if title_sub:
        dlg = [d for d in dlg if _norm(title_sub) in _norm(d.title)]
    out = []
    for d in dlg:
        info = {"hwnd": d.hwnd, "title": d.title, "rect": list(d.rect)}
        ctrl = d.try_control()
        if ctrl is None:
            out.append(info); continue
        buttons, edits, texts = [], [], []
        def scan(c, depth=0):
            if depth > 5:
                return
            try:
                ch = c.GetChildren()
            except Exception:
                return
            for x in ch:
                try:
                    n = x.Name or ""
                    t = x.ControlTypeName
                except Exception:
                    continue
                if t == "ButtonControl" and n:
                    buttons.append(n)
                elif t in ("EditControl", "ComboBoxControl"):
                    edits.append({"name": n, "rect": list(x.BoundingRectangle)})
                elif t == "TextControl" and n:
                    texts.append(n[:80])
                scan(x, depth + 1)
        try:
            scan(ctrl)
        except Exception:
            pass
        info.update(buttons=buttons, edits=edits, texts=texts[:30])
        if screenshot_dir:
            p = os.path.join(screenshot_dir, f"dlg_{d.hwnd}.png")
            try:
                screenshot(d, p)
                info["screenshot"] = p
            except Exception:
                pass
        out.append(info)
    return out


def dialog_respond(inst: Instance, title_sub: str, button_text: str | None = None,
                   edit_value: str | None = None, edit_index: int = 0,
                   keys_after: str | None = None) -> bool:
    """向对话框按钮按名 Invoke；Edit 用 UIA SetValue；失败退化为坐标点击+粘贴。"""
    dlgs = [d for d in dialog_windows(inst.pid)
            if _norm(title_sub) in _norm(d.title)]
    if not dlgs:
        raise AxureError(f"无标题含 {title_sub!r} 的对话框")
    d = dlgs[0]
    USER32.SetForegroundWindow(d.hwnd)
    time.sleep(0.25)
    ctrl = d.as_control()
    if edit_value is not None:
        done = False
        edits = ctrl.GetChildren()
        def find_edits(c, acc, depth=0):
            if depth > 5:
                return
            try:
                for x in c.GetChildren():
                    if x.ControlTypeName in ("EditControl", "ComboBoxControl"):
                        acc.append(x)
                    find_edits(x, acc, depth + 1)
            except Exception:
                pass
        acc = []
        find_edits(ctrl, acc)
        if len(acc) > edit_index:
            e = acc[edit_index]
            r = e.BoundingRectangle
            _mouse((r.left + r.right) // 2, (r.top + r.bottom) // 2)
            time.sleep(0.1)
            auto.SendKeys("^{a}")
            time.sleep(0.08)
            _clip_text(edit_value)
            auto.SendKeys("^{v}")
            done = True
        if not done:
            raise AxureError("对话框中未找到可编辑输入框")
    if button_text:
        def find_btn(c, want, depth=0):
            if depth > 5:
                return None
            try:
                for x in c.GetChildren():
                    if x.ControlTypeName == "ButtonControl" and want in _norm(x.Name or ""):
                        return x
                    r = find_btn(x, want, depth + 1)
                    if r is not None:
                        return r
            except Exception:
                return None
            return None
        b = find_btn(ctrl, _norm(button_text))
        if b is None:
            raise AxureError(f"对话框 {d.title!r} 中无按钮 {button_text!r}")
        br = b.BoundingRectangle
        _mouse((br.left + br.right) // 2, (br.top + br.bottom) // 2)
    if keys_after:
        auto.SendKeys(keys_after)
    time.sleep(0.4)
    return True


def open_document(pid: int, path: str, on_dirty: str = "save",
                  timeout: float = 120) -> dict:
    """当前实例 文件>打开 一个 .rp：处理可能的未保存确认（on_dirty=save|discard|cancel）。"""
    d0 = pick_instance(pid=pid)
    before_title = d0.title
    clear_blocking_modals(pid)
    r = run_menu(d0, ["文件", "打开"])
    od = wait_dialog(pid, "打开") or wait_dialog(pid, "Open")
    if od is not None:
        # 直接走文件对话框路径
        try:
            res = file_dialog_act(pid, path, od.title, prefer=True)
        except AxureError:
            res = {}
    else:
        res = {"note": "未检测到打开对话框"}
    # 处理未保存确认
    t0 = time.time()
    while time.time() - t0 < timeout:
        time.sleep(0.6)
        cur = pick_instance(pid=pid)
        if _norm(os.path.basename(path)) in _norm(cur.title):
            return {"opened": True, "title": cur.title, "menu": r, **res}
        conf = [d for d in dialog_windows(pid)
                if d.title in ("Save", "保存") or "未保存" in d.title
                or "changes" in d.title.lower() or "已经存在" in d.title or d.title.startswith("确认")]
        if conf:
            _handle_confirm_dialogs(pid, prefer_yes=on_dirty)
    cur = pick_instance(pid=pid)
    return {"opened": _norm(os.path.basename(path)) in _norm(cur.title),
            "title": cur.title, "menu": r, **res}


def file_dialog_act(pid: int, path: str, title_sub: str,
                    prefer: bool = True) -> dict:
    """通用文件对话框操作（另存为/打开）：WM_SETTEXT 路径 → 左主按钮。"""
    d = wait_dialog(pid, title_sub, exact=True)
    if d is None:
        raise AxureError(f"未见 {title_sub!r} 对话框")
    USER32.SetForegroundWindow(d.hwnd)
    time.sleep(0.4)
    kids = _enum_children(d.hwnd)
    edit = next((k for k in kids if k[1] == "Edit"), None)
    if edit is None:
        raise AxureError("文件对话框中未找到 Edit 子窗")
    _smt(edit[0], 0x000C, 0, os.path.abspath(path))
    time.sleep(0.25)
    kids = _enum_children(d.hwnd)
    btns = sorted([k for k in kids if k[1] == "Button"], key=lambda k: k[2][0])
    if btns:
        _post(btns[0][0], 0x00F5)  # 左按钮=主按钮（打开/保存）
    return {"dialog": d.title, "path": path}


def wait_dialog(pid: int, title_sub: str, timeout: float = 12.0,
                exact: bool = False) -> Instance | None:
    t0 = time.time()
    while time.time() - t0 < timeout:
        for d in dialog_windows(pid):
            hit = (_norm(d.title) == _norm(title_sub)) if exact                 else (_norm(title_sub) in _norm(d.title))
            if hit:
                return d
        time.sleep(0.5)
    return None


def _smt(hwnd, msg, wp=0, lp=None, ms=1500):
    """SendMessageTimeout：防跨进程 UI 线程繁忙导致死锁。返回结果或 None。"""
    res = C.c_long(0)
    try:
        ok = USER32.SendMessageTimeoutW(hwnd, msg, C.wparam(wp),
                                        C.c_void_p(lp) if isinstance(lp, int) else lp,
                                        0x0002, ms, C.byref(res))
    except Exception:
        return None
    return res.value if ok else None


def _post(hwnd, msg, wp=0, lp=0):
    USER32.PostMessageW(hwnd, msg, wp, lp)


def _get_text(hwnd: int, cap: int = 256) -> str:
    buf = C.create_unicode_buffer(cap)
    n = _smt(hwnd, 0x000D, cap, C.byref(buf))
    return buf.value if n else ""


def _enum_children(hwnd: int) -> list[tuple[int, str, tuple]]:
    out = []
    def cb(h, l):
        cn = C.create_unicode_buffer(256)
        USER32.GetClassNameW(h, cn, 256)
        if USER32.IsWindowVisible(h):
            r = W.RECT()
            USER32.GetWindowRect(h, C.byref(r))
            out.append((h, cn.value, (r.left, r.top, r.right, r.bottom)))
        return True
    USER32.EnumChildWindows(hwnd, C.WINFUNCTYPE(C.c_bool, W.HWND, W.LPARAM)(cb), 0)
    return out


def _wm_text(hwnd: int) -> str:
    return _get_text(hwnd)


def file_dialog_save(pid: int, path: str, title_sub: str = "另存为",
                     confirm_overwrite: bool = False) -> dict:
    """对 Axure 的标准 IFileDialog（藏于自绘外壳）：WM_SETTEXT 写文件名子窗
    → BM_CLICK 保存按钮（左按钮）→ 纯 Win32 处理覆盖确认。"""
    WM_SETTEXT, BM_CLICK = 0x000C, 0x00F5
    d = wait_dialog(pid, title_sub, exact=True)
    if d is None:
        raise AxureError(f"未见 {title_sub!r} 对话框")
    USER32.SetForegroundWindow(d.hwnd)
    time.sleep(0.5)
    kids = _enum_children(d.hwnd)
    edit = next((k for k in kids if k[1] == "Edit"), None)
    t0 = time.time()
    while edit is None and time.time() - t0 < 8:
        time.sleep(0.5)
        kids = _enum_children(d.hwnd)
        edit = next((k for k in kids if k[1] == "Edit"), None)
    if edit is None:
        raise AxureError("文件对话框中未找到文件名 Edit 子窗")
    path = os.path.abspath(path)
    _smt(edit[0], WM_SETTEXT, 0, path)
    time.sleep(0.25)
    btns = sorted([k for k in kids if k[1] == "Button"], key=lambda k: k[2][0])
    clicked = False
    if btns:  # 左按钮 = 保存
        _post(btns[0][0], BM_CLICK)
        clicked = True
    t0 = time.time()
    overwrite = None
    while time.time() - t0 < 25:
        time.sleep(0.4)
        res = _handle_confirm_dialogs(pid, prefer_yes=confirm_overwrite)
        if res:
            overwrite = res[1]
        if not USER32.IsWindow(d.hwnd):
            break
        if os.path.exists(path) and not USER32.IsWindow(d.hwnd):
            break
    return {"dialog": d.title, "path": path, "clicked_save": clicked,
            "overwrite_alert": overwrite, "exists": os.path.exists(path)}


# ---------------------------------------------------------------- HTML 导出解析
def read_export_pages(html_dir: str) -> dict:
    """解析生成 HTML 的站点地图：页面=根目录 html 文件名（RP9 导出规则）。"""
    base = os.path.abspath(html_dir)
    if not os.path.isdir(base):
        raise AxureError(f"导出目录不存在: {base}")
    pages = []
    for fn in sorted(os.listdir(base)):
        if not fn.lower().endswith(".html"):
            continue
        stem = fn[:-5]
        if stem.lower() in ("index", "start"):
            continue
        pages.append(stem)
    sitemap = None
    for root, _dirs, files in os.walk(base):
        if "document.js" in files and root.endswith("data"):
            sitemap = os.path.join(root, "document.js")
            break
    index = os.path.join(base, "index.html")
    return {"html_dir": base, "document_js": sitemap,
            "index": index if os.path.exists(index) else None,
            "pages": pages, "page_count": len(pages)}


def find_recent_export(project_stem: str) -> str | None:
    """按导出惯例猜最近的输出目录：<dir>/<stem>/<stem>.html 或 <stem>.files。"""
    # 常见记忆目录由调用方给出；这里只检查 scratch 逻辑不在此层
    return None
