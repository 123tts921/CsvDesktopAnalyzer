using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using ScottPlot;
using ScottPlot.WinForms;
using DrawingColor = System.Drawing.Color;
using DrawingFontStyle = System.Drawing.FontStyle;

namespace CsvDesktopAnalyzer;

public partial class Form1 : Form
{
    private const string DisplayModeMaxPoints = "最大点数抽样";
    private const string DisplayModeFixedInterval = "固定时间间隔";

    private static readonly string[] TimeFormats =
    {
        "yyyy/M/d H:mm:ss",
        "yyyy/M/d H:mm",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy/MM/dd HH:mm",
        "yyyy-M-d H:mm:ss",
        "yyyy-M-d H:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy/M/d",
        "yyyy-MM-dd",
        "yyyy-M-d"
    };

    private static readonly DrawingColor AppBackColor = DrawingColor.FromArgb(245, 247, 250);
    private static readonly DrawingColor PanelBackColor = DrawingColor.White;
    private static readonly DrawingColor AccentColor = DrawingColor.FromArgb(44, 102, 230);
    private static readonly DrawingColor BorderColor = DrawingColor.FromArgb(220, 224, 230);
    private static readonly DrawingColor TextColor = DrawingColor.FromArgb(40, 44, 52);
    private static readonly DrawingColor MutedTextColor = DrawingColor.FromArgb(108, 117, 125);

    private string? _loadedFilePath;
    private string? _timeColumn;
    private CachedCsvData? _cachedData;
    private bool _refreshing;
    private readonly FormsPlot _formsPlot = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Label _plotSummaryLabel = new();

    public Form1()
    {
        InitializeComponent();
        InitializeRuntimeUi();
    }

    private void InitializeRuntimeUi()
    {
        Text = "CSV 数据分析工具";
        Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, DrawingFontStyle.Regular, GraphicsUnit.Point);
        MinimumSize = new Size(1380, 860);
        StartPosition = FormStartPosition.CenterScreen;

        headerTitleLabel.Text = "CSV 数据分析工具";
        headerHintLabel.Text = "选择文件后即可筛选时间和列";
        fileLabel.Text = "当前文件";
        timeColumnLabel.Text = "时间列";
        chartTypeLabel.Text = "图表类型";
        sampleLimitLabel.Text = "每条曲线最多点数";
        displayModeLabel.Text = "显示模式";
        intervalLabel.Text = "固定时间间隔";
        startLabel.Text = "开始时间";
        endLabel.Text = "结束时间";
        filtersTitleLabel.Text = "图表设置";
        dateTitleLabel.Text = "时间范围";
        searchTitleLabel.Text = "变量筛选";
        plotTitleLabel.Text = "图表视图";
        plotHintLabel.Text = "右侧显示当前筛选后的趋势图";
        searchTextBox.PlaceholderText = "例如：电能、温度、压力";
        browseButton.Text = "选择文件";
        loadButton.Text = "加载";
        selectVisibleButton.Text = "全选当前结果";
        clearSelectionButton.Text = "清空选择";
        drawButton.Text = "绘制图表";
        refreshPlotButton.Text = "刷新图表";
        resetViewButton.Text = "重置视图";
        exportPlotButton.Text = "导出图片";

        filePathTextBox.Text = string.Empty;
        plotHostPanel.Controls.Add(_formsPlot);
        _plotSummaryLabel.AutoSize = true;
        _plotSummaryLabel.Location = new Point(360, 74);
        _plotSummaryLabel.Text = "未加载文件 | 0 条曲线 | 等待绘图";
        rightPanel.Controls.Add(_plotSummaryLabel);
        _plotSummaryLabel.BringToFront();

        chartTypeComboBox.Items.AddRange(new object[] { "折线图", "点线图", "散点图" });
        chartTypeComboBox.SelectedIndex = 0;

        sampleLimitComboBox.Items.AddRange(new object[] { "3000", "6000", "10000", "20000" });
        sampleLimitComboBox.SelectedIndex = 1;

        displayModeComboBox.Items.AddRange(new object[] { DisplayModeMaxPoints, DisplayModeFixedInterval });
        displayModeComboBox.SelectedIndex = 0;

        intervalComboBox.Items.AddRange(new object[] { "1分钟", "5分钟", "10分钟", "30分钟", "1小时" });
        intervalComboBox.SelectedIndex = 2;

        if (columnGrid.Columns["AxisColumn"] is DataGridViewComboBoxColumn axisColumn)
            axisColumn.Items.AddRange("Y1", "Y2");

        columnGrid.AutoGenerateColumns = false;
        columnGrid.AllowUserToAddRows = false;
        columnGrid.AllowUserToDeleteRows = false;
        columnGrid.RowHeadersVisible = false;
        columnGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        columnGrid.MultiSelect = false;

        ApplyTheme();
        ConfigurePlot();
        SetControlState(false);
        statusLabel.Text = "请选择 CSV 文件。";
    }

    private void ApplyTheme()
    {
        BackColor = AppBackColor;
        rootLayout.BackColor = AppBackColor;
        headerPanel.BackColor = PanelBackColor;
        rightPanel.BackColor = AppBackColor;
        plotHostPanel.BackColor = PanelBackColor;
        leftLayout.BackColor = AppBackColor;
        filtersPanel.BackColor = PanelBackColor;
        datePanel.BackColor = PanelBackColor;
        actionPanel.BackColor = AppBackColor;
        statusStrip.BackColor = PanelBackColor;

        headerTitleLabel.ForeColor = TextColor;
        headerTitleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, DrawingFontStyle.Bold, GraphicsUnit.Point);
        headerHintLabel.ForeColor = MutedTextColor;
        headerHintLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, DrawingFontStyle.Regular, GraphicsUnit.Point);

        foreach (Control control in new Control[]
        {
            fileLabel, timeColumnLabel, chartTypeLabel, sampleLimitLabel, displayModeLabel,
            intervalLabel, startLabel, endLabel, filtersTitleLabel, dateTitleLabel, searchTitleLabel, plotTitleLabel
        })
        {
            control.ForeColor = TextColor;
            control.BackColor = DrawingColor.Transparent;
            control.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, DrawingFontStyle.Bold, GraphicsUnit.Point);
        }

        plotHintLabel.ForeColor = MutedTextColor;
        plotHintLabel.BackColor = DrawingColor.Transparent;
        plotHintLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, DrawingFontStyle.Regular, GraphicsUnit.Point);
        _plotSummaryLabel.ForeColor = MutedTextColor;
        _plotSummaryLabel.BackColor = DrawingColor.Transparent;
        _plotSummaryLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, DrawingFontStyle.Regular, GraphicsUnit.Point);

        StylePanel(headerPanel);
        StylePanel(filtersPanel);
        StylePanel(datePanel);
        StylePanel(plotHostPanel);
        filtersPanel.BorderStyle = BorderStyle.FixedSingle;
        datePanel.BorderStyle = BorderStyle.FixedSingle;
        plotHostPanel.BorderStyle = BorderStyle.FixedSingle;

        StyleButton(browseButton, false);
        StyleButton(loadButton, true);
        StyleButton(selectVisibleButton, false);
        StyleButton(clearSelectionButton, false);
        StyleButton(drawButton, true);
        StyleButton(refreshPlotButton, false);
        StyleButton(resetViewButton, false);
        StyleButton(exportPlotButton, false);
        plotToolbar.BackColor = DrawingColor.Transparent;

        foreach (Control control in new Control[]
        {
            filePathTextBox, searchTextBox, timeColumnComboBox, chartTypeComboBox,
            sampleLimitComboBox, displayModeComboBox, intervalComboBox
        })
        {
            control.ForeColor = TextColor;
            control.BackColor = DrawingColor.White;
        }

        columnGrid.BackgroundColor = DrawingColor.White;
        columnGrid.BorderStyle = BorderStyle.None;
        columnGrid.GridColor = BorderColor;
        columnGrid.EnableHeadersVisualStyles = false;
        columnGrid.ColumnHeadersDefaultCellStyle.BackColor = DrawingColor.FromArgb(236, 240, 248);
        columnGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
        columnGrid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font(Font, DrawingFontStyle.Bold);
        columnGrid.ColumnHeadersHeight = 36;
        columnGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        columnGrid.DefaultCellStyle.SelectionBackColor = DrawingColor.FromArgb(225, 234, 252);
        columnGrid.DefaultCellStyle.SelectionForeColor = TextColor;
        columnGrid.DefaultCellStyle.BackColor = DrawingColor.White;
        columnGrid.AlternatingRowsDefaultCellStyle.BackColor = DrawingColor.FromArgb(248, 250, 252);
        columnGrid.DefaultCellStyle.ForeColor = TextColor;
        columnGrid.RowTemplate.Height = 30;
    }

    private static void StylePanel(Control control)
    {
        control.Padding = control.Padding == Padding.Empty ? new Padding(10) : control.Padding;
        control.BackColor = PanelBackColor;
    }

    private void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? AccentColor : BorderColor;
        button.BackColor = primary ? AccentColor : DrawingColor.White;
        button.ForeColor = primary ? DrawingColor.White : TextColor;
        button.Font = new System.Drawing.Font(Font, primary ? DrawingFontStyle.Bold : DrawingFontStyle.Regular);
    }

    private void ConfigurePlot()
    {
        _formsPlot.Plot.Clear();
        _formsPlot.Plot.Axes.DateTimeTicksBottom();
        _formsPlot.Plot.Font.Set("Microsoft YaHei UI");
        _formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#F5F7FA");
        _formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
        _formsPlot.Plot.ShowLegend(Alignment.UpperLeft);
        _formsPlot.Refresh();
    }

    private void SetControlState(bool enabled)
    {
        timeColumnComboBox.Enabled = enabled;
        chartTypeComboBox.Enabled = enabled;
        sampleLimitComboBox.Enabled = enabled;
        displayModeComboBox.Enabled = enabled;
        intervalComboBox.Enabled = enabled;
        startPicker.Enabled = enabled;
        endPicker.Enabled = enabled;
        searchTextBox.Enabled = enabled;
        selectVisibleButton.Enabled = enabled;
        clearSelectionButton.Enabled = enabled;
        drawButton.Enabled = enabled;
        refreshPlotButton.Enabled = enabled;
        resetViewButton.Enabled = enabled;
        exportPlotButton.Enabled = enabled;
        columnGrid.Enabled = enabled;
        UpdateSamplingControls();
    }

    private void UpdateSamplingControls()
    {
        bool isFixedInterval = displayModeComboBox.Text == DisplayModeFixedInterval;
        sampleLimitComboBox.Enabled = !_refreshing && !isFixedInterval && displayModeComboBox.Enabled;
        intervalComboBox.Enabled = !_refreshing && isFixedInterval && displayModeComboBox.Enabled;
    }

    private void browseButton_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "CSV 文件 (*.csv;*.txt)|*.csv;*.txt|所有文件 (*.*)|*.*",
            FileName = filePathTextBox.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            filePathTextBox.Text = dialog.FileName;
    }

    private void loadButton_Click(object sender, EventArgs e)
    {
        string path = filePathTextBox.Text.Trim();
        if (!File.Exists(path))
        {
            MessageBox.Show(this, "请先选择有效的 CSV 文件。", "加载失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            statusLabel.Text = "正在加载 CSV 到内存...";
            Cursor = Cursors.WaitCursor;
            Refresh();

            _cachedData = LoadCsvData(path, timeColumnComboBox.Text);
            _loadedFilePath = path;
            _timeColumn = _cachedData.TimeColumn;

            PopulateTimeColumns();
            PopulateColumnGrid();

            startPicker.Value = _cachedData.MinTime;
            endPicker.Value = _cachedData.MaxTime;

            SetControlState(true);
            ConfigurePlot();
            statusLabel.Text = $"{Path.GetFileName(path)} | 共 {_cachedData.RowCount:N0} 行 | {_cachedData.NumericColumns.Count} 个可绘图列";
            UpdatePlotSummary(0, "等待绘图");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "加载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _cachedData = null;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private CachedCsvData LoadCsvData(string path, string? requestedTimeColumn)
    {
        using var parser = CreateParser(path);
        string[]? headers = parser.ReadFields();
        if (headers is null || headers.Length == 0)
            throw new InvalidOperationException("CSV 文件没有表头。");

        List<string> allHeaders = headers.Select(x => x?.Trim() ?? string.Empty).ToList();
        string timeColumn = ResolveTimeColumn(allHeaders, requestedTimeColumn);
        int timeIndex = allHeaders.IndexOf(timeColumn);
        if (timeIndex < 0)
            throw new InvalidOperationException("无法识别时间列。");

        Dictionary<int, int> numericScore = new();
        for (int i = 0; i < allHeaders.Count; i++)
        {
            if (i != timeIndex)
                numericScore[i] = 0;
        }

        int sampleLimit = 1500;
        int scanned = 0;
        DateTime? minTime = null;
        DateTime? maxTime = null;

        while (!parser.EndOfData)
        {
            string[]? fields = parser.ReadFields();
            if (fields is null || timeIndex >= fields.Length)
                continue;
            if (!TryParseDateTime(fields[timeIndex], out DateTime timestamp))
                continue;

            minTime = !minTime.HasValue || timestamp < minTime.Value ? timestamp : minTime;
            maxTime = !maxTime.HasValue || timestamp > maxTime.Value ? timestamp : maxTime;

            if (scanned < sampleLimit)
            {
                foreach (int index in numericScore.Keys.ToList())
                {
                    if (index < fields.Length && TryParseDouble(fields[index], out _))
                        numericScore[index]++;
                }
            }

            scanned++;
        }

        if (!minTime.HasValue || !maxTime.HasValue)
            throw new InvalidOperationException("无法识别时间列，请更换时间字段。");

        List<string> numericColumns = numericScore
            .Where(x => x.Value > 0)
            .Select(x => allHeaders[x.Key])
            .ToList();

        Dictionary<string, int> numericIndexes = allHeaders
            .Select((name, index) => new { name, index })
            .Where(x => numericColumns.Contains(x.name))
            .ToDictionary(x => x.name, x => x.index);

        using var fullParser = CreateParser(path);
        _ = fullParser.ReadFields();

        List<DateTime> timestamps = new();
        Dictionary<string, List<double?>> valuesByColumn = numericColumns.ToDictionary(x => x, _ => new List<double?>());

        while (!fullParser.EndOfData)
        {
            string[]? fields = fullParser.ReadFields();
            if (fields is null || timeIndex >= fields.Length)
                continue;
            if (!TryParseDateTime(fields[timeIndex], out DateTime timestamp))
                continue;

            timestamps.Add(timestamp);
            foreach (string column in numericColumns)
            {
                int valueIndex = numericIndexes[column];
                double? value = null;
                if (valueIndex < fields.Length && TryParseDouble(fields[valueIndex], out double parsed))
                    value = parsed;

                valuesByColumn[column].Add(value);
            }
        }

        var columnArrays = valuesByColumn.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        return new CachedCsvData(path, allHeaders, numericColumns, timeColumn, timestamps.ToArray(), columnArrays, minTime.Value, maxTime.Value);
    }

    private void PopulateTimeColumns()
    {
        if (_cachedData is null)
            return;

        _refreshing = true;
        timeColumnComboBox.Items.Clear();
        foreach (string header in _cachedData.Headers)
            timeColumnComboBox.Items.Add(header);
        timeColumnComboBox.SelectedItem = _cachedData.TimeColumn;
        _refreshing = false;
        UpdateSamplingControls();
    }

    private void PopulateColumnGrid()
    {
        if (_cachedData is null)
            return;

        List<(string Name, string Axis)> currentSelections = GetSelections();
        HashSet<string> selectedNames = currentSelections.Select(x => x.Name).ToHashSet();
        Dictionary<string, string> selectedAxes = currentSelections.ToDictionary(x => x.Name, x => x.Axis);

        _refreshing = true;
        columnGrid.Rows.Clear();

        string query = searchTextBox.Text.Trim().ToLowerInvariant();
        foreach (string name in _cachedData.NumericColumns)
        {
            if (!string.IsNullOrEmpty(query) && !name.ToLowerInvariant().Contains(query))
                continue;

            bool isSelected = selectedNames.Contains(name);
            string axis = selectedAxes.TryGetValue(name, out string? savedAxis) ? savedAxis : "Y1";
            int row = columnGrid.Rows.Add(isSelected, name, axis);
            columnGrid.Rows[row].Cells["NameColumn"].ReadOnly = true;
        }

        _refreshing = false;
    }

    private static string ResolveTimeColumn(List<string> headers, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested) && headers.Contains(requested))
            return requested;

        string? direct = headers.FirstOrDefault(x => x == "时间");
        if (direct is not null)
            return direct;

        string? fuzzy = headers.FirstOrDefault(x =>
            x.Contains("时间", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("日期", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("time", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("date", StringComparison.OrdinalIgnoreCase));

        return fuzzy ?? headers[0];
    }

    private static TextFieldParser CreateParser(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        foreach (Encoding encoding in new[] { new UTF8Encoding(true), new UTF8Encoding(false), Encoding.GetEncoding("GB18030") })
        {
            try
            {
                using var sr = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true);
                _ = sr.ReadLine();

                TextFieldParser parser = new(path, encoding)
                {
                    TextFieldType = FieldType.Delimited,
                    HasFieldsEnclosedInQuotes = true,
                    TrimWhiteSpace = false
                };
                parser.SetDelimiters(",", "\t", ";");
                return parser;
            }
            catch (DecoderFallbackException)
            {
            }
        }

        TextFieldParser fallback = new(path, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        fallback.SetDelimiters(",", "\t", ";");
        return fallback;
    }

    private static bool TryParseDateTime(string? raw, out DateTime value)
    {
        raw = raw?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = default;
            return false;
        }

        return DateTime.TryParseExact(raw, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
            || DateTime.TryParse(raw, out value);
    }

    private static bool TryParseDouble(string? raw, out double value)
    {
        raw = raw?.Trim().Replace(",", string.Empty);
        return double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private void searchTextBox_TextChanged(object sender, EventArgs e)
    {
        if (_cachedData is not null)
            PopulateColumnGrid();
    }

    private void selectVisibleButton_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in columnGrid.Rows)
            row.Cells["SelectColumn"].Value = true;
    }

    private void clearSelectionButton_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in columnGrid.Rows)
            row.Cells["SelectColumn"].Value = false;
    }

    private void timeColumnComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_refreshing || string.IsNullOrWhiteSpace(filePathTextBox.Text))
            return;

        loadButton_Click(sender, e);
    }

    private void displayModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_refreshing)
            return;

        UpdateSamplingControls();
    }

    private void drawButton_Click(object sender, EventArgs e)
    {
        if (_cachedData is null || string.IsNullOrWhiteSpace(_loadedFilePath) || string.IsNullOrWhiteSpace(_timeColumn))
            return;

        List<(string Name, string Axis)> selections = GetSelections();
        if (selections.Count == 0)
        {
            MessageBox.Show(this, "请至少选择一个指标列。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (startPicker.Value >= endPicker.Value)
        {
            MessageBox.Show(this, "开始时间必须早于结束时间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var request = new SeriesRequest(
                selections,
                startPicker.Value,
                endPicker.Value,
                displayModeComboBox.Text,
                int.Parse(sampleLimitComboBox.Text, CultureInfo.InvariantCulture),
                ParseInterval(intervalComboBox.Text));

            var data = BuildSeriesFromCache(_cachedData, request);
            DrawPlot(data, selections);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "绘图失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void refreshPlotButton_Click(object sender, EventArgs e)
    {
        if (_cachedData is null)
            return;

        drawButton_Click(sender, e);
    }

    private void resetViewButton_Click(object sender, EventArgs e)
    {
        _formsPlot.Plot.Axes.AutoScale();
        _formsPlot.Refresh();
        if (_cachedData is not null)
            UpdatePlotSummary(GetSelections().Count, "已重置视图");
    }

    private void exportPlotButton_Click(object sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片 (*.png)|*.png",
            FileName = $"chart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            int width = Math.Max(plotHostPanel.Width, 1200);
            int height = Math.Max(plotHostPanel.Height, 700);
            _formsPlot.Plot.SavePng(dialog.FileName, width, height);
            statusLabel.Text = $"已导出图片：{Path.GetFileName(dialog.FileName)}";
            if (_cachedData is not null)
                UpdatePlotSummary(GetSelections().Count, $"已导出 {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private List<(string Name, string Axis)> GetSelections()
    {
        List<(string Name, string Axis)> items = new();
        foreach (DataGridViewRow row in columnGrid.Rows)
        {
            bool selected = row.Cells["SelectColumn"].Value as bool? == true;
            if (!selected)
                continue;

            string? name = row.Cells["NameColumn"].Value?.ToString();
            string axis = row.Cells["AxisColumn"].Value?.ToString() ?? "Y1";
            if (!string.IsNullOrWhiteSpace(name))
                items.Add((name, axis));
        }

        return items;
    }

    private static Dictionary<string, (double[] Xs, double[] Ys)> BuildSeriesFromCache(CachedCsvData data, SeriesRequest request)
    {
        Dictionary<string, (double[] Xs, double[] Ys)> result = new();

        foreach (var selection in request.Selections)
        {
            if (!data.ValuesByColumn.TryGetValue(selection.Name, out double?[]? values))
            {
                result[selection.Name] = (Array.Empty<double>(), Array.Empty<double>());
                continue;
            }

            List<TimeValuePoint> points = new();
            for (int i = 0; i < data.Timestamps.Length; i++)
            {
                DateTime timestamp = data.Timestamps[i];
                if (timestamp < request.Start || timestamp > request.End)
                    continue;

                double? value = values[i];
                if (!value.HasValue)
                    continue;

                points.Add(new TimeValuePoint(timestamp, value.Value));
            }

            result[selection.Name] = ProcessSeries(points, request);
        }

        return result;
    }

    private static (double[] Xs, double[] Ys) ProcessSeries(List<TimeValuePoint> points, SeriesRequest request)
    {
        if (request.DisplayMode == DisplayModeFixedInterval)
            return ResampleByInterval(points, request.Interval);

        return DownSample(points, request.MaxPoints);
    }

    private static (double[] Xs, double[] Ys) DownSample(List<TimeValuePoint> points, int limit)
    {
        if (points.Count == 0)
            return (Array.Empty<double>(), Array.Empty<double>());

        if (points.Count <= limit || limit <= 0)
        {
            return (
                points.Select(p => p.Timestamp.ToOADate()).ToArray(),
                points.Select(p => p.Value).ToArray());
        }

        List<double> xs = new(limit);
        List<double> ys = new(limit);
        double scale = (points.Count - 1d) / (limit - 1d);
        int previousIndex = -1;

        for (int i = 0; i < limit; i++)
        {
            int sourceIndex = (int)Math.Round(i * scale, MidpointRounding.AwayFromZero);
            if (sourceIndex >= points.Count)
                sourceIndex = points.Count - 1;
            if (sourceIndex == previousIndex)
                continue;

            xs.Add(points[sourceIndex].Timestamp.ToOADate());
            ys.Add(points[sourceIndex].Value);
            previousIndex = sourceIndex;
        }

        if (xs.Count == 0 || xs[^1] != points[^1].Timestamp.ToOADate())
        {
            if (xs.Count == limit)
            {
                xs[^1] = points[^1].Timestamp.ToOADate();
                ys[^1] = points[^1].Value;
            }
            else
            {
                xs.Add(points[^1].Timestamp.ToOADate());
                ys.Add(points[^1].Value);
            }
        }

        return (xs.ToArray(), ys.ToArray());
    }

    private static (double[] Xs, double[] Ys) ResampleByInterval(List<TimeValuePoint> points, TimeSpan interval)
    {
        if (points.Count == 0)
            return (Array.Empty<double>(), Array.Empty<double>());

        if (interval <= TimeSpan.Zero)
        {
            return (
                points.Select(p => p.Timestamp.ToOADate()).ToArray(),
                points.Select(p => p.Value).ToArray());
        }

        List<double> xs = new();
        List<double> ys = new();
        HashSet<long> seenBuckets = new();

        foreach (var point in points)
        {
            long bucket = point.Timestamp.Ticks / interval.Ticks;
            if (!seenBuckets.Add(bucket))
                continue;

            xs.Add(point.Timestamp.ToOADate());
            ys.Add(point.Value);
        }

        return (xs.ToArray(), ys.ToArray());
    }

    private static TimeSpan ParseInterval(string text)
    {
        return text switch
        {
            "1分钟" => TimeSpan.FromMinutes(1),
            "5分钟" => TimeSpan.FromMinutes(5),
            "10分钟" => TimeSpan.FromMinutes(10),
            "30分钟" => TimeSpan.FromMinutes(30),
            "1小时" => TimeSpan.FromHours(1),
            _ => TimeSpan.FromMinutes(10)
        };
    }

    private void DrawPlot(Dictionary<string, (double[] Xs, double[] Ys)> data, List<(string Name, string Axis)> selections)
    {
        _formsPlot.Plot.Clear();
        _formsPlot.Plot.Axes.DateTimeTicksBottom();
        _formsPlot.Plot.Font.Set("Microsoft YaHei UI");

        var rightAxis = _formsPlot.Plot.Axes.AddRightAxis();
        rightAxis.Label.Text = "Y2";
        rightAxis.IsVisible = selections.Any(x => x.Axis == "Y2");

        string chartMode = chartTypeComboBox.Text;
        for (int i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];
            var seriesData = data[selection.Name];
            if (seriesData.Xs.Length == 0)
                continue;

            var scatter = _formsPlot.Plot.Add.Scatter(seriesData.Xs, seriesData.Ys);
            scatter.LegendText = selection.Name;
            scatter.Color = ScottPlot.Color.FromHex(CurveColor(i));
            scatter.LineWidth = chartMode == "散点图" ? 0 : 2;
            scatter.MarkerSize = chartMode == "折线图" ? 0 : 5;

            if (selection.Axis == "Y2")
                scatter.Axes.YAxis = rightAxis;
        }

        _formsPlot.Plot.Axes.Left.Label.Text = "Y1";
        _formsPlot.Plot.Title($"{startPicker.Value:yyyy-MM-dd HH:mm} 至 {endPicker.Value:yyyy-MM-dd HH:mm}");
        _formsPlot.Plot.ShowLegend(Alignment.UpperLeft);
        _formsPlot.Plot.Axes.AutoScale();
        _formsPlot.Refresh();

        int points = data.Values.DefaultIfEmpty().Max(series => series.Xs?.Length ?? 0);
        string samplingDescription = displayModeComboBox.Text == DisplayModeFixedInterval
            ? $"固定时间间隔 {intervalComboBox.Text}"
            : $"最大点数抽样 {sampleLimitComboBox.Text}";

        statusLabel.Text = $"{Path.GetFileName(_loadedFilePath)} | {samplingDescription} | 当前最多 {points} 点/列 | {selections.Count} 条曲线";
        UpdatePlotSummary(selections.Count, $"{samplingDescription} | 最多 {points} 点/列");
    }

    private void UpdatePlotSummary(int selectedSeriesCount, string detail)
    {
        string fileName = string.IsNullOrWhiteSpace(_loadedFilePath) ? "未加载文件" : Path.GetFileName(_loadedFilePath);
        _plotSummaryLabel.Text = $"{fileName} | {selectedSeriesCount} 条曲线 | {detail}";
    }

    private static string CurveColor(int index)
    {
        string[] colors =
        {
            "#2C66E6", "#18A957", "#F59E0B", "#E24A3B", "#7C3AED",
            "#0EA5E9", "#DB2777", "#475569", "#65A30D", "#C2410C"
        };
        return colors[index % colors.Length];
    }

    private sealed record SeriesRequest(
        List<(string Name, string Axis)> Selections,
        DateTime Start,
        DateTime End,
        string DisplayMode,
        int MaxPoints,
        TimeSpan Interval);

    private sealed record TimeValuePoint(DateTime Timestamp, double Value);

    private sealed record CachedCsvData(
        string Path,
        List<string> Headers,
        List<string> NumericColumns,
        string TimeColumn,
        DateTime[] Timestamps,
        Dictionary<string, double?[]> ValuesByColumn,
        DateTime MinTime,
        DateTime MaxTime)
    {
        public int RowCount => Timestamps.Length;
    }
}
