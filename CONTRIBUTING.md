# Contributing

感谢愿意一起把 Axure 变成可编程工具。

## 开发环境

```bash
git clone <repo>
cd axure-mcp
pip install -r requirements.txt
cd bridge && dotnet build -c Debug
cd .. && python selfcheck_code.py
```

## 提交前必须通过

```bash
python selfcheck_code.py   # 91 工具全链路
python selfcheck.py        # GUI 兜底
```

两条都必须是 `RESULT: PASS`。

## 代码约定

- **C# 桥**：一个能力一个 `Bridge*.cs` partial 文件；新增命令在 `CodeBridge.Dispatch` 注册。
- **反射**：优先用 `FindMethod` / `FindProperty` / `SetProperty`，不要硬编码单个版本的方法签名。
- **持久化**：写操作最后统一走 `SavePage(ctx, page)`；文档级修改走 `SaveManager`。
- **Python 工具**：`@mcp.tool()` + 一段中文 docstring；参数用 snake_case，桥内用 camelCase。
- **错误信息**：抛具体异常并带上下文（哪个 cmd、哪个页面、哪个 index）。

## 新增一个交互动作

1. 在 `BridgeInteractions.cs` 的 `CreateInteractionAction` 加 case
2. 动作类构造后用 `TrySetProperty(action, "IxMap", ix)`（需要上下文的动作）
3. 在 `server.py` 的 `axure_code_add_action` docstring 补动作名
4. 在 `selfcheck_code.py` 加一条调用
5. 实跑 `generate-html`，确认导出 `data.js` 里出现对应 action 类型

## 新增一个元件类型

1. `BridgeWidgets.cs` 的 `IsAdvancedWidget` 加类型
2. `CreateAdvancedWidget` 加构造与属性设置
3. 注意 owner 必须是 `page`/`master`（`zayC.vayw`），子图只用于 `AddToDiagram`
4. 动态面板/中继器子对象用 `CurrentPanelDiagram` / `BaseDiagram`

## 不要提交

- Axure 安装包、DLL、授权文件
- 反编译产物（`re/`）
- `bridge/bin`、`bridge/obj`
- 含个人路径的配置
- 生成的 demo 产物（用脚本重建）

## PR

- 一个 PR 一个能力
- 描述里附 `selfcheck_code.py` 的 PASS 输出片段
- 涉及导出的，附产物大小/页数
