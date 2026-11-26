"""CI-safe self-check: verifies MCP tool registration without requiring Axure.

The full `selfcheck_code.py` needs a licensed Axure RP 9 install because it
loads Axure's object model. This check only boots the MCP server, lists tools,
and verifies the expected code-level surface is present.
"""
from __future__ import annotations

import asyncio
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

REQUIRED = {
    "axure_code_new_project",
    "axure_code_add_page",
    "axure_code_add_widget",
    "axure_code_add_panel_state",
    "axure_code_repeater_set_cell",
    "axure_code_table_set_cell",
    "axure_code_add_link",
    "axure_code_add_action",
    "axure_code_add_variable",
    "axure_code_add_master",
    "axure_code_add_adaptive_view",
    "axure_code_add_named_style",
    "axure_code_generate_html",
    "axure_code_export_word",
    "axure_code_export_pdf",
    "axure_code_export_all_images",
    "axure_code_export_csv_report",
    "axure_code_batch",
    "axure_code_find_types",
    "axure_code_describe_type",
}

MIN_TOOLS = 90


async def main() -> int:
    from mcp import ClientSession, StdioServerParameters
    from mcp.client.stdio import stdio_client

    bridge = HERE / "bridge" / "bin" / "Release" / "net48" / "axure-bridge.exe"
    if not bridge.exists():
        bridge = HERE / "bridge" / "bin" / "Debug" / "net48" / "axure-bridge.exe"
    print(f"[{'ok' if bridge.exists() else 'FAIL'}] bridge artifact: {bridge}")

    params = StdioServerParameters(command=sys.executable, args=["server.py"], cwd=str(HERE))
    async with stdio_client(params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            tools = {t.name for t in (await session.list_tools()).tools}

    print(f"[{'ok' if len(tools) >= MIN_TOOLS else 'FAIL'}] tools={len(tools)} (min {MIN_TOOLS})")
    missing = sorted(REQUIRED - tools)
    print(f"[{'ok' if not missing else 'FAIL'}] required tools missing={missing}")

    ok = bridge.exists() and len(tools) >= MIN_TOOLS and not missing
    print("RESULT:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
