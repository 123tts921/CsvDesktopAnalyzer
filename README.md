# CsvDesktopAnalyzer

> **当前主版本（新）**：基于 `Electron + Vue 3 + TDesign + ECharts` 的跨平台桌面版，源码在 `src/`、`electron/` 下，功能 100% 对齐旧版。详见下方「Electron 版」章节。
>
> **旧版（遗留）**：`.NET WinForms + ScottPlot` 桌面版，源码在 `CsvDesktopAnalyzer/`，仍可构建使用，但不再作为主版本维护。

一个面向 Windows（及 macOS / Linux）的 CSV 桌面分析工具，用来按时间范围查看、筛选和对比多列数据。

它适合这类场景：
- 选择 1 列、2 列或多列放在同一张图上比较
- 按时间范围查看趋势
- 在大量时序数据里做抽样显示
- 把工具直接发给别人使用，不要求对方安装开发环境

## 当前版本特点（Electron 版）

- 跨平台桌面端，基于 `Electron + Vue 3 + TDesign + ECharts`
- CSV 多编码解析（UTF-8 / GB18030 自动探测 + BOM 处理）
- 时间列自动识别（精确匹配 `时间` → 模糊匹配 `时间/日期/time/date`）
- 数值列采样识别（前 1500 行打分，逗号数字自动去除千分位）
- 多列同图对比 + `Y1 / Y2` 双轴
- 每条曲线独立图表类型（折线图 / 点线图 / 散点图）
- 两种取点方式：固定点数 / 固定时间间隔
- 时间范围筛选 + `dataZoom` 缩放与区间刷选
- 导出 PNG 图表（跨平台写文件）
- Windows 安装包 + 便携单文件分发
- 完整测试覆盖：单元测试 + Worker 集成测试 + Playwright UI 冒烟 + Python/JS 解析一致性对照

## 适合的数据

- 设备运行日志
- 传感器时序数据
- 温度、压力、流量、功率等过程数据
- 电能、产量、能耗等趋势对比数据

## 使用方式

1. 打开程序
2. 选择 CSV 文件
3. 选择时间列
4. 勾选需要比较的数据列
5. 为每条曲线选择 `Y1 / Y2` 和图表类型
6. 设置时间范围
7. 选择显示模式并绘制图表

## 发布文件

GitHub Release 中提供两类文件：

- `CsvDesktopAnalyzerSetup.exe`
  - 安装版
  - 适合直接给普通用户安装使用

- `CsvDesktopAnalyzer.exe`
  - 便携版单文件程序
  - 适合直接双击运行

## 项目结构

- `src/`
  - Electron 版前端源码（Vue 3 + TDesign + ECharts）
  - `stores/analyzer.js`：Pinia 状态（单一事实来源）
  - `lib/csvParse.js`：编码探测 / 时间列识别 / 数值列采样（对齐旧版算法）
  - `lib/sampling.js`：固定点数 / 固定间隔降采样
  - `lib/chartOption.js`：ECharts 多轴 option 构造
  - `workers/csv.worker.js`：CSV 解析 Web Worker
  - `components/`：Header / Filter / TimeRange / ColumnTable / ChartView / StatusBar
- `electron/`
  - 主进程（`main.js`）与预加载（`preload.js`），仅暴露 `api.openCsv / readBuffer / savePng / writeBuffer`
- `CsvDesktopAnalyzer/`
  - 旧版 WinForms 主程序源码（遗留）
- `installer/`
  - 旧版安装器源码
- `REFACTOR_SPEC.md`
  - 重构规格书（行为基线 + 模块契约 + 验收标准）

早期还保留了一些原型文件：

- `csv_chart_viewer.html`
- `csv_chart_viewer.py`
- `desktop_csv_analyzer.py`

## Electron 版（新）

### 技术栈
Electron + Vite + Vue 3 + Pinia + TDesign Vue Next + ECharts + PapaParse。

### 本地开发
```bash
npm install
npm run dev          # 同时启动 Vite 开发服务器与 Electron 窗口
```

### 生成桌面程序
```bash
npm run build:web    # 构建前端到 dist/
npm run dist:win     # 生成 Windows 安装包 + 便携单文件（需在 Windows 上执行）
# 或仅便携版：
npm run pack:portable
```
输出位于 `publish/`：
- `CsvDesktopAnalyzer Setup.exe`（安装版）
- `CsvDesktopAnalyzer.exe`（便携版单文件）

> 注：在 Windows 上构建 Windows 安装包最稳妥；在非 Windows 平台构建 `nsis` 目标需要额外环境，便携目标可跨平台生成。

### 功能对照（与旧版一致）
CSV 选择 / 多编码解析 / 时间列自动识别 / 数值列采样识别 / 多列同图 / Y1·Y2 轴 / 每曲线图表类型（折线·点线·散点）/ 时间范围筛选 / 固定点数·固定间隔取点 / 导出 PNG / 列搜索 / 全选清空 / 重置刷新 / 状态栏；并新增 `dataZoom` 缩放与区间刷选、跨平台支持。

## 测试

```bash
npm install                 # 安装依赖（若已删除 node_modules 可重新安装）
npm test                    # 运行 Vitest 单元测试（csvParse / sampling / chartOption / csv.worker）
npm run test:e2e            # 运行 Playwright UI 冒烟测试（自动构建并托管 web 产物）
```

- `tests/csvParse.test.js`、`tests/sampling.test.js`、`tests/chartOption.test.js`：纯函数与 ECharts option 构造单测
- `tests/csv.worker.test.js`：在 Node 下模拟 Worker 运行时，验证 `onmessage → postMessage` 消息契约与 `parseCsv` 端到端
- `tests/e2e/smoke.spec.js`：真实 Chromium 中加载构建产物，覆盖 加载 → 列识别 → 全选 → 绘制 → 画布渲染 → 导出图片 全链路（含"至少 1 列"校验）
- `tests/consistency/compare.py` + `js_parse.mjs`：原始 PySide 解析逻辑 vs 新 JS 实现的一致性对照
- `tests/consistency/inspect_yaxis.mjs <csv>`：列出各数值列 min/max，定位 y 轴被拉高的来源列

> 注：Playwright 首次运行需 `npx playwright install chromium`；e2e 在 web 模式通过 mock `window.api` 验证 UI 渲染链路，`node_modules` 删除后执行 `npm install` 即可恢复。

## 旧版（WinForms）本地开发

```powershell
dotnet restore .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj --configfile .\NuGet.Config
dotnet build .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj --configfile .\NuGet.Config
dotnet run --project .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj --configfile .\NuGet.Config
```

## 生成单文件程序

```powershell
dotnet publish .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish\CsvDesktopAnalyzerSingleFileSelfContained `
  --configfile .\NuGet.Config
```

输出文件：

- `publish/CsvDesktopAnalyzerSingleFileSelfContained/CsvDesktopAnalyzer.exe`

## 生成安装包

```powershell
.\build_installer.ps1
```

输出文件：

- `publish/CsvDesktopAnalyzerSetup.exe`

## 运行环境

- Windows 10 / 11 x64
- 开发构建需要 `.NET SDK 9`

最终用户使用安装版或单文件版时，不需要单独安装 .NET。

## License

MIT
