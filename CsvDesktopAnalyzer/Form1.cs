using System.Drawing;
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
    private const string DisplayModeMaxPoints = "固定点数";
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

    private static readonly DrawingColor AppBackColor = DrawingColor.FromArgb(238, 243, 247);
    private static readonly DrawingColor ShellBackColor = DrawingColor.FromArgb(249, 250, 251);
    private static readonly DrawingColor PanelBackColor = DrawingColor.White;
    private static readonly DrawingColor PanelAltBackColor = DrawingColor.FromArgb(246, 248, 251);
    private static readonly DrawingColor AccentColor = DrawingColor.FromArgb(15, 108, 189);
    private static readonly DrawingColor AccentSoftColor = DrawingColor.FromArgb(222, 236, 249);
    private static readonly DrawingColor BorderColor = DrawingColor.FromArgb(221, 226, 232);
    private static readonly DrawingColor TextColor = DrawingColor.FromArgb(32, 31, 30);
    private static readonly DrawingColor MutedTextColor = DrawingColor.FromArgb(96, 94, 92);

    private string? _loadedFilePath;
    private string? _timeColumn;
    private CachedCsvData? _cachedData;
    private bool _refreshing;
    private readonly FormsPlot _formsPlot = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Label _plotSummaryLabel = new();
    private readonly TableLayoutPanel _headerLayout = new();
    private readonly TableLayoutPanel _fileBarLayout = new();
    private readonly TableLayoutPanel _fileActionLayout = new();
    private readonly Panel _fileBarPanel = new();
    private readonly TableLayoutPanel _rightLayout = new();

    public Form1()
    {
        InitializeComponent();
        InitializeRuntimeUi();
    }

    private void InitializeRuntimeUi()
    {
        Text = "CSV 桌面分析器";
        Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, DrawingFontStyle.Regular, GraphicsUnit.Point);
        MinimumSize = new Size(1440, 920);
        StartPosition = FormStartPosition.CenterScreen;
        ShowIcon = true;
        ShowInTaskbar = true;
        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
        }
        Resize += (_, _) => ApplyFlatLayout();
        Shown += (_, _) => ApplyFlatLayout();

        headerTitleLabel.Text = "CSV 桌面分析器";
        headerHintLabel.Text = "多列对比、多轴映射、时间范围筛选";
        fileLabel.Text = "当前文件";
        timeColumnLabel.Text = "时间列";
        chartTypeLabel.Text = "默认图表类型";
        sampleLimitLabel.Text = "每条曲线最多点数";
        displayModeLabel.Text = "显示模式";
        intervalLabel.Text = "固定时间间隔";
        startLabel.Text = "开始时间";
        endLabel.Text = "结束时间";
        filtersTitleLabel.Text = "图表配置";
        dateTitleLabel.Text = "时间范围";
        searchTitleLabel.Text = "变量筛选";
        plotTitleLabel.Text = "多序列趋势对比";
        plotHintLabel.Text = "右侧显示当前筛选后的图表，颜色与坐标轴自动对应";
        searchTextBox.PlaceholderText = "例如：电能、温度、压力";
        browseButton.Text = "选择文件";
        loadButton.Text = "加载";
        selectVisibleButton.Text = "全选当前结果";
        clearSelectionButton.Text = "清空选择";
        drawButton.Text = "绘制图表";
        refreshPlotButton.Text = "刷新";
        resetViewButton.Text = "重置视图";
        exportPlotButton.Text = "导出图片";
        statusLabel.Text = "准备就绪";
        SelectColumn.HeaderText = "选择";
        NameColumn.HeaderText = "数据列";
        AxisColumn.HeaderText = "轴";
        SeriesTypeColumn.HeaderText = "图表类型";
        headerHintLabel.Visible = false;
        plotHintLabel.Visible = false;

        filePathTextBox.Text = string.Empty;
        plotHostPanel.Controls.Add(_formsPlot);
        _plotSummaryLabel.AutoSize = true;
        _plotSummaryLabel.AutoEllipsis = false;
        _plotSummaryLabel.TextAlign = ContentAlignment.TopLeft;
        _plotSummaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _plotSummaryLabel.MaximumSize = new Size(0, 0);
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
        if (columnGrid.Columns["SeriesTypeColumn"] is DataGridViewComboBoxColumn seriesTypeColumn)
            seriesTypeColumn.Items.AddRange("折线图", "点线图", "散点图");

        columnGrid.AutoGenerateColumns = false;
        columnGrid.AllowUserToAddRows = false;
        columnGrid.AllowUserToDeleteRows = false;
        columnGrid.AllowUserToResizeRows = false;
        columnGrid.RowHeadersVisible = false;
        columnGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        columnGrid.MultiSelect = false;
        columnGrid.ScrollBars = ScrollBars.Vertical;

        splitter.Panel1MinSize = 430;
        splitter.Panel2MinSize = 760;
        splitter.SplitterDistance = 460;
        statusStrip.SizingGrip = false;

        BuildAdaptiveShell();
        ApplyTheme();
        ApplyFlatLayout();
        ConfigurePlot();
        SetControlState(false);
        statusLabel.Text = "请选择 CSV 文件。";
    }

    private void BuildAdaptiveShell()
    {
        BuildHeaderLayout();
        BuildLeftCards();
        BuildRightLayout();
    }

    private void BuildHeaderLayout()
    {
        headerPanel.SuspendLayout();
        headerPanel.Controls.Clear();

        _headerLayout.SuspendLayout();
        _headerLayout.Controls.Clear();
        _headerLayout.ColumnStyles.Clear();
        _headerLayout.RowStyles.Clear();
        _headerLayout.ColumnCount = 1;
        _headerLayout.RowCount = 2;
        _headerLayout.AutoSize = true;
        _headerLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _headerLayout.Dock = DockStyle.Fill;
        _headerLayout.Margin = Padding.Empty;
        _headerLayout.Padding = Padding.Empty;
        _headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        headerTitleLabel.Dock = DockStyle.Fill;
        headerTitleLabel.Margin = new Padding(0, 0, 0, 2);
        headerHintLabel.Dock = DockStyle.Fill;
        headerHintLabel.Margin = Padding.Empty;

        _fileBarPanel.SuspendLayout();
        _fileBarPanel.Controls.Clear();
        _fileBarPanel.AutoSize = false;
        _fileBarPanel.Dock = DockStyle.Fill;
        _fileBarPanel.Margin = new Padding(0, 8, 0, 0);
        _fileBarPanel.Padding = Padding.Empty;
        _fileBarPanel.Height = 38;

        fileLabel.AutoSize = false;
        fileLabel.Width = 90;
        fileLabel.MinimumSize = new Size(90, 34);
        fileLabel.Height = 34;
        fileLabel.Margin = Padding.Empty;
        fileLabel.Dock = DockStyle.None;
        fileLabel.TextAlign = ContentAlignment.MiddleLeft;
        filePathTextBox.Dock = DockStyle.None;
        filePathTextBox.Multiline = false;
        filePathTextBox.BorderStyle = BorderStyle.FixedSingle;
        filePathTextBox.Margin = Padding.Empty;
        filePathTextBox.Anchor = AnchorStyles.None;
        browseButton.Dock = DockStyle.None;
        browseButton.Margin = Padding.Empty;
        browseButton.Padding = new Padding(0);
        browseButton.AutoEllipsis = false;
        browseButton.TextAlign = ContentAlignment.MiddleCenter;
        loadButton.Dock = DockStyle.None;
        loadButton.Margin = Padding.Empty;
        loadButton.Padding = new Padding(0);
        loadButton.AutoEllipsis = false;
        loadButton.TextAlign = ContentAlignment.MiddleCenter;

        _fileBarPanel.Controls.Add(fileLabel);
        _fileBarPanel.Controls.Add(filePathTextBox);
        _fileBarPanel.Controls.Add(browseButton);
        _fileBarPanel.Controls.Add(loadButton);
        _fileBarPanel.ResumeLayout(false);

        _headerLayout.Controls.Add(headerTitleLabel, 0, 0);
        _headerLayout.Controls.Add(_fileBarPanel, 0, 1);
        _headerLayout.ResumeLayout(false);
        _headerLayout.PerformLayout();

        headerPanel.Controls.Add(_headerLayout);
        headerPanel.ResumeLayout(false);
    }

    private void BuildLeftCards()
    {
        filtersPanel.SuspendLayout();
        filtersPanel.ColumnStyles.Clear();
        filtersPanel.RowStyles.Clear();
        filtersPanel.ColumnCount = 1;
        filtersPanel.RowCount = 10;
        filtersPanel.AutoSize = true;
        filtersPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        filtersPanel.Dock = DockStyle.Top;
        filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int index = 0; index < 10; index++)
            filtersPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        filtersPanel.SetCellPosition(timeColumnLabel, new TableLayoutPanelCellPosition(0, 0));
        filtersPanel.SetCellPosition(timeColumnComboBox, new TableLayoutPanelCellPosition(0, 1));
        filtersPanel.SetCellPosition(chartTypeLabel, new TableLayoutPanelCellPosition(0, 2));
        filtersPanel.SetCellPosition(chartTypeComboBox, new TableLayoutPanelCellPosition(0, 3));
        filtersPanel.SetCellPosition(displayModeLabel, new TableLayoutPanelCellPosition(0, 4));
        filtersPanel.SetCellPosition(displayModeComboBox, new TableLayoutPanelCellPosition(0, 5));
        filtersPanel.SetCellPosition(sampleLimitLabel, new TableLayoutPanelCellPosition(0, 6));
        filtersPanel.SetCellPosition(sampleLimitComboBox, new TableLayoutPanelCellPosition(0, 7));
        filtersPanel.SetCellPosition(intervalLabel, new TableLayoutPanelCellPosition(0, 8));
        filtersPanel.SetCellPosition(intervalComboBox, new TableLayoutPanelCellPosition(0, 9));
        filtersPanel.ResumeLayout(false);
        filtersPanel.PerformLayout();

        datePanel.SuspendLayout();
        datePanel.ColumnStyles.Clear();
        datePanel.RowStyles.Clear();
        datePanel.ColumnCount = 1;
        datePanel.RowCount = 4;
        datePanel.AutoSize = true;
        datePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        datePanel.Dock = DockStyle.Top;
        datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int index = 0; index < 4; index++)
            datePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        datePanel.SetCellPosition(startLabel, new TableLayoutPanelCellPosition(0, 0));
        datePanel.SetCellPosition(startPicker, new TableLayoutPanelCellPosition(0, 1));
        datePanel.SetCellPosition(endLabel, new TableLayoutPanelCellPosition(0, 2));
        datePanel.SetCellPosition(endPicker, new TableLayoutPanelCellPosition(0, 3));
        datePanel.ResumeLayout(false);
        datePanel.PerformLayout();
    }

    private void BuildRightLayout()
    {
        rightPanel.SuspendLayout();
        rightPanel.Controls.Clear();

        _rightLayout.SuspendLayout();
        _rightLayout.Controls.Clear();
        _rightLayout.ColumnStyles.Clear();
        _rightLayout.RowStyles.Clear();
        _rightLayout.ColumnCount = 1;
        _rightLayout.RowCount = 4;
        _rightLayout.Dock = DockStyle.Fill;
        _rightLayout.Margin = Padding.Empty;
        _rightLayout.Padding = Padding.Empty;
        _rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        plotTitleLabel.Dock = DockStyle.Fill;
        plotTitleLabel.Margin = new Padding(0, 0, 0, 8);
        plotToolbar.Dock = DockStyle.Fill;
        plotToolbar.AutoSize = true;
        plotToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        plotToolbar.WrapContents = true;
        plotToolbar.Margin = new Padding(0, 0, 0, 8);
        _plotSummaryLabel.Dock = DockStyle.Fill;
        _plotSummaryLabel.Margin = new Padding(0, 0, 0, 12);
        plotHostPanel.Dock = DockStyle.Fill;
        plotHostPanel.Margin = Padding.Empty;

        _rightLayout.Controls.Add(plotTitleLabel, 0, 0);
        _rightLayout.Controls.Add(plotToolbar, 0, 1);
        _rightLayout.Controls.Add(_plotSummaryLabel, 0, 2);
        _rightLayout.Controls.Add(plotHostPanel, 0, 3);
        _rightLayout.ResumeLayout(false);
        _rightLayout.PerformLayout();

        rightPanel.Controls.Add(_rightLayout);
        rightPanel.ResumeLayout(false);
    }

    private void ApplyTheme()
    {
        BackColor = AppBackColor;
        rootLayout.BackColor = AppBackColor;
        splitter.BackColor = AppBackColor;
        headerPanel.BackColor = PanelBackColor;
        leftScrollPanel.BackColor = PanelBackColor;
        leftLayout.BackColor = PanelBackColor;
        filtersPanel.BackColor = PanelAltBackColor;
        datePanel.BackColor = PanelAltBackColor;
        actionPanel.BackColor = DrawingColor.Transparent;
        rightPanel.BackColor = PanelBackColor;
        plotHostPanel.BackColor = PanelBackColor;
        statusStrip.BackColor = ShellBackColor;
        statusStrip.ForeColor = MutedTextColor;
        statusLabel.ForeColor = MutedTextColor;
        _headerLayout.BackColor = DrawingColor.Transparent;
        _fileBarLayout.BackColor = DrawingColor.Transparent;
        _rightLayout.BackColor = DrawingColor.Transparent;

        headerTitleLabel.ForeColor = TextColor;
        headerTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, DrawingFontStyle.Bold, GraphicsUnit.Point);
        headerHintLabel.ForeColor = MutedTextColor;
        headerHintLabel.Font = new System.Drawing.Font("Segoe UI", 10F, DrawingFontStyle.Regular, GraphicsUnit.Point);
        headerHintLabel.BackColor = DrawingColor.Transparent;

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
        plotHintLabel.Font = new System.Drawing.Font("Segoe UI", 9F, DrawingFontStyle.Regular, GraphicsUnit.Point);
        _plotSummaryLabel.ForeColor = MutedTextColor;
        _plotSummaryLabel.BackColor = DrawingColor.Transparent;
        _plotSummaryLabel.Font = new System.Drawing.Font("Segoe UI", 9F, DrawingFontStyle.Regular, GraphicsUnit.Point);

        StylePanel(headerPanel, PanelBackColor);
        StylePanel(leftScrollPanel, PanelBackColor);
        StylePanel(filtersPanel, PanelAltBackColor);
        StylePanel(datePanel, PanelAltBackColor);
        StylePanel(plotHostPanel, PanelBackColor);
        headerPanel.BorderStyle = BorderStyle.FixedSingle;
        filtersPanel.BorderStyle = BorderStyle.FixedSingle;
        datePanel.BorderStyle = BorderStyle.FixedSingle;
        rightPanel.BorderStyle = BorderStyle.FixedSingle;
        plotHostPanel.BorderStyle = BorderStyle.FixedSingle;

        StyleButton(browseButton, false);
        StyleButton(loadButton, true);
        StyleButton(selectVisibleButton, false);
        StyleButton(clearSelectionButton, false);
        StyleButton(drawButton, true);
        StyleButton(refreshPlotButton, true);
        StyleButton(resetViewButton, false);
        StyleButton(exportPlotButton, false);
        plotToolbar.BackColor = DrawingColor.Transparent;

        foreach (Control control in new Control[]
        {
            filePathTextBox, searchTextBox, timeColumnComboBox, chartTypeComboBox,
            sampleLimitComboBox, displayModeComboBox, intervalComboBox
        })
        {
            StyleInput(control);
        }

        StyleDatePicker(startPicker);
        StyleDatePicker(endPicker);

        columnGrid.BackgroundColor = DrawingColor.White;
        columnGrid.BorderStyle = BorderStyle.FixedSingle;
        columnGrid.GridColor = BorderColor;
        columnGrid.EnableHeadersVisualStyles = false;
        columnGrid.ColumnHeadersDefaultCellStyle.BackColor = PanelAltBackColor;
        columnGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
        columnGrid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font(Font, DrawingFontStyle.Bold);
        columnGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        columnGrid.ColumnHeadersHeight = 38;
        columnGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        columnGrid.DefaultCellStyle.SelectionBackColor = AccentSoftColor;
        columnGrid.DefaultCellStyle.SelectionForeColor = TextColor;
        columnGrid.DefaultCellStyle.BackColor = DrawingColor.White;
        columnGrid.AlternatingRowsDefaultCellStyle.BackColor = PanelAltBackColor;
        columnGrid.DefaultCellStyle.ForeColor = TextColor;
        columnGrid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
        columnGrid.RowTemplate.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
        columnGrid.RowTemplate.Height = 32;
    }

    private static void StylePanel(Control control, DrawingColor color)
    {
        control.Padding = control.Padding == Padding.Empty ? new Padding(12) : control.Padding;
        control.BackColor = color;
    }

    private void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? AccentColor : BorderColor;
        button.BackColor = primary ? AccentColor : DrawingColor.White;
        button.ForeColor = primary ? DrawingColor.White : TextColor;
        button.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, primary ? DrawingFontStyle.Bold : DrawingFontStyle.Regular);
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(10, 0, 10, 0);
    }

    private static void StyleInput(Control control)
    {
        control.ForeColor = TextColor;
        control.BackColor = DrawingColor.White;
    }

    private static void StyleDatePicker(DateTimePicker picker)
    {
        picker.CalendarForeColor = TextColor;
        picker.CalendarMonthBackground = DrawingColor.White;
        picker.CalendarTitleBackColor = AccentColor;
        picker.CalendarTitleForeColor = DrawingColor.White;
        picker.CalendarTrailingForeColor = MutedTextColor;
    }

    private void ApplyFlatLayout()
    {
        rootLayout.RowStyles[0].Height = 132F;
        headerPanel.Padding = new Padding(24, 14, 24, 14);
        UpdateFileBarMetrics();
        UpdateFileBarResponsiveLayout();

        leftScrollPanel.Padding = new Padding(16);
        leftLayout.Width = Math.Max(400, leftScrollPanel.ClientSize.Width - 32);
        filtersPanel.Margin = new Padding(3, 4, 3, 16);
        datePanel.Margin = new Padding(3, 4, 3, 16);
        searchTextBox.Margin = new Padding(3, 4, 3, 12);
        actionPanel.Margin = new Padding(0, 0, 0, 12);
        actionPanel.WrapContents = true;
        actionPanel.AutoSize = true;
        actionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        selectVisibleButton.Size = new Size(148, 36);
        clearSelectionButton.Size = new Size(120, 36);
        drawButton.Size = new Size(120, 36);
        plotTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 16F, DrawingFontStyle.Bold, GraphicsUnit.Point);
        refreshPlotButton.Size = new Size(118, 36);
        resetViewButton.Size = new Size(118, 36);
        exportPlotButton.Size = new Size(118, 36);
        refreshPlotButton.Margin = new Padding(0, 0, 10, 10);
        resetViewButton.Margin = new Padding(0, 0, 10, 10);
        exportPlotButton.Margin = new Padding(0, 0, 0, 10);
        rightPanel.Padding = new Padding(18);
        _plotSummaryLabel.MaximumSize = new Size(Math.Max(320, rightPanel.ClientSize.Width - 36), 0);

        filtersPanel.Padding = new Padding(18, 18, 18, 10);
        datePanel.Padding = new Padding(18, 18, 18, 10);
        timeColumnLabel.Margin = new Padding(0, 0, 0, 6);
        chartTypeLabel.Margin = new Padding(0, 0, 0, 6);
        sampleLimitLabel.Margin = new Padding(0, 0, 0, 6);
        displayModeLabel.Margin = new Padding(0, 0, 0, 6);
        intervalLabel.Margin = new Padding(0, 0, 0, 6);
        timeColumnComboBox.Margin = new Padding(0, 0, 0, 14);
        chartTypeComboBox.Margin = new Padding(0, 0, 0, 14);
        sampleLimitComboBox.Margin = new Padding(0, 0, 0, 14);
        displayModeComboBox.Margin = new Padding(0, 0, 0, 14);
        intervalComboBox.Margin = new Padding(0);
        startLabel.Margin = new Padding(0, 0, 0, 6);
        endLabel.Margin = new Padding(0, 10, 0, 6);
        startPicker.Margin = new Padding(0);
        endPicker.Margin = new Padding(0);

        int gridHeight = Math.Max(340, leftScrollPanel.ClientSize.Height - columnGrid.Top - 20);
        columnGrid.Height = gridHeight;
        columnGrid.Width = leftLayout.Width - 6;
        SelectColumn.Width = 56;
        AxisColumn.Width = 86;
        SeriesTypeColumn.Width = 132;
        NameColumn.Width = Math.Max(140, columnGrid.Width - SelectColumn.Width - AxisColumn.Width - SeriesTypeColumn.Width - 32);
    }

    private void UpdateFileBarMetrics()
    {
        Size labelTextSize = TextRenderer.MeasureText(fileLabel.Text, fileLabel.Font);
        Size browseTextSize = TextRenderer.MeasureText(browseButton.Text, browseButton.Font);
        Size loadTextSize = TextRenderer.MeasureText(loadButton.Text, loadButton.Font);

        int measuredHeight = Math.Max(labelTextSize.Height, Math.Max(browseTextSize.Height, loadTextSize.Height)) + 10;
        int controlHeight = Math.Min(38, Math.Max(34, measuredHeight));
        int labelWidth = Math.Max(88, labelTextSize.Width + 16);
        int browseWidth = Math.Max(144, browseTextSize.Width + 56);
        int loadWidth = Math.Max(96, loadTextSize.Width + 48);

        fileLabel.Width = labelWidth;
        fileLabel.MinimumSize = new Size(labelWidth, controlHeight);
        fileLabel.Height = controlHeight;

        filePathTextBox.MinimumSize = new Size(200, controlHeight);
        filePathTextBox.Height = controlHeight;

        browseButton.Size = new Size(browseWidth, controlHeight);
        browseButton.MinimumSize = new Size(browseWidth, controlHeight);
        loadButton.Size = new Size(loadWidth, controlHeight);
        loadButton.MinimumSize = new Size(loadWidth, controlHeight);
        _fileBarPanel.Height = controlHeight;
    }

    private void UpdateFileBarResponsiveLayout()
    {
        int availableWidth = Math.Max(0, headerPanel.ClientSize.Width - headerPanel.Padding.Horizontal);
        int controlHeight = filePathTextBox.MinimumSize.Height;
        int labelWidth = fileLabel.MinimumSize.Width;
        int browseWidth = browseButton.MinimumSize.Width;
        int loadWidth = loadButton.MinimumSize.Width;
        int labelGap = 14;
        int fieldGap = 18;
        int buttonGap = 16;
        int rightGuard = 24;
        int totalFixedWidth = labelWidth + labelGap + fieldGap + browseWidth + buttonGap + loadWidth + rightGuard;
        int minPathWidth = 280;
        int maxPathWidth = 620;
        int preferredPathWidth = Math.Min(maxPathWidth, Math.Max(minPathWidth, (int)Math.Round(availableWidth * 0.34)));
        int pathWidth = Math.Max(minPathWidth, Math.Min(preferredPathWidth, availableWidth - totalFixedWidth));

        if (availableWidth - totalFixedWidth < minPathWidth)
            pathWidth = Math.Max(180, availableWidth - totalFixedWidth);

        int x = 0;
        int y = 0;

        _fileBarPanel.SuspendLayout();
        _fileBarPanel.Height = controlHeight;
        fileLabel.Bounds = new Rectangle(x, y, labelWidth, controlHeight);
        x += labelWidth + labelGap;
        filePathTextBox.Bounds = new Rectangle(x, y + 1, pathWidth, Math.Max(22, controlHeight - 2));
        x += pathWidth + fieldGap;
        browseButton.Bounds = new Rectangle(x, y, browseWidth, controlHeight);
        x += browseWidth + buttonGap;
        loadButton.Bounds = new Rectangle(x, y, loadWidth, controlHeight);
        _fileBarPanel.ResumeLayout(true);
    }

    private void ConfigurePlot()
    {
        _formsPlot.Reset(new Plot());
        _formsPlot.Plot.Axes.DateTimeTicksBottom();
        _formsPlot.Plot.Font.Set("Microsoft YaHei UI");
        _formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
        _formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
        _formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E5EBF1");
        _formsPlot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("#F2F5F8");
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

        List<(string Name, string Axis, string ChartType)> currentSelections = GetSelections();
        HashSet<string> selectedNames = currentSelections.Select(x => x.Name).ToHashSet();
        Dictionary<string, string> selectedAxes = currentSelections.ToDictionary(x => x.Name, x => x.Axis);
        Dictionary<string, string> selectedChartTypes = currentSelections.ToDictionary(x => x.Name, x => x.ChartType);

        _refreshing = true;
        columnGrid.Rows.Clear();

        string query = searchTextBox.Text.Trim().ToLowerInvariant();
        foreach (string name in _cachedData.NumericColumns)
        {
            if (!string.IsNullOrEmpty(query) && !name.ToLowerInvariant().Contains(query))
                continue;

            bool isSelected = selectedNames.Contains(name);
            string axis = selectedAxes.TryGetValue(name, out string? savedAxis) ? savedAxis : "Y1";
            string chartType = selectedChartTypes.TryGetValue(name, out string? savedChartType) ? savedChartType : chartTypeComboBox.Text;
            int row = columnGrid.Rows.Add(isSelected, name, axis, chartType);
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

        List<(string Name, string Axis, string ChartType)> selections = GetSelections();
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

    private List<(string Name, string Axis, string ChartType)> GetSelections()
    {
        List<(string Name, string Axis, string ChartType)> items = new();
        foreach (DataGridViewRow row in columnGrid.Rows)
        {
            bool selected = row.Cells["SelectColumn"].Value as bool? == true;
            if (!selected)
                continue;

            string? name = row.Cells["NameColumn"].Value?.ToString();
            string axis = row.Cells["AxisColumn"].Value?.ToString() ?? "Y1";
            string chartType = row.Cells["SeriesTypeColumn"].Value?.ToString() ?? chartTypeComboBox.Text;
            if (!string.IsNullOrWhiteSpace(name))
                items.Add((name, axis, chartType));
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

    private void DrawPlot(Dictionary<string, (double[] Xs, double[] Ys)> data, List<(string Name, string Axis, string ChartType)> selections)
    {
        ConfigurePlot();
        _formsPlot.Plot.Axes.DateTimeTicksBottom();
        _formsPlot.Plot.Font.Set("Microsoft YaHei UI");

        var y1Selections = selections.Where(x => x.Axis == "Y1").ToList();
        var y2Selections = selections.Where(x => x.Axis == "Y2").ToList();
        Dictionary<string, ScottPlot.IYAxis> y1Axes = new();
        Dictionary<string, ScottPlot.IYAxis> y2Axes = new();

        for (int i = 0; i < y1Selections.Count; i++)
        {
            var selection = y1Selections[i];
            ScottPlot.IYAxis axis = i == 0 ? _formsPlot.Plot.Axes.Left : _formsPlot.Plot.Axes.AddLeftAxis();
            axis.Label.Text = selection.Name;
            axis.IsVisible = true;
            y1Axes[selection.Name] = axis;
        }

        foreach (var selection in y2Selections)
        {
            var axis = _formsPlot.Plot.Axes.AddRightAxis();
            axis.Label.Text = selection.Name;
            axis.IsVisible = true;
            y2Axes[selection.Name] = axis;
        }

        for (int i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];
            var seriesData = data[selection.Name];
            if (seriesData.Xs.Length == 0)
                continue;

            var scatter = _formsPlot.Plot.Add.Scatter(seriesData.Xs, seriesData.Ys);
            scatter.LegendText = selection.Name;
            ScottPlot.Color curveColor = ScottPlot.Color.FromHex(CurveColor(i));
            scatter.Color = curveColor;
            ApplySeriesChartType(scatter, selection.ChartType);

            if (selection.Axis == "Y1" && y1Axes.TryGetValue(selection.Name, out ScottPlot.IYAxis? leftAxis))
            {
                scatter.Axes.YAxis = leftAxis;
                if (leftAxis is ScottPlot.AxisPanels.AxisBase leftAxisBase)
                    leftAxisBase.Color(curveColor);
            }
            else if (selection.Axis == "Y2" && y2Axes.TryGetValue(selection.Name, out ScottPlot.IYAxis? rightAxis))
            {
                scatter.Axes.YAxis = rightAxis;
                if (rightAxis is ScottPlot.AxisPanels.AxisBase rightAxisBase)
                    rightAxisBase.Color(curveColor);
            }
        }

        _formsPlot.Plot.Title($"{startPicker.Value:yyyy-MM-dd HH:mm} 至 {endPicker.Value:yyyy-MM-dd HH:mm}");
        _formsPlot.Plot.ShowLegend(Alignment.UpperLeft);
        _formsPlot.Plot.Axes.AutoScale();
        _formsPlot.Refresh();

        int points = data.Values.DefaultIfEmpty().Max(series => series.Xs?.Length ?? 0);
        string samplingDescription = displayModeComboBox.Text == DisplayModeFixedInterval
            ? $"固定时间间隔 {intervalComboBox.Text}"
            : $"固定点数 {sampleLimitComboBox.Text}";

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
            "#2F80ED", "#20A39E", "#D48A1F", "#C95D73", "#6D73D9",
            "#0EA5E9", "#8B5CF6", "#4B5563", "#65A30D", "#C2410C"
        };
        return colors[index % colors.Length];
    }

    private static void ApplySeriesChartType(ScottPlot.Plottables.Scatter scatter, string chartType)
    {
        switch (chartType)
        {
            case "散点图":
                scatter.LineWidth = 0;
                scatter.MarkerSize = 5;
                break;
            case "点线图":
                scatter.LineWidth = 2;
                scatter.MarkerSize = 5;
                break;
            default:
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0;
                break;
        }
    }

    private sealed record SeriesRequest(
        List<(string Name, string Axis, string ChartType)> Selections,
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

internal sealed class SingleLineButton : Button
{
    public SingleLineButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Rectangle bounds = ClientRectangle;
        DrawingColor backColor = Enabled ? BackColor : SystemColors.Control;
        DrawingColor borderColor = Enabled ? FlatAppearance.BorderColor : SystemColors.ControlDark;
        DrawingColor textColor = Enabled ? ForeColor : SystemColors.GrayText;

        using SolidBrush background = new(backColor);
        e.Graphics.FillRectangle(background, bounds);
        ControlPaint.DrawBorder(e.Graphics, bounds, borderColor, ButtonBorderStyle.Solid);

        Rectangle textBounds = Rectangle.Inflate(bounds, -6, 0);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textBounds,
            textColor,
            TextFormatFlags.SingleLine |
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }
}
