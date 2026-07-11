// 用新的 JS 解析实现（src/lib/csvParse.js）解析给定 CSV，输出 JSON。
// 字段与原始 PySide 版的 CsvDataBackend.load_csv 对齐，便于一致性比对。
import { readFileSync } from 'node:fs';
import { parseCsv } from '../../src/lib/csvParse.js';

const p = process.argv[2];
const text = readFileSync(p, 'utf8');
const r = parseCsv(text, undefined);

function fmt(ms) {
  const d = new Date(ms);
  const z = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${z(d.getMonth() + 1)}-${z(d.getDate())} ${z(d.getHours())}:${z(d.getMinutes())}:${z(d.getSeconds())}`;
}

console.log(
  JSON.stringify({
    timeColumn: r.timeColumn,
    numericColumns: [...r.numericColumns].sort(),
    rowCount: r.rowCount,
    minTime: fmt(r.minTime),
    maxTime: fmt(r.maxTime),
  })
);
