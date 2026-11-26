# axure-mcp

**English** | [简体中文](README.zh-CN.md)

[![Windows CI](https://github.com/NaIWaniubi/axure-mcp/actions/workflows/windows.yml/badge.svg)](https://github.com/NaIWaniubi/axure-mcp/actions/workflows/windows.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![MCP Tools](https://img.shields.io/badge/MCP%20tools-91-6f42c1)](docs/TOOLS.md)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4)](#requirements)

> **Give your agent hands inside Axure RP 9.**
> 91 MCP tools. 75 of them drive Axure's internal object model directly — no GUI, no mouse, no stolen focus.

Axure is the delivery format of an entire industry: `.rp` files, dynamic panels, variable conditions, masters, specification documents. But using it still means clicking.

**axure-mcp doesn't replace Axure. It gives Axure a programmable cockpit.**
An agent can create projects, pages, widgets, styles, interactions, variables, masters, and exports — all in the background.

```text
Agent  →  MCP (stdio)  →  JSON bridge  →  Axure RP 9 object model
```

---

## Why Axure, not another Figma

**Because the delivery format is not negotiable.**

| Context | Reality |
|---|---|
| Coursework / enterprise specs | Must hand in a `.rp` — double-clickable, editable |
| Legacy projects | Years of prototypes, component libraries, review notes live in `.rp` |
| Interaction fidelity | Variables + conditions + multi-case + panel states + repeater datasets |
| Deliverables | Spec docs, page inventories, annotations — all in one file |
| Review standards | Textbooks, rubrics, and acceptance templates are built around Axure |

Axure's problem was never "not capable enough". It's that **manual operation is heavy**:

- Restyling 20 buttons = 200 clicks
- Building 4 pages = a whole afternoon of dragging
- No CI, no agent access, no versioned generation

This project does exactly one thing: **keep Axure's delivery format and expressiveness, replace the clicking with code.**

---

## What you get

### Projects & pages

- Create `.rp`, page CRUD, page folders, move/reorder, recursive page tree
- Page size, background, alignment, adaptive views (Desktop / Mobile / Tablet)

### Widgets (12 advanced types)

Rectangle, button, text field, text area, dropdown, list box, checkbox, radio, image, table, dynamic panel, repeater, ellipse, horizontal/vertical line.

- **Dynamic panel**: multiple states, widgets per state, active state switching
- **Repeater**: dataset columns/rows/cells, item template
- **Table**: row/column sizing, cell text
- **Nested object tree**: read/write/delete via paths like `/9/state:1/0`

### Styling

Fill, font, size, bold/italic/underline, border, corner radius, opacity, rotation, padding.
Named styles for both widget and page stylesheets — create, read, apply, delete.

### Interactions (14 action types)

Conditional cases, show/hide/toggle, set text, set value, set variable, wait, open link (current/new window), scroll to widget, fire event, enable/disable, move, resize.

### Variables & expressions

Global variable CRUD, `[[var]]` interpolation, OnPageLoad assignment, variable conditions, set-variable actions.

### Masters & adaptive views

Create/rename/delete masters, add widgets inside a master, apply to pages, remove instances.
Create/resize adaptive views and attach them to pages.

### Exports

| Format | Notes |
|---|---|
| HTML | Axure's native generator; extension-install redirect automatically disabled |
| Word | Spec document `.docx` |
| PDF | Pure-code image PDF writer — no print driver, no Acrobat |
| PNG | Current page / all pages, offscreen render |
| CSV | Page + widget inventory |
| Image optimization | Axure's optimizer, measured 63 KB → 30 KB |

### Infrastructure

- **Single-process batch**: 121-widget prototype build drops from ~10 min to **~50 s**
- **Reflection introspection**: `find_types` / `describe_type` to explore Axure internals
- **Object tree inspection**: `get_widget` / `dump_page` / `list_widget_tree`
- **GUI fallback**: menus, screenshots, dialogs, canvas mouse/keyboard

Full catalog: [docs/TOOLS.md](docs/TOOLS.md).

---

## Quick start

### Requirements

- Windows 10 / 11
- **A licensed Axure RP 9 installation** (this repo does not ship Axure itself)
- .NET Framework 4.8 + .NET SDK
- Python 3.10+

> **Legal note:** Axure is commercial software. This repository contains only our own code — no Axure installer, DLLs, decompiled sources, or license files. Install and license Axure yourself.

### 1. Python dependencies

```bash
pip install -r requirements.txt
```

### 2. Build the C# bridge

```bash
cd bridge
dotnet build -c Debug
```

Output: `bridge/bin/Debug/net48/axure-bridge.exe`

> The project references `Newtonsoft.Json` and `SharpZipLib` from NuGet, not from the Axure install directory, so it builds on any Windows machine with the .NET SDK.

### 3. Run the self-checks

```bash
cd ..
python selfcheck_ci.py     # 91 tools registered (no Axure required)
python selfcheck_code.py   # full object-model regression (requires Axure)
python selfcheck.py        # GUI fallback regression (requires Axure)
```

### 4. Build the demos

```bash
python demo/build_workspace.py          # 4 pages / 121 widgets / 15 links
python demo/build_advanced_widgets.py   # 12 advanced widget types
python demo/build_full_feature_demo.py  # masters + views + variables + conditions + all exports
```

### 5. Preview locally

```bash
python -m http.server 18780 --bind 127.0.0.1 --directory demo/ai_workspace_html
```

Open <http://127.0.0.1:18780/index.html>.

> Do **not** open exported HTML via `file://` — Axure's runtime redirects to an extension-install page. Serve it over HTTP.

### 6. Register the MCP server

ZCode (`~/.zcode/cli/config.json`), Claude Desktop (`claude_desktop_config.json`), or any MCP client:

```json
{
  "mcpServers": {
    "axure": {
      "type": "stdio",
      "command": "python",
      "args": ["E:\\path\\to\\axure-mcp\\server.py"]
    }
  }
}
```

On Windows, prefer an absolute path to `python.exe` to avoid PATH differences.

---

## Usage

```python
# New project
axure_code_new_project(path="demo/demo.rp", name="My Prototype")

# Folder + page
axure_code_add_folder(path="demo/demo.rp", name="Business")
axure_code_add_page(path="demo/demo.rp", name="Home", parent_folder="Business")

# Styled button
axure_code_add_widget(
    path="demo/demo.rp", page="Home", type="button", text="Get started",
    x=80, y=120, w=200, h=52,
    style={"fill": "#2563EB", "textColor": "#FFFFFF", "borderWidth": 0,
           "cornerRadius": 12, "fontSize": 18, "bold": True},
)

# Click → open another page
axure_code_add_link(path="demo/demo.rp", page="Home", index=0, target_page="Details")

# Variable + OnLoad assignment
axure_code_add_variable(path="demo/demo.rp", name="userName", value="Guest")
axure_code_add_page_load_set_variable(path="demo/demo.rp", page="Home",
                                      name="userName", value="Admin")

# Conditional action chain
axure_code_add_action(path="demo/demo.rp", page="Home", index=1, action="set-text",
                      target_index=0, text="Triggered",
                      condition={"kind": "global", "variable": "count",
                                 "op": "equals", "value": "1"})
axure_code_add_action(path="demo/demo.rp", page="Home", index=1, action="show",
                      target_index=3, append_to_last_case=True)

# Exports
axure_code_generate_html(path="demo/demo.rp", out_dir="demo/html")
axure_code_export_word(path="demo/demo.rp", out_path="demo/spec.docx")
axure_code_export_pdf(path="demo/demo.rp", out_path="demo/prototype.pdf")
```

More parameters and return shapes: [docs/USAGE.md](docs/USAGE.md).

---

## Architecture

```text
axure-mcp/
├── server.py                 # FastMCP stdio server (91 tools)
├── axure_core.py             # GUI primitives (menus/dialogs/screenshots)
├── selfcheck_ci.py           # CI-safe tool registration check
├── selfcheck_code.py         # full object-model regression
├── selfcheck.py              # GUI fallback regression
├── bridge/
│   ├── CodeBridge.cs         # JSON dispatch + project/page/widget core
│   ├── BridgeWidgets.cs      # advanced widgets
│   ├── BridgeInteractions.cs # 14 interaction actions
│   ├── BridgeVariables.cs    # global variables / expressions
│   ├── BridgeMasters.cs      # masters
│   ├── BridgeAdaptive.cs     # adaptive views / page settings
│   ├── BridgeStyles.cs       # named styles
│   ├── BridgePageTree.cs     # folders / move / delete
│   ├── BridgeTree.cs         # object tree traversal / path editing
│   ├── BridgeExport.cs       # Word / images / CSV / optimization
│   ├── BridgePdf.cs          # pure-code PDF writer
│   └── BridgeReflection.cs   # reflection helpers
├── demo/                     # reproducible build scripts
├── docs/
│   ├── USAGE.md              # full usage guide
│   └── TOOLS.md              # 91-tool catalog
├── .github/workflows/        # Windows build + CI check
├── LICENSE
└── README.md
```

### How it works

Axure RP 9 is a mixed-mode .NET/WinForms application whose Core/Model/AxDoc assemblies are ILMerged into `AxureRP9.exe` and obfuscated. It exposes no COM or scripting API.

The bridge:

1. Spawns a **32-bit .NET host** (`axure-bridge.exe`) that loads `AxureRP9.exe` as a managed assembly.
2. Resolves internal types by reflection (`m0Sq.A0St`, `W0u2.m0uQ`, `s0Vz.C0V2`, `aax3.saxb`, …).
3. Calls Axure's own load/save pipeline — it never rewrites `.rp` bytes or patches the executable.
4. Exposes a **JSON command protocol** over stdin/stdout; `server.py` wraps it as MCP tools.

This is why it can build real `.rp` files that Axure opens natively, instead of emulating the format.

---

## Status

| Check | Result |
|---|---|
| `selfcheck_ci.py` | **PASS** — 91 tools registered, required tool set present |
| `selfcheck_code.py` | **PASS** — full object-model chain |
| `selfcheck.py` | **PASS** — GUI fallback chain |
| Main prototype | 4 pages / 121 widgets / 15 OnClick links, browser click-through verified |
| Advanced widgets | 12 types, panel dual-state, repeater 2 rows, table cells, image embed |
| Masters | applied master renders in exported HTML |
| Named styles | applied style turns button blue with white text |
| Exports | Word 79.9 KB / PDF 2 pages (pypdf-readable) / PNG ×2 / CSV 13 rows |

---

## Known limitations

- Tied to **Axure RP 9.0.0.3754** internal obfuscated type names. Upgrading Axure requires remapping.
- Cloud/Team publishing depends on Axure's client window object; falls back to GUI automation.
- Page duplication is rejected by Axure's object model (`Copy method should not be called for Package Objects`).
- Exported HTML with adaptive views may not initialize `$axure.document`, so click chains don't run; plain pages work.
- The 32-bit bridge can OOM on very large interaction graphs — batch in smaller chunks.
- Windows only (WinForms / UIA / PrintWindow / GDI+).

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). New widget types, interaction actions, export formats, and Axure version ports are welcome.

- One capability per PR
- Include `selfcheck_code.py` PASS output
- For exports, include artifact size/page count

---

## License

[MIT](LICENSE). Axure RP is a trademark and commercial product of Axure Software Solutions. This project is not affiliated with Axure.
