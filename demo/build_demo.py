import subprocess, json, os, sys
EXE = r'E:\myzcode\axure-mcp\bridge\bin\Debug\net48\axure-bridge.exe'
RP = r'E:\myzcode\axure-mcp\demo\ai_prototype.rp'

def call(cmd, **kw):
    req = {"cmd": cmd, **kw}
    r = subprocess.run([EXE, json.dumps(req, ensure_ascii=False)], capture_output=True, text=True, encoding='utf-8', timeout=120)
    if r.returncode != 0:
        print("ERROR", cmd, r.stderr[:500]); sys.exit(1)
    out = json.loads(r.stdout)
    print(cmd, "->", json.dumps(out, ensure_ascii=False)[:240])
    return out

if os.path.exists(RP): os.remove(RP)
call("new-project", path=RP, name="AI代码原型")
call("add-page", path=RP, name="首页")
call("add-page", path=RP, name="登录页")
call("add-widget", path=RP, page="首页", type="rectangle", text="欢迎使用 AI 原型", x=100, y=100, w=400, h=100)
call("add-widget", path=RP, page="首页", type="rectangle", text="这是纯代码创建的第二个元件", x=100, y=240, w=300, h=60)
call("add-widget", path=RP, page="登录页", type="button", text="登录", x=100, y=150, w=200, h=50)
call("add-widget", path=RP, page="登录页", type="input", text="", x=100, y=80, w=200, h=40)
pages = call("list-pages", path=RP)
widgets = call("list-widgets", path=RP, page="首页")
print("FINAL pages:", [p["name"] for p in pages["pages"]])
print("FINAL widgets:", [(w["text"], w["x"], w["y"], w["w"], w["h"]) for w in widgets["widgets"]])
print("RP size:", os.path.getsize(RP))
