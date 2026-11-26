"""Pure-code Axure workspace prototype builder.

Creates ai_workspace.rp and exports a runnable HTML prototype using only the
C# object-model bridge. No GUI automation, mouse, or keyboard is used.
"""
from __future__ import annotations

import json
import os
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT / "bridge" / "bin" / "Debug" / "net48" / "axure-bridge.exe"
RP = ROOT / "demo" / "ai_workspace.rp"
HTML = ROOT / "demo" / "ai_workspace_html"
_QUEUE: list[dict] = []
_QUEUING = False


def _raw_call(req: dict) -> dict:
    r = subprocess.run(
        [str(EXE)], input=json.dumps(req, ensure_ascii=False),
        capture_output=True, text=True, encoding="utf-8", timeout=300,
    )
    if r.returncode:
        raise RuntimeError(f"{req.get('cmd')} failed:\n{r.stderr or r.stdout}")
    return json.loads(r.stdout)


def call(cmd: str, **payload) -> dict:
    global _QUEUE
    req = {"cmd": cmd, **payload}
    if _QUEUING:
        _QUEUE.append(req)
        return {}
    return _raw_call(req)


def flush() -> dict:
    global _QUEUE
    if not _QUEUE:
        return {"ok": True, "count": 0, "results": []}
    result = _raw_call({"cmd": "batch", "ops": _QUEUE})
    _QUEUE = []
    return result


def rect(page: str, text: str, x: float, y: float, w: float, h: float, **style) -> None:
    call("add-widget", path=str(RP), page=page, type="rectangle", text=text,
         x=x, y=y, w=w, h=h, style=style)


def button(page: str, text: str, x: float, y: float, w: float, h: float, **style) -> None:
    call("add-widget", path=str(RP), page=page, type="button", text=text,
         x=x, y=y, w=w, h=h, style=style)


def text(page: str, value: str, x: float, y: float, w: float, h: float,
         size: float = 18, color: str = "#0F172A", bold: bool = False,
         fill: str = "#FFFFFF") -> None:
    rect(page, value, x, y, w, h, fill=fill, borderWidth=0,
         fontSize=size, textColor=color, bold=bold)


def side_text(page: str, value: str, x: float, y: float, w: float, h: float,
              size: float = 18, color: str = "#94A3B8", bold: bool = False) -> None:
    text(page, value, x, y, w, h, size, color, bold, fill="#0F172A")


def main_text(page: str, value: str, x: float, y: float, w: float, h: float,
              size: float = 18, color: str = "#0F172A", bold: bool = False) -> None:
    text(page, value, x, y, w, h, size, color, bold, fill="#F5F7FB")


def card(page: str, x: float, y: float, w: float, h: float) -> None:
    rect(page, "", x, y, w, h, fill="#FFFFFF", borderColor="#E2E8F0",
         borderWidth=1, cornerRadius=14)


def link(page: str, index: int, target: str) -> None:
    call("add-link", path=str(RP), page=page, index=index, targetPage=target)


def build() -> None:
    global _QUEUING
    if HTML.exists():
        shutil.rmtree(HTML)
    _QUEUING = True
    call("new-project", path=str(RP), name="AI 原型工作台")

    # Add in reverse because the bridge inserts new pages at tree index 0.
    for name in ("详情页", "工作台", "登录页", "首页"):
        call("add-page", path=str(RP), name=name)

    # ---------------------------------------------------------------- 首页
    p = "首页"
    rect(p, "", 0, 0, 1200, 800, fill="#F5F7FB", borderWidth=0)
    rect(p, "", 0, 0, 240, 800, fill="#0F172A", borderWidth=0)
    rect(p, "AI", 32, 30, 64, 64, fill="#2563EB", borderWidth=0,
         cornerRadius=16, fontSize=28, textColor="#FFFFFF", bold=True)
    side_text(p, "原型工作台", 112, 38, 150, 40, 22, "#FFFFFF", True)
    side_text(p, "OVERVIEW", 34, 132, 160, 24, 12, "#64748B", True)
    side_text(p, "首页", 34, 178, 160, 34, 18, "#FFFFFF", True)
    side_text(p, "登录页", 34, 226, 160, 34, 18, "#94A3B8")
    side_text(p, "工作台", 34, 274, 160, 34, 18, "#94A3B8")
    side_text(p, "详情页", 34, 322, 160, 34, 18, "#94A3B8")
    side_text(p, "PURE CODE / ZERO GUI", 34, 736, 190, 24, 10, "#64748B", True)

    main_text(p, "欢迎使用 AI 原型", 288, 54, 620, 54, 34, "#0F172A", True)
    main_text(p, "这个工程完全由代码创建：页面、元件、样式与导出均未使用鼠标。",
         288, 116, 760, 36, 16, "#64748B")
    button(p, "进入工作台", 920, 64, 220, 52, fill="#2563EB", textColor="#FFFFFF",
           borderWidth=0, cornerRadius=12, fontSize=18, bold=True)

    card(p, 288, 190, 268, 150)
    text(p, "页面", 312, 212, 100, 26, 14, "#64748B", True)
    text(p, "4", 312, 244, 100, 56, 36, "#0F172A", True)
    text(p, "完整信息架构", 312, 304, 200, 22, 12, "#94A3B8")

    card(p, 576, 190, 268, 150)
    text(p, "元件", 600, 212, 100, 26, 14, "#64748B", True)
    text(p, "24+", 600, 244, 120, 56, 36, "#0F172A", True)
    text(p, "代码级对象模型", 600, 304, 200, 22, 12, "#94A3B8")

    card(p, 864, 190, 268, 150)
    text(p, "GUI 操作", 888, 212, 120, 26, 14, "#64748B", True)
    text(p, "0", 888, 244, 100, 56, 36, "#16A34A", True)
    text(p, "不抢鼠标 / 不抢焦点", 888, 304, 210, 22, 12, "#94A3B8")

    card(p, 288, 374, 844, 300)
    text(p, "能力清单", 320, 402, 300, 36, 22, "#0F172A", True)
    for i, (title, desc) in enumerate([
        ("01  对象模型 CRUD", "工程、页面、元件、样式全部通过内部 API 操作"),
        ("02  真实 HTML 导出", "直接调用 Axure 原型生成器，输出可运行页面"),
        ("03  MCP 直连", "agent 通过 axure_code_* 工具后台调用"),
    ]):
        yy = 462 + i * 62
        rect(p, "", 320, yy, 44, 44, fill="#DBEAFE", borderWidth=0, cornerRadius=10)
        text(p, str(i + 1), 334, yy + 8, 20, 26, 16, "#2563EB", True)
        text(p, title[4:], 382, yy - 2, 260, 30, 17, "#0F172A", True)
        text(p, desc, 382, yy + 28, 680, 26, 13, "#64748B")

    # ---------------------------------------------------------------- 登录页
    p = "登录页"
    rect(p, "", 0, 0, 1200, 800, fill="#F5F7FB", borderWidth=0)
    rect(p, "", 0, 0, 520, 800, fill="#0F172A", borderWidth=0)
    rect(p, "AI", 56, 52, 64, 64, fill="#2563EB", borderWidth=0,
         cornerRadius=16, fontSize=28, textColor="#FFFFFF", bold=True)
    side_text(p, "安全、快速、可复用", 56, 190, 400, 50, 30, "#FFFFFF", True)
    side_text(p, "一个由 agent 直接操控内部对象模型生成的登录流程原型。",
         56, 258, 390, 70, 17, "#94A3B8")
    side_text(p, "NO MOUSE  /  NO FOCUS STEAL", 56, 724, 390, 24, 11, "#64748B", True)

    card(p, 650, 120, 430, 560)
    text(p, "登录", 706, 176, 300, 46, 30, "#0F172A", True)
    text(p, "使用账号继续进入工作台", 706, 230, 320, 30, 15, "#64748B")
    text(p, "账号", 706, 296, 120, 28, 14, "#334155", True)
    call("add-widget", path=str(RP), page=p, type="input", text="", x=706, y=330,
         w=318, h=52, style={"fill": "#FFFFFF", "borderColor": "#CBD5E1",
                             "borderWidth": 1, "cornerRadius": 10, "fontSize": 16})
    text(p, "密码", 706, 408, 120, 28, 14, "#334155", True)
    call("add-widget", path=str(RP), page=p, type="input", text="", x=706, y=442,
         w=318, h=52, style={"fill": "#FFFFFF", "borderColor": "#CBD5E1",
                             "borderWidth": 1, "cornerRadius": 10, "fontSize": 16})
    button(p, "登录", 706, 532, 318, 54, fill="#2563EB", textColor="#FFFFFF",
           borderWidth=0, cornerRadius=12, fontSize=18, bold=True)
    text(p, "返回首页", 706, 614, 318, 28, 14, "#2563EB", True)

    # ---------------------------------------------------------------- 工作台
    p = "工作台"
    rect(p, "", 0, 0, 1200, 800, fill="#F5F7FB", borderWidth=0)
    rect(p, "", 0, 0, 240, 800, fill="#0F172A", borderWidth=0)
    rect(p, "AI", 32, 30, 64, 64, fill="#2563EB", borderWidth=0,
         cornerRadius=16, fontSize=28, textColor="#FFFFFF", bold=True)
    side_text(p, "原型工作台", 112, 38, 150, 40, 22, "#FFFFFF", True)
    side_text(p, "WORKSPACE", 34, 132, 160, 24, 12, "#64748B", True)
    side_text(p, "首页", 34, 178, 160, 34, 18, "#94A3B8")
    side_text(p, "登录页", 34, 226, 160, 34, 18, "#94A3B8")
    side_text(p, "工作台", 34, 274, 160, 34, 18, "#FFFFFF", True)
    side_text(p, "详情页", 34, 322, 160, 34, 18, "#94A3B8")
    side_text(p, "PURE CODE / ZERO GUI", 34, 736, 190, 24, 10, "#64748B", True)

    main_text(p, "工作台", 288, 54, 500, 54, 34, "#0F172A", True)
    main_text(p, "所有内容均由代码生成，可继续扩展真实业务组件。", 288, 116, 760, 36, 16, "#64748B")
    button(p, "查看详情", 920, 64, 220, 52, fill="#0F172A", textColor="#FFFFFF",
           borderWidth=0, cornerRadius=12, fontSize=18, bold=True)

    for i, (title, value, desc, color) in enumerate([
        ("进行中", "08", "任务持续推进", "#2563EB"),
        ("已完成", "24", "交付物已归档", "#16A34A"),
        ("待处理", "03", "等待下一步", "#D97706"),
    ]):
        x = 288 + i * 288
        card(p, x, 190, 268, 150)
        text(p, title, x + 24, 212, 120, 26, 14, "#64748B", True)
        text(p, value, x + 24, 244, 120, 56, 36, color, True)
        text(p, desc, x + 24, 304, 210, 22, 12, "#94A3B8")

    card(p, 288, 374, 844, 330)
    text(p, "最近任务", 320, 404, 300, 36, 22, "#0F172A", True)
    rows = [
        ("AX-MCP-001", "后台对象模型控制", "已完成", "#16A34A"),
        ("AX-MCP-002", "真实 HTML 原型导出", "已完成", "#16A34A"),
        ("AX-MCP-003", "交互链路点击验证", "进行中", "#2563EB"),
    ]
    for i, (tid, name, status, color) in enumerate(rows):
        yy = 466 + i * 68
        rect(p, "", 320, yy, 780, 54, fill="#F8FAFC", borderWidth=0, cornerRadius=8)
        text(p, tid, 338, yy + 14, 150, 26, 13, "#64748B", True)
        text(p, name, 500, yy + 12, 330, 28, 16, "#0F172A", True)
        text(p, status, 988, yy + 14, 90, 26, 13, color, True)

    # ---------------------------------------------------------------- 详情页
    p = "详情页"
    rect(p, "", 0, 0, 1200, 800, fill="#F5F7FB", borderWidth=0)
    rect(p, "", 0, 0, 240, 800, fill="#0F172A", borderWidth=0)
    rect(p, "AI", 32, 30, 64, 64, fill="#2563EB", borderWidth=0,
         cornerRadius=16, fontSize=28, textColor="#FFFFFF", bold=True)
    side_text(p, "原型工作台", 112, 38, 150, 40, 22, "#FFFFFF", True)
    side_text(p, "DETAIL", 34, 132, 160, 24, 12, "#64748B", True)
    side_text(p, "首页", 34, 178, 160, 34, 18, "#94A3B8")
    side_text(p, "登录页", 34, 226, 160, 34, 18, "#94A3B8")
    side_text(p, "工作台", 34, 274, 160, 34, 18, "#94A3B8")
    side_text(p, "详情页", 34, 322, 160, 34, 18, "#FFFFFF", True)
    side_text(p, "PURE CODE / ZERO GUI", 34, 736, 190, 24, 10, "#64748B", True)

    main_text(p, "任务详情", 288, 54, 500, 54, 34, "#0F172A", True)
    main_text(p, "AX-MCP-003 · 交互链路点击验证", 288, 116, 760, 36, 16, "#64748B")
    button(p, "返回工作台", 920, 64, 220, 52, fill="#2563EB", textColor="#FFFFFF",
           borderWidth=0, cornerRadius=12, fontSize=18, bold=True)

    card(p, 288, 190, 520, 500)
    text(p, "任务说明", 320, 224, 300, 36, 22, "#0F172A", True)
    text(p, "验证从首页进入工作台、再从工作台查看详情的完整点击链路。",
         320, 286, 440, 70, 17, "#334155")
    text(p, "验收标准", 320, 386, 300, 32, 18, "#0F172A", True)
    text(p, "• 页面跳转无需刷新\n• 按钮命中区域正确\n• 导出 HTML 可直接打开",
         320, 432, 440, 120, 16, "#64748B")
    button(p, "返回首页", 320, 610, 180, 50, fill="#0F172A", textColor="#FFFFFF",
           borderWidth=0, cornerRadius=10, fontSize=16, bold=True)

    card(p, 832, 190, 300, 500)
    text(p, "状态", 860, 224, 200, 32, 18, "#64748B", True)
    text(p, "进行中", 860, 266, 200, 44, 28, "#2563EB", True)
    text(p, "负责人", 860, 356, 200, 28, 14, "#64748B", True)
    text(p, "AI Agent", 860, 392, 200, 36, 20, "#0F172A", True)
    text(p, "创建方式", 860, 472, 200, 28, 14, "#64748B", True)
    text(p, "纯代码", 860, 508, 200, 36, 20, "#0F172A", True)
    text(p, "GUI 调用", 860, 584, 200, 28, 14, "#64748B", True)
    text(p, "0 次", 860, 620, 200, 36, 20, "#16A34A", True)

    # Wire standard Axure OnClick -> Open Page interactions.
    for page, index, target in [
        ("首页", 6, "登录页"), ("首页", 7, "工作台"), ("首页", 8, "详情页"),
        ("首页", 12, "工作台"),
        ("登录页", 13, "工作台"), ("登录页", 14, "首页"),
        ("工作台", 5, "首页"), ("工作台", 6, "登录页"), ("工作台", 8, "详情页"),
        ("工作台", 12, "详情页"),
        ("详情页", 5, "首页"), ("详情页", 6, "登录页"), ("详情页", 7, "工作台"),
        ("详情页", 12, "工作台"), ("详情页", 18, "首页"),
    ]:
        link(page, index, target)

    _QUEUING = False
    flush()
    pages = call("list-pages", path=str(RP))["pages"]
    assert [x["name"] for x in pages] == ["首页", "登录页", "工作台", "详情页"], pages
    result = call("generate-html", path=str(RP), outDir=str(HTML))
    assert result["exists"], result
    info = call("project-info", path=str(RP))
    assert info["pages"] == 4 and info["widgets"] == 121, info
    print(json.dumps({"rp": str(RP), "html": str(HTML), "index": result["path"],
                      "pages": [x["name"] for x in pages], "widgets": info["widgets"]},
                     ensure_ascii=False, indent=2))


if __name__ == "__main__":
    build()
