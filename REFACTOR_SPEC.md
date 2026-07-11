# CsvDesktopAnalyzer 重构规格书（Spec）

> 目的：将现有 `.NET WinForms + ScottPlot` 桌面工具，重构为 **Electron + Vue 3 + TDesign + ECharts** 桌面应用。
> 本文档是实现的唯一事实来源（single source of truth）。后续任何编码、评审、验收都必须以此为准，避免上下文丢失导致的实现错位。
> 适用读者：实现工程师、代码评审、自动化 Agent。

---

## 0. 文档约定

- **现状基线**：指当前 `CsvDesktopAnalyzer/` 下 WinForms 版本的真实行为，来自 `Form1.cs` / `Form1.Designer.cs` / `Program.cs`。
- **目标实现**：指本次重构产出的 Electron 应用。
- **行为对齐**：目标实现必须 100% 复现"现状基线"中列出的可观测行为；标记 🆕 的为允许增强项，不破坏原有行为。
- **禁止范围蔓延**：未在本 spec 列出、且未标记 🆕 的功能，一律不实现。

---

## 1. 目标与范围

### 1.1 目标
- 用 Web 技术栈重建 GUI，获得跨平台（Win/Mac/Linux）、组件化、响应式、易维护的能力。
- 功能 100% 对齐现有 WinForms 版本（见第 8 节功能等价矩阵）。
- 维持"免环境、双击即用"的分发体验（安装包 + 便携单文件 exe）。

### 1.2 不在范围内（明确边界）
- 不改动 CSV 的解析语义、时间/数值识别规则、采样算法（必须逐字节对齐，见第 5、6 节）。
- 不新增数据导出格式（仅保留 PNG 导出）。
- 不接入网络、不收集数据、不做账号体系。
- 不实现 Y3/Y4 轴（现状仅 Y1/Y2，见 5.4）。
- 不做实时/增量数据接入。

---

## 2. 目标架构

```
┌─────────────────────────────────────────────┐
│  Electron 主进程 (Node.js)                    │
│  - BrowserWindow 管理                         │
│  - 文件对话框 (showOpenDialog/SaveDialog)     │
│  - 文件读取 (fs.readFile → Buffer)            │
│  - IPC 处理 (ipcMain.handle)                  │
└───────────────┬─────────────────────────────┘
                │ contextBridge (仅暴露最小 API)
┌───────────────▼─────────────────────────────┐
│  Renderer (Vue 3 SPA)                         │
│  - Pinia 状态                                │
│  - TDesign Vue Next 组件                      │
│  - ECharts 图表                              │
│  - Web Worker: CSV 解析 + 采样 (PapaParse)    │
└─────────────────────────────────────────────┘
```

安全约束（强制）：
- `contextIsolation: true`，`sandbox: true`，`nodeIntegration: false`。
- 仅通过 `contextBridge.exposeInMainWorld` 暴露第 4.2 节列出的 API。
- 主进程不做任意命令执行、不暴露 `fs` 全量能力，只读指定路径。

---

## 3. 技术栈与依赖

| 类别 | 选型 | 说明 |
|---|---|---|
| 壳 | `electron` (^31) | 主进程 Node.js，文件读写方便 |
| 构建 | `vite` (^5) + `electron-builder` (^24) | 前端打包 + 桌面分发 |
| 框架 | `vue` (^3.4) | 组合式 API |
| 状态 | `pinia` (^2) | 单一状态源 |
| UI | `tdesign-vue-next` (^1.9) | TDesign Vue 3 版 |
| 图表 | `echarts` (^5.5) | 多 Y 轴 / dataZoom / 导出 |
| CSV | `papaparse` (^5.4) | 流式 + worker 解析 |
| 编码探测 | `jschardet` (^1.6) | 检测 GBK/GB18030/UTF8 |

> 注：PapaParse 自带 `worker` 选项；但本项目统一用"主进程读 Buffer → Renderer Web Worker 解析"模式，便于编码探测与内存控制。

---

## 4. 进程间契约（IPC）

### 4.1 主进程暴露的 IPC 通道

| 通道 | 方向 | 入参 | 返回 | 说明 |
|---|---|---|---|---|
| `dialog:open-csv` | renderer→main | 无 | `string \| null` | 打开 CSV 选择对话框，返回绝对路径 |
| `fs:read-buffer` | renderer→main | `path: string` | `ArrayBuffer` | 读取文件为 Buffer（限制常规文件；超 500MB 抛错） |
| `dialog:save-png` | renderer→main | 建议文件名 | `string \| null` | 打开保存对话框，返回路径 |
| `fs:write-buffer` | renderer→main | `path: string, buf: ArrayBuffer` | `boolean` | 将 PNG 字节写入用户选定路径（沙箱下渲染进程无 fs，导出必经此通道） |

### 4.2 预加载暴露的 `window.api`

```js
// electron/preload.js —— 仅暴露以下 4 个方法，禁止扩展
contextBridge.exposeInMainWorld('api', {
  openCsv: () => ipcRenderer.invoke('dialog:open-csv'),
  readBuffer: (path) => ipcRenderer.invoke('fs:read-buffer', path),
  savePng: (defaultName) => ipcRenderer.invoke('dialog:save-png', defaultName),
  writeBuffer: (path, buf) => ipcRenderer.invoke('fs:write-buffer', path, buf),
});
```

> 实现约束：Renderer 中所有文件操作只能经由 `window.api`，不得直接访问 Node。

---

## 5. 现状行为基线（必须对齐）

> 以下内容从 `Form1.cs` 精确提取，目标实现必须复现。

### 5.1 时间格式（11 种，按顺序尝试）
```
yyyy/M/d H:mm:ss
yyyy/M/d H:mm
yyyy/MM/dd HH:mm:ss
yyyy/MM/dd HH:mm
yyyy-M-d H:mm:ss
yyyy-M-d H:mm
yyyy-MM-dd HH:mm:ss
yyyy-MM-dd HH:mm
yyyy/M/d
yyyy-MM-dd
yyyy-M-d
```
解析顺序：先 `TryParseExact(11种)` → 失败再 `DateTime.TryParse` → 再 `DateTime.TryParse`（本地文化）。

### 5.2 编码与分隔符
- 编码探测顺序：`UTF8(BOM)` → `UTF8(无BOM)` → `GB18030`；均失败回退 UTF8。
- 分隔符集合：`,` `\t` `;`（同时支持）。
- 引号包裹：`HasFieldsEnclosedInQuotes = true`，`TrimWhiteSpace = false`。

### 5.3 时间列识别 `ResolveTimeColumn`
1. 若请求的列名在表头中且非空 → 用它。
2. 否则精确匹配表头 == `"时间"`。
3. 否则模糊匹配（不区分大小写）包含 `时间` / `日期` / `time` / `date` 的第一个。
4. 否则取 `headers[0]`。

### 5.4 数值列识别（采样打分）
- 最多扫描前 **1500** 行（含时间列本身排除）。
- 对每列统计"可解析为 double"的行数（逗号去除后按 `invariant` 浮点解析）。
- 仅保留得分 `> 0` 的列作为"可绘图数值列"。
- **轴数量固定为 2**：`Y1`（左轴）、`Y2`（右轴）。现状不支持更多轴。

### 5.5 默认选项（必须与现状一致）
| 控件 | 选项 | 默认值 |
|---|---|---|
| 默认图表类型 | 折线图 / 点线图 / 散点图 | 折线图 |
| 每条曲线最多点数 | 3000 / 6000 / 10000 / 20000 | 6000 |
| 显示模式 | 固定点数 / 固定时间间隔 | 固定点数 |
| 固定时间间隔 | 1分钟 / 5分钟 / 10分钟 / 30分钟 / 1小时 | 10分钟 |

### 5.6 采样算法（精确对齐）

**固定点数 `DownSample(points, limit)`**
- `points` 为按时间升序的 `(timestamp, value)` 列表。
- 若 `count <= limit` 或 `limit <= 0`：原样返回。
- 否则按 `scale = (count-1)/(limit-1)` 等比取索引 `round(i*scale)`。
- 跳过与上一索引相同的点（去重）。
- 保证末尾点一定存在（若最后一个点未被取到，则替换/追加末点）。
- X 以 OA 日期（`ToOADate`）表示，目标实现改为 **UTC 毫秒时间戳** 亦可，但绘图坐标须一致。

**固定时间间隔 `ResampleByInterval(points, interval)`**
- 按 `bucket = timestamp.Ticks / interval.Ticks` 分桶。
- 每个桶只保留**第一个**点。
- `interval <= 0` 时原样返回。
- 间隔映射：1分钟=60s，5分钟=300s，10分钟=600s，30分钟=1800s，1小时=3600s。

### 5.7 绘图行为（对齐 `DrawPlot`）
- 标题：`"{start:yyyy-MM-dd HH:mm} 至 {end:yyyy-MM-dd HH:mm}"`。
- Legend 位置：左上（`Alignment.UpperLeft`）。
- 坐标轴：Y1 用左轴；分配为 Y2 的曲线用右轴（`AddRightAxis`）。
- 曲线颜色：按 `CurveColor(index)` 调色板循环（见 5.8）。
- 类型映射：
  - 折线图：`LineWidth=2, MarkerSize=0`
  - 点线图：`LineWidth=2, MarkerSize=5`
  - 散点图：`LineWidth=0, MarkerSize=5`
- 绘制后 `AutoScale`。
- 无数据（Xs 长度为 0）的曲线不绘制。

### 5.8 曲线调色板（必须一致）
```
#2F80ED #20A39E #D48A1F #C95D73 #6D73D9
#0EA5E9 #8B5CF6 #4B5563 #65A30D #C2410C
```
按曲线索引 `index % 10` 取色。

### 5.9 校验与联动规则
- 必须至少选择 1 个指标列，否则弹提示"请至少选择一个指标列。"
- `start >= end`：弹提示"开始时间必须早于结束时间。"
- 切换"时间列"下拉 → 重新加载整个文件。
- 切换"显示模式" → `固定点数`时启用"最多点数"、禁用"固定间隔"；`固定时间间隔`时反之。
- 加载成功后：时间范围 picker 的 min/max 设为数据 min/max；清空选择、禁用/启用相关控件。

### 5.10 导出 PNG
- 保存对话框过滤器 `PNG 图片 (*.png)|*.png`。
- 默认文件名：`chart_{yyyyMMdd_HHmmss}.png`。
- 导出尺寸：取 `max(图表宽,1200) × max(图表高,700)`（目标用 ECharts `getDataURL({pixelRatio})` 等效实现）。

### 5.11 状态栏文案格式
- 加载后：`"{文件名} | 共 {行数:N0} 行 | {数值列数} 个可绘图列"`
- 绘图后：`"{文件名} | {采样描述} | 当前最多 {点数} 点/列 | {曲线数} 条曲线"`
  - 采样描述：`固定点数 6000` 或 `固定时间间隔 10分钟`

---

## 6. 数据模型（Pinia store 精确字段）

```ts
// src/stores/analyzer.ts
interface ColumnMeta { name: string; numeric: boolean }
interface Selection  { name: string; axis: 'Y1'|'Y2'; chartType: '折线图'|'点线图'|'散点图' }

state:
  filePath: string | null
  headers: string[]
  timeColumn: string | null
  numericColumns: string[]          // 仅数值列名
  loaded: boolean
  // 时间范围（毫秒时间戳）
  timeRange: [number, number] | null
  dataMinTime: number | null
  dataMaxTime: number | null
  // 筛选与配置
  searchText: string
  defaultChartType: '折线图'|'点线图'|'散点图'   // 默认 折线图
  displayMode: '固定点数'|'固定时间间隔'         // 默认 固定点数
  maxPoints: number                               // 默认 6000
  interval: '1分钟'|'5分钟'|'10分钟'|'30分钟'|'1小时' // 默认 10分钟
  selections: Selection[]
  // 运行状态
  status: string
  plotSummary: string

getters:
  visibleColumns: 按 searchText(不区分大小写 contains) 过滤 numericColumns
  selectedSpecs: selections 中 axis/chartType 映射到当前行

actions:
  loadFile(path)
  setTimeColumn(name)      // 触发 reload
  setDisplayMode(mode)     // 切换启用状态
  draw()
  resetView()
  exportPng()
```

> 实现约束：所有"当前选择状态"集中存储，不再依赖表格行内隐式状态（对齐现状但更健壮）。

**实现注记（数据模型实际形态）**：目标实现以 `config: Record<string, { selected: boolean; axis: 'Y1'|'Y2'; chartType: '折线图'|'点线图'|'散点图' }>`（按列名索引）作为选择状态的唯一来源，等价替代 spec 草拟的 `selections: Selection[]` 数组形式；`selectedSpecs` getter 由 `config` 派生出 `Selection[]` 供绘图使用。二者语义一致，后续以 `config` 为准。

---

## 7. 模块规格

### 7.1 Electron 主进程 `electron/main.js`
- 创建 `BrowserWindow`（初始 1464×921，最小 1440×920，居中），`contextIsolation/sandbox` 开启。
- 注册第 4.1 节 3 个 IPC 通道。
- 文件读取需校验为普通文件、大小上限建议 500MB（超出给提示，不崩溃）。

### 7.2 CSV 解析管线 `src/workers/csv.worker.js` + `src/lib/csvParse.js`
职责（对齐 5.2–5.4）：
1. 接收 `ArrayBuffer` → `jschardet` 探测编码 → `TextDecoder` 解码。
2. PapaParse 解析（`header:true, skipEmptyLines:true`）。
3. 还原表头数组（trim）。
4. `resolveTimeColumn(headers, requested)`（算法见 5.3）。
5. 采样打分识别数值列（前 1500 行，算法见 5.4）。
6. 解析全部行：输出 `{ timestamps: number[], valuesByColumn: Record<string, (number|null)[]> }`，时间戳统一为 **毫秒**。
7. postMessage 回主线程写入 Pinia（不跨 worker 保留大对象引用）。

### 7.3 采样模块 `src/lib/sampling.js`
- `downSample(points, limit)`：严格按 5.6 算法。
- `resampleByInterval(points, ms)`：严格按 5.6 算法。
- `buildSeries(cache, request)`：按时间范围过滤 + 调用上述两函数，输出 `Record<name, {xs:number[], ys:number[]}>`（对齐 `BuildSeriesFromCache`）。

### 7.4 图表模块 `src/lib/chartOption.js` + `src/components/ChartView.vue`
- `buildOption(selections, seriesData, {start,end})`：
  - `yAxis`: `[{name:'Y1'}]` 加（若有 Y2）`[{name:'Y2', position:'right'}]`。
  - `series`: 每条按 5.7/5.8 映射类型与颜色；`yAxisIndex` = Y2?1:0。
  - `xAxis`: `type:'time'`, `min/max` = 时间范围。
  - `tooltip.trigger='axis'`，`legend` 左上。
  - 🆕 `dataZoom:[{type:'inside'},{type:'slider'}]` 提供缩放/区间刷选（增强，不破坏原行为）。
  - 标题格式见 5.7。
- 导出：`chart.getDataURL({ type:'png', pixelRatio(≥2, 保证≥1200×700), backgroundColor:'#fff' })` → Blob → `window.api.savePng` 取路径 → `window.api.writeBuffer` 写入（对齐 5.10）。

### 7.5 UI 组件规格

| 组件 | 职责 | 主要 TDesign 组件 | 状态来源 |
|---|---|---|---|
| `App.vue` | 三段式布局（顶栏 / 分栏 / 底栏） | `t-layout` | — |
| `HeaderBar.vue` | 标题 + 文件路径输入 + 选择文件/加载 | `t-input` `t-button` | `filePath`, `loadFile` |
| `FilterPanel.vue` | 时间列 / 默认图表类型 / 最多点数 / 显示模式 / 固定间隔 | `t-select` | 对应 store 字段 |
| `TimeRangePanel.vue` | 开始/结束时间范围 | `t-date-range-picker` | `timeRange` |
| `ColumnTable.vue` | 列搜索 + 列勾选表（选择/数据列/Y轴/图表类型） | `t-input` `t-table` | `visibleColumns`, `selections` |
| `ChartView.vue` | ECharts 容器 + 工具栏（刷新/重置/导出） | `t-button` `t-space` | `draw/resetView/exportPng` |
| `StatusBar.vue` | 底栏状态文案 | `t-tag` 文本 | `status`,`plotSummary` |

布局约束（对齐现状观感）：
- 顶栏高度约 88px，含标题 + 文件路径 + 选择文件/加载按钮（右对齐）。
- 主体左右分栏：左 ~460px（筛选+列表），右自适应（图表）。
- 底栏状态条高度约 28px。

### 7.6 行为规则落地（对齐 5.9）
- 校验、联动、默认值全部由 Pinia actions / computed 控制，组件只负责展示与派发。
- 加载成功后的控件启用/禁用、时间范围 min/max 绑定，严格按 5.9。

---

## 8. 功能等价矩阵

| # | 功能 | 现状实现 | 目标实现 | 状态 |
|---|---|---|---|---|
| 1 | 选择/加载 CSV | OpenFileDialog | `api.openCsv` + `api.readBuffer` | ✅对齐 |
| 2 | 多编码/分隔符/引号 | TextFieldParser+编码探测 | jschardet+PapaParse | ✅对齐 |
| 3 | 时间列识别 | ResolveTimeColumn | 同算法移植 | ✅对齐 |
| 4 | 数值列采样识别 | 1500行打分 | 同算法移植 | ✅对齐 |
| 5 | 全量内存缓存 | CachedCsvData | store + worker 回传 | ✅对齐 |
| 6 | 多列同图 | ScottPlot 多 scatter | ECharts 多 series | ✅对齐 |
| 7 | Y1/Y2 轴 | Left/RightAxis | yAxis[0]/[1] | ✅对齐 |
| 8 | 每曲线类型 | LineWidth/MarkerSize | line+symbol | ✅对齐 |
| 9 | 时间范围筛选 | DateTimePicker | t-date-range-picker | ✅对齐 |
| 10 | 固定点数 | DownSample | sampling.downSample | ✅对齐 |
| 11 | 固定间隔 | ResampleByInterval | sampling.resampleByInterval | ✅对齐 |
| 12 | 导出 PNG | SavePng | getDataURL+savePng | ✅对齐 |
| 13 | 列搜索 | TextBox 过滤 | t-input 过滤 | ✅对齐 |
| 14 | 全选/清空 | 遍历行 | t-table 选择 | ✅对齐 |
| 15 | 重置/刷新 | AutoScale/重绘 | dispatchAction/setOption | ✅对齐 |
| 16 | 状态栏 | StatusStrip | StatusBar 组件 | ✅对齐 |
| 17 | 🆕 缩放/区间刷选 | 无（仅按钮重绘） | dataZoom inside+slider | 增强 |
| 18 | 🆕 跨平台 | Windows only | win/mac/linux | 增强 |

---

## 9. 构建与分发

`electron-builder.yml`：
```yaml
appId: com.csvanalyzer.app
productName: CsvDesktopAnalyzer
files:
  - dist/**/*
  - electron/**/*
win:
  target: [nsis, portable]
  artifactName: CsvDesktopAnalyzer-${name}.${ext}
nsis:
  oneClick: false
  perMachine: false
  allowToChangeInstallationDirectory: true
```

脚本（package.json）：
```json
{
  "scripts": {
    "dev": "vite",
    "build:web": "vite build",
    "build": "npm run build:web && electron-builder",
    "pack:portable": "npm run build:web && electron-builder --win portable"
  }
}
```
产出对齐现状 README：
- `CsvDesktopAnalyzer Setup.exe`（安装版）
- `CsvDesktopAnalyzer.exe`（便携单文件）

---

## 10. 迁移阶段（建议提交节奏）

1. **P0 脚手架**：Vite+Vue+TDesign+ECharts+Electron 跑通空壳 + 三段式布局 + 安全配置。
2. **P1 加载链路**：对话框→读取→Worker 解析→时间/数值识别→store（验收：加载同一 CSV，列/时间范围与现状一致）。
3. **P2 筛选与列表**：Y1/Y2、每曲线类型、搜索、全选/清空、联动规则。
4. **P3 绘图**：多轴、类型、降采样、导出 PNG、重置/刷新。
5. **P4 增强**：dataZoom 刷选、状态栏、异常处理（对齐 MessageBox 提示文案）。
6. **P5 打包**：electron-builder win（nsis+portable），更新 README 的"本地开发/生成"章节。
7. **P6 收尾**：原 WinForms 可保留为 fallback 或下架；清理根目录 Python/HTML 原型。

---

## 11. 验收标准（Definition of Done）

- [ ] 用同一份 CSV 加载，识别出的时间列、数值列数量、时间范围 min/max 与现状 WinForms 版**完全一致**。
- [ ] 选 2 列分别分配到 Y1/Y2，绘制后左右轴、颜色、线型与现状一致。
- [ ] 固定点数（6000）与固定间隔（10分钟）两种模式下，点数/曲线行为与现状一致（可抽样比对）。
- [ ] 导出 PNG 文件名格式、默认尺寸与现状一致。
- [ ] 校验提示文案（至少 1 列、开始<结束）与现状一致。
- [ ] 构建产出安装包 + 便携 exe，可在干净 Windows 上双击运行。
- [ ] 无 `nodeIntegration`、无暴露多余 IPC（安全约束通过）。

---

## 12. 风险与对策

| 风险 | 对策 |
|---|---|
| 大 CSV 卡 UI | PapaParse 流式 + Web Worker；降采样逻辑复用现状；时间戳用 TypedArray |
| 浏览器内存上限 | 单文件上限 500MB 提示；只保留数值列与时间戳 |
| 包体大 | electron-builder 压缩；接受比 .NET 大但分发等价 |
| ECharts 大点渲染 | 依赖降采样（现状已覆盖）；dataZoom 仅渲染可视区 |
| 行为漂移 | 以本文档第 5 节为基线，验收标准逐条比对 |

---

## 13. 附录：复用映射（现状 → 目标）

| 现状符号 | 目标位置 |
|---|---|
| `Form1.LoadCsvData` | `csv.worker.js` + `csvParse.js` |
| `ResolveTimeColumn` | `csvParse.js:resolveTimeColumn` |
| `BuildSeriesFromCache` / `DownSample` / `ResampleByInterval` | `sampling.js` |
| `DrawPlot` / `CurveColor` / `ApplySeriesChartType` | `chartOption.js` |
| `GetSelections` / DataGridView 状态 | `stores/analyzer.ts:config`（按列名索引）+ `selectedSpecs` getter |
| `statusLabel` / `_plotSummaryLabel` | `StatusBar.vue` |
| ScottPlot `FormsPlot` | `ChartView.vue` + ECharts |
| WinForms Designer 布局 | `App.vue` + TDesign 组件布局 |
