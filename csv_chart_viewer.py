import csv
import math
import os
import threading
from bisect import bisect_left, bisect_right
from datetime import datetime
from tkinter import BOTH, END, EXTENDED, HORIZONTAL, LEFT, RIGHT, VERTICAL, W, X, Y
from tkinter import Listbox, StringVar, Tk, filedialog, messagebox
from tkinter import ttk

import matplotlib

matplotlib.use("TkAgg")
import matplotlib.dates as mdates
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk
from matplotlib.figure import Figure


DATE_FORMATS = (
    "%Y/%m/%d %H:%M:%S",
    "%Y/%m/%d %H:%M",
    "%Y-%m-%d %H:%M:%S",
    "%Y-%m-%d %H:%M",
    "%Y/%m/%d",
    "%Y-%m-%d",
)

DEFAULT_CSV = r"C:\Users\Wulalala\Desktop\2025-2-1至2026-3-1 数据合集 - 副本_列重排_completed.csv"
MAX_POINTS_PER_LINE = 5000


def detect_encoding(path):
    for encoding in ("utf-8-sig", "utf-8", "gb18030"):
        try:
            with open(path, "r", encoding=encoding, newline="") as handle:
                handle.read(4096)
            return encoding
        except UnicodeDecodeError:
            continue
    return "utf-8-sig"


def parse_datetime_text(text):
    text = (text or "").strip()
    if not text:
        return None
    for fmt in DATE_FORMATS:
        try:
            return datetime.strptime(text, fmt)
        except ValueError:
            pass
    return None


def parse_float(text):
    text = (text or "").strip()
    if not text:
        return math.nan
    try:
        return float(text.replace(",", ""))
    except ValueError:
        return math.nan


def format_dt(value):
    return value.strftime("%Y-%m-%d %H:%M") if value else ""


def read_csv_for_chart(path, requested_time_column):
    encoding = detect_encoding(path)
    with open(path, "r", encoding=encoding, newline="") as handle:
        reader = csv.reader(handle)
        headers = next(reader, [])
        if not headers:
            raise ValueError("CSV 文件没有表头。")

        if requested_time_column in headers:
            time_column = requested_time_column
        elif "时间" in headers:
            time_column = "时间"
        else:
            time_column = headers[0]

        time_index = headers.index(time_column)
        data_columns = [col for col in headers if col != time_column]
        data_by_column = {col: [] for col in data_columns}
        valid_counts = {col: 0 for col in data_columns}
        times = []

        for row in reader:
            if len(row) < len(headers):
                row = row + [""] * (len(headers) - len(row))
            time_value = parse_datetime_text(row[time_index] if time_index < len(row) else "")
            if time_value is None:
                continue
            times.append(time_value)
            for idx, col in enumerate(headers):
                if col == time_column:
                    continue
                value = parse_float(row[idx] if idx < len(row) else "")
                data_by_column[col].append(value)
                if not math.isnan(value):
                    valid_counts[col] += 1

    order = sorted(range(len(times)), key=times.__getitem__)
    if order != list(range(len(times))):
        times = [times[i] for i in order]
        for col in data_columns:
            values = data_by_column[col]
            data_by_column[col] = [values[i] for i in order]

    numeric_columns = [col for col in data_columns if valid_counts[col] > 0]
    return {
        "encoding": encoding,
        "headers": headers,
        "time_column": time_column,
        "times": times,
        "data_by_column": data_by_column,
        "numeric_columns": numeric_columns,
    }


class CsvChartViewer:
    def __init__(self, root):
        self.root = root
        self.root.title("CSV 多列时间趋势比较")
        self.root.geometry("1380x820")
        self.root.minsize(1120, 680)

        self.file_path = StringVar(value=DEFAULT_CSV if os.path.exists(DEFAULT_CSV) else "")
        self.time_column = StringVar(value="时间")
        self.search_text = StringVar()
        self.start_text = StringVar()
        self.end_text = StringVar()
        self.status_text = StringVar(value="请选择 CSV 文件。")
        self.downsample_text = StringVar(value=f"自动抽样：最多 {MAX_POINTS_PER_LINE} 点/列")

        self.headers = []
        self.times = []
        self.data_by_column = {}
        self.numeric_columns = []
        self.visible_columns = []
        self.start_index = 0
        self.end_index = 0
        self._loading = False
        self._hover_annotation = None

        self._setup_style()
        self._build_ui()

        if self.file_path.get():
            self.load_csv_async()

    def _setup_style(self):
        style = ttk.Style()
        try:
            style.theme_use("clam")
        except Exception:
            pass
        style.configure("TFrame", background="#f5f7fb")
        style.configure("Card.TFrame", background="#ffffff")
        style.configure("TLabel", background="#f5f7fb", foreground="#172033")
        style.configure("Card.TLabel", background="#ffffff", foreground="#172033")
        style.configure("Muted.TLabel", background="#ffffff", foreground="#667085")
        style.configure("TButton", padding=(10, 7))
        style.configure("Accent.TButton", background="#0f766e", foreground="#ffffff", padding=(12, 8))
        style.map("Accent.TButton", background=[("active", "#0b5f59")], foreground=[("active", "#ffffff")])

    def _build_ui(self):
        root = ttk.Frame(self.root, padding=12)
        root.pack(fill=BOTH, expand=True)

        toolbar = ttk.Frame(root, style="Card.TFrame", padding=10)
        toolbar.pack(fill=X, pady=(0, 10))

        ttk.Label(toolbar, text="CSV 文件", style="Card.TLabel").pack(side=LEFT, padx=(0, 6))
        ttk.Entry(toolbar, textvariable=self.file_path, width=74).pack(side=LEFT, fill=X, expand=True, padx=(0, 8))
        ttk.Button(toolbar, text="选择文件", command=self.choose_file).pack(side=LEFT, padx=(0, 8))
        ttk.Button(toolbar, text="加载", style="Accent.TButton", command=self.load_csv_async).pack(side=LEFT)

        main = ttk.PanedWindow(root, orient=HORIZONTAL)
        main.pack(fill=BOTH, expand=True)

        left = ttk.Frame(main, style="Card.TFrame", padding=12)
        right = ttk.Frame(main, style="Card.TFrame", padding=8)
        main.add(left, weight=1)
        main.add(right, weight=4)

        self._build_left_panel(left)
        self._build_chart_panel(right)

        status = ttk.Frame(root, style="Card.TFrame", padding=(10, 7))
        status.pack(fill=X, pady=(10, 0))
        ttk.Label(status, textvariable=self.status_text, style="Card.TLabel").pack(side=LEFT)
        ttk.Label(status, textvariable=self.downsample_text, style="Muted.TLabel").pack(side=RIGHT)

    def _build_left_panel(self, parent):
        ttk.Label(parent, text="时间列", style="Card.TLabel").pack(anchor=W)
        self.time_combo = ttk.Combobox(parent, textvariable=self.time_column, state="readonly")
        self.time_combo.pack(fill=X, pady=(4, 12))
        self.time_combo.bind("<<ComboboxSelected>>", lambda _event: self.load_csv_async())

        ttk.Label(parent, text="指标搜索", style="Card.TLabel").pack(anchor=W)
        search = ttk.Entry(parent, textvariable=self.search_text)
        search.pack(fill=X, pady=(4, 8))
        search.bind("<KeyRelease>", lambda _event: self.refresh_column_list())

        button_row = ttk.Frame(parent, style="Card.TFrame")
        button_row.pack(fill=X, pady=(0, 8))
        ttk.Button(button_row, text="全选可见", command=self.select_visible).pack(side=LEFT, padx=(0, 6))
        ttk.Button(button_row, text="清空", command=self.clear_selection).pack(side=LEFT)

        list_frame = ttk.Frame(parent, style="Card.TFrame")
        list_frame.pack(fill=BOTH, expand=True)
        self.column_list = Listbox(list_frame, selectmode=EXTENDED, exportselection=False, height=20)
        yscroll = ttk.Scrollbar(list_frame, orient=VERTICAL, command=self.column_list.yview)
        self.column_list.configure(yscrollcommand=yscroll.set)
        self.column_list.pack(side=LEFT, fill=BOTH, expand=True)
        yscroll.pack(side=RIGHT, fill=Y)
        self.column_list.bind("<<ListboxSelect>>", lambda _event: self.draw_chart())

        ttk.Label(parent, text="时间范围", style="Card.TLabel").pack(anchor=W, pady=(14, 4))
        ttk.Label(parent, text="开始", style="Muted.TLabel").pack(anchor=W)
        ttk.Entry(parent, textvariable=self.start_text).pack(fill=X, pady=(2, 6))
        ttk.Label(parent, text="结束", style="Muted.TLabel").pack(anchor=W)
        ttk.Entry(parent, textvariable=self.end_text).pack(fill=X, pady=(2, 8))

        ttk.Button(parent, text="应用输入时间", command=self.apply_time_entries).pack(fill=X, pady=(0, 8))
        ttk.Button(parent, text="重置完整时间", command=self.reset_time_range).pack(fill=X, pady=(0, 8))

        ttk.Label(parent, text="开始位置", style="Muted.TLabel").pack(anchor=W)
        self.start_scale = ttk.Scale(parent, from_=0, to=100, orient=HORIZONTAL, command=self.on_start_scale)
        self.start_scale.pack(fill=X, pady=(2, 8))
        ttk.Label(parent, text="结束位置", style="Muted.TLabel").pack(anchor=W)
        self.end_scale = ttk.Scale(parent, from_=0, to=100, orient=HORIZONTAL, command=self.on_end_scale)
        self.end_scale.pack(fill=X, pady=(2, 8))

        ttk.Label(
            parent,
            text="提示：图表工具栏可缩放、平移、保存图片；鼠标悬停曲线可查看最近数据点。",
            style="Muted.TLabel",
            wraplength=290,
        ).pack(anchor=W, pady=(8, 0))

    def _build_chart_panel(self, parent):
        self.figure = Figure(figsize=(9, 5), dpi=100)
        self.ax = self.figure.add_subplot(111)
        self.ax.set_title("请选择指标列")
        self.ax.grid(True, alpha=0.25)

        self.canvas = FigureCanvasTkAgg(self.figure, master=parent)
        self.canvas.get_tk_widget().pack(fill=BOTH, expand=True)
        self.toolbar = NavigationToolbar2Tk(self.canvas, parent, pack_toolbar=False)
        self.toolbar.update()
        self.toolbar.pack(fill=X)
        self.canvas.mpl_connect("motion_notify_event", self.on_hover)

    def choose_file(self):
        path = filedialog.askopenfilename(
            title="选择 CSV 文件",
            filetypes=[("CSV 文件", "*.csv"), ("所有文件", "*.*")],
        )
        if path:
            self.file_path.set(path)
            self.load_csv_async()

    def load_csv_async(self):
        if self._loading:
            return
        path = self.file_path.get().strip()
        if not path or not os.path.exists(path):
            messagebox.showwarning("文件不存在", "请先选择一个有效的 CSV 文件。")
            return
        self._loading = True
        self.status_text.set("正在读取 CSV，请稍候...")
        time_column = self.time_column.get()
        threading.Thread(target=self._load_csv_worker, args=(path, time_column), daemon=True).start()

    def _load_csv_worker(self, path, time_column):
        try:
            data = read_csv_for_chart(path, time_column)
            self.root.after(0, lambda: self._load_csv_done(data))
        except Exception as exc:
            self.root.after(0, lambda error=exc: self._load_csv_failed(error))

    def _load_csv_done(self, data):
        self._loading = False
        self.headers = data["headers"]
        self.time_column.set(data["time_column"])
        self.times = data["times"]
        self.data_by_column = data["data_by_column"]
        self.numeric_columns = data["numeric_columns"]
        self.time_combo["values"] = self.headers

        self.start_index = 0
        self.end_index = len(self.times) - 1
        self.start_scale.configure(from_=0, to=max(self.end_index, 1))
        self.end_scale.configure(from_=0, to=max(self.end_index, 1))
        self.reset_time_range()
        self.refresh_column_list()
        self.status_text.set(
            f"已加载 {len(self.times):,} 行、{len(self.headers)} 列；可绘图数值列 {len(self.numeric_columns)} 个；编码：{data['encoding']}"
        )

    def _load_csv_failed(self, exc):
        self._loading = False
        self.status_text.set("读取失败。")
        messagebox.showerror("读取失败", str(exc))

    def refresh_column_list(self):
        query = self.search_text.get().strip().lower()
        selected_names = self.get_selected_columns()
        self.column_list.delete(0, END)
        self.visible_columns = [col for col in self.numeric_columns if not query or query in col.lower()]
        for col in self.visible_columns:
            self.column_list.insert(END, col)
        for index, col in enumerate(self.visible_columns):
            if col in selected_names:
                self.column_list.selection_set(index)

    def get_selected_columns(self):
        return [self.visible_columns[i] for i in self.column_list.curselection()]

    def select_visible(self):
        self.column_list.selection_set(0, END)
        self.draw_chart()

    def clear_selection(self):
        self.column_list.selection_clear(0, END)
        self.draw_chart()

    def reset_time_range(self):
        if not self.times:
            return
        self.start_index = 0
        self.end_index = len(self.times) - 1
        self.start_scale.set(self.start_index)
        self.end_scale.set(self.end_index)
        self.update_time_text()
        self.draw_chart()

    def update_time_text(self):
        if not self.times:
            return
        self.start_text.set(format_dt(self.times[self.start_index]))
        self.end_text.set(format_dt(self.times[self.end_index]))

    def on_start_scale(self, value):
        if not self.times:
            return
        idx = int(float(value))
        self.start_index = max(0, min(idx, self.end_index - 1))
        self.start_scale.set(self.start_index)
        self.update_time_text()
        self.draw_chart()

    def on_end_scale(self, value):
        if not self.times:
            return
        idx = int(float(value))
        self.end_index = min(len(self.times) - 1, max(idx, self.start_index + 1))
        self.end_scale.set(self.end_index)
        self.update_time_text()
        self.draw_chart()

    def apply_time_entries(self):
        if not self.times:
            return
        start = parse_datetime_text(self.start_text.get())
        end = parse_datetime_text(self.end_text.get())
        if start is None or end is None or start >= end:
            messagebox.showwarning("时间格式错误", "请输入有效的开始和结束时间，例如 2025-02-01 00:00。")
            return
        start_idx = bisect_left(self.times, start)
        end_idx = bisect_right(self.times, end) - 1
        if start_idx >= len(self.times) or end_idx < 0 or start_idx >= end_idx:
            messagebox.showwarning("超出范围", "输入时间不在当前 CSV 的时间范围内。")
            return
        self.start_index = start_idx
        self.end_index = end_idx
        self.start_scale.set(self.start_index)
        self.end_scale.set(self.end_index)
        self.update_time_text()
        self.draw_chart()

    def draw_chart(self):
        if not self.times:
            return

        columns = self.get_selected_columns()
        self.ax.clear()
        self.ax.grid(True, alpha=0.25)

        if not columns:
            self.ax.set_title("请选择一个或多个指标列")
            self.canvas.draw_idle()
            return

        start = min(self.start_index, self.end_index)
        end = max(self.start_index, self.end_index)
        count = end - start + 1
        if count <= 0:
            return

        step = max(1, count // MAX_POINTS_PER_LINE)
        indices = range(start, end + 1, step)
        x_values = [self.times[i] for i in indices]

        for col in columns:
            values = self.data_by_column[col]
            y_values = [values[i] for i in indices]
            self.ax.plot(x_values, y_values, linewidth=1.2, label=col)

        self.ax.set_title(f"{format_dt(self.times[start])} 至 {format_dt(self.times[end])}")
        self.ax.set_xlabel("时间")
        self.ax.set_ylabel("数值")
        self.ax.legend(loc="best", fontsize=8)
        self.figure.autofmt_xdate()
        self.figure.tight_layout()
        self.downsample_text.set(f"当前显示 {count:,} 行；绘图抽样步长：{step}")
        self.canvas.draw_idle()

    def on_hover(self, event):
        if event.inaxes != self.ax or not self.times or event.xdata is None:
            return
        columns = self.get_selected_columns()
        if not columns:
            return

        try:
            hovered = mdates.num2date(event.xdata).replace(tzinfo=None)
        except Exception:
            return

        local_times = self.times[self.start_index : self.end_index + 1]
        if not local_times:
            return
        pos = bisect_left(local_times, hovered)
        if pos <= 0:
            nearest_idx = self.start_index
        elif pos >= len(local_times):
            nearest_idx = self.end_index
        else:
            before = local_times[pos - 1]
            after = local_times[pos]
            nearest_idx = self.start_index + (pos - 1 if hovered - before <= after - hovered else pos)

        lines = [format_dt(self.times[nearest_idx])]
        for col in columns[:8]:
            value = self.data_by_column[col][nearest_idx]
            if not math.isnan(value):
                lines.append(f"{col}: {value:g}")
        if len(columns) > 8:
            lines.append(f"... 还有 {len(columns) - 8} 列")

        anchor_value = self.data_by_column[columns[0]][nearest_idx]
        if math.isnan(anchor_value):
            anchor_value = 0
        if self._hover_annotation is None:
            self._hover_annotation = self.ax.annotate(
                "",
                xy=(0, 0),
                xytext=(15, 15),
                textcoords="offset points",
                bbox=dict(boxstyle="round,pad=0.4", fc="white", ec="#cbd5e1", alpha=0.95),
                fontsize=8,
            )
        self._hover_annotation.xy = (self.times[nearest_idx], anchor_value)
        self._hover_annotation.set_text("\n".join(lines))
        self._hover_annotation.set_visible(True)
        self.canvas.draw_idle()


def main():
    root = Tk()
    CsvChartViewer(root)
    root.mainloop()


if __name__ == "__main__":
    main()
