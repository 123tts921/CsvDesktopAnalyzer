# CsvDesktopAnalyzer

### 面向大体量 CSV 的 Windows 桌面分析工具

用于按时间序列查看、筛选和对比 CSV 数据列，适合把 1 列、2 列或多列放到同一张图上做趋势分析。

## 这个项目解决什么问题

很多 CSV 查看工具在数据量大、列数多、时间跨度长时会出现几个常见问题：

- 浏览器版页面容易卡顿
- 图表和左侧字段列表容易挤在一起
- 时间范围筛选不方便
- 多列对比时不直观
- 发给别人使用时，还需要额外装环境

`CsvDesktopAnalyzer` 的目标很直接：

- 做成可直接双击运行的 Windows 桌面程序
- 支持大体量 CSV 的时间序列可视化
- 支持按列对比、按时间范围查看、按规则抽样
- 支持生成单文件程序和安装包，方便分发给别人

## 快速使用

Windows 用户优先下载 Release 中的安装包：

- `CsvDesktopAnalyzerSetup.exe`

安装完成后即可直接使用。

如果你不想安装，也可以使用便携版：

- `CsvDesktopAnalyzer.exe`

使用流程：

1. 打开程序
2. 选择 CSV 文件
3. 选择时间列和要对比的数值列
4. 选择图表类型
5. 设置时间范围或抽样方式
6. 刷新图表查看结果

## 主要功能

- 选择 1 列、2 列或多列放在同一张图上比较
- 支持时间范围筛选
- 支持图表类型切换
  - 折线图
  - 点线图
  - 散点图
- 支持两种取点方式
  - 最大点数抽样
  - 固定时间间隔
- 支持双 Y 轴
- 支持左侧变量搜索
- 支持刷新图表、重置视图、导出 PNG 图片
- 支持单文件发布
- 支持安装包发布

## 适用场景

- 设备运行日志分析
- 传感器时间序列对比
- 能耗、流量、温度等指标趋势查看
- 多字段随时间变化的联动观察
- 给非开发人员分发可直接使用的数据分析工具

## 项目结构

- `CsvDesktopAnalyzer/`
  - 主程序源码
  - 基于 `.NET WinForms + ScottPlot`
- `installer/`
  - 安装器源码
- `build_installer.ps1`
  - 构建安装包脚本
- `publish/`
  - 发布产物目录
- `csv_chart_viewer.py`
  - 早期 Python 原型
- `csv_chart_viewer.html`
  - 早期 HTML 原型
- `desktop_csv_analyzer.py`
  - 早期桌面试验程序

当前以 `CsvDesktopAnalyzer/` 下的 WinForms 桌面版为准。

## 发布文件

当前仓库主要提供两类可分发文件：

- 安装版：`CsvDesktopAnalyzerSetup.exe`
- 便携版：`CsvDesktopAnalyzer.exe`

区别：

- 安装版会安装到 `C:\Program Files\CsvDesktopAnalyzer`
- 安装版会创建桌面和开始菜单快捷方式
- 便携版无需安装，适合直接发文件使用

## 本地开发

```powershell
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

生成结果：

- `publish/CsvDesktopAnalyzerSingleFileSelfContained/CsvDesktopAnalyzer.exe`

## 生成安装包

```powershell
.\build_installer.ps1
```

生成结果：

- `publish/CsvDesktopAnalyzerSetup.exe`

## 运行环境

- Windows 10 / 11 x64
- `.NET SDK 9` 用于开发和构建

对最终使用者来说：

- 使用安装包或单文件版时，不需要单独安装 .NET 运行时

## 当前边界

这个项目当前重点是桌面端 CSV 时序分析，不是通用 BI 平台，因此边界比较明确：

- 不做数据库接入
- 不做多人协作
- 不做 Web 部署
- 不做复杂报表系统
- 目前主要面向 Windows

## 后续可继续增强的方向

- 自定义安装目录
- 应用图标与版本信息
- 更细的采样策略
- 多图联动
- 字段分组管理
- 主题切换
- 最近打开文件记录

## License

MIT
