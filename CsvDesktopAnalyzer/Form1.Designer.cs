namespace CsvDesktopAnalyzer;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        headerTitleLabel = new Label();
        headerHintLabel = new Label();
        loadButton = new Button();
        browseButton = new Button();
        filePathTextBox = new TextBox();
        fileLabel = new Label();
        splitter = new SplitContainer();
        leftScrollPanel = new Panel();
        leftLayout = new TableLayoutPanel();
        filtersTitleLabel = new Label();
        filtersPanel = new TableLayoutPanel();
        timeColumnLabel = new Label();
        chartTypeLabel = new Label();
        sampleLimitLabel = new Label();
        displayModeLabel = new Label();
        intervalLabel = new Label();
        timeColumnComboBox = new ComboBox();
        chartTypeComboBox = new ComboBox();
        sampleLimitComboBox = new ComboBox();
        displayModeComboBox = new ComboBox();
        intervalComboBox = new ComboBox();
        dateTitleLabel = new Label();
        datePanel = new TableLayoutPanel();
        startLabel = new Label();
        endLabel = new Label();
        startPicker = new DateTimePicker();
        endPicker = new DateTimePicker();
        searchTitleLabel = new Label();
        searchTextBox = new TextBox();
        actionPanel = new FlowLayoutPanel();
        selectVisibleButton = new Button();
        clearSelectionButton = new Button();
        drawButton = new Button();
        columnGrid = new DataGridView();
        SelectColumn = new DataGridViewCheckBoxColumn();
        NameColumn = new DataGridViewTextBoxColumn();
        AxisColumn = new DataGridViewComboBoxColumn();
        SeriesTypeColumn = new DataGridViewComboBoxColumn();
        rightPanel = new Panel();
        plotTitleLabel = new Label();
        plotHintLabel = new Label();
        plotToolbar = new FlowLayoutPanel();
        exportPlotButton = new Button();
        resetViewButton = new Button();
        refreshPlotButton = new Button();
        plotHostPanel = new Panel();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitter).BeginInit();
        splitter.Panel1.SuspendLayout();
        splitter.Panel2.SuspendLayout();
        splitter.SuspendLayout();
        leftScrollPanel.SuspendLayout();
        leftLayout.SuspendLayout();
        filtersPanel.SuspendLayout();
        datePanel.SuspendLayout();
        actionPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)columnGrid).BeginInit();
        rightPanel.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(splitter, 0, 1);
        rootLayout.Controls.Add(statusStrip, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        rootLayout.Size = new Size(1464, 921);
        rootLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.Controls.Add(headerTitleLabel);
        headerPanel.Controls.Add(headerHintLabel);
        headerPanel.Controls.Add(loadButton);
        headerPanel.Controls.Add(browseButton);
        headerPanel.Controls.Add(filePathTextBox);
        headerPanel.Controls.Add(fileLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Padding = new Padding(16, 12, 16, 12);
        headerPanel.Size = new Size(1464, 82);
        headerPanel.TabIndex = 0;
        // 
        // headerTitleLabel
        // 
        headerTitleLabel.AutoSize = true;
        headerTitleLabel.Location = new Point(16, 14);
        headerTitleLabel.Name = "headerTitleLabel";
        headerTitleLabel.Size = new Size(104, 17);
        headerTitleLabel.TabIndex = 0;
        headerTitleLabel.Text = "CSV 数据分析工具";
        // 
        // headerHintLabel
        // 
        headerHintLabel.AutoSize = true;
        headerHintLabel.Location = new Point(16, 41);
        headerHintLabel.Name = "headerHintLabel";
        headerHintLabel.Size = new Size(176, 17);
        headerHintLabel.TabIndex = 1;
        headerHintLabel.Text = "选择文件后即可筛选时间和列";
        // 
        // loadButton
        // 
        loadButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        loadButton.Location = new Point(1348, 23);
        loadButton.Name = "loadButton";
        loadButton.Size = new Size(100, 34);
        loadButton.TabIndex = 5;
        loadButton.Text = "加载";
        loadButton.UseVisualStyleBackColor = true;
        loadButton.Click += loadButton_Click;
        // 
        // browseButton
        // 
        browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseButton.Location = new Point(1240, 23);
        browseButton.Name = "browseButton";
        browseButton.Size = new Size(100, 34);
        browseButton.TabIndex = 4;
        browseButton.Text = "选择文件";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += browseButton_Click;
        // 
        // filePathTextBox
        // 
        filePathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        filePathTextBox.Location = new Point(296, 28);
        filePathTextBox.Name = "filePathTextBox";
        filePathTextBox.Size = new Size(936, 23);
        filePathTextBox.TabIndex = 3;
        // 
        // fileLabel
        // 
        fileLabel.AutoSize = true;
        fileLabel.Location = new Point(228, 31);
        fileLabel.Name = "fileLabel";
        fileLabel.Size = new Size(56, 17);
        fileLabel.TabIndex = 2;
        fileLabel.Text = "当前文件";
        // 
        // splitter
        // 
        splitter.Dock = DockStyle.Fill;
        splitter.Location = new Point(3, 85);
        splitter.Name = "splitter";
        // 
        // splitter.Panel1
        // 
        splitter.Panel1.Controls.Add(leftScrollPanel);
        // 
        // splitter.Panel2
        // 
        splitter.Panel2.Controls.Add(rightPanel);
        splitter.Size = new Size(1458, 805);
        splitter.SplitterDistance = 470;
        splitter.TabIndex = 1;
        // 
        // leftScrollPanel
        // 
        leftScrollPanel.AutoScroll = true;
        leftScrollPanel.Controls.Add(leftLayout);
        leftScrollPanel.Dock = DockStyle.Fill;
        leftScrollPanel.Location = new Point(0, 0);
        leftScrollPanel.Name = "leftScrollPanel";
        leftScrollPanel.Padding = new Padding(12);
        leftScrollPanel.Size = new Size(470, 805);
        leftScrollPanel.TabIndex = 0;
        // 
        // leftLayout
        // 
        leftLayout.AutoSize = true;
        leftLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        leftLayout.ColumnCount = 1;
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        leftLayout.Controls.Add(filtersTitleLabel, 0, 0);
        leftLayout.Controls.Add(filtersPanel, 0, 1);
        leftLayout.Controls.Add(dateTitleLabel, 0, 2);
        leftLayout.Controls.Add(datePanel, 0, 3);
        leftLayout.Controls.Add(searchTitleLabel, 0, 4);
        leftLayout.Controls.Add(searchTextBox, 0, 5);
        leftLayout.Controls.Add(actionPanel, 0, 6);
        leftLayout.Controls.Add(columnGrid, 0, 7);
        leftLayout.Dock = DockStyle.Top;
        leftLayout.Location = new Point(12, 12);
        leftLayout.Name = "leftLayout";
        leftLayout.RowCount = 8;
        leftLayout.RowStyles.Add(new RowStyle());
        leftLayout.RowStyles.Add(new RowStyle());
        leftLayout.RowStyles.Add(new RowStyle());
        leftLayout.RowStyles.Add(new RowStyle());
        leftLayout.RowStyles.Add(new RowStyle());
        leftLayout.RowStyles.Add(new RowStyle());
        leftLayout.RowStyles.Add(new RowStyle());
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 420F));
        leftLayout.Size = new Size(446, 862);
        leftLayout.TabIndex = 0;
        // 
        // filtersTitleLabel
        // 
        filtersTitleLabel.AutoSize = true;
        filtersTitleLabel.Dock = DockStyle.Fill;
        filtersTitleLabel.Location = new Point(3, 0);
        filtersTitleLabel.Margin = new Padding(3, 0, 3, 8);
        filtersTitleLabel.Name = "filtersTitleLabel";
        filtersTitleLabel.Size = new Size(440, 17);
        filtersTitleLabel.TabIndex = 0;
        filtersTitleLabel.Text = "图表设置";
        // 
        // filtersPanel
        // 
        filtersPanel.ColumnCount = 2;
        filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        filtersPanel.Controls.Add(timeColumnLabel, 0, 0);
        filtersPanel.Controls.Add(chartTypeLabel, 1, 0);
        filtersPanel.Controls.Add(timeColumnComboBox, 0, 1);
        filtersPanel.Controls.Add(chartTypeComboBox, 1, 1);
        filtersPanel.Controls.Add(sampleLimitLabel, 0, 2);
        filtersPanel.Controls.Add(displayModeLabel, 1, 2);
        filtersPanel.Controls.Add(sampleLimitComboBox, 0, 3);
        filtersPanel.Controls.Add(displayModeComboBox, 1, 3);
        filtersPanel.Controls.Add(intervalLabel, 0, 4);
        filtersPanel.Controls.Add(intervalComboBox, 0, 5);
        filtersPanel.Dock = DockStyle.Fill;
        filtersPanel.Location = new Point(3, 28);
        filtersPanel.Margin = new Padding(3, 3, 3, 14);
        filtersPanel.Name = "filtersPanel";
        filtersPanel.Padding = new Padding(16);
        filtersPanel.RowCount = 6;
        filtersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        filtersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        filtersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        filtersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        filtersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        filtersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        filtersPanel.Size = new Size(440, 228);
        filtersPanel.TabIndex = 1;
        filtersPanel.SetColumnSpan(intervalComboBox, 2);
        // 
        // timeColumnLabel
        // 
        timeColumnLabel.AutoSize = true;
        timeColumnLabel.Location = new Point(19, 16);
        timeColumnLabel.Name = "timeColumnLabel";
        timeColumnLabel.Size = new Size(44, 17);
        timeColumnLabel.TabIndex = 0;
        timeColumnLabel.Text = "时间列";
        // 
        // chartTypeLabel
        // 
        chartTypeLabel.AutoSize = true;
        chartTypeLabel.Location = new Point(223, 16);
        chartTypeLabel.Name = "chartTypeLabel";
        chartTypeLabel.Size = new Size(56, 17);
        chartTypeLabel.TabIndex = 1;
        chartTypeLabel.Text = "图表类型";
        // 
        // sampleLimitLabel
        // 
        sampleLimitLabel.AutoSize = true;
        sampleLimitLabel.Location = new Point(19, 88);
        sampleLimitLabel.Name = "sampleLimitLabel";
        sampleLimitLabel.Size = new Size(92, 17);
        sampleLimitLabel.TabIndex = 2;
        sampleLimitLabel.Text = "每条曲线最多点数";
        // 
        // displayModeLabel
        // 
        displayModeLabel.AutoSize = true;
        displayModeLabel.Location = new Point(223, 88);
        displayModeLabel.Name = "displayModeLabel";
        displayModeLabel.Size = new Size(56, 17);
        displayModeLabel.TabIndex = 3;
        displayModeLabel.Text = "显示模式";
        // 
        // intervalLabel
        // 
        intervalLabel.AutoSize = true;
        intervalLabel.Location = new Point(19, 160);
        intervalLabel.Name = "intervalLabel";
        intervalLabel.Size = new Size(80, 17);
        intervalLabel.TabIndex = 4;
        intervalLabel.Text = "固定时间间隔";
        // 
        // timeColumnComboBox
        // 
        timeColumnComboBox.Dock = DockStyle.Fill;
        timeColumnComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        timeColumnComboBox.FormattingEnabled = true;
        timeColumnComboBox.Location = new Point(19, 43);
        timeColumnComboBox.Name = "timeColumnComboBox";
        timeColumnComboBox.Size = new Size(198, 25);
        timeColumnComboBox.TabIndex = 5;
        timeColumnComboBox.SelectedIndexChanged += timeColumnComboBox_SelectedIndexChanged;
        // 
        // chartTypeComboBox
        // 
        chartTypeComboBox.Dock = DockStyle.Fill;
        chartTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        chartTypeComboBox.FormattingEnabled = true;
        chartTypeComboBox.Location = new Point(223, 43);
        chartTypeComboBox.Name = "chartTypeComboBox";
        chartTypeComboBox.Size = new Size(198, 25);
        chartTypeComboBox.TabIndex = 6;
        // 
        // sampleLimitComboBox
        // 
        sampleLimitComboBox.Dock = DockStyle.Fill;
        sampleLimitComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        sampleLimitComboBox.FormattingEnabled = true;
        sampleLimitComboBox.Location = new Point(19, 115);
        sampleLimitComboBox.Name = "sampleLimitComboBox";
        sampleLimitComboBox.Size = new Size(198, 25);
        sampleLimitComboBox.TabIndex = 7;
        // 
        // displayModeComboBox
        // 
        displayModeComboBox.Dock = DockStyle.Fill;
        displayModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        displayModeComboBox.FormattingEnabled = true;
        displayModeComboBox.Location = new Point(223, 115);
        displayModeComboBox.Name = "displayModeComboBox";
        displayModeComboBox.Size = new Size(198, 25);
        displayModeComboBox.TabIndex = 8;
        displayModeComboBox.SelectedIndexChanged += displayModeComboBox_SelectedIndexChanged;
        // 
        // intervalComboBox
        // 
        intervalComboBox.Dock = DockStyle.Fill;
        intervalComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        intervalComboBox.FormattingEnabled = true;
        intervalComboBox.Location = new Point(19, 187);
        intervalComboBox.Name = "intervalComboBox";
        intervalComboBox.Size = new Size(402, 25);
        intervalComboBox.TabIndex = 9;
        // 
        // dateTitleLabel
        // 
        dateTitleLabel.AutoSize = true;
        dateTitleLabel.Dock = DockStyle.Fill;
        dateTitleLabel.Location = new Point(3, 270);
        dateTitleLabel.Margin = new Padding(3, 0, 3, 8);
        dateTitleLabel.Name = "dateTitleLabel";
        dateTitleLabel.Size = new Size(440, 17);
        dateTitleLabel.TabIndex = 2;
        dateTitleLabel.Text = "时间范围";
        // 
        // datePanel
        // 
        datePanel.ColumnCount = 2;
        datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        datePanel.Controls.Add(startLabel, 0, 0);
        datePanel.Controls.Add(endLabel, 1, 0);
        datePanel.Controls.Add(startPicker, 0, 1);
        datePanel.Controls.Add(endPicker, 1, 1);
        datePanel.Dock = DockStyle.Fill;
        datePanel.Location = new Point(3, 298);
        datePanel.Margin = new Padding(3, 3, 3, 14);
        datePanel.Name = "datePanel";
        datePanel.Padding = new Padding(16);
        datePanel.RowCount = 2;
        datePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        datePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        datePanel.Size = new Size(440, 104);
        datePanel.TabIndex = 3;
        // 
        // startLabel
        // 
        startLabel.AutoSize = true;
        startLabel.Location = new Point(19, 16);
        startLabel.Name = "startLabel";
        startLabel.Size = new Size(56, 17);
        startLabel.TabIndex = 0;
        startLabel.Text = "开始时间";
        // 
        // endLabel
        // 
        endLabel.AutoSize = true;
        endLabel.Location = new Point(223, 16);
        endLabel.Name = "endLabel";
        endLabel.Size = new Size(56, 17);
        endLabel.TabIndex = 1;
        endLabel.Text = "结束时间";
        // 
        // startPicker
        // 
        startPicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        startPicker.Dock = DockStyle.Fill;
        startPicker.Format = DateTimePickerFormat.Custom;
        startPicker.Location = new Point(19, 43);
        startPicker.Name = "startPicker";
        startPicker.Size = new Size(198, 23);
        startPicker.TabIndex = 2;
        // 
        // endPicker
        // 
        endPicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        endPicker.Dock = DockStyle.Fill;
        endPicker.Format = DateTimePickerFormat.Custom;
        endPicker.Location = new Point(223, 43);
        endPicker.Name = "endPicker";
        endPicker.Size = new Size(198, 23);
        endPicker.TabIndex = 3;
        // 
        // searchTitleLabel
        // 
        searchTitleLabel.AutoSize = true;
        searchTitleLabel.Dock = DockStyle.Fill;
        searchTitleLabel.Location = new Point(3, 416);
        searchTitleLabel.Margin = new Padding(3, 0, 3, 8);
        searchTitleLabel.Name = "searchTitleLabel";
        searchTitleLabel.Size = new Size(440, 17);
        searchTitleLabel.TabIndex = 4;
        searchTitleLabel.Text = "变量筛选";
        // 
        // searchTextBox
        // 
        searchTextBox.Dock = DockStyle.Fill;
        searchTextBox.Location = new Point(3, 444);
        searchTextBox.Margin = new Padding(3, 3, 3, 10);
        searchTextBox.Name = "searchTextBox";
        searchTextBox.PlaceholderText = "例如：电能、温度、压力";
        searchTextBox.Size = new Size(440, 23);
        searchTextBox.TabIndex = 5;
        searchTextBox.TextChanged += searchTextBox_TextChanged;
        // 
        // actionPanel
        // 
        actionPanel.Controls.Add(selectVisibleButton);
        actionPanel.Controls.Add(clearSelectionButton);
        actionPanel.Controls.Add(drawButton);
        actionPanel.Dock = DockStyle.Fill;
        actionPanel.Location = new Point(3, 480);
        actionPanel.Name = "actionPanel";
        actionPanel.Padding = new Padding(0, 4, 0, 6);
        actionPanel.Size = new Size(440, 42);
        actionPanel.TabIndex = 6;
        // 
        // selectVisibleButton
        // 
        selectVisibleButton.Location = new Point(3, 7);
        selectVisibleButton.Name = "selectVisibleButton";
        selectVisibleButton.Size = new Size(126, 29);
        selectVisibleButton.TabIndex = 0;
        selectVisibleButton.Text = "全选当前结果";
        selectVisibleButton.UseVisualStyleBackColor = true;
        selectVisibleButton.Click += selectVisibleButton_Click;
        // 
        // clearSelectionButton
        // 
        clearSelectionButton.Location = new Point(135, 7);
        clearSelectionButton.Name = "clearSelectionButton";
        clearSelectionButton.Size = new Size(102, 29);
        clearSelectionButton.TabIndex = 1;
        clearSelectionButton.Text = "清空选择";
        clearSelectionButton.UseVisualStyleBackColor = true;
        clearSelectionButton.Click += clearSelectionButton_Click;
        // 
        // drawButton
        // 
        drawButton.Location = new Point(243, 7);
        drawButton.Name = "drawButton";
        drawButton.Size = new Size(102, 29);
        drawButton.TabIndex = 2;
        drawButton.Text = "绘制图表";
        drawButton.UseVisualStyleBackColor = true;
        drawButton.Click += drawButton_Click;
        // 
        // columnGrid
        // 
        columnGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        columnGrid.Columns.AddRange(new DataGridViewColumn[] { SelectColumn, NameColumn, AxisColumn, SeriesTypeColumn });
        columnGrid.Dock = DockStyle.Fill;
        columnGrid.Location = new Point(3, 528);
        columnGrid.Name = "columnGrid";
        columnGrid.Size = new Size(440, 331);
        columnGrid.TabIndex = 7;
        // 
        // SelectColumn
        // 
        SelectColumn.HeaderText = "选中";
        SelectColumn.Name = "SelectColumn";
        SelectColumn.Width = 52;
        // 
        // NameColumn
        // 
        NameColumn.HeaderText = "指标列";
        NameColumn.Name = "NameColumn";
        NameColumn.ReadOnly = true;
        NameColumn.Width = 180;
        // 
        // AxisColumn
        // 
        AxisColumn.HeaderText = "Y轴";
        AxisColumn.Name = "AxisColumn";
        AxisColumn.Width = 70;
        // 
        // SeriesTypeColumn
        // 
        SeriesTypeColumn.HeaderText = "图表类型";
        SeriesTypeColumn.Name = "SeriesTypeColumn";
        SeriesTypeColumn.Width = 110;
        // 
        // rightPanel
        // 
        rightPanel.Controls.Add(plotHostPanel);
        rightPanel.Controls.Add(_plotSummaryLabel);
        rightPanel.Controls.Add(plotToolbar);
        rightPanel.Controls.Add(plotHintLabel);
        rightPanel.Controls.Add(plotTitleLabel);
        rightPanel.Dock = DockStyle.Fill;
        rightPanel.Location = new Point(0, 0);
        rightPanel.Name = "rightPanel";
        rightPanel.Padding = new Padding(12);
        rightPanel.Size = new Size(984, 805);
        rightPanel.TabIndex = 0;
        // 
        // plotTitleLabel
        // 
        plotTitleLabel.AutoSize = true;
        plotTitleLabel.Location = new Point(16, 14);
        plotTitleLabel.Name = "plotTitleLabel";
        plotTitleLabel.Size = new Size(56, 17);
        plotTitleLabel.TabIndex = 0;
        plotTitleLabel.Text = "图表视图";
        // 
        // plotHintLabel
        // 
        plotHintLabel.AutoSize = true;
        plotHintLabel.Location = new Point(16, 38);
        plotHintLabel.Name = "plotHintLabel";
        plotHintLabel.Size = new Size(176, 17);
        plotHintLabel.TabIndex = 1;
        plotHintLabel.Text = "右侧显示当前筛选后的趋势图";
        // 
        // plotToolbar
        // 
        plotToolbar.Controls.Add(refreshPlotButton);
        plotToolbar.Controls.Add(resetViewButton);
        plotToolbar.Controls.Add(exportPlotButton);
        plotToolbar.Location = new Point(16, 66);
        plotToolbar.Name = "plotToolbar";
        plotToolbar.Size = new Size(320, 36);
        plotToolbar.TabIndex = 2;
        // 
        // exportPlotButton
        // 
        exportPlotButton.Location = new Point(219, 3);
        exportPlotButton.Name = "exportPlotButton";
        exportPlotButton.Size = new Size(98, 30);
        exportPlotButton.TabIndex = 2;
        exportPlotButton.Text = "导出图片";
        exportPlotButton.UseVisualStyleBackColor = true;
        exportPlotButton.Click += exportPlotButton_Click;
        // 
        // resetViewButton
        // 
        resetViewButton.Location = new Point(111, 3);
        resetViewButton.Name = "resetViewButton";
        resetViewButton.Size = new Size(102, 30);
        resetViewButton.TabIndex = 1;
        resetViewButton.Text = "重置视图";
        resetViewButton.UseVisualStyleBackColor = true;
        resetViewButton.Click += resetViewButton_Click;
        // 
        // refreshPlotButton
        // 
        refreshPlotButton.Location = new Point(3, 3);
        refreshPlotButton.Name = "refreshPlotButton";
        refreshPlotButton.Size = new Size(102, 30);
        refreshPlotButton.TabIndex = 0;
        refreshPlotButton.Text = "刷新图表";
        refreshPlotButton.UseVisualStyleBackColor = true;
        refreshPlotButton.Click += refreshPlotButton_Click;
        // 
        // plotHostPanel
        // 
        plotHostPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        plotHostPanel.Location = new Point(16, 110);
        plotHostPanel.Name = "plotHostPanel";
        plotHostPanel.Size = new Size(952, 679);
        plotHostPanel.TabIndex = 3;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Location = new Point(0, 899);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1464, 22);
        statusStrip.TabIndex = 2;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(32, 17);
        statusLabel.Text = "就绪";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1464, 921);
        Controls.Add(rootLayout);
        Name = "Form1";
        Text = "CSV 数据分析工具";
        splitter.Panel1.ResumeLayout(false);
        splitter.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitter).EndInit();
        splitter.ResumeLayout(false);
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        leftScrollPanel.ResumeLayout(false);
        leftScrollPanel.PerformLayout();
        leftLayout.ResumeLayout(false);
        leftLayout.PerformLayout();
        filtersPanel.ResumeLayout(false);
        filtersPanel.PerformLayout();
        datePanel.ResumeLayout(false);
        datePanel.PerformLayout();
        actionPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)columnGrid).EndInit();
        rightPanel.ResumeLayout(false);
        rightPanel.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel rootLayout;
    private Panel headerPanel;
    private Label headerTitleLabel;
    private Label headerHintLabel;
    private Button loadButton;
    private Button browseButton;
    private TextBox filePathTextBox;
    private Label fileLabel;
    private SplitContainer splitter;
    private Panel leftScrollPanel;
    private TableLayoutPanel leftLayout;
    private Label filtersTitleLabel;
    private TableLayoutPanel filtersPanel;
    private Label timeColumnLabel;
    private Label chartTypeLabel;
    private Label sampleLimitLabel;
    private Label displayModeLabel;
    private Label intervalLabel;
    private ComboBox timeColumnComboBox;
    private ComboBox chartTypeComboBox;
    private ComboBox sampleLimitComboBox;
    private ComboBox displayModeComboBox;
    private ComboBox intervalComboBox;
    private Label dateTitleLabel;
    private TableLayoutPanel datePanel;
    private Label startLabel;
    private Label endLabel;
    private DateTimePicker startPicker;
    private DateTimePicker endPicker;
    private Label searchTitleLabel;
    private TextBox searchTextBox;
    private FlowLayoutPanel actionPanel;
    private Button selectVisibleButton;
    private Button clearSelectionButton;
    private Button drawButton;
    private DataGridView columnGrid;
    private DataGridViewCheckBoxColumn SelectColumn;
    private DataGridViewTextBoxColumn NameColumn;
    private DataGridViewComboBoxColumn AxisColumn;
    private DataGridViewComboBoxColumn SeriesTypeColumn;
    private Panel rightPanel;
    private Label plotTitleLabel;
    private Label plotHintLabel;
    private FlowLayoutPanel plotToolbar;
    private Button exportPlotButton;
    private Button resetViewButton;
    private Button refreshPlotButton;
    private Panel plotHostPanel;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;
}
