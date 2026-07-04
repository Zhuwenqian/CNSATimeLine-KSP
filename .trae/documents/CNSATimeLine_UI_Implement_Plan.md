# CNSATimeLine 模组 UI 与自动加速功能实施计划（简化版）

## 1. 摘要（Summary）

本计划为 CNSATimeLine KSP 模组增加一套简单的 UI 与自动加速能力：

在**太空中心（SpaceCentre）**场景按 `Ctrl+L` 弹出窗口，列出 `中国航天大事时间线.txt` 中的所有事件，每个事件旁提供“加速到此时”按钮。点击后将游戏全局时间（UT）加速到该事件真实时间前 10 小时。飞行场景相关功能本次暂不实现。

时间映射依赖已安装的 `RSSTimeFormatter.dll`：该模组将 `KSPUtil.dateTimeFormatter` 替换为 `RealDateTimeFormatter`，并以可配置的 `epoch`（默认 1951-01-01）为基准，将 KSP 的 UT 秒数映射为真实地球日期时间。本模组通过反射读取该 `epoch`，再将事件的真实日期时间反算为 KSP UT 秒数。

## 2. 当前状态分析（Current State Analysis）

已探索的关键文件与发现：

- **`CNSATimeLineMod.cs`**：现有入口类，仅标注 `[KSPAddon(KSPAddon.Startup.Flight, false)]`，包含 RSSTimeFormatter 依赖检测示例，需要改为 SpaceCentre 场景入口。
- **`CNSATimeLine.csproj`**：目标框架 .NET Framework 4.7.2，已引用 KSP/Unity 程序集与 `RSSTimeFormatter.dll`。
- **`中国航天大事时间线.txt`**：纯文本数据源，格式为 `YYYY-MM-DD HH:MM:SS | 事件描述`，部分行标注精度（精确到日/分/秒）。解析器需兼容秒位为 `00` 占位的情况。
- **`RSSTimeFormatter.dll`** / GitHub 源码：
  - `RealDateTimeFormatter` 内部持有 `DateTime epoch`，默认值为 `1951-01-01`。
  - 转换关系：`PrintDate(ut)` = `epoch.AddSeconds(ut)`；反推：`ut = (targetDateTime - epoch).TotalSeconds`。
  - epoch 可通过 `GameData` 中 `RSSTimeFormatter` 节点的 `epoch` 值自定义，因此不能写死，需运行时反射获取。

## 3. 拟议修改（Proposed Changes）

### 3.1 新增 `TimeLineEvent.cs`

- **用途**：定义单条时间线事件的数据结构。
- **内容**：
  - `DateTime EventDateTime`：解析后的真实地球日期时间。
  - `string Description`：事件描述。
  - `string Precision`：原始精度标注（精确到日/分/秒）。
  - `double UtcSeconds`：映射到 KSP 的 UT 秒数。
- **设计理由**：将解析、时间映射与 UI 展示解耦，便于复用。

### 3.2 新增 `TimeLineDataLoader.cs`

- **用途**：在模组初始化时读取 `GameData/CNSATimeLine/中国航天大事时间线.txt`，解析为 `List<TimeLineEvent>`。
- **内容**：
  - 使用相对路径 `KSPUtil.ApplicationRootPath + "/GameData/CNSATimeLine/中国航天大事时间线.txt"` 读取，兼容 Windows/Linux/Docker。
  - 正则解析每一行：`^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \| (.+)$`。
  - 读取精度标注行（以 `【` 开头）作为上一条事件的精度。
  - 调用 `TimeLineConverter` 计算 `UtcSeconds`。
- **可调参数注释**：文件路径、解析失败时是否忽略行。

### 3.3 新增 `TimeLineConverter.cs`

- **用途**：将真实地球日期时间与 KSP UT 秒数互相转换。
- **内容**：
  - 通过反射读取 `KSPUtil.dateTimeFormatter` 的实际类型；如果为 `RSSTimeFormatter.RealDateTimeFormatter`，读取其私有字段 `epoch`。
  - 若 RSSTimeFormatter 未加载，回退到默认 epoch `1951-01-01`（与 RSSTimeFormatter 默认一致），并写入日志警告。
  - 提供 `DateTimeToUtcSeconds(DateTime)` 与 `UtcSecondsToDateTime(double)` 方法。
- **可调参数注释**：fallbackEpoch 可在代码中调整，影响未安装 RSSTimeFormatter 时的映射结果。

### 3.4 新增 `TimeLineWindow.cs`

- **用途**：太空中心场景的 IMGUI 窗口，响应 `Ctrl+L` 热键。
- **内容**：
  - 维护 `bool isVisible`。
  - `Update()` 中检测 `Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)`（或 `RightControl`）切换窗口可见性。
  - `OnGUI()` 中绘制滚动列表，每行显示：事件日期描述 + “加速到此时”按钮。
  - 按钮点击逻辑：
    - 目标 UT = `event.UtcSeconds - 10 * 3600`。
    - 若目标 UT <= 当前 UT，按钮置灰（不能倒着加速）。
    - 否则调用 `TimeWarp.fetch.WarpTo(targetUT)`。
- **可调参数注释**：热键组合、提前量（默认 10 小时）、窗口尺寸与位置。

### 3.5 修改 `CNSATimeLineMod.cs`

- **用途**：改为太空中心场景入口，加载数据与 UI。
- **内容**：
  - 将 `[KSPAddon(KSPAddon.Startup.Flight, false)]` 改为 `[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]`。
  - 在 `Awake()` 或 `Start()` 中调用 `TimeLineDataLoader.Load()` 并创建 `TimeLineWindow` 组件。
  - 移除原示例中无意义的 `enableFormatterCheck` 示例调用，改为在 `TimeLineConverter` 中按需检测。

### 3.6 修改 `CNSATimeLine.csproj`

- 添加 `<Compile Include="..." />` 条目以包含新增 C# 文件（`TimeLineEvent.cs`、`TimeLineDataLoader.cs`、`TimeLineConverter.cs`、`TimeLineWindow.cs`）。
- 保持所有路径为相对路径。

### 3.7 更新文档

- **`readme/开发文档.md`**：补充新模块说明、时间映射原理。
- **`readme/实施文档.md`**：更新目录结构、部署说明（新增需复制 `中国航天大事时间线.txt` 到 `GameData/CNSATimeLine/`）。
- **`readme/功能更新.md`**：新增一条 2026-07-04 的功能新增记录。

## 4. 假设与决策（Assumptions & Decisions）

1. **时间映射**：依赖 `RSSTimeFormatter.dll` 已加载并替换 `KSPUtil.dateTimeFormatter`。若未加载，回退 epoch 为 `1951-01-01`，与 RSSTimeFormatter 默认一致。
2. **不能倒退加速**：当目标 UT 小于等于当前 UT 时，“加速到此时”按钮禁用并置灰。
3. **文件部署**：`中国航天大事时间线.txt` 需放置于 `GameData/CNSATimeLine/`，与 DLL 同级，确保游戏内相对路径读取。
4. **时间加速 API**：使用 `TimeWarp.fetch.WarpTo(double ut)` 实现自动变速到目标 UT。
5. **飞行场景功能**：本次暂不实现，计划文件中保留扩展接口说明即可。

## 5. 验证步骤（Verification Steps）

1. 编译项目，确认 `bin/Release/CNSATimeLine.dll` 生成且无编译错误。
2. 部署到 KSP 的 `GameData/CNSATimeLine/`，并确保同目录包含：
   - `CNSATimeLine.dll`
   - `RSSTimeFormatter.dll`
   - `中国航天大事时间线.txt`
3. 在太空中心按 `Ctrl+L`，确认窗口弹出并正确列出所有事件。
4. 点击某事件“加速到此时”，确认全局时间加速到该事件前 10 小时并自动停止。
5. 当某事件当前 UT 已晚于 `eventUT - 36000` 时，确认对应按钮灰色。
6. 关闭 RSSTimeFormatter 后测试，确认使用 fallback epoch 1951-01-01 仍可计算并运行。

## 6. 10 条典型输入与预期结果

| 编号 | 输入/场景 | 预期结果 |
|------|-----------|----------|
| 1 | 太空中心按 `Ctrl+L` | 弹出事件列表窗口 |
| 2 | 列表中事件“1970-04-24 21:35:44 东方红一号发射”，当前 UT 早于该时间前 10h | 按钮可用，点击后 WarpTo 到 `eventUT - 36000` |
| 3 | 列表中某事件当前 UT 已晚于 `eventUT - 36000` | “加速到此时”按钮灰色 |
| 4 | 事件行秒位为 `00`（精确到日） | 正常解析为 `00` 秒，不影响 WarpTo 目标计算 |
| 5 | `中国航天大事时间线.txt` 存在但含空行 | 空行被忽略，其余事件正常显示 |
| 6 | `中国航天大事时间线.txt` 缺失 | 记录错误日志，窗口显示空列表或提示 |
| 7 | RSSTimeFormatter 已安装且 epoch 自定义为 1960-01-01 | 读取自定义 epoch，事件 UT 映射正确 |
| 8 | RSSTimeFormatter 未安装 | 使用 fallback epoch 1951-01-01，仍能计算并运行 |
| 9 | 窗口打开时按 `Ctrl+L` | 窗口关闭 |
| 10 | 窗口关闭时按 `Ctrl+L` | 窗口打开并恢复上次滚动位置 |
