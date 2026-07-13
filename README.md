# CsvDesktopAnalyzer

Windows 桌面端 CSV 时序数据分析工具。

适合这类场景：

- 在一张图上比较 1 列、2 列或多列数据
- 按时间范围查看趋势
- 为不同量纲数据分配 `Y1 / Y2`
- 在大体量 CSV 上做抽样显示
- 直接发一个 `exe` 给别人使用

## 当前发布版

当前可发布单文件：

- `publish/CsvDesktopAnalyzerSingleFileSelfContained/CsvDesktopAnalyzer.exe`

特点：

- 单文件 `exe`
- Windows 10 / 11 x64 可直接运行
- 已内置软件图标

## 主要功能

- 选择 CSV 文件
- 自动识别时间列与数值列
- 多列同图比较
- 每条曲线独立选择图表类型
  - 折线图
  - 点线图
  - 散点图
- `Y1 / Y2` 轴分配
- 时间范围筛选
- 两种显示模式
  - 固定点数
  - 固定时间间隔
- 导出 PNG 图表

## 技术栈

- `.NET 9`
- `WinForms`
- `ScottPlot 5`

## 源码目录

- `CsvDesktopAnalyzer/`
  - 主程序源码
- `CsvDesktopAnalyzer/assets/`
  - 软件图标资源

## 本地构建

```powershell
dotnet build .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj -c Release
```

## 生成单文件发布版

```powershell
dotnet publish .\CsvDesktopAnalyzer\CsvDesktopAnalyzer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=false `
  -o .\publish\CsvDesktopAnalyzerSingleFileSelfContained
```

## 运行环境

- Windows 10 / 11 x64
- 开发构建需要 `.NET SDK 9`

## License

MIT
