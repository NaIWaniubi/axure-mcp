"""Build a full-feature Axure prototype and export HTML/Word/PDF/images/CSV."""
from __future__ import annotations

import json
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT / "bridge" / "bin" / "Debug" / "net48" / "axure-bridge.exe"
RP = ROOT / "demo" / "full_feature.rp"
HTML = ROOT / "demo" / "full_feature_html"
EXPORTS = ROOT / "demo" / "full_feature_exports"
IMAGE = ROOT / "demo" / "workspace_home_final.png"
_QUEUE: list[dict] = []


def raw(req: dict) -> dict:
    r = subprocess.run([str(EXE)], input=json.dumps(req, ensure_ascii=False),
                       capture_output=True, text=True, encoding="utf-8", timeout=300)
    if r.returncode:
        raise RuntimeError(f"{req.get('cmd')} failed:\n{r.stderr or r.stdout}")
    return json.loads(r.stdout)


def q(cmd: str, **payload) -> None:
    _QUEUE.append({"cmd": cmd, **payload})


def call(cmd: str, **payload) -> dict:
    return raw({"cmd": cmd, **payload})


def build() -> None:
    for p in (HTML, EXPORTS):
        if p.exists():
            shutil.rmtree(p)
    q("new-project", path=str(RP), name="Axure Full Feature Demo")
    q("add-folder", path=str(RP), name="业务页面")
    q("add-page", path=str(RP), name="详情页", parentFolder="业务页面")
    q("add-page", path=str(RP), name="首页", parentFolder="业务页面")
    q("add-master", path=str(RP), name="页头")
    q("add-master-widget", path=str(RP), master="页头", type="rectangle", text="AI 原型工作台",
      x=0, y=0, w=1200, h=72, style={"fill": "#0F172A", "textColor": "#FFFFFF", "borderWidth": 0, "fontSize": 24, "bold": True})
    q("add-named-style", path=str(RP), kind="widget", name="Primary",
      style={"fill": "#2563EB", "textColor": "#FFFFFF", "borderWidth": 0, "cornerRadius": 12, "fontSize": 18, "bold": True})
    q("add-widget", path=str(RP), page="首页", type="rectangle", text="", x=0, y=0, w=1200, h=800,
      style={"fill": "#F5F7FB", "borderWidth": 0})
    q("add-widget", path=str(RP), page="首页", type="rectangle", text="全功能原型", x=80, y=120, w=600, h=60,
      style={"fill": "#F5F7FB", "borderWidth": 0, "fontSize": 36, "bold": True})
    q("add-widget", path=str(RP), page="首页", type="button", text="触发动作", x=80, y=210, w=180, h=52)
    q("add-widget", path=str(RP), page="首页", type="dynamic-panel", text="", x=80, y=300, w=320, h=180, stateName="默认状态")
    q("add-widget", path=str(RP), page="首页", type="repeater", text="", x=440, y=300, w=360, h=180, fitToContent=True,
      data={"columns": ["姓名", "部门"], "rows": [["Alice", "研发"], ["Bob", "设计"]]})
    q("add-widget", path=str(RP), page="首页", type="table", text="", x=80, y=520, w=360, h=140,
      rows=3, columns=2, cells=[["项目", "状态"], ["对象模型", "完成"], ["导出", "完成"]])
    q("add-widget", path=str(RP), page="首页", type="image", text="", imagePath=str(IMAGE), x=840, y=300, w=280, h=180)
    q("apply-master", path=str(RP), page="首页", master="页头", background=True)
    q("apply-named-style", path=str(RP), kind="widget", name="Primary", page="首页", index=2)
    q("add-adaptive-view", path=str(RP), page="首页", name="Mobile", width=375, height=812,
      defaultWidth=1200, defaultHeight=800)
    q("set-page-size", path=str(RP), page="首页", width=1200, height=800)
    q("set-page-style", path=str(RP), page="首页", pname="FILL", value="#F5F7FB")
    q("add-variable", path=str(RP), name="userName", value="Guest")
    q("add-variable", path=str(RP), name="count", value="1")
    q("add-page-load-set-variable", path=str(RP), page="首页", name="userName", value="Admin")
    q("add-action", path=str(RP), page="首页", index=2, action="set-text", targetIndex=1,
      text="动作已触发", condition={"kind": "global", "variable": "count", "op": "equals", "value": "1"})
    q("add-action", path=str(RP), page="首页", index=2, action="show", targetIndex=3, appendToLastCase=True)
    q("add-action", path=str(RP), page="首页", index=2, action="wait", waitMs=200, appendToLastCase=True)
    q("add-action", path=str(RP), page="首页", index=2, action="hide", targetIndex=3, appendToLastCase=True)
    q("add-link", path=str(RP), page="首页", index=2, targetPage="详情页")
    q("add-widget", path=str(RP), page="详情页", type="rectangle", text="详情页", x=80, y=120, w=500, h=60,
      style={"fill": "#F5F7FB", "borderWidth": 0, "fontSize": 36, "bold": True})
    q("apply-master", path=str(RP), page="详情页", master="页头", background=True)
    raw({"cmd": "batch", "ops": _QUEUE})
    _QUEUE.clear()

    call("add-panel-state", path=str(RP), page="首页", index=3, name="第二状态")
    call("add-panel-widget", path=str(RP), page="首页", index=3, stateIndex=0,
         type="rectangle", text="默认状态内容", x=0, y=0, w=320, h=180,
         style={"fill": "#DBEAFE", "borderWidth": 0, "fontSize": 22})
    call("add-panel-widget", path=str(RP), page="首页", index=3, stateIndex=1,
         type="rectangle", text="第二状态内容", x=0, y=0, w=320, h=180,
         style={"fill": "#DCFCE7", "borderWidth": 0, "fontSize": 22})
    call("repeater-add-item", path=str(RP), page="首页", index=4,
         type="rectangle", text="模板行", x=0, y=0, w=360, h=40,
         style={"fill": "#FFFFFF", "borderColor": "#E2E8F0", "borderWidth": 1})

    html = call("generate-html", path=str(RP), outDir=str(HTML))
    word = call("export-word", path=str(RP), outPath=str(EXPORTS / "spec.docx"))
    pdf = call("export-pdf", path=str(RP), outPath=str(EXPORTS / "prototype.pdf"))
    images = call("export-all-images", path=str(RP), outDir=str(EXPORTS / "images"))
    csv = call("export-csv-report", path=str(RP), outPath=str(EXPORTS / "report.csv"))
    info = call("project-info", path=str(RP))
    print(json.dumps({"rp": str(RP), "html": html["path"], "word": word["path"],
                      "pdf": pdf["path"], "images": images["count"], "csv": csv["path"],
                      "pages": info["pages"], "widgets": info["widgets"]}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    build()
