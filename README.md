# CsvDesktopAnalyzer

一个面向 Windows 的 CSV 桌面分析工具，用来按时间范围查看、筛选和对比多列数据。

它适合这类场景：
- 选择 1 列、2 列或多列放在同一张图上比较
- 按时间范围查看趋势
- 在大量时序数据里做抽样显示
- 把工具直接发给别人使用，不要求对方安装开发环境

## 当前版本特点

- 桌面端，基于 `.NET WinForms + ScottPlot`
- 支持 CSV 时间列识别
- 支持多列同图对比
- 支持 `Y1 / Y2` 轴分配
- 支持每条曲线独立选择图表类型
  - 折线图
  - 点线图
  - 散点图
- 支持两种取点方式
  - 固定点数
  - 固定时间间隔
- 支持导出 PNG 图表
- 支持单文件 EXE 和安装包分发

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

- `CsvDesktopAnalyzer/`
  - 主程序源码
- `installer/`
  - 安装器源码
- `build_installer.ps1`
  - 生成安装包脚本
- `publish/`
  - 发布产物目录
- `build_temp/`
  - 过程版本和临时构建输出

早期还保留了一些原型文件：

- `csv_chart_viewer.html`
- `csv_chart_viewer.py`
- `desktop_csv_analyzer.py`

当前以 `CsvDesktopAnalyzer/` 下的 WinForms 桌面版为准。

## 本地开发

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
