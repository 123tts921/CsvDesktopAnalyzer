import math
import os
import sys
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path


def _add_windows_dll_dirs():
    if not hasattr(os, "add_dll_directory"):
        return
    roots = []
    for entry in sys.path:
        if not entry:
            continue
        path = Path(entry)
        if path.name.lower() == "site-packages":
            roots.append(path)
    for root in roots:
        for name in ("PySide6", "shiboken6"):
            candidate = root / name
            if candidate.exists():
                os.add_dll_directory(str(candidate))


_add_windows_dll_dirs()

import duckdb
import pyqtgraph as pg
from PySide6.QtCore import QDateTime, Qt, Signal
from PySide6.QtGui import QAction
from PySide6.QtWidgets import (
    QApplication,
    QComboBox,
    QDateTimeEdit,
    QFileDialog,
    QHBoxLayout,
    QHeaderView,
    QLabel,
    QLineEdit,
    QMainWindow,
    QMessageBox,
    QPushButton,
    QSizePolicy,
    QSplitter,
    QTableWidget,
    QTableWidgetItem,
    QVBoxLayout,
    QWidget,
)


APP_TITLE = "CSV 多列时间趋势比较"
DEFAULT_CSV = r"C:\Users\Wulalala\Desktop\2025-2-1至2026-3-1 数据合集 - 副本_列重排_completed.csv"
TIME_FORMATS = (
    "%Y/%m/%d %H:%M:%S",
    "%Y/%m/%d %H:%M",
    "%Y-%m-%d %H:%M:%S",
    "%Y-%m-%d %H:%M",
    "%Y/%m/%d",
    "%Y-%m-%d",
)
MAX_POINTS = 6000
AXIS_COLORS = ["#1f77b4", "#ff7f0e", "#2ca02c", "#d62728"]
CURVE_COLORS = [
    "#1f77b4",
    "#ff7f0e",
    "#2ca02c",
    "#d62728",
    "#9467bd",
    "#8c564b",
    "#e377c2",
    "#7f7f7f",
    "#bcbd22",
    "#17becf",
]


@dataclass
class ColumnMeta:
    name: str
    numeric: bool


class DateAxis(pg.AxisItem):
    def tickStrings(self, values, scale, spacing):
        labels = []
        for value in values:
            try:
                labels.append(datetime.fromtimestamp(value).strftime("%Y-%m-%d\n%H:%M"))
            except Exception:
                labels.append("")
        return labels


class CsvDataBackend:
    def __init__(self):
        self.conn = duckdb.connect(":memory:")
        self.table_name = "csv_data"
        self.time_expr = None
        self.time_column = None
        self.file_path = None
        self.headers = []
        self.numeric_columns = []
        self.min_time = None
        self.max_time = None

    def quote(self, name):
        return '"' + name.replace('"', '""') + '"'

    def _build_time_expr(self, column):
        quoted = self.quote(column)
        exprs = [f"try_strptime({quoted}, '{fmt.replace('%', '%%')}')" for fmt in TIME_FORMATS]
        return f"coalesce({', '.join(exprs)})"

    def load_csv(self, path, requested_time_column=None):
        self.conn.execute(f"drop table if exists {self.table_name}")
        sql = (
            f"create table {self.table_name} as "
            "select * from read_csv_auto(?, all_varchar=true, sample_size=-1, ignore_errors=true)"
        )
        self.conn.execute(sql, [path])
        self.file_path = path
        self.headers = [row[1] for row in self.conn.execute(f"pragma table_info('{self.table_name}')").fetchall()]
        if not self.headers:
            raise ValueError("CSV 没有可读取的表头。")

        time_column = requested_time_column if requested_time_column in self.headers else None
        if not time_column:
            for name in self.headers:
                if name == "时间" or "时间" in name or "日期" in name or "time" in name.lower() or "date" in name.lower():
                    time_column = name
                    break
        if not time_column:
            time_column = self.headers[0]

        self.time_column = time_column
        self.time_expr = self._build_time_expr(time_column)

        result = self.conn.execute(
            f"select min({self.time_expr}), max({self.time_expr}) from {self.table_name}"
        ).fetchone()
        self.min_time, self.max_time = result
        if self.min_time is None or self.max_time is None:
            raise ValueError("无法识别时间列，请换一个时间字段。")

        self.numeric_columns = []
        sample_limit = 800
        for name in self.headers:
            if name == self.time_column:
                continue
            quoted = self.quote(name)
            row = self.conn.execute(
                f"""
                select count(*)
                from (
                  select try_cast(replace({quoted}, ',', '') as double) as v
                  from {self.table_name}
                  where {self.time_expr} is not null
                  limit {sample_limit}
                ) t
                where v is not null
                """
            ).fetchone()
            if row and row[0] > 0:
                self.numeric_columns.append(name)

        return {
            "headers": self.headers,
            "time_column": self.time_column,
            "numeric_columns": self.numeric_columns,
            "min_time": self.min_time,
            "max_time": self.max_time,
        }

    def query_series(self, columns, start_dt, end_dt):
        if not columns:
            return []
        quoted_time = self.time_expr
        selects = [f"{quoted_time} as ts"]
        for name in columns:
            selects.append(
                f"try_cast(replace({self.quote(name)}, ',', '') as double) as {self.quote(name)}"
            )
        sql = (
            f"select {', '.join(selects)} "
            f"from {self.table_name} "
            f"where {quoted_time} between ? and ? and {quoted_time} is not null "
            f"order by {quoted_time}"
        )
        rows = self.conn.execute(sql, [start_dt, end_dt]).fetchall()
        return rows


class AxisCombo(QComboBox):
    changed = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.currentIndexChanged.connect(self.changed.emit)


class AnalyzerWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(APP_TITLE)
        self.resize(1500, 920)

        self.backend = CsvDataBackend()
        self.axis_count = 1
        self.current_rows = []
        self.visible_names = []

        self._build_ui()
        if os.path.exists(DEFAULT_CSV):
            self.file_edit.setText(DEFAULT_CSV)

    def _build_ui(self):
        central = QWidget()
        self.setCentralWidget(central)
        outer = QVBoxLayout(central)
        outer.setContentsMargins(10, 10, 10, 10)
        outer.setSpacing(10)

        top = QHBoxLayout()
        outer.addLayout(top)
        top.addWidget(QLabel("CSV 文件"))
        self.file_edit = QLineEdit()
        top.addWidget(self.file_edit, 1)
        browse_btn = QPushButton("选择文件")
        browse_btn.clicked.connect(self.choose_file)
        top.addWidget(browse_btn)
        load_btn = QPushButton("加载")
        load_btn.clicked.connect(self.load_current_file)
        top.addWidget(load_btn)

        splitter = QSplitter(Qt.Horizontal)
        outer.addWidget(splitter, 1)

        left = QWidget()
        left_layout = QVBoxLayout(left)
        left_layout.setContentsMargins(0, 0, 0, 0)
        left_layout.setSpacing(8)
        splitter.addWidget(left)

        row1 = QHBoxLayout()
        left_layout.addLayout(row1)
        self.time_combo = QComboBox()
        self.time_combo.currentTextChanged.connect(self.reload_with_time_column)
        self.chart_type = QComboBox()
        self.chart_type.addItems(["折线图", "点线图", "散点图"])
        self.chart_type.currentIndexChanged.connect(self.redraw)
        row1.addWidget(self._labeled("时间列", self.time_combo))
        row1.addWidget(self._labeled("图表类型", self.chart_type))

        row2 = QHBoxLayout()
        left_layout.addLayout(row2)
        self.start_edit = QDateTimeEdit()
        self.start_edit.setDisplayFormat("yyyy-MM-dd HH:mm")
        self.start_edit.setCalendarPopup(True)
        self.end_edit = QDateTimeEdit()
        self.end_edit.setDisplayFormat("yyyy-MM-dd HH:mm")
        self.end_edit.setCalendarPopup(True)
        row2.addWidget(self._labeled("开始时间", self.start_edit))
        row2.addWidget(self._labeled("结束时间", self.end_edit))

        row3 = QHBoxLayout()
        left_layout.addLayout(row3)
        self.search_edit = QLineEdit()
        self.search_edit.textChanged.connect(self.refresh_table)
        row3.addWidget(self._labeled("搜索指标", self.search_edit), 1)

        row4 = QHBoxLayout()
        left_layout.addLayout(row4)
        select_btn = QPushButton("全选可见")
        select_btn.clicked.connect(self.select_visible)
        clear_btn = QPushButton("清空")
        clear_btn.clicked.connect(self.clear_selection)
        draw_btn = QPushButton("绘制")
        draw_btn.clicked.connect(self.redraw)
        add_axis_btn = QPushButton("增加 Y 轴")
        add_axis_btn.clicked.connect(self.add_axis)
        row4.addWidget(select_btn)
        row4.addWidget(clear_btn)
        row4.addWidget(draw_btn)
        row4.addWidget(add_axis_btn)

        self.table = QTableWidget(0, 3)
        self.table.setHorizontalHeaderLabels(["选", "指标列", "Y轴"])
        self.table.verticalHeader().setVisible(False)
        self.table.horizontalHeader().setSectionResizeMode(0, QHeaderView.ResizeToContents)
        self.table.horizontalHeader().setSectionResizeMode(1, QHeaderView.Stretch)
        self.table.horizontalHeader().setSectionResizeMode(2, QHeaderView.ResizeToContents)
        self.table.setAlternatingRowColors(True)
        left_layout.addWidget(self.table, 1)

        self.status = QLabel("未加载数据")
        left_layout.addWidget(self.status)

        right = QWidget()
        right_layout = QVBoxLayout(right)
        right_layout.setContentsMargins(0, 0, 0, 0)
        right_layout.setSpacing(8)
        splitter.addWidget(right)

        self.main_plot = pg.PlotWidget(axisItems={"bottom": DateAxis("bottom")})
        self.main_plot.showGrid(x=True, y=True, alpha=0.2)
        self.main_plot.addLegend()
        self.main_plot.setBackground("w")
        right_layout.addWidget(self.main_plot, 4)

        self.overview_plot = pg.PlotWidget(axisItems={"bottom": DateAxis("bottom")})
        self.overview_plot.showGrid(x=False, y=False, alpha=0.1)
        self.overview_plot.setBackground("#f8fafc")
        self.overview_plot.setMaximumHeight(160)
        right_layout.addWidget(self.overview_plot, 1)

        self.region = pg.LinearRegionItem()
        self.region.sigRegionChanged.connect(self._region_changed)
        self.overview_plot.addItem(self.region)

        splitter.setStretchFactor(0, 0)
        splitter.setStretchFactor(1, 1)
        splitter.setSizes([460, 1040])

        open_action = QAction("打开 CSV", self)
        open_action.triggered.connect(self.choose_file)
        self.addAction(open_action)

    def _labeled(self, text, widget):
        box = QWidget()
        layout = QVBoxLayout(box)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(4)
        layout.addWidget(QLabel(text))
        layout.addWidget(widget)
        return box

    def choose_file(self):
        path, _ = QFileDialog.getOpenFileName(self, "选择 CSV 文件", self.file_edit.text() or "", "CSV Files (*.csv *.txt);;All Files (*.*)")
        if path:
            self.file_edit.setText(path)
            self.load_current_file()

    def load_current_file(self):
        path = self.file_edit.text().strip()
        if not path or not os.path.exists(path):
            QMessageBox.warning(self, APP_TITLE, "请先选择有效的 CSV 文件。")
            return
        try:
            meta = self.backend.load_csv(path, self.time_combo.currentText() or None)
        except Exception as exc:
            QMessageBox.critical(self, APP_TITLE, str(exc))
            return

        self.time_combo.blockSignals(True)
        self.time_combo.clear()
        self.time_combo.addItems(meta["headers"])
        self.time_combo.setCurrentText(meta["time_column"])
        self.time_combo.blockSignals(False)

        min_dt = QDateTime(meta["min_time"])
        max_dt = QDateTime(meta["max_time"])
        self.start_edit.setDateTime(min_dt)
        self.end_edit.setDateTime(max_dt)
        self.start_edit.setMinimumDateTime(min_dt)
        self.start_edit.setMaximumDateTime(max_dt)
        self.end_edit.setMinimumDateTime(min_dt)
        self.end_edit.setMaximumDateTime(max_dt)

        self.refresh_table()
        self.status.setText(f"{os.path.basename(path)} | {len(meta['numeric_columns'])} 个可绘图列")
        self.main_plot.clear()
        self.main_plot.addLegend()
        self.overview_plot.clear()
        self.overview_plot.addItem(self.region)

    def reload_with_time_column(self):
        if self.file_edit.text().strip():
            self.load_current_file()

    def refresh_table(self):
        query = self.search_edit.text().strip().lower()
        names = [name for name in self.backend.numeric_columns if not query or query in name.lower()]
        self.visible_names = names
        self.table.setRowCount(len(names))
        for row, name in enumerate(names):
            check = QTableWidgetItem()
            check.setFlags(Qt.ItemIsEnabled | Qt.ItemIsUserCheckable)
            check.setCheckState(Qt.Unchecked)
            self.table.setItem(row, 0, check)

            name_item = QTableWidgetItem(name)
            name_item.setFlags(Qt.ItemIsEnabled | Qt.ItemIsSelectable)
            self.table.setItem(row, 1, name_item)

            combo = AxisCombo()
            combo.addItems([f"Y{i + 1}" for i in range(self.axis_count)])
            self.table.setCellWidget(row, 2, combo)

    def select_visible(self):
        for row in range(self.table.rowCount()):
            item = self.table.item(row, 0)
            if item:
                item.setCheckState(Qt.Checked)

    def clear_selection(self):
        for row in range(self.table.rowCount()):
            item = self.table.item(row, 0)
            if item:
                item.setCheckState(Qt.Unchecked)

    def add_axis(self):
        self.axis_count = min(4, self.axis_count + 1)
        for row in range(self.table.rowCount()):
            combo = self.table.cellWidget(row, 2)
            current = combo.currentIndex() if combo else 0
            if combo:
                combo.blockSignals(True)
                combo.clear()
                combo.addItems([f"Y{i + 1}" for i in range(self.axis_count)])
                combo.setCurrentIndex(min(current, self.axis_count - 1))
                combo.blockSignals(False)

    def _selected_specs(self):
        specs = []
        for row in range(self.table.rowCount()):
            item = self.table.item(row, 0)
            if item and item.checkState() == Qt.Checked:
                name = self.table.item(row, 1).text()
                combo = self.table.cellWidget(row, 2)
                axis_index = combo.currentIndex() if combo else 0
                specs.append((name, axis_index))
        return specs

    def redraw(self):
        specs = self._selected_specs()
        if not specs:
            return
        start_dt = self.start_edit.dateTime().toPython()
        end_dt = self.end_edit.dateTime().toPython()
        if start_dt >= end_dt:
            QMessageBox.warning(self, APP_TITLE, "开始时间必须早于结束时间。")
            return

        columns = [name for name, _axis in specs]
        try:
            rows = self.backend.query_series(columns, start_dt, end_dt)
        except Exception as exc:
            QMessageBox.critical(self, APP_TITLE, str(exc))
            return
        if not rows:
            QMessageBox.information(self, APP_TITLE, "当前时间范围没有可绘图数据。")
            return

        self.current_rows = rows
        self._draw_main_plot(specs, rows)
        self._draw_overview(rows, columns[0])
        self.status.setText(f"{os.path.basename(self.backend.file_path)} | 当前 {len(rows)} 行 | {len(specs)} 条曲线")

    def _draw_main_plot(self, specs, rows):
        self.main_plot.clear()
        self.main_plot.addLegend()
        timestamps = [row[0].timestamp() for row in rows]
        mode = self.chart_type.currentText()

        for axis_index in range(self.axis_count):
            if axis_index == 0:
                self.main_plot.getAxis("left").setLabel(f"Y{axis_index + 1}", color=AXIS_COLORS[axis_index])
                self.main_plot.getAxis("left").setPen(pg.mkPen(AXIS_COLORS[axis_index], width=1))
            else:
                axis = pg.ViewBox()
                self.main_plot.scene().addItem(axis)
                self.main_plot.getPlotItem().layout.addItem(pg.AxisItem("right"), 2, axis_index + 2)

        for index, (name, axis_index) in enumerate(specs):
            color = CURVE_COLORS[index % len(CURVE_COLORS)]
            values = [row[index + 1] for row in rows]
            x = []
            y = []
            step = max(1, len(values) // MAX_POINTS)
            for i in range(0, len(values), step):
                value = values[i]
                if value is None or (isinstance(value, float) and math.isnan(value)):
                    continue
                x.append(timestamps[i])
                y.append(float(value))

            if mode == "散点图":
                item = pg.ScatterPlotItem(x=x, y=y, pen=pg.mkPen(color), brush=pg.mkBrush(color), size=5, name=name)
            elif mode == "点线图":
                item = pg.PlotDataItem(x=x, y=y, pen=pg.mkPen(color, width=1.5), symbol="o", symbolSize=5, symbolBrush=color, name=name)
            else:
                item = pg.PlotDataItem(x=x, y=y, pen=pg.mkPen(color, width=1.5), name=name)
            self.main_plot.addItem(item)

    def _draw_overview(self, rows, column_name):
        self.overview_plot.clear()
        self.overview_plot.addItem(self.region)
        timestamps = [row[0].timestamp() for row in rows]
        values = []
        for row in rows:
            value = row[1]
            values.append(float(value) if value is not None and not (isinstance(value, float) and math.isnan(value)) else math.nan)
        self.overview_plot.plot(timestamps, values, pen=pg.mkPen("#94a3b8", width=1))
        self.region.setRegion((timestamps[0], timestamps[-1]))

    def _region_changed(self):
        region = self.region.getRegion()
        self.main_plot.setXRange(region[0], region[1], padding=0)


def main():
    app = QApplication(sys.argv)
    pg.setConfigOptions(antialias=True, foreground="#111827")
    window = AnalyzerWindow()
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
