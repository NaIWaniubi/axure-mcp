# Axure MCP 控制桥 — 最终规划与验收（2026-11-25）

## 1. 目标

让 agent 通过 MCP(stdio) 在后台直接操控本机 Axure RP 9：
工程/页面/元件 CRUD、样式、点击跳转交互、HTML 原型导出；GUI 自动化仅作兜底。

## 2. 环境兼容矩阵

| 项 | 实测值 |
|---|---|
| 产品 | Axure RP 9 Team Edition 9.0.0.3754 |
| 主程序 | `D:\Program Files (x86)\Axure\Axure RP 9\AxureRP9.exe` |
| 运行时 | .NET Framework 4.5.2 / WinForms / x86 |
| 业务程序集 | Core/Model/AxDoc/Client/Generators 已 ILMerge 进主 exe |
| 原生接口 | 无官方 COM / 脚本 API；内部对象模型可通过反射调用 |
| `.rp` | `AC EF 09 00` 帧化半 JSON；页面流需 gzip 后写回 |
| UIA | 菜单可读；画布/面板/多数对话框自绘 |
| 白屏 | MuMu/GameViewer 虚拟显示适配器劫持 DComp；`GpuPreference=2` 修复 |

## 3. 最终架构

```text
Agent
  └─ MCP stdio (server.py, 28 tools)
       ├─ axure_code_* ── JSON subprocess ── axure-bridge.exe (x86/net48)
       │                                      └─ 反射调用 AxureRP9.exe 内部对象模型
       └─ axure_* GUI ── UIA / Win32 / PrintWindow（兜底）
```

代码级链路不启动 Axure GUI，不发送鼠标/键盘消息，不修改 Axure 原程序字节。

## 4. 阶段切片

| 阶段 | 状态 | 验收断言 |
|---|---|---|
| S0 环境/格式侦察 | 完成 | 版本、进程、程序集、`.rp`/`.rppkg` 格式确认 |
| S1 GUI 控制原语 | 完成 | 菜单、IFileDialog、PrintWindow、后台消息通过 |
| S2 全量反编译 | 完成 | ILSpy 输出 2825 个 C# 文件 |
| S3 对象模型初始化 | 完成 | x86 反射宿主可加载 Axure 程序集与资源 |
| S4 工程/页面/元件 CRUD | 完成 | 新建、加页、加元件、gzip 持久化、回读通过 |
| S5 样式 API | 完成 | 填充、字体、边框、圆角、透明度写入并导出 |
| S6 交互 API | 完成 | OnClick → Current Window → Target Page 导出可点击 |
| S7 HTML 生成 | 完成 | `n9xc.Generate` 输出完整可运行原型 |
| S8 MCP 接入 | 完成 | 28 工具 stdio 注册与调用通过 |
| S9 真机验收 | 完成 | 浏览器首页→工作台→详情页点击闭环通过 |

## 5. 关键反射映射

| 能力 | 类型/方法 |
|---|---|
| 文档管理 | `m0Sq.A0St.Load / Save / F0Ba / N0B9` |
| 页面 | `W0u2.m0uQ`、`Q0IK.W0Iu.Sitemap` |
| 元件 | `s0Vz.C0V2`、`Axure.DocumentModel.Objects.Button/TextBox/VectorShape` |
| 位置/尺寸 | `l0or(q6X7,bool,bool)`、`L0ob(B6BA,bool,bool)` |
| 样式 | `aax3.saxb.oanl(PName,object,bool)` |
| 页面流 | `jaqN.taqY.IaqR(page)` + `GZipOutputStream` |
| 删除页面 | `Walp.sald(Guid)` + `Sitemap.l0HS(node)` + `uaPZ.Delete(Guid,null)` |
| 交互容器 | `j0be.K0bF`、`C0V2.sf04/p0FN` |
| 事件/Case | `IxEventType.OnClick`、`IxCase` |
| 跳转动作 | `IxLinkWindowAction(LinkType.Current)`、`IxTarget(Page)` |
| HTML | `cRNf.mRNI.eRNE(outDir)` + `x9xW.n9xc.Generate(manager)` |

## 6. 验收证据

- `python selfcheck_code.py`：**PASS**
  - 28 工具注册
  - new-project / add-page / add-widget / update-widget / delete-widget
  - add-link / generate-html / rename-page / delete-page
- `python selfcheck.py`：**PASS**
  - GUI 状态、菜单树、截图、HTML 导出、导出解析闭环
- `python demo/build_workspace.py`：**PASS**
  - 4 页、121 个元件、15 条 OnClick 跳转
  - 输出 `demo/ai_workspace.rp` 与 `demo/ai_workspace_html/index.html`
- 真实浏览器：**PASS**
  - 首页 → 工作台 → 详情页
  - URL hash、标题、页面正文均正确切换
  - 截图：`workspace_home_final.png`、`workspace_dashboard_final.png`、`workspace_detail_final.png`

## 7. 已知边界

- 依赖 Axure RP 9 当前混淆类型名；版本升级需重新校准反射映射。
- 尚未接入的对象模型功能先用 `axure_ui_command` 兜底。
- 代码桥生成 HTML 时复制源文件到临时目录，避免 Axure GUI 独占锁。
- GUI 截图若再出现白屏，先检查虚拟显示适配器并设置 `GpuPreference=2`。

## 8. 全功能覆盖路线（2026-11-25 启动）

目标：把 Axure 常用与高级对象模型全部接入代码桥，形成可审计的 API 覆盖矩阵。

1. **基础设施**
   - 单进程 `batch` 批量执行：已完成，121 元件原型构建从约 10 分钟降至 50 秒。
   - `describe-type` 反射自描述：已完成。
   - `get-widget` / `dump-page` 对象树回读：已完成。
2. **高级元件**（已完成）：动态面板及状态、中继器及数据集/项目模板、图片、表格、文本域、下拉框、列表框、复选框、单选按钮、圆、横线、竖线。动态面板子图改用 `SaveAllInternally` 内嵌持久化。
3. **母版与视图**（已完成）：母版 CRUD/应用、自适应视图、页面尺寸/背景/对齐、命名样式、页面文件夹。
4. **变量与表达式**（已完成）：全局变量 CRUD、`[[var]]` 插值求值、OnLoad 赋值、变量条件与 `set-variable` 动作。
5. **复杂交互**（已完成）：条件 Case、显示/隐藏、设文本/值、等待、变量、新窗口链接、滚动、触发事件、启用/禁用、移动、缩放。
6. **导出/导入/发布**（已完成）：Word 说明书、当前页图片、全部页图片、图像型 PDF、CSV 清单、图片优化、图片导入。Cloud/Team 保留 GUI 兜底。
7. **深度编辑**（已完成）：通用对象树遍历、嵌套路径读写删、现有 `.rp` 的对象级修改与回读。
8. **全量验收**（已完成）：`selfcheck_code.py` 91 工具全链路 PASS；`selfcheck.py` GUI 兜底 PASS；全功能 demo 一次导出 HTML/Word/PDF/PNG/CSV。

## 9. 最终交付（2026-11-25）

- 代码级工具 **75 个**，MCP 工具总计 **91 个**，桥命令 **74 条**。
- 高级元件 12 类；母版、自适应视图、命名样式、全局变量、14 种交互动作全部接入。
- 导出：HTML、Word、图像型 PDF、全部页 PNG、CSV、图片优化。
- 全功能 demo：`demo/build_full_feature_demo.py`，2 页 10 元件，一次跑通全部导出。
- 磁盘：反编译源（约 44MB）已迁至 C 盘 temp 并以目录联接保留原路径，E 盘释放约 13GB。
