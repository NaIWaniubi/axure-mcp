# 使用说明

本文件覆盖安装、注册、核心概念、常用工作流和排错。工具全量清单见 [TOOLS.md](TOOLS.md)。

---

## 1. 安装

### 1.1 环境

| 依赖 | 要求 |
|---|---|
| 操作系统 | Windows 10 / 11（x64） |
| Axure | RP 9 Team/Pro 9.0.0.3754，已自行安装并授权 |
| .NET | .NET Framework 4.8 + .NET SDK 6/7/8（用于编译 net48 项目） |
| Python | 3.10+ |

> 仓库不含 Axure 安装包、DLL、授权文件或反编译源码。请自行从 Axure 官方获取合法授权。

### 1.2 Python 依赖

```bash
pip install -r requirements.txt
```

### 1.3 编译桥

```bash
cd bridge
dotnet build -c Debug
```

验证：

```bash
echo '{"cmd":"project-info","path":"<某个 .rp 路径>"}' | bridge/bin/Debug/net48/axure-bridge.exe
```

### 1.5 自检

```bash
python selfcheck_ci.py     # 不需要 Axure：验证 91 个 MCP 工具注册
python selfcheck_code.py   # 需要 Axure：完整对象模型回归
python selfcheck.py        # 需要 Axure：GUI 兜底回归
```

CI 使用 `selfcheck_ci.py`，因为 GitHub 托管 runner 不安装 Axure。

### 1.6 注册 MCP

ZCode (`~/.zcode/cli/config.json`)、Claude Desktop (`claude_desktop_config.json`) 或任意 MCP 客户端：

```json
{
  "mcpServers": {
    "axure": {
      "type": "stdio",
      "command": "python",
      "args": ["E:\\path\\to\\axure-mcp\\server.py"],
      "env": {}
    }
  }
}
```

Windows 下建议写 Python 绝对路径，避免 PATH 差异。

---

## 2. 核心概念

### 2.1 两种工作模式

| 模式 | 工具前缀 | 特点 |
|---|---|---|
| 代码级 | `axure_code_*` | 反射操作对象模型，不启动 GUI，速度快，可进 CI |
| GUI 兜底 | `axure_*` | 菜单、截图、对话框、画布鼠标；用于代码级未覆盖的能力 |

优先用代码级；代码级没有的再用 `axure_ui_command`。

### 2.2 稳定寻址

- **页面按名字**：所有工具用 `page="首页"`。
- **元件按索引**：`index` 是页面 `DiagramObjects` 的下标，从 0 开始，按添加顺序。
- **嵌套元件按路径**：`widget_path="/9/state:1/0"` 表示「第 9 个元件的第 1 个状态里的第 0 个元件」。
  - `/10/item/0`：中继器项目模板第 0 个元件
  - `/8/cells/0`：表格第 0 个单元格

索引会随增删变化，批量脚本建议「先建后删」或使用路径。

### 2.3 持久化

每次写操作都会：
1. 序列化页面对象图（含动态面板子状态、中继器模板）
2. 写回页面包
3. 保存 `.rp` 容器

不需要额外调用保存。GUI 的 `axure_save` 只用于已打开的工程。

---

## 3. 常用工作流

### 3.1 从零建一个可点击原型

```python
# 1) 工程 + 页面
axure_code_new_project(path="demo/app.rp", name="App")
axure_code_add_page(path="demo/app.rp", name="首页")
axure_code_add_page(path="demo/app.rp", name="详情页")

# 2) 首页元件
axure_code_add_widget(path="demo/app.rp", page="首页", type="rectangle",
    text="", x=0, y=0, w=1200, h=800,
    style={"fill": "#F5F7FB", "borderWidth": 0})
axure_code_add_widget(path="demo/app.rp", page="首页", type="button",
    text="查看详情", x=80, y=120, w=180, h=52,
    style={"fill": "#2563EB", "textColor": "#FFFFFF", "borderWidth": 0,
           "cornerRadius": 12, "fontSize": 18, "bold": True})

# 3) 点击跳转
axure_code_add_link(path="demo/app.rp", page="首页", index=1, target_page="详情页")

# 4) 导出
axure_code_generate_html(path="demo/app.rp", out_dir="demo/app_html")
```

### 3.2 批量构建（快 10 倍）

单条工具每次都要启动一次桥进程。批量写用 `axure_code_batch`：

```python
axure_code_batch(ops=[
    {"cmd": "add-widget", "path": rp, "page": "首页", "type": "button",
     "text": f"按钮{i}", "x": 40, "y": 40 + i * 60, "w": 160, "h": 44},
    for i in range(30)
])
```

实测：121 个元件从约 10 分钟降到 **50 秒**。

### 3.3 动态面板

```python
# 建面板（默认自带 1 个状态）
axure_code_add_widget(path=rp, page="首页", type="dynamic-panel",
    x=80, y=300, w=320, h=180, state_name="默认状态")

# 加第 2 个状态
axure_code_add_panel_state(path=rp, page="首页", index=3, name="第二状态")

# 往状态里放元件
axure_code_add_panel_widget(path=rp, page="首页", index=3, state_index=0,
    type="rectangle", text="第一屏", x=0, y=0, w=320, h=180,
    style={"fill": "#DBEAFE", "borderWidth": 0, "fontSize": 22})

# 切换当前状态
axure_code_set_panel_state(path=rp, page="首页", index=3, state_index=1)
```

### 3.4 中继器

```python
axure_code_add_widget(path=rp, page="列表页", type="repeater",
    x=40, y=80, w=600, h=300, fit_to_content=True,
    data={"columns": ["姓名", "部门"],
          "rows": [["Alice", "研发"], ["Bob", "设计"]]})

# 模板行
axure_code_repeater_add_item(path=rp, page="列表页", index=0,
    type="rectangle", text="行", x=0, y=0, w=600, h=40,
    style={"fill": "#FFFFFF", "borderColor": "#E2E8F0", "borderWidth": 1})

# 改单元格
axure_code_repeater_set_cell(path=rp, page="列表页", index=0,
    row=0, column=0, value="Carol")
```

### 3.5 变量与条件交互

```python
axure_code_add_variable(path=rp, name="count", value="1")
axure_code_add_variable(path=rp, name="userName", value="Guest")

# 页面 OnLoad 赋值
axure_code_add_page_load_set_variable(path=rp, page="首页",
    name="userName", value="Admin")

# 条件动作：count == "1" 时改标题
axure_code_add_action(path=rp, page="首页", index=1, action="set-text",
    target_index=0, text="已触发",
    condition={"kind": "global", "variable": "count", "op": "equals", "value": "1"})

# 同一 case 内继续追加（顺序执行）
axure_code_add_action(path=rp, page="首页", index=1, action="wait",
    wait_ms=200, append_to_last_case=True)
axure_code_add_action(path=rp, page="首页", index=1, action="show",
    target_index=3, append_to_last_case=True)
```

条件还支持：
- `{"kind":"text","target_index":2,"op":"equals","value":"yes"}`
- `{"kind":"selected","target_index":2,"value":true}`
- `{"kind":"visible","target_index":2,"value":false}`
- `{"op":"and","conditions":[...]}` / `{"op":"or","conditions":[...]}`

### 3.6 母版

```python
axure_code_add_master(path=rp, name="页头")
axure_code_add_master_widget(path=rp, master="页头", type="rectangle",
    text="产品名", w=1200, h=72,
    style={"fill": "#0F172A", "textColor": "#FFFFFF", "borderWidth": 0})
axure_code_apply_master(path=rp, page="首页", master="页头", background=True)
```

### 3.7 命名样式

```python
axure_code_add_named_style(path=rp, kind="widget", name="Primary",
    style={"fill": "#2563EB", "textColor": "#FFFFFF", "borderWidth": 0,
           "cornerRadius": 12, "fontSize": 18, "bold": True})
axure_code_apply_named_style(path=rp, kind="widget", name="Primary",
    page="首页", index=1)
```

### 3.8 自适应视图

```python
axure_code_add_adaptive_view(path=rp, page="首页", name="Mobile",
    width=375, height=812, default_width=1200, default_height=800)
axure_code_set_adaptive_view_size(path=rp, page="首页", view="Mobile",
    width=390, height=844)
```

### 3.9 导出全家桶

```python
axure_code_generate_html(path=rp, out_dir="out/html")
axure_code_export_word(path=rp, out_path="out/spec.docx")
axure_code_export_pdf(path=rp, out_path="out/prototype.pdf")
axure_code_export_all_images(path=rp, out_dir="out/images")
axure_code_export_csv_report(path=rp, out_path="out/report.csv")
axure_code_optimize_image(input_path="out/images/首页.png",
                          output_path="out/images/首页_opt.png")
```

---

## 4. 排错

| 现象 | 原因 / 处理 |
|---|---|
| 桥进程报 `无法加载 Axure 程序集` | Axure 未安装、版本不符、或桥不是 x86。确认 `D:\Program Files (x86)\Axure\Axure RP 9\AxureRP9.exe` 存在 |
| 打开的 HTML 跳到扩展安装页 | 用 `file://` 打开了。用 HTTP 服务打开；导出器已自动禁用该跳转 |
| 页面点击无反应 | 启用自适应视图的导出页可能不初始化 `$axure.document`；用普通页面或重新导出 |
| `batch` 中途 OOM | 32 位进程内存上限。拆小批次，或分多个进程执行 |
| `export-csv-report` 卡住 | 已改用自写 CSV 导出器；若仍卡，升级到最新代码 |
| 中文乱码 | 确保 `.rp` 路径与输出目录为 UTF-8 可表示；Windows 文件名避免非法字符 |
| `dotnet build` 找不到 DLL | csproj 已使用 NuGet 包，执行 `dotnet restore` 后重试 |

---

## 5. 设计取舍

- **不伪造 `.rp` 二进制**：`.rp` 是帧化半 JSON + 字符串驻留，直接构造风险极高。桥走 Axure 自己的加载/保存链路。
- **不改 Axure 程序字节**：全部通过反射调用内部类型，升级只需校准映射。
- **不抢焦点**：代码级链路不启动 GUI；需要 GUI 时才用 `axure_launch`。
- **页面流内嵌子包**：动态面板状态图用 `SaveAllInternally`，否则状态存在但内容不落盘。
- **CSV 自实现**：内置 `Y9n2` 生成器会阻塞等待 UI 上下文。

---

## 6. 版本与兼容

当前针对 **Axure RP 9.0.0.3754**。Axure 使用了混淆类型名（如 `s0Vz.C0V2`、`aax3.saxb`），升级版本后需要：

1. 用 ILSpy 重新反编译 AxureRP9.exe
2. 用 `axure_code_find_types` / `describe_type` 对比类型与方法
3. 更新 `Bridge*.cs` 中的类型名

欢迎提交新版本适配 PR。
