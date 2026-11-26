"""一键自检：通过 stdio MCP 协议遍历所有工具（只读类 + 一次完整导出闭环）。"""
import asyncio, json, sys, os, glob, time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

DEMO = os.path.join(os.environ.get("TEMP", "."), "axure_mcp_demo", "demo_selfcheck.rp")

async def main() -> int:
    from mcp import ClientSession, StdioServerParameters
    from mcp.client.stdio import stdio_client
    fails = []
    sp = StdioServerParameters(command=sys.executable, args=["server.py"], cwd=HERE)
    async with stdio_client(sp) as (r, w):
        async with ClientSession(r, w) as s:
            await s.initialize()
            tools = {t.name for t in (await s.list_tools()).tools}
            print(f"[ok] tools={len(tools)}")
            async def call(n, a=None, allow_err=False):
                out = await s.call_tool(n, a or {})
                txt = out.content[0].text if out.content else ""
                ok = not out.isError
                print(f"[{'ok' if ok else 'FAIL'}] {n}: {txt[:110].replace(chr(10),' ')}")
                if not ok and not allow_err:
                    fails.append(n)
                try: return json.loads(txt)
                except Exception: return None
            launched_by_us = False
            st = await call("axure_status")
            inst = next((i for i in st.get("instances", [])
                         if "Axure" in i["title"]), None) if st else None
            if inst is None:
                print("[info] 无运行实例，启动空白实例自检")
                inst = await call("axure_launch", {})
                launched_by_us = True
            await call("axure_menu_tree")
            if inst and inst.get("pid"):
                pid = inst["pid"]
                await call("axure_dismiss_nags", {"pid": pid})
                await call("axure_screenshot", {"pid": pid, "name": "selfcheck"})
                await call("axure_ui_command", {"pid": pid, "menu_path": ["视图", "重置视图"]})
                # 完整导出闭环（用上次的输出目录）
                await call("axure_export_html", {"pid": pid})
                known = glob.glob(r"E:\abc\Axure\HTML\*") + \
                        glob.glob(r"C:\Users\chen\Downloads\HTML\*")
                if known:
                    rd = await call("axure_read_export", {"html_dir": known[0]})
                    if rd and rd.get("page_count", 0) >= 0:
                        print(f"[info] export pages={rd['page_count']}")
                else:
                    print("[info] 未找到已知导出目录，跳过 read_export 校验")
            if launched_by_us:
                import subprocess
                subprocess.run(["taskkill", "/PID", str(inst["pid"]), "/F"],
                               capture_output=True)
                print("[info] 已关闭自检启动的临时实例")
            print("RESULT:", "PASS" if not fails else f"FAIL {fails}")
            return 0 if not fails else 1
if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
