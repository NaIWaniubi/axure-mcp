"""Code-only MCP selfcheck: exercises the object-model tools over stdio."""
from __future__ import annotations

import asyncio
import json
import os
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))


async def main() -> int:
    from mcp import ClientSession, StdioServerParameters
    from mcp.client.stdio import stdio_client

    sp = StdioServerParameters(command=sys.executable, args=["server.py"], cwd=str(HERE))
    fails: list[str] = []
    async with stdio_client(sp) as (r, w):
        async with ClientSession(r, w) as s:
            await s.initialize()
            tools = {t.name for t in (await s.list_tools()).tools}
            required = {
                "axure_code_call", "axure_code_new_project", "axure_code_list_pages",
                "axure_code_add_page", "axure_code_rename_page", "axure_code_delete_page",
                "axure_code_list_widgets", "axure_code_add_widget",
                "axure_code_update_widget", "axure_code_delete_widget",
                "axure_code_add_link", "axure_code_generate_html",
                "axure_code_batch", "axure_code_describe_type", "axure_code_find_types",
                "axure_code_get_widget", "axure_code_dump_page",
                "axure_code_project_info", "axure_code_set_widget_property",
                "axure_code_set_page_property", "axure_code_set_style_property",
                "axure_code_add_panel_state", "axure_code_add_panel_widget",
                "axure_code_set_panel_state", "axure_code_list_panel_states",
                "axure_code_repeater_add_column", "axure_code_repeater_add_row",
                "axure_code_repeater_set_cell", "axure_code_repeater_add_item",
                "axure_code_list_repeater_data", "axure_code_table_set_cell",
                "axure_code_table_set_size", "axure_code_add_list_option", "axure_code_list_options",
                "axure_code_list_widget_tree", "axure_code_get_widget_path",
                "axure_code_update_widget_path", "axure_code_delete_widget_path",
                "axure_code_add_action", "axure_code_list_variables", "axure_code_get_variable",
                "axure_code_add_variable", "axure_code_update_variable", "axure_code_delete_variable",
                "axure_code_evaluate_expression", "axure_code_add_page_load_set_variable",
                "axure_code_clear_interactions", "axure_code_list_page_tree",
                "axure_code_add_folder", "axure_code_rename_folder", "axure_code_delete_folder",
                "axure_code_move_page", "axure_code_list_masters", "axure_code_add_master",
                "axure_code_rename_master", "axure_code_delete_master", "axure_code_add_master_widget",
                "axure_code_apply_master", "axure_code_remove_master",
                "axure_code_list_adaptive_views", "axure_code_add_adaptive_view",
                "axure_code_set_adaptive_view_size", "axure_code_set_page_size", "axure_code_set_page_style",
                "axure_code_list_named_styles", "axure_code_get_named_style", "axure_code_add_named_style",
                "axure_code_apply_named_style", "axure_code_delete_named_style",
                "axure_code_export_word", "axure_code_export_page_image", "axure_code_export_all_images",
                "axure_code_export_csv_report", "axure_code_export_pdf", "axure_code_optimize_image",
            }
            missing = sorted(required - tools)
            print(f"[{'ok' if not missing else 'FAIL'}] tools={len(tools)} missing={missing}")
            if missing:
                fails.extend(missing)

            async def call(name: str, args: dict) -> dict:
                out = await s.call_tool(name, args)
                txt = out.content[0].text if out.content else ""
                ok = not out.isError
                print(f"[{'ok' if ok else 'FAIL'}] {name}: {txt[:140].replace(chr(10), ' ')}")
                if not ok:
                    fails.append(name)
                    return {}
                return json.loads(txt)

            with tempfile.TemporaryDirectory(prefix="axure-mcp-code-") as td:
                rp = os.path.join(td, "selfcheck.rp")
                out = os.path.join(td, "html")
                await call("axure_code_new_project", {"path": rp, "name": "MCP Code Selfcheck"})
                await call("axure_code_project_info", {"path": rp})
                await call("axure_code_find_types", {"query": "Button", "max_types": 10})
                await call("axure_code_describe_type", {"name": "Axure.DocumentModel.Objects.Button", "max_members": 10})
                await call("axure_code_batch", {"ops": [
                    {"cmd": "add-page", "path": rp, "name": "首页"},
                    {"cmd": "add-page", "path": rp, "name": "工作台"},
                ]})
                await call("axure_code_add_widget", {
                    "path": rp, "page": "首页", "type": "button", "text": "进入工作台",
                    "x": 100, "y": 100, "w": 220, "h": 52,
                    "style": {"fill": "#2563EB", "textColor": "#FFFFFF", "borderWidth": 0,
                              "cornerRadius": 12, "fontSize": 18, "bold": True},
                })
                await call("axure_code_get_widget", {"path": rp, "page": "首页", "index": 0})
                await call("axure_code_dump_page", {"path": rp, "page": "首页"})
                await call("axure_code_set_widget_property", {
                    "path": rp, "page": "首页", "index": 0,
                    "property": "Name", "value": "btn_enter",
                })
                await call("axure_code_set_style_property", {
                    "path": rp, "page": "首页", "index": 0,
                    "pname": "ROTATION", "value": 0,
                })
                await call("axure_code_add_widget", {
                    "path": rp, "page": "首页", "type": "rectangle", "text": "待删除",
                    "x": 100, "y": 200, "w": 120, "h": 40,
                })
                await call("axure_code_update_widget", {
                    "path": rp, "page": "首页", "index": 1, "text": "已更新",
                    "style": {"fill": "#F1F5F9", "textColor": "#0F172A"},
                })
                await call("axure_code_add_link", {
                    "path": rp, "page": "首页", "index": 0, "target_page": "工作台",
                })
                await call("axure_code_add_variable", {"path": rp, "name": "userName", "value": "Alice"})
                await call("axure_code_evaluate_expression", {"path": rp, "text": "Hello [[userName]]"})
                await call("axure_code_add_page_load_set_variable", {
                    "path": rp, "page": "首页", "name": "userName", "value": "Bob",
                })
                await call("axure_code_add_action", {
                    "path": rp, "page": "首页", "index": 0, "action": "set-variable",
                    "variable": "userName", "value": "Carol",
                })
                await call("axure_code_add_action", {
                    "path": rp, "page": "首页", "index": 0, "action": "show", "target_index": 0,
                })
                await call("axure_code_list_variables", {"path": rp})
                await call("axure_code_delete_widget", {"path": rp, "page": "首页", "index": 1})
                pages = await call("axure_code_list_pages", {"path": rp})
                if [p["name"] for p in pages.get("pages", [])] != ["工作台", "首页"]:
                    fails.append("page-order")
                widgets = await call("axure_code_list_widgets", {"path": rp, "page": "首页"})
                if widgets.get("count") != 1:
                    fails.append("widget-count")
                await call("axure_code_batch", {"ops": [
                    {"cmd": "add-widget", "path": rp, "page": "首页", "type": "combobox", "text": "",
                     "options": ["A", "B"]},
                    {"cmd": "add-widget", "path": rp, "page": "首页", "type": "table", "text": "",
                     "rows": 2, "columns": 2, "cells": [["A", "B"], ["C", "D"]]},
                    {"cmd": "add-widget", "path": rp, "page": "首页", "type": "dynamic-panel", "text": "",
                     "stateName": "S1"},
                    {"cmd": "add-widget", "path": rp, "page": "首页", "type": "repeater", "text": "",
                     "data": {"columns": ["Name"], "rows": [["Alice"]]}},
                ]})
                await call("axure_code_add_panel_state", {
                    "path": rp, "page": "首页", "index": 3, "name": "S2",
                })
                await call("axure_code_add_panel_widget", {
                    "path": rp, "page": "首页", "index": 3, "state_index": 1,
                    "type": "rectangle", "text": "S2 content",
                })
                await call("axure_code_list_panel_states", {"path": rp, "page": "首页", "index": 3})
                await call("axure_code_list_widget_tree", {"path": rp, "page": "首页", "depth": 4})
                await call("axure_code_get_widget_path", {
                    "path": rp, "page": "首页", "widget_path": "/3/state:1/0",
                })
                await call("axure_code_update_widget_path", {
                    "path": rp, "page": "首页", "widget_path": "/3/state:1/0", "text": "S2 updated",
                })
                await call("axure_code_repeater_set_cell", {
                    "path": rp, "page": "首页", "index": 4, "row": 0, "column": 0, "value": "Bob",
                })
                await call("axure_code_list_repeater_data", {"path": rp, "page": "首页", "index": 4})
                await call("axure_code_table_set_cell", {
                    "path": rp, "page": "首页", "index": 2, "row": 0, "column": 0, "text": "updated",
                })
                await call("axure_code_add_list_option", {
                    "path": rp, "page": "首页", "index": 1, "text": "C", "selected": False,
                })
                await call("axure_code_list_options", {"path": rp, "page": "首页", "index": 1})
                await call("axure_code_batch", {"ops": [
                    {"cmd": "add-master", "path": rp, "name": "Header"},
                    {"cmd": "add-master-widget", "path": rp, "master": "Header", "type": "rectangle",
                     "text": "Master Header", "w": 300, "h": 50, "style": {"fill": "#0F172A", "textColor": "#FFFFFF"}},
                    {"cmd": "apply-master", "path": rp, "page": "首页", "master": "Header", "w": 300, "h": 50},
                    {"cmd": "add-named-style", "path": rp, "kind": "widget", "name": "Primary",
                     "style": {"fill": "#2563EB", "textColor": "#FFFFFF", "borderWidth": 0}},
                    {"cmd": "apply-named-style", "path": rp, "kind": "widget", "name": "Primary",
                     "page": "首页", "index": 0},
                    {"cmd": "add-adaptive-view", "path": rp, "page": "首页", "name": "Mobile",
                     "width": 375, "height": 812, "defaultWidth": 1200, "defaultHeight": 800},
                    {"cmd": "set-adaptive-view-size", "path": rp, "page": "首页", "view": "Mobile",
                     "width": 390, "height": 844},
                    {"cmd": "set-page-size", "path": rp, "page": "首页", "width": 1280, "height": 720},
                    {"cmd": "set-page-style", "path": rp, "page": "首页", "pname": "FILL", "value": "#F5F7FB"},
                ]})
                await call("axure_code_list_masters", {"path": rp})
                await call("axure_code_list_adaptive_views", {"path": rp, "page": "首页"})
                await call("axure_code_list_named_styles", {"path": rp, "kind": "widget"})
                await call("axure_code_get_named_style", {"path": rp, "kind": "widget", "name": "Primary"})
                await call("axure_code_export_word", {"path": rp, "out_path": os.path.join(td, "spec.docx")})
                await call("axure_code_export_page_image", {
                    "path": rp, "page": "首页", "out_path": os.path.join(td, "home.png"),
                })
                await call("axure_code_export_all_images", {"path": rp, "out_dir": os.path.join(td, "images")})
                await call("axure_code_export_pdf", {"path": rp, "out_path": os.path.join(td, "prototype.pdf")})
                await call("axure_code_export_csv_report", {"path": rp, "out_path": os.path.join(td, "report.csv")})
                await call("axure_code_optimize_image", {
                    "input_path": os.path.join(td, "home.png"),
                    "output_path": os.path.join(td, "home_opt.png"),
                })
                await call("axure_code_remove_master", {"path": rp, "page": "首页", "master": "Header"})
                await call("axure_code_delete_master", {"path": rp, "master": "Header"})
                await call("axure_code_delete_named_style", {"path": rp, "kind": "widget", "name": "Primary"})

                tree_rp = os.path.join(td, "tree.rp")
                await call("axure_code_batch", {"ops": [
                    {"cmd": "new-project", "path": tree_rp, "name": "Tree"},
                    {"cmd": "add-folder", "path": tree_rp, "name": "Folder"},
                    {"cmd": "add-page", "path": tree_rp, "name": "Nested", "parentFolder": "Folder"},
                ]})
                await call("axure_code_list_page_tree", {"path": tree_rp})
                await call("axure_code_move_page", {"path": tree_rp, "page": "Nested", "index": 0})
                await call("axure_code_rename_folder", {"path": tree_rp, "folder": "Folder", "name": "Renamed"})
                await call("axure_code_delete_folder", {"path": tree_rp, "folder": "Renamed"})

                await call("axure_code_rename_page", {"path": rp, "page": "工作台", "name": "控制台"})
                html = await call("axure_code_generate_html", {"path": rp, "out_dir": out})
                index = Path(html.get("path", ""))
                if not index.exists():
                    fails.append("html-index")
                else:
                    text = index.read_text(encoding="utf-8", errors="ignore")
                    if "<html" not in text.lower():
                        fails.append("html-content")
                await call("axure_code_clear_interactions", {"path": rp, "page": "首页", "index": 0})
                await call("axure_code_delete_page", {"path": rp, "page": "控制台"})
                pages = await call("axure_code_list_pages", {"path": rp})
                if [p["name"] for p in pages.get("pages", [])] != ["首页"]:
                    fails.append("page-delete")

    print("RESULT:", "PASS" if not fails else f"FAIL {sorted(set(fails))}")
    return 0 if not fails else 1


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
