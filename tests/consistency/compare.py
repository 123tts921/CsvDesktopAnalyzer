#!/usr/bin/env python3
"""一致性对照：原始 PySide 版解析逻辑 vs 新 JS(Electron) 版解析逻辑。

原始实现见 desktop_csv_analyzer.py 的 CsvDataBackend.load_csv：
  - 时间列：requested → 首个包含 时间/日期/time/date 的列 → 第一列
  - 数值列：对每个非时间列，取前 800 行（且时间可解析）中可转 double 的行数，>0 即数值
  - 时间范围：min/max
这里用标准库忠实复刻上述判定（避免引入 duckdb/PySide 依赖），
再调用 js_parse.mjs 得到新实现结果，逐项比对。
"""
import csv
import json
import os
import subprocess
import sys
from datetime import datetime

TIME_FORMATS = (
    "%Y/%m/%d %H:%M:%S",
    "%Y/%m/%d %H:%M",
    "%Y-%m-%d %H:%M:%S",
    "%Y-%m-%d %H:%M",
    "%Y/%m/%d",
    "%Y-%m-%d",
)


def parse_dt(s):
    s = (s or "").strip()
    if not s:
        return None
    for fmt in TIME_FORMATS:
        try:
            return datetime.strptime(s, fmt)
        except ValueError:
            pass
    return None


def detect_time_column(headers, requested):
    if requested and requested in headers:
        return requested
    for name in headers:
        low = name.lower()
        if name == "时间" or "时间" in name or "日期" in name or "time" in low or "date" in low:
            return name
    return headers[0]


def original_parse(path):
    with open(path, newline="", encoding="utf-8-sig") as h:
        reader = csv.reader(h)
        headers = [x.strip() for x in next(reader)]
        rows = list(reader)

    tc = detect_time_column(headers, None)
    idx = headers.index(tc)

    numeric = []
    for i, name in enumerate(headers):
        if i == idx:
            continue
        cnt = 0
        scanned = 0
        for row in rows:
            if scanned >= 800:
                break
            if parse_dt(row[idx]) is None:
                continue
            try:
                float((row[i] if i < len(row) else "").replace(",", ""))
                cnt += 1
            except ValueError:
                pass
            scanned += 1
        if cnt > 0:
            numeric.append(name)

    times = [parse_dt(row[idx]) for row in rows if parse_dt(row[idx]) is not None]
    return {
        "timeColumn": tc,
        "numericColumns": sorted(numeric),
        "rowCount": len(times),
        "minTime": min(times).strftime("%Y-%m-%d %H:%M:%S"),
        "maxTime": max(times).strftime("%Y-%m-%d %H:%M:%S"),
    }


def main():
    if len(sys.argv) < 2:
        print("用法: python3 compare.py <sample.csv>")
        sys.exit(2)
    path = sys.argv[1]
    py = original_parse(path)
    out = subprocess.check_output(
        ["node", os.path.join(os.path.dirname(__file__), "js_parse.mjs"), path]
    )
    js = json.loads(out)

    print("原始 PySide 版 :", py)
    print("新版 Electron  :", js)

    ok = py == js
    print("一致性判定 :", "PASS" if ok else "FAIL")
    if not ok:
        for k in py:
            if py[k] != js.get(k):
                print(f"  差异字段 {k}: py={py[k]} | js={js.get(k)}")
        sys.exit(1)


if __name__ == "__main__":
    main()
