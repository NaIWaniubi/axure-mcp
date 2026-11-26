"""Build an advanced-widget Axure prototype using only the code bridge."""
from __future__ import annotations

import json
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT / "bridge" / "bin" / "Debug" / "net48" / "axure-bridge.exe"
RP = ROOT / "demo" / "advanced_widgets.rp"
HTML = ROOT / "demo" / "advanced_widgets_html"
IMAGE = ROOT / "demo" / "workspace_home_final.png"
PAGE = "高级元件"
_QUEUE: list[dict] = []


def _raw(req: dict) -> dict:
    r = subprocess.run([str(EXE)], input=json.dumps(req, ensure_ascii=False),
                       capture_output=True, text=True, encoding="utf-8", timeout=300)
    if r.returncode:
        raise RuntimeError(f"{req.get('cmd')} failed:\n{r.stderr or r.stdout}")
    return json.loads(r.stdout)


def queue(cmd: str, **payload) -> None:
    _QUEUE.append({"cmd": cmd, **payload})


def call(cmd: str, **payload) -> dict:
    return _raw({"cmd": cmd, **payload})


def build() -> None:
    if HTML.exists():
        shutil.rmtree(HTML)
    queue("new-project", path=str(RP), name="Advanced Widgets")
    queue("add-page", path=str(RP), name=PAGE)
    widgets = [
        {"type": "textarea", "text": "多行文本", "placeholder": "请输入", "x": 20, "y": 20, "w": 240, "h": 100},
        {"type": "combobox", "text": "", "x": 20, "y": 140, "w": 180, "h": 32, "options": ["中国", "美国", "日本"]},
        {"type": "listbox", "text": "", "x": 220, "y": 140, "w": 180, "h": 100, "multiple": True,
         "options": [{"text": "A", "selected": True}, {"text": "B", "selected": False}]},
        {"type": "checkbox", "text": "同意条款", "selected": True, "x": 20, "y": 260, "w": 140, "h": 24},
        {"type": "radio", "text": "选项A", "radioGroup": "Group1", "selected": True, "x": 180, "y": 260, "w": 120, "h": 24},
        {"type": "ellipse", "text": "", "x": 20, "y": 310, "w": 80, "h": 80,
         "style": {"fill": "#1677FF", "borderWidth": 0}},
        {"type": "hline", "text": "", "x": 120, "y": 350, "w": 200, "h": 1,
         "style": {"fill": "#0F172A", "borderWidth": 0}},
        {"type": "vline", "text": "", "x": 340, "y": 310, "w": 1, "h": 80,
         "style": {"fill": "#0F172A", "borderWidth": 0}},
        {"type": "table", "text": "", "x": 20, "y": 420, "w": 300, "h": 120, "rows": 3, "columns": 2,
         "cells": [["R0C0", "R0C1"], ["R1C0", "R1C1"], ["R2C0", "R2C1"]]},
        {"type": "dynamic-panel", "text": "", "x": 360, "y": 20, "w": 300, "h": 200, "stateName": "默认状态"},
        {"type": "repeater", "text": "", "x": 360, "y": 240, "w": 300, "h": 200, "fitToContent": True,
         "data": {"columns": ["姓名", "部门"], "rows": [["Alice", "研发"], ["Bob", "设计"]]}},
        {"type": "image", "text": "", "imagePath": str(IMAGE), "x": 20, "y": 560, "w": 320, "h": 180},
    ]
    for widget in widgets:
        queue("add-widget", path=str(RP), page=PAGE, **widget)
    _raw({"cmd": "batch", "ops": _QUEUE})
    _QUEUE.clear()

    call("add-panel-state", path=str(RP), page=PAGE, index=9, name="第二状态")
    call("add-panel-widget", path=str(RP), page=PAGE, index=9, stateIndex=0,
         type="rectangle", text="默认状态内容", x=0, y=0, w=300, h=200,
         style={"fill": "#DBEAFE", "borderWidth": 0, "fontSize": 24})
    call("add-panel-widget", path=str(RP), page=PAGE, index=9, stateIndex=1,
         type="rectangle", text="第二状态内容", x=0, y=0, w=300, h=200,
         style={"fill": "#F1F5F9", "borderWidth": 0, "fontSize": 24})
    call("repeater-add-item", path=str(RP), page=PAGE, index=10,
         type="rectangle", text="模板行", x=0, y=0, w=300, h=40,
         style={"fill": "#FFFFFF", "borderColor": "#E2E8F0", "borderWidth": 1})
    call("table-set-cell", path=str(RP), page=PAGE, index=8, row=1, column=1, text="已更新")
    result = call("generate-html", path=str(RP), outDir=str(HTML))
    assert result["exists"], result
    data_js = (HTML / "files" / PAGE / "data.js").read_text(encoding="utf-8")
    page_html = (HTML / f"{PAGE}.html").read_text(encoding="utf-8")
    combined = data_js + "\n" + page_html
    for expected in ("默认状态", "默认状态内容", "第二状态", "第二状态内容", "Alice", "Bob", "已更新"):
        assert expected in combined, expected
    print(json.dumps({"rp": str(RP), "html": str(HTML), "index": result["path"],
                      "widgets": len(widgets), "verified": True}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    build()
