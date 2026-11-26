# axure-mcp

[English](README.md) | **简体中文**

> **Give your agent hands inside Axure RP 9.**
> 91 MCP tools. 75 of them drive Axure's internal object model directly — no GUI, no mouse, no focus stealing.

Axure 是这个行业和这门课的通用交付格式：`.rp` 文件、动态面板、变量条件、母版、规格说明书。
但它的操作方式还停留在手点。

**axure-mcp 不解构 Axure，而是给它装一个可编程驾驶舱。**
Agent 可以通过 MCP 直接新建工程、建页面、拖元件、改样式、写交互、加变量、套母版、导出 HTML/Word/PDF——全部在后台完成。

```text
Agent  →  MCP (stdio)  →  JSON bridge  →  Axure RP 9 object model
```

---

## 为什么是 Axure，而不是重做一个 Figma

**因为交付格式换不动。**

| 场景 | 现实 |
|---|---|
| 课程作业 / 企业需求文档 | 要交 `.rp`，双击能开、能继续编辑 |
| 存量项目 | 多年原型库、组件规范、评审记录都在 `.rp` 里 |
| 交互表达 | 变量 + 条件 + 多 case + 动态面板多状态 + 中继器数据集 |
| 交付文档 | 规格说明书、页面清单、标注，一个文件全都有 |
| 评审口径 | 教材、评分表、验收模板按 Axure 能力设计 |

Axure 的问题从来不是"能力不够"，而是**手工操作太重**：

- 改 20 个按钮样式要点 200 次
- 建 4 个页面要拖一下午
- 无法进 CI、无法被 agent 调用、无法版本化生成

这个项目只做一件事：**保留 Axure 的交付格式和表达能力，把手点换成代码。**

---

## 能力一览

### 工程与页面

- 新建 `.rp`、页面 CRUD、页面文件夹、移动/排序、递归页面树
- 页面尺寸、背景、对齐、自适应视图（Desktop / Mobile / Tablet）

### 元件（12 类高级元件全支持）

矩形、按钮、输入框、文本域、下拉框、列表框、复选框、单选按钮、图片、表格、动态面板、中继器、圆、横线、竖线。

- 动态面板：多状态、状态内元件、当前状态切换
- 中继器：数据集列/行/单元格、项目模板
- 表格：行列数、单元格文本
- 嵌套对象树：`/9/state:1/0` 路径直接读写删

### 样式

填充、字体、字号、粗体/斜体/下划线、边框、圆角、透明度、旋转、内边距。
命名样式支持 widget / page 两套样式表，新建、读取、应用、删除。

### 交互（14 种动作）

条件 Case、显示/隐藏/切换、设置文本、设置值、设置变量、等待、当前/新窗口打开链接、滚动到元件、触发事件、启用/禁用、移动、缩放。

### 变量与表达式

全局变量 CRUD、`[[var]]` 插值求值、OnPageLoad 赋值、变量条件、变量赋值动作。

### 母版与自适应

母版新建/改名/删除、母版内加元件、应用到页面、从页面移除；
自适应视图新建/改尺寸/页面关联。

### 导出

| 格式 | 说明 |
|---|---|
| HTML | Axure 原生生成器，自动禁用扩展安装页跳转 |
| Word | 规格说明书 `.docx` |
| PDF | 纯代码图像型 PDF 写出器，不依赖打印驱动 |
| PNG | 当前页 / 全部页离屏渲染 |
| CSV | 页面+元件对象清单 |
| 图片优化 | 调用 Axure 优化器，实测 63KB → 30KB |

### 基础设施

- **单进程 batch**：121 元件原型构建从约 10 分钟降到 **50 秒**
- **反射自描述**：`find_types` / `describe_type` 随时查 Axure 内部类型
- **对象树自省**：`get_widget` / `dump_page` / `list_widget_tree`
- **GUI 兜底**：菜单、截图、对话框、画布鼠标键盘

完整工具清单见 [docs/TOOLS.md](docs/TOOLS.md)。

---

## 快速开始

### 0. 前置条件

- Windows 10/11
- **已自行安装并授权 Axure RP 9**（本项目不分发 Axure 安装包或授权）
- .NET Framework 4.8 + .NET SDK（编译 C# 桥）
- Python 3.10+

> 法律说明：Axure 是商业软件。仓库只包含我们自己的代码，不包含 Axure 安装包、DLL、反编译源码或授权文件。使用时请自行从 Axure 官方获取合法授权。

### 1. 安装 Python 依赖

```bash
pip install -r requirements.txt
```

### 2. 编译 C# 桥

```bash
cd bridge
dotnet build -c Debug
```

产物：`bridge/bin/Debug/net48/axure-bridge.exe`

### 3. 跑自检

```bash
cd ..
python selfcheck_code.py   # 91 工具全链路，约 2 分钟
python selfcheck.py        # GUI 兜底通道
```

### 4. 生成演示工程

```bash
python demo/build_workspace.py        # 4 页 / 121 元件 / 15 条跳转
python demo/build_advanced_widgets.py # 12 类高级元件
python demo/build_full_feature_demo.py# 母版 + 视图 + 变量 + 条件交互 + 全格式导出
```

### 5. 本地预览

```bash
python -m http.server 18780 --bind 127.0.0.1 --directory demo/ai_workspace_html
```

打开 <http://127.0.0.1:18780/index.html>。

> 不要用 `file://` 打开导出的 HTML——Axure 运行时会跳转扩展安装页。用 HTTP 打开。

### 6. 注册到 ZCode / Claude Desktop

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

---

## 使用示例

```python
# 新建工程
axure_code_new_project(path="demo/demo.rp", name="My Prototype")

# 加页面 + 文件夹
axure_code_add_folder(path="demo/demo.rp", name="业务页面")
axure_code_add_page(path="demo/demo.rp", name="首页", parent_folder="业务页面")

# 加元件（带样式）
axure_code_add_widget(
    path="demo/demo.rp", page="首页", type="button", text="立即开始",
    x=80, y=120, w=200, h=52,
    style={"fill": "#2563EB", "textColor": "#FFFFFF", "borderWidth": 0,
           "cornerRadius": 12, "fontSize": 18, "bold": True},
)

# 加交互：点击 → 打开另一页
axure_code_add_link(path="demo/demo.rp", page="首页", index=0, target_page="详情页")

# 加变量 + OnLoad 赋值
axure_code_add_variable(path="demo/demo.rp", name="userName", value="Guest")
axure_code_add_page_load_set_variable(path="demo/demo.rp", page="首页",
                                      name="userName", value="Admin")

# 导出
axure_code_generate_html(path="demo/demo.rp", out_dir="demo/html")
axure_code_export_word(path="demo/demo.rp", out_path="demo/spec.docx")
axure_code_export_pdf(path="demo/demo.rp", out_path="demo/prototype.pdf")
```

更多参数与返回值见 [docs/USAGE.md](docs/USAGE.md)。

---

## 仓库结构

```text
axure-mcp/
├── server.py                 # FastMCP stdio 服务（91 工具）
├── axure_core.py             # GUI 控制原语（菜单/对话框/截图）
├── selfcheck_code.py         # 代码级全链路回归
├── selfcheck.py              # GUI 兜底回归
├── bridge/
│   ├── CodeBridge.cs         # JSON 命令分发 + 工程/页面/元件核心
│   ├── BridgeWidgets.cs      # 高级元件
│   ├── BridgeInteractions.cs # 14 种交互动作
│   ├── BridgeVariables.cs    # 全局变量/表达式
│   ├── BridgeMasters.cs      # 母版
│   ├── BridgeAdaptive.cs     # 自适应视图/页面设置
│   ├── BridgeStyles.cs       # 命名样式
│   ├── BridgePageTree.cs     # 文件夹/移动/删除
│   ├── BridgeTree.cs         # 对象树遍历/路径编辑
│   ├── BridgeExport.cs       # Word/图片/CSV/优化
│   ├── BridgePdf.cs          # 纯代码 PDF 写出器
│   └── BridgeReflection.cs   # 反射helper
├── demo/                     # 可复现构建脚本
├── docs/
│   ├── USAGE.md              # 完整使用说明
│   └── TOOLS.md              # 91 工具清单
├── LICENSE
└── README.md
```

---

## 验收状态

| 项 | 结果 |
|---|---|
| `selfcheck_code.py` | **PASS**，91 工具注册，全链路 |
| `selfcheck.py` | **PASS**，GUI 兜底 |
| 主原型 | 4 页 / 121 元件 / 15 条 OnClick，浏览器点击跳转实测通过 |
| 高级元件 | 12 类，动态面板双状态、中继器 2 行、表格、图片全部通过 |
| 母版 | 应用后 HTML 正确渲染母版内容 |
| 命名样式 | 应用后按钮实测变为蓝底白字 |
| 导出 | Word 79.9KB / PDF 2 页（pypdf 可解析）/ PNG 2 张 / CSV 13 行 |

---

## 已知边界

- 依赖 **Axure RP 9.0.0.3754** 的混淆类型名；Axure 升级后需重新校准反射映射。
- Cloud/Team 发布依赖 Axure 客户端窗口对象，纯代码桥无法承载，保留 GUI 兜底。
- 页面复制被 Axure 对象模型显式拒绝（Package 对象不可 `Copy`）。
- 导出 HTML 启用自适应视图后，运行时可能不初始化 `$axure.document`，点击链不执行；普通页面正常。
- 32 位桥进程处理超大交互图可能 OOM，大工程建议分批操作。
- 仅支持 Windows（WinForms / UIA / PrintWindow / GDI+）。

---

## 贡献

见 [CONTRIBUTING.md](CONTRIBUTING.md)。欢迎补充元件类型、交互动作、导出格式和 Axure 版本适配。

## 许可

[MIT](LICENSE)。Axure RP 是 Axure Software Solutions 的商标与商业产品，本项目与其无隶属关系。
