# MCP 工具总览

当前共 **91 个工具**：代码级 **75 个**，GUI 兜底 **16 个**。

> 代码级工具通过反射直接操作 Axure 内部对象模型，不启动 GUI、不抢鼠标；GUI 工具用于截图、菜单兜底和模态框处理。

## 代码级工具（推荐）

| 工具 | 说明 |
|---|---|
| `axure_code_call` | 通用纯代码桥：直接调用 Axure 内部对象模型，不启动 GUI、不抢鼠标。 |
| `axure_code_batch` | 单进程批量执行多个纯代码命令，避免重复启动 Axure 程序集。 |
| `axure_code_describe_type` | 反射查看 Axure 内部类型的构造器、属性和方法，便于扩展 API。 |
| `axure_code_find_types` | 按名称搜索 Axure 内部 CLR 类型，辅助全功能 API 扩展。 |
| `axure_code_new_project` | 纯代码新建 .rp 工程。默认拒绝覆盖已有文件。 |
| `axure_code_list_pages` | 纯代码读取工程页面树。 |
| `axure_code_project_info` | 读取工程摘要：文件大小、页数、元件总数、文档类型。 |
| `axure_code_list_page_tree` | 读取完整页面树，包含文件夹层级。 |
| `axure_code_add_folder` | 添加页面文件夹。 |
| `axure_code_rename_folder` | 重命名页面文件夹。 |
| `axure_code_delete_folder` | 删除文件夹及其中的页面。 |
| `axure_code_move_page` | 移动页面到指定文件夹和位置。 |
| `axure_code_add_page` | 纯代码添加页面，可指定父文件夹和位置。 |
| `axure_code_rename_page` | 纯代码重命名页面。 |
| `axure_code_delete_page` | 纯代码彻底删除页面（页面树摘除 + 页面包物理删除 + 保存）。 |
| `axure_code_list_widgets` | 纯代码列出页面元件。 |
| `axure_code_get_widget` | 读取元件完整模型：基础信息、样式、交互树（自省用）。 |
| `axure_code_dump_page` | 递归读取页面对象树，用于高级对象逆向与验证。 |
| `axure_code_list_widget_tree` | 递归列出页面元件树，包含动态面板状态、中继器模板、表格单元格。 |
| `axure_code_get_widget_path` | 按对象树路径读取元件，如 /9/state:1/0。 |
| `axure_code_update_widget_path` | 按对象树路径修改元件。 |
| `axure_code_delete_widget_path` | 按对象树路径删除元件。 |
| `axure_code_add_widget` | 纯代码添加基础/高级元件；高级元件支持 options/rows/columns/cells/data/imagePath/stateName 等参数。 |
| `axure_code_update_widget` | 纯代码修改元件名称、文本、位置、尺寸或样式。 |
| `axure_code_set_widget_property` | 通用设置元件可写属性，适用于已逆向但尚未单独封装的基础属性。 |
| `axure_code_set_style_property` | 按 Axure PName 通用设置样式属性，如 ROTATION/PADDING_LEFT/OPACITY。 |
| `axure_code_set_page_property` | 通用设置页面可写属性。 |
| `axure_code_delete_widget` | 纯代码删除页面元件。 |
| `axure_code_add_panel_state` | 给动态面板添加状态。 |
| `axure_code_add_panel_widget` | 向动态面板指定状态添加元件。 |
| `axure_code_set_panel_state` | 设置动态面板当前状态。 |
| `axure_code_list_panel_states` | 读取动态面板状态列表。 |
| `axure_code_repeater_add_column` | 给中继器数据集添加列。 |
| `axure_code_repeater_add_row` | 给中继器数据集添加行。 |
| `axure_code_repeater_set_cell` | 设置中继器数据集单元格。 |
| `axure_code_repeater_add_item` | 向中继器项目模板添加元件。 |
| `axure_code_list_repeater_data` | 读取中继器数据集。 |
| `axure_code_table_set_cell` | 设置表格单元格文本。 |
| `axure_code_table_set_size` | 调整表格行列数。 |
| `axure_code_add_list_option` | 给下拉框/列表框添加选项。 |
| `axure_code_list_options` | 读取下拉框/列表框选项。 |
| `axure_code_add_link` | 纯代码给元件添加交互：当前窗口单击打开目标页面。 |
| `axure_code_list_variables` | 读取全局变量列表。 |
| `axure_code_get_variable` | 读取全局变量。 |
| `axure_code_add_variable` | 新增全局变量并设置默认值。 |
| `axure_code_update_variable` | 重命名或修改全局变量值。 |
| `axure_code_delete_variable` | 删除全局变量。 |
| `axure_code_evaluate_expression` | 按 Axure 变量插值规则求值文本表达式。 |
| `axure_code_add_page_load_set_variable` | 添加页面 OnLoad 时设置全局变量的动作。 |
| `axure_code_list_masters` | 读取母版列表。 |
| `axure_code_add_master` | 新建母版。 |
| `axure_code_rename_master` | 重命名母版。 |
| `axure_code_delete_master` | 删除母版。 |
| `axure_code_add_master_widget` | 向母版添加元件。 |
| `axure_code_apply_master` | 将母版应用到页面。 |
| `axure_code_remove_master` | 从页面移除母版实例。 |
| `axure_code_list_adaptive_views` | 读取页面自适应视图。 |
| `axure_code_add_adaptive_view` | 为页面添加自适应视图。 |
| `axure_code_set_adaptive_view_size` | 设置自适应视图尺寸。 |
| `axure_code_set_page_size` | 设置页面尺寸。 |
| `axure_code_set_page_style` | 设置页面样式属性，如 FILL/PAGE_ALIGNMENT。 |
| `axure_code_list_named_styles` | 列出命名样式；kind=widget\|page。 |
| `axure_code_get_named_style` | 读取命名样式完整模型。 |
| `axure_code_add_named_style` | 新建命名样式。 |
| `axure_code_apply_named_style` | 应用命名样式到元件或页面。 |
| `axure_code_delete_named_style` | 删除命名样式。 |
| `axure_code_add_action` | 给元件添加复杂交互动作：show/hide/toggle/wait/set-text/set-value/set-variable/open-link/ |
| `axure_code_clear_interactions` | 清空元件或页面的全部交互。 |
| `axure_code_export_word` | 纯代码导出 Word 规格说明书。 |
| `axure_code_export_page_image` | 纯代码离屏渲染当前页图片。 |
| `axure_code_export_all_images` | 纯代码渲染所有页面图片。 |
| `axure_code_optimize_image` | 调用 Axure 图片优化器压缩图片。 |
| `axure_code_export_csv_report` | 导出 CSV 规格报告（页面/母版清单）。 |
| `axure_code_export_pdf` | 纯代码导出图像型 PDF；page 为空时导出全部页面。 |
| `axure_code_generate_html` | 纯代码生成可运行的 Axure HTML 原型，返回 index.html。 |

## GUI 兜底工具

| 工具 | 说明 |
|---|---|
| `axure_status` | 列出运行中的 Axure RP 9 实例、各自标题、当前对话框与忙模态状态。 |
| `axure_launch` | 启动 Axure RP 9。path 给 .rp 文件则直接打开该工程（命令行通道，最可靠）。 |
| `axure_open` | 打开 .rp 工程。mode=instance：命令行直开新实例（推荐，可靠）； |
| `axure_save` | 保存当前工程。不给 path 走 文件>保存；给 path 走 另存为（需要操作保存对话框， |
| `axure_ui_command` | 执行任意菜单命令。menu_path 逐级匹配，例如 ["发布","生成 HTML"] 或 |
| `axure_menu_tree` | 枚举菜单树（agent 的“能力地图”）。top 给顶级菜单名则只展开该支。 |
| `axure_dismiss_nags` | 清理骚扰/恢复/告警弹窗（IDM corrupt、Warning、Alert 确定）。返回处置标题。 |
| `axure_dialog_dump` | 列出当前对话框：标题/矩形/UIA 可见控件（若有）+ 每个对话框的截图路径。 |
| `axure_dialog_respond` | 操作对话框：按名点按钮 / 向首个输入框填值（剪贴板粘贴，支持中文） / 发按键。 |
| `axure_canvas` | 在 Axure 窗口上操作鼠标。x,y 为屏幕绝对坐标；relative_to=window 时相对窗口左上角。 |
| `axure_canvas_drag` | 窗口内拖拽（相对坐标）。用于画布拉元件/缩放选区。 |
| `axure_keys` | 向 Axure 前台窗口发按键序列（uiautomation SendKeys 语法：^S {Enter} {ESC}）。 |
| `axure_type` | 输入文本（剪贴板粘贴路径对中文不稳，走 SendKeys 逐字）。replace=先全选替换。 |
| `axure_screenshot` | 截图。默认整窗口；region=[l,t,r,b]（相对窗口像素）截局部。 |
| `axure_export_html` | 发布>生成 HTML（使用上次记忆的输出目录）：菜单→对话框→Enter→等空闲。 |
| `axure_read_export` | 解析已生成的 HTML 导出目录 → 页面列表/入口/关键文件。 |
