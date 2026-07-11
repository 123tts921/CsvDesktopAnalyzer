// 诊断 y 轴：列出每个数值列的真实 min/max，定位把轴拉高的列
// 用法: node tests/consistency/inspect_yaxis.mjs <你的csv路径>
import { readFileSync } from 'node:fs';
import { parseCsv, decodeBuffer } from '../../src/lib/csvParse.js';

const path = process.argv[2];
if (!path) {
  console.error('用法: node tests/consistency/inspect_yaxis.mjs <csv路径>');
  process.exit(1);
}
const buf = new Uint8Array(readFileSync(path));
const text = decodeBuffer(buf);
const r = parseCsv(text, undefined);

console.log(`时间列: ${r.timeColumn} | 有效行数: ${r.rowCount}`);
console.log('数值列及真实范围 (按最大值降序):');
const rows = r.numericColumns
  .map((n) => {
    const vals = (r.valuesByColumn[n] || []).filter(
      (v) => v != null && !Number.isNaN(v)
    );
    const min = vals.length ? Math.min(...vals) : NaN;
    const max = vals.length ? Math.max(...vals) : NaN;
    return { n, min, max };
  })
  .sort((a, b) => b.max - a.max);

for (const { n, min, max } of rows) {
  console.log(`  ${n.padEnd(22)} min=${min}  max=${max}`);
}
