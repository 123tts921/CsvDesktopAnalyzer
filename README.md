# CsvDesktopAnalyzer

Windows 桌面版 CSV 数据分析工具。

当前主程序位于 `CsvDesktopAnalyzer/`，使用 .NET WinForms + ScottPlot 开发，适合对大体量 CSV 做时间序列对比分析。

## 功能

- 选择 1 列、2 列或多列在同一张图上比较
- 时间范围筛选
- 图表类型切换
  - 折线图
  - 点线图
  - 散点图
- 两种取点方式
  - 最大点数抽样
  - 固定时间间隔
- 双 Y 轴
- 搜索变量列
- 刷新图表 / 重置视图 / 导出图片

## 项目结构

- `CsvDesktopAnalyzer/`: 当前桌面版源码
- `csv_chart_viewer.py`: 早期 Python 原型
- `csv_chart_viewer.html`: 早期 HTML 原型
- `desktop_csv_analyzer.py`: 早期桌面试验稿

当前应以 `CsvDesktopAnalyzer/` 为准。

## 本地运行

```powershell
dotnet build .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj --configfile .\NuGet.Config
dotnet run --project .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj --configfile .\NuGet.Config
```

## 单文件发布

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

## 说明

- 建议运行环境：Windows 10/11 x64
- 发布后的单文件版本体积较大，属于正常现象，因为运行时被一并打包进去了
