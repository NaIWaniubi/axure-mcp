# Axure MCP 能力矩阵

状态说明：✅ 已打通并回归；🚧 逆向/实现中；⬜ 待接入。

## 工程与页面

| 功能 | 状态 | API / 工具 |
|---|---|---|
| 新建工程 | ✅ | `axure_code_new_project` |
| 工程摘要 | ✅ | `axure_code_project_info` |
| 页面列表 | ✅ | `axure_code_list_pages` |
| 添加页面 | ✅ | `axure_code_add_page` |
| 重命名页面 | ✅ | `axure_code_rename_page` |
| 删除页面 | ✅ | `axure_code_delete_page` |
| 页面文件夹/移动/排序/复制 | 🚧 | 文件夹/移动/排序已接入；复制页面待接入 |
| 页面尺寸/背景/对齐 | ✅ | `axure_code_set_page_size` / `set_page_style` |
| 打开/保存/另存为 GUI | ✅ | `axure_open` / `axure_save` |

## 元件

| 功能 | 状态 | API / 工具 |
|---|---|---|
| 矩形/按钮/输入框 | ✅ | `axure_code_add_widget` |
| 文本域 | ✅ | `type=textarea` |
| 下拉框/列表框 | ✅ | `type=combobox/listbox` + `add_list_option` |
| 复选框/单选按钮 | ✅ | `type=checkbox/radio` |
| 图片 | ✅ | `type=image` + `imagePath` |
| 表格 | ✅ | `type=table` + `table_set_cell/set_size` |
| 动态面板 | ✅ | `type=dynamic-panel` + `add_panel_state/add_panel_widget` |
| 中继器 | ✅ | `type=repeater` + `repeater_*` |
| 圆/横线/竖线 | ✅ | `type=ellipse/hline/vline` |
| 通用属性修改 | ✅ | `set_widget_property` / `set_page_property` |
| 删除元件 | ✅ | `delete_widget` / `delete_widget_path` |
| 嵌套对象树编辑 | ✅ | `list_widget_tree` + `*_widget_path` |

## 样式

| 功能 | 状态 | API / 工具 |
|---|---|---|
| 填充/字体/边框/圆角/透明度 | ✅ | `style` 参数 |
| 旋转/内边距等通用 PName | ✅ | `axure_code_set_style_property` |
| 样式回读 | ✅ | `list_widgets` / `get_widget` |
| 命名样式/样式表 | ✅ | `axure_code_*_named_style` |

## 交互

| 功能 | 状态 | API / 工具 |
|---|---|---|
| OnClick 打开当前页面 | ✅ | `axure_code_add_link` |
| 条件 Case | ✅ | `axure_code_add_action.condition` |
| 显示/隐藏 | ✅ | `action=show/hide/toggle` |
| 设置文本/值 | ✅ | `action=set-text/set-value` |
| 等待/延迟 | ✅ | `action=wait` |
| 设置变量 | ✅ | `action=set-variable` |
| 新窗口打开链接 | ✅ | `action=open-link, newWindow=true` |
| 滚动/移动/缩放/启用禁用 | ✅ | `action=scroll/move/resize/enable/disable` |
| 触发事件 | ✅ | `action=fire-event` |

## 变量与表达式

| 功能 | 状态 | API / 工具 |
|---|---|---|
| 全局变量 | ✅ | `axure_code_*_variable` |
| 页面变量 | ✅ | 统一使用全局变量模型 |
| OnPageLoad | ✅ | `axure_code_add_page_load_set_variable` |
| 表达式读写/求值 | ✅ | `axure_code_evaluate_expression` + 条件/动作 AST |

## 母版与自适应视图

| 功能 | 状态 | API / 工具 |
|---|---|---|
| 母版 CRUD/应用 | ✅ | `axure_code_*_master` / `add_master_widget` |
| 自适应视图 | ✅ | `axure_code_*_adaptive_view` |
| 页面设置 | ✅ | `axure_code_set_page_size` / `set_page_style` |
| 命名样式 | ✅ | `axure_code_*_named_style` |

## 导出与发布

| 功能 | 状态 | API / 工具 |
|---|---|---|
| HTML 原型 | ✅ | `axure_code_generate_html` |
| HTML 页面解析 | ✅ | `axure_read_export` |
| HTML 运行时交互 | ⚠️ | 普通页面已验证可点击跳转；启用自适应视图的页面在当前导出中未初始化 `$axure.document`，点击链不执行（demo 级问题，API/导出不受影响） |
| Word 说明书 | ✅ | `axure_code_export_word` |
| 当前页图片 | ✅ | `axure_code_export_page_image` |
| 所有页图片 | ✅ | `axure_code_export_all_images` |
| PDF | ✅ | `axure_code_export_pdf`（图像型 PDF） |
| CSV 规格报告 | ✅ | `axure_code_export_csv_report` |
| 图片优化 | ✅ | `axure_code_optimize_image` |
| 图片导入 | ✅ | `add-widget type=image` |
| Cloud/Team 发布 | 🚧 | 依赖 Axure 客户端窗口，暂用 `axure_ui_command` 兜底 |

## 基础设施

| 功能 | 状态 | API / 工具 |
|---|---|---|
| 单进程批量执行 | ✅ | `axure_code_batch` |
| CLR 类型搜索 | ✅ | `axure_code_find_types` |
| 类型反射自描述 | ✅ | `axure_code_describe_type` |
| 对象树自省 | ✅ | `dump_page` / `list_widget_tree` |
| GUI 菜单兜底 | ✅ | `axure_ui_command` |
| 截图/对话框 | ✅ | `axure_screenshot` / `axure_dialog_*` |
