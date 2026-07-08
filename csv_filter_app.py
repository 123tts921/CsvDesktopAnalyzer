import csv
import os
import queue
import threading
from datetime import datetime
from tkinter import (
    BOTH,
    END,
    EXTENDED,
    HORIZONTAL,
    LEFT,
    RIGHT,
    VERTICAL,
    W,
    X,
    Y,
    BooleanVar,
    Canvas,
    Listbox,
    StringVar,
    Tk,
    filedialog,
    font,
    messagebox,
)
from tkinter import ttk


DATE_FORMATS = (
    "%Y/%m/%d %H:%M:%S",
    "%Y/%m/%d %H:%M",
    "%Y-%m-%d %H:%M:%S",
    "%Y-%m-%d %H:%M",
    "%Y/%m/%d",
    "%Y-%m-%d",
)

COLORS = {
    "bg": "#f4f7fb",
    "panel": "#ffffff",
    "panel_soft": "#f8fafc",
    "text": "#172033",
    "muted": "#667085",
    "line": "#d9e2ec",
    "accent": "#0f766e",
    "accent_dark": "#0b5f59",
    "accent_soft": "#d8f3ef",
    "blue": "#2563eb",
    "danger": "#b42318",
    "tree_heading": "#eef3f8",
    "selection": "#cdebe6",
}


def parse_datetime(value):
    text = (value or "").strip()
    if not text:
        return None
    for fmt in DATE_FORMATS:
        try:
            return datetime.strptime(text, fmt)
        except ValueError:
            pass
    return None


def detect_encoding(path):
    for encoding in ("utf-8-sig", "utf-8", "gb18030"):
        try:
            with open(path, "r", encoding=encoding, newline="") as handle:
                handle.read(4096)
            return encoding
        except UnicodeDecodeError:
            continue
    return "utf-8-sig"


def compare_value(raw, operator, expected):
    actual = (raw or "").strip()
    expected = (expected or "").strip()

    if operator == "包含":
        return expected in actual
    if operator == "不包含":
        return expected not in actual
    if operator == "等于":
        return actual == expected
    if operator == "不等于":
        return actual != expected
    if operator == "为空":
        return actual == ""
    if operator == "非空":
        return actual != ""

    try:
        left = float(actual)
        right = float(expected)
    except ValueError:
        return False

    if operator == ">":
        return left > right
    if operator == ">=":
        return left >= right
    if operator == "<":
        return left < right
    if operator == "<=":
        return left <= right
    return True


class CsvFilterApp:
    def __init__(self, root):
        self.root = root
        self.root.title("CSV 数据筛选导出工具")
        self.root.geometry("1240x720")
        self.root.minsize(1000, 560)
        self.root.configure(bg=COLORS["bg"])

        self.file_path = StringVar()
        self.encoding = StringVar(value="utf-8-sig")
        self.time_column = StringVar()
        self.start_time = StringVar()
        self.end_time = StringVar()
        self.column_search = StringVar()
        self.condition_column = StringVar()
        self.condition_operator = StringVar(value="包含")
        self.condition_value = StringVar()
        self.output_path = StringVar()
        self.include_bom = BooleanVar(value=True)
        self.status = StringVar(value="请选择 CSV 文件。")
        self.progress_text = StringVar(value="")
        self.summary_text = StringVar(value="未载入数据")

        self.headers = []
        self.visible_headers = []
        self.selected_columns = set()
        self.conditions = []
        self.worker_queue = queue.Queue()

        self._setup_style()
        self._build_ui()
        self._poll_worker_queue()

    def _setup_style(self):
        style = ttk.Style()
        try:
            style.theme_use("clam")
        except Exception:
            pass

        default_font = font.nametofont("TkDefaultFont")
        default_font.configure(family="Microsoft YaHei UI", size=10)
        self.root.option_add("*Font", default_font)

        style.configure(".", background=COLORS["bg"], foreground=COLORS["text"])
        style.configure("App.TFrame", background=COLORS["bg"])
        style.configure("Card.TFrame", background=COLORS["panel"], relief="flat")
        style.configure("Soft.TFrame", background=COLORS["panel_soft"], relief="flat")
        style.configure("Header.TFrame", background=COLORS["text"])
        style.configure("Status.TFrame", background=COLORS["panel"])

        style.configure("Title.TLabel", background=COLORS["text"], foreground="#ffffff", font=("Microsoft YaHei UI", 18, "bold"))
        style.configure("Subtitle.TLabel", background=COLORS["text"], foreground="#cbd5e1", font=("Microsoft YaHei UI", 10))
        style.configure("Section.TLabel", background=COLORS["panel"], foreground=COLORS["text"], font=("Microsoft YaHei UI", 11, "bold"))
        style.configure("Muted.TLabel", background=COLORS["panel"], foreground=COLORS["muted"], font=("Microsoft YaHei UI", 9))
        style.configure("SoftMuted.TLabel", background=COLORS["panel_soft"], foreground=COLORS["muted"], font=("Microsoft YaHei UI", 9))
        style.configure("Status.TLabel", background=COLORS["panel"], foreground=COLORS["muted"])

        style.configure("TEntry", fieldbackground="#ffffff", bordercolor=COLORS["line"], lightcolor=COLORS["line"], darkcolor=COLORS["line"], padding=7)
        style.configure("TCombobox", fieldbackground="#ffffff", bordercolor=COLORS["line"], lightcolor=COLORS["line"], darkcolor=COLORS["line"], padding=6)
        style.configure("TCheckbutton", background=COLORS["panel"], foreground=COLORS["text"])

        style.configure("TButton", padding=(12, 8), borderwidth=0, focusthickness=0)
        style.map("TButton", background=[("active", "#e8eef5")])
        style.configure("Accent.TButton", background=COLORS["accent"], foreground="#ffffff", padding=(14, 9), font=("Microsoft YaHei UI", 10, "bold"))
        style.map("Accent.TButton", background=[("active", COLORS["accent_dark"]), ("pressed", COLORS["accent_dark"])], foreground=[("active", "#ffffff")])
        style.configure("Secondary.TButton", background="#e8eef5", foreground=COLORS["text"], padding=(12, 8))
        style.map("Secondary.TButton", background=[("active", "#dbe5ee")])
        style.configure("Danger.TButton", background="#fee4e2", foreground=COLORS["danger"], padding=(12, 8))
        style.map("Danger.TButton", background=[("active", "#ffd7d2")])

        style.configure("Treeview", background="#ffffff", fieldbackground="#ffffff", foreground=COLORS["text"], rowheight=28, borderwidth=0)
        style.map("Treeview", background=[("selected", COLORS["selection"])], foreground=[("selected", COLORS["text"])])
        style.configure("Treeview.Heading", background=COLORS["tree_heading"], foreground=COLORS["text"], relief="flat", padding=(8, 7), font=("Microsoft YaHei UI", 10, "bold"))
        style.map("Treeview.Heading", background=[("active", "#e0e8f1")])
        style.configure("Horizontal.TScrollbar", background=COLORS["panel"], troughcolor=COLORS["bg"], bordercolor=COLORS["bg"], arrowcolor=COLORS["muted"])
        style.configure("Vertical.TScrollbar", background=COLORS["panel"], troughcolor=COLORS["bg"], bordercolor=COLORS["bg"], arrowcolor=COLORS["muted"])

    def _build_ui(self):
        root_frame = ttk.Frame(self.root, style="App.TFrame", padding=14)
        root_frame.pack(fill=BOTH, expand=True)

        self._build_header(root_frame)

        main = ttk.PanedWindow(root_frame, orient=HORIZONTAL)
        main.pack(fill=BOTH, expand=True, pady=(12, 10))

        left_outer, left_panel = self._build_scrollable_panel(main)
        right_panel = ttk.Frame(main, style="App.TFrame")
        main.add(left_outer, weight=1)
        main.add(right_panel, weight=3)

        self._build_column_panel(left_panel)
        self._build_filter_panel(left_panel)
        self._build_preview_panel(right_panel)
        self._build_status_bar(root_frame)

    def _build_scrollable_panel(self, parent):
        outer = ttk.Frame(parent, style="App.TFrame")
        canvas = Canvas(outer, bg=COLORS["bg"], borderwidth=0, highlightthickness=0)
        scrollbar = ttk.Scrollbar(outer, orient=VERTICAL, command=canvas.yview)
        content = ttk.Frame(canvas, style="App.TFrame")
        content_id = canvas.create_window((0, 0), window=content, anchor="nw")

        def update_scrollregion(_event=None):
            canvas.configure(scrollregion=canvas.bbox("all"))

        def update_content_width(event):
            canvas.itemconfigure(content_id, width=event.width)

        def on_mousewheel(event):
            canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")

        content.bind("<Configure>", update_scrollregion)
        canvas.bind("<Configure>", update_content_width)
        canvas.bind("<Enter>", lambda _event: canvas.bind_all("<MouseWheel>", on_mousewheel))
        canvas.bind("<Leave>", lambda _event: canvas.unbind_all("<MouseWheel>"))
        canvas.configure(yscrollcommand=scrollbar.set)

        canvas.pack(side=LEFT, fill=BOTH, expand=True)
        scrollbar.pack(side=RIGHT, fill=Y)
        return outer, content

    def _build_header(self, parent):
        header = ttk.Frame(parent, style="Header.TFrame", padding=(18, 16))
        header.pack(fill=X)

        title_block = ttk.Frame(header, style="Header.TFrame")
        title_block.pack(side=LEFT, fill=X, expand=True)
        ttk.Label(title_block, text="CSV 数据筛选导出工具", style="Title.TLabel").pack(anchor=W)
        ttk.Label(title_block, text="面向大体量运行数据的列筛选、时间筛选、条件筛选与 CSV 导出", style="Subtitle.TLabel").pack(anchor=W, pady=(4, 0))

        actions = ttk.Frame(header, style="Header.TFrame")
        actions.pack(side=RIGHT)
        ttk.Button(actions, text="选择 CSV", style="Accent.TButton", command=self.select_file).pack(side=LEFT, padx=(0, 8))
        ttk.Button(actions, text="读取表头", style="Secondary.TButton", command=self.load_file).pack(side=LEFT)

        file_bar = ttk.Frame(parent, style="Card.TFrame", padding=(14, 10))
        file_bar.pack(fill=X, pady=(10, 0))
        ttk.Label(file_bar, text="当前文件", style="Muted.TLabel").pack(side=LEFT, padx=(0, 10))
        ttk.Entry(file_bar, textvariable=self.file_path).pack(side=LEFT, fill=X, expand=True)
        ttk.Label(file_bar, textvariable=self.summary_text, style="Muted.TLabel").pack(side=RIGHT, padx=(12, 0))

    def _build_column_panel(self, parent):
        frame = ttk.Frame(parent, style="Card.TFrame", padding=14)
        frame.pack(fill=BOTH, expand=True)

        top = ttk.Frame(frame, style="Card.TFrame")
        top.pack(fill=X, pady=(0, 10))
        ttk.Label(top, text="输出列", style="Section.TLabel").pack(side=LEFT)
        ttk.Label(top, text="搜索后只影响当前列表显示", style="Muted.TLabel").pack(side=RIGHT)

        search_row = ttk.Frame(frame, style="Card.TFrame")
        search_row.pack(fill=X, pady=(0, 10))
        ttk.Entry(search_row, textvariable=self.column_search).pack(side=LEFT, fill=X, expand=True)
        self.column_search.set("")
        search_row.bind("<KeyRelease>", lambda _event: self.refresh_column_list())

        self.column_search.trace_add("write", lambda *_args: self.refresh_column_list())

        list_row = ttk.Frame(frame, style="Card.TFrame")
        list_row.pack(fill=BOTH, expand=True)
        self.column_listbox = Listbox(
            list_row,
            selectmode=EXTENDED,
            exportselection=False,
            activestyle="none",
            borderwidth=0,
            highlightthickness=1,
            highlightbackground=COLORS["line"],
            highlightcolor=COLORS["accent"],
            background="#ffffff",
            foreground=COLORS["text"],
            selectbackground=COLORS["selection"],
            selectforeground=COLORS["text"],
            font=("Microsoft YaHei UI", 10),
        )
        self.column_listbox.pack(side=LEFT, fill=BOTH, expand=True)
        self.column_listbox.bind("<<ListboxSelect>>", lambda _event: self.sync_visible_column_selection())
        scrollbar = ttk.Scrollbar(list_row, orient=VERTICAL, command=self.column_listbox.yview)
        scrollbar.pack(side=RIGHT, fill=Y)
        self.column_listbox.configure(yscrollcommand=scrollbar.set)

        button_row = ttk.Frame(frame, style="Card.TFrame")
        button_row.pack(fill=X, pady=(12, 0))
        ttk.Button(button_row, text="全选", style="Secondary.TButton", command=self.select_all_columns).pack(side=LEFT)
        ttk.Button(button_row, text="清空", style="Danger.TButton", command=self.clear_columns).pack(side=LEFT, padx=8)
        ttk.Button(button_row, text="反选", style="Secondary.TButton", command=self.invert_columns).pack(side=LEFT)

    def _build_filter_panel(self, parent):
        frame = ttk.Frame(parent, style="Card.TFrame", padding=14)
        frame.pack(fill=X, pady=(12, 0))

        ttk.Label(frame, text="筛选条件", style="Section.TLabel").grid(row=0, column=0, columnspan=4, sticky=W, pady=(0, 10))

        ttk.Label(frame, text="时间列", style="Muted.TLabel").grid(row=1, column=0, sticky=W, pady=5)
        self.time_combo = ttk.Combobox(frame, textvariable=self.time_column, state="readonly")
        self.time_combo.grid(row=1, column=1, columnspan=3, sticky="ew", padx=(10, 0), pady=5)

        ttk.Label(frame, text="开始时间", style="Muted.TLabel").grid(row=2, column=0, sticky=W, pady=5)
        ttk.Entry(frame, textvariable=self.start_time).grid(row=2, column=1, columnspan=3, sticky="ew", padx=(10, 0), pady=5)
        ttk.Label(frame, text="结束时间", style="Muted.TLabel").grid(row=3, column=0, sticky=W, pady=5)
        ttk.Entry(frame, textvariable=self.end_time).grid(row=3, column=1, columnspan=3, sticky="ew", padx=(10, 0), pady=5)

        ttk.Separator(frame).grid(row=4, column=0, columnspan=4, sticky="ew", pady=12)

        ttk.Label(frame, text="条件列", style="Muted.TLabel").grid(row=5, column=0, sticky=W, pady=5)
        self.condition_column_combo = ttk.Combobox(frame, textvariable=self.condition_column, state="readonly")
        self.condition_column_combo.grid(row=5, column=1, columnspan=3, sticky="ew", padx=(10, 0), pady=5)

        ttk.Label(frame, text="规则", style="Muted.TLabel").grid(row=6, column=0, sticky=W, pady=5)
        ttk.Combobox(
            frame,
            textvariable=self.condition_operator,
            state="readonly",
            values=("包含", "不包含", "等于", "不等于", "为空", "非空", ">", ">=", "<", "<="),
            width=10,
        ).grid(row=6, column=1, sticky="ew", padx=(10, 8), pady=5)
        ttk.Label(frame, text="值", style="Muted.TLabel").grid(row=6, column=2, sticky=W, pady=5)
        ttk.Entry(frame, textvariable=self.condition_value).grid(row=6, column=3, sticky="ew", padx=(8, 0), pady=5)

        condition_actions = ttk.Frame(frame, style="Card.TFrame")
        condition_actions.grid(row=7, column=0, columnspan=4, sticky="ew", pady=(8, 10))
        ttk.Button(condition_actions, text="添加条件", style="Secondary.TButton", command=self.add_condition).pack(side=LEFT, fill=X, expand=True)
        ttk.Button(condition_actions, text="删除条件", style="Danger.TButton", command=self.remove_condition).pack(side=LEFT, fill=X, expand=True, padx=(8, 0))

        self.condition_listbox = Listbox(
            frame,
            height=5,
            exportselection=False,
            borderwidth=0,
            highlightthickness=1,
            highlightbackground=COLORS["line"],
            highlightcolor=COLORS["accent"],
            background=COLORS["panel_soft"],
            foreground=COLORS["text"],
            selectbackground=COLORS["selection"],
            selectforeground=COLORS["text"],
            font=("Microsoft YaHei UI", 10),
        )
        self.condition_listbox.grid(row=8, column=0, columnspan=4, sticky="ew", pady=(0, 10))

        ttk.Checkbutton(frame, text="导出 UTF-8 BOM，便于 Excel 正确打开中文", variable=self.include_bom).grid(
            row=9, column=0, columnspan=4, sticky=W, pady=(0, 12)
        )

        action_row = ttk.Frame(frame, style="Card.TFrame")
        action_row.grid(row=10, column=0, columnspan=4, sticky="ew")
        ttk.Button(action_row, text="预览筛选结果", style="Secondary.TButton", command=self.preview_rows).pack(side=LEFT, fill=X, expand=True)
        ttk.Button(action_row, text="导出 CSV", style="Accent.TButton", command=self.export_csv).pack(side=LEFT, fill=X, expand=True, padx=(8, 0))

        frame.columnconfigure(1, weight=1)
        frame.columnconfigure(3, weight=1)

    def _build_preview_panel(self, parent):
        frame = ttk.Frame(parent, style="Card.TFrame", padding=14)
        frame.pack(fill=BOTH, expand=True)

        top = ttk.Frame(frame, style="Card.TFrame")
        top.pack(fill=X, pady=(0, 10))
        ttk.Label(top, text="数据预览", style="Section.TLabel").pack(side=LEFT)
        ttk.Label(top, text="最多显示前 500 行，导出时会处理完整文件", style="Muted.TLabel").pack(side=RIGHT)

        table_frame = ttk.Frame(frame, style="Card.TFrame")
        table_frame.pack(fill=BOTH, expand=True)
        self.preview_tree = ttk.Treeview(table_frame, show="headings")
        y_scroll = ttk.Scrollbar(table_frame, orient=VERTICAL, command=self.preview_tree.yview)
        x_scroll = ttk.Scrollbar(table_frame, orient=HORIZONTAL, command=self.preview_tree.xview)
        self.preview_tree.configure(yscrollcommand=y_scroll.set, xscrollcommand=x_scroll.set)

        self.preview_tree.grid(row=0, column=0, sticky="nsew")
        y_scroll.grid(row=0, column=1, sticky="ns")
        x_scroll.grid(row=1, column=0, sticky="ew")

        table_frame.rowconfigure(0, weight=1)
        table_frame.columnconfigure(0, weight=1)

    def _build_status_bar(self, parent):
        bottom = ttk.Frame(parent, style="Status.TFrame", padding=(12, 9))
        bottom.pack(fill=X)
        ttk.Label(bottom, textvariable=self.status, style="Status.TLabel").pack(side=LEFT, fill=X, expand=True)
        ttk.Label(bottom, textvariable=self.progress_text, style="Status.TLabel").pack(side=RIGHT)

    def select_file(self):
        path = filedialog.askopenfilename(
            title="选择 CSV 文件",
            filetypes=(("CSV 文件", "*.csv"), ("所有文件", "*.*")),
        )
        if path:
            self.file_path.set(path)
            self.output_path.set("")
            self.load_file()

    def load_file(self):
        path = self.file_path.get().strip()
        if not path or not os.path.exists(path):
            messagebox.showwarning("提示", "请先选择有效的 CSV 文件。")
            return

        self.status.set("正在读取表头和时间范围...")
        self.summary_text.set(os.path.basename(path))
        self.root.update_idletasks()

        try:
            encoding = detect_encoding(path)
            with open(path, "r", encoding=encoding, newline="") as handle:
                reader = csv.reader(handle)
                headers = next(reader)
        except Exception as exc:
            messagebox.showerror("读取失败", str(exc))
            self.status.set("读取失败。")
            return

        self.encoding.set(encoding)
        self.headers = [header.strip() for header in headers]
        self.visible_headers = list(self.headers)
        self.selected_columns = set(self.headers)
        self.time_combo["values"] = self.headers
        self.condition_column_combo["values"] = self.headers

        default_time = next((name for name in self.headers if name in ("时间", "日期", "datetime", "time")), self.headers[0])
        self.time_column.set(default_time)
        self.condition_column.set(self.headers[0])
        self.refresh_column_list()

        threading.Thread(target=self._scan_time_range, args=(path, encoding, default_time), daemon=True).start()

    def _scan_time_range(self, path, encoding, time_column):
        min_time = None
        max_time = None
        total = 0
        try:
            with open(path, "r", encoding=encoding, newline="") as handle:
                reader = csv.DictReader(handle)
                for row in reader:
                    total += 1
                    value = parse_datetime(row.get(time_column))
                    if value is None:
                        continue
                    min_time = value if min_time is None or value < min_time else min_time
                    max_time = value if max_time is None or value > max_time else max_time
                    if total % 20000 == 0:
                        self.worker_queue.put(("progress", f"已扫描 {total:,} 行"))
            self.worker_queue.put(("time_range", min_time, max_time, total))
        except Exception as exc:
            self.worker_queue.put(("error", f"扫描时间范围失败：{exc}"))

    def refresh_column_list(self):
        if not hasattr(self, "column_listbox"):
            return
        keyword = self.column_search.get().strip().lower()
        self.visible_headers = [name for name in self.headers if keyword in name.lower()]
        self.column_listbox.delete(0, END)
        for index, name in enumerate(self.visible_headers):
            self.column_listbox.insert(END, name)
            if name in self.selected_columns:
                self.column_listbox.selection_set(index)

    def sync_visible_column_selection(self):
        visible_set = set(self.visible_headers)
        selected_visible = {self.visible_headers[index] for index in self.column_listbox.curselection()}
        self.selected_columns.difference_update(visible_set)
        self.selected_columns.update(selected_visible)

    def get_selected_columns(self):
        self.sync_visible_column_selection()
        return [name for name in self.headers if name in self.selected_columns]

    def select_all_columns(self):
        self.selected_columns.update(self.visible_headers)
        self.column_listbox.selection_set(0, END)

    def clear_columns(self):
        self.selected_columns.difference_update(self.visible_headers)
        self.column_listbox.selection_clear(0, END)

    def invert_columns(self):
        visible_set = set(self.visible_headers)
        selected_visible = {self.visible_headers[index] for index in self.column_listbox.curselection()}
        self.selected_columns.difference_update(visible_set)
        self.selected_columns.update(visible_set - selected_visible)
        self.refresh_column_list()

    def add_condition(self):
        column = self.condition_column.get()
        operator = self.condition_operator.get()
        value = self.condition_value.get()
        if not column:
            messagebox.showwarning("提示", "请选择条件列。")
            return
        if operator not in ("为空", "非空") and value == "":
            messagebox.showwarning("提示", "请输入条件值。")
            return
        self.conditions.append((column, operator, value))
        self.condition_listbox.insert(END, f"{column} {operator} {value}".strip())
        self.condition_value.set("")

    def remove_condition(self):
        indexes = list(self.condition_listbox.curselection())
        for index in reversed(indexes):
            self.condition_listbox.delete(index)
            del self.conditions[index]

    def _build_filter_state(self):
        selected_columns = self.get_selected_columns()
        if not selected_columns:
            selected_columns = list(self.headers)

        start = parse_datetime(self.start_time.get())
        end = parse_datetime(self.end_time.get())
        if self.start_time.get().strip() and start is None:
            raise ValueError("开始时间格式无法识别，请使用 2025/2/1 0:00 或 2025-02-01 00:00。")
        if self.end_time.get().strip() and end is None:
            raise ValueError("结束时间格式无法识别，请使用 2025/3/1 0:00 或 2025-03-01 00:00。")
        if start and end and start > end:
            raise ValueError("开始时间不能晚于结束时间。")

        return {
            "path": self.file_path.get().strip(),
            "encoding": self.encoding.get(),
            "time_column": self.time_column.get(),
            "start": start,
            "end": end,
            "columns": selected_columns,
            "conditions": list(self.conditions),
        }

    def _row_matches(self, row, state):
        time_column = state["time_column"]
        if state["start"] or state["end"]:
            row_time = parse_datetime(row.get(time_column))
            if row_time is None:
                return False
            if state["start"] and row_time < state["start"]:
                return False
            if state["end"] and row_time > state["end"]:
                return False

        for column, operator, value in state["conditions"]:
            if not compare_value(row.get(column), operator, value):
                return False
        return True

    def preview_rows(self):
        try:
            state = self._build_filter_state()
        except ValueError as exc:
            messagebox.showwarning("筛选条件有误", str(exc))
            return

        threading.Thread(target=self._preview_worker, args=(state,), daemon=True).start()
        self.status.set("正在生成预览...")

    def _preview_worker(self, state):
        rows = []
        scanned = 0
        matched = 0
        try:
            with open(state["path"], "r", encoding=state["encoding"], newline="") as handle:
                reader = csv.DictReader(handle)
                for row in reader:
                    scanned += 1
                    if self._row_matches(row, state):
                        matched += 1
                        if len(rows) < 500:
                            rows.append([row.get(column, "") for column in state["columns"]])
                    if len(rows) >= 500 and scanned % 20000 == 0:
                        break
            self.worker_queue.put(("preview", state["columns"], rows, scanned, matched))
        except Exception as exc:
            self.worker_queue.put(("error", f"预览失败：{exc}"))

    def export_csv(self):
        try:
            state = self._build_filter_state()
        except ValueError as exc:
            messagebox.showwarning("筛选条件有误", str(exc))
            return

        path = filedialog.asksaveasfilename(
            title="保存筛选结果",
            defaultextension=".csv",
            initialfile="筛选结果.csv",
            filetypes=(("CSV 文件", "*.csv"), ("所有文件", "*.*")),
        )
        if not path:
            return

        state["output_path"] = path
        state["output_encoding"] = "utf-8-sig" if self.include_bom.get() else "utf-8"
        threading.Thread(target=self._export_worker, args=(state,), daemon=True).start()
        self.status.set("正在导出...")

    def _export_worker(self, state):
        scanned = 0
        matched = 0
        try:
            with open(state["path"], "r", encoding=state["encoding"], newline="") as source:
                reader = csv.DictReader(source)
                with open(state["output_path"], "w", encoding=state["output_encoding"], newline="") as target:
                    writer = csv.DictWriter(target, fieldnames=state["columns"], extrasaction="ignore")
                    writer.writeheader()
                    for row in reader:
                        scanned += 1
                        if self._row_matches(row, state):
                            matched += 1
                            writer.writerow({column: row.get(column, "") for column in state["columns"]})
                        if scanned % 10000 == 0:
                            self.worker_queue.put(("progress", f"已处理 {scanned:,} 行，匹配 {matched:,} 行"))
            self.worker_queue.put(("export_done", state["output_path"], scanned, matched))
        except Exception as exc:
            self.worker_queue.put(("error", f"导出失败：{exc}"))

    def _render_preview(self, columns, rows):
        self.preview_tree.delete(*self.preview_tree.get_children())
        self.preview_tree["columns"] = columns
        for column in columns:
            width = min(max(len(column) * 14, 96), 240)
            self.preview_tree.heading(column, text=column)
            self.preview_tree.column(column, width=width, minwidth=90, anchor=W)
        for row in rows:
            self.preview_tree.insert("", END, values=row)

    def _poll_worker_queue(self):
        try:
            while True:
                message = self.worker_queue.get_nowait()
                kind = message[0]
                if kind == "progress":
                    self.progress_text.set(message[1])
                elif kind == "time_range":
                    min_time, max_time, total = message[1], message[2], message[3]
                    if min_time:
                        self.start_time.set(min_time.strftime("%Y/%m/%d %H:%M"))
                    if max_time:
                        self.end_time.set(max_time.strftime("%Y/%m/%d %H:%M"))
                    self.summary_text.set(f"{len(self.headers)} 列 · {total:,} 行 · {self.encoding.get()}")
                    self.status.set(f"已读取 {len(self.headers)} 列，扫描 {total:,} 行。")
                    self.progress_text.set("")
                elif kind == "preview":
                    columns, rows, scanned, matched = message[1], message[2], message[3], message[4]
                    self._render_preview(columns, rows)
                    self.status.set(f"预览完成：扫描 {scanned:,} 行，匹配 {matched:,} 行，显示前 {len(rows)} 行。")
                    self.progress_text.set("")
                elif kind == "export_done":
                    path, scanned, matched = message[1], message[2], message[3]
                    self.status.set(f"导出完成：扫描 {scanned:,} 行，导出 {matched:,} 行。")
                    self.progress_text.set(path)
                    messagebox.showinfo("导出完成", f"已导出 {matched:,} 行到：\n{path}")
                elif kind == "error":
                    self.status.set(message[1])
                    self.progress_text.set("")
                    messagebox.showerror("错误", message[1])
        except queue.Empty:
            pass
        self.root.after(150, self._poll_worker_queue)


def main():
    root = Tk()
    CsvFilterApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
