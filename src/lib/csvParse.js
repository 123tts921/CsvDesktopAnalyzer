import Papa from 'papaparse';
import jschardet from 'jschardet';

// —— 编码解码（对齐 spec 5.2：BOM → UTF-8 → GB18030 回退）——
export function decodeBuffer(buffer) {
  const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
  if (bytes[0] === 0xef && bytes[1] === 0xbb && bytes[2] === 0xbf) {
    return new TextDecoder('utf-8').decode(bytes.subarray(3));
  }
  const latin1 = new TextDecoder('latin1').decode(bytes);
  let enc = 'utf-8';
  try {
    const det = jschardet.detect(latin1);
    const e = (det && det.encoding ? det.encoding : 'utf-8').toLowerCase();
    if (e.startsWith('utf-8') || e === 'ascii') enc = 'utf-8';
    else if (e.includes('gb') || e.includes('18030') || e.includes('gbk')) enc = 'gb18030';
  } catch (_) {
    /* 使用默认 utf-8 */
  }
  try {
    return new TextDecoder(enc).decode(bytes);
  } catch (_) {
    return new TextDecoder('utf-8').decode(bytes);
  }
}

// —— 时间解析（对齐 spec 5.1：11 种格式 + 宽松回退）——
export function parseDateTime(raw) {
  if (raw == null) return null;
  const s = String(raw).trim();
  if (!s) return null;
  const m = /^(\d{4})[-\/](\d{1,2})[-\/](\d{1,2})(?:[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?)?$/.exec(s);
  if (m) {
    const y = +m[1];
    const mo = +m[2];
    const d = +m[3];
    const hh = m[4] ? +m[4] : 0;
    const mi = m[5] ? +m[5] : 0;
    const ss = m[6] ? +m[6] : 0;
    const dt = new Date(y, mo - 1, d, hh, mi, ss);
    if (dt.getFullYear() === y && dt.getMonth() === mo - 1 && dt.getDate() === d) {
      return dt.getTime();
    }
  }
  const d2 = new Date(s);
  if (!Number.isNaN(d2.getTime())) return d2.getTime();
  return null;
}

// —— 数值解析（对齐 spec 5.4：去逗号 + invariant 浮点）——
export function parseDouble(raw) {
  if (raw == null) return null;
  const s = String(raw).trim().replace(/,/g, '');
  if (s === '') return null;
  const n = Number(s);
  return Number.isFinite(n) ? n : null;
}

// —— 时间列识别（对齐 spec 5.3）——
export function resolveTimeColumn(headers, requested) {
  const list = headers.map((h) => (h == null ? '' : String(h)));
  if (requested && list.includes(requested)) return requested;
  const exact = list.find((h) => h === '时间');
  if (exact) return exact;
  const fuzzy = list.find(
    (h) => /时间|日期/i.test(h) || /time|date/i.test(h)
  );
  return fuzzy || (list[0] || '');
}

// —— CSV 全量解析（对齐 spec 5.3 / 5.4）——
export function parseCsv(text, requestedTimeColumn) {
  const result = Papa.parse(text, { header: true, skipEmptyLines: true });
  const rows = result.data || [];
  const headers = (result.meta.fields || []).map((h) => (h == null ? '' : String(h).trim()));
  if (!headers.length) throw new Error('CSV 文件没有表头。');

  const timeColumn = resolveTimeColumn(headers, requestedTimeColumn);
  const timeIndex = headers.indexOf(timeColumn);
  if (timeIndex < 0) throw new Error('无法识别时间列。');

  // 数值列采样识别（前 1500 行打分）
  const sampleLimit = 1500;
  const score = {};
  headers.forEach((_h, i) => {
    if (i !== timeIndex) score[i] = 0;
  });
  let scanned = 0;
  let minTime = null;
  let maxTime = null;
  for (const row of rows) {
    const tv = parseDateTime(row[timeColumn]);
    if (tv == null) continue;
    if (minTime == null || tv < minTime) minTime = tv;
    if (maxTime == null || tv > maxTime) maxTime = tv;
    if (scanned < sampleLimit) {
      for (const i of Object.keys(score)) {
        if (parseDouble(row[headers[Number(i)]]) != null) score[i] += 1;
      }
      scanned += 1;
    }
  }
  if (minTime == null || maxTime == null) {
    throw new Error('无法识别时间列，请更换时间字段。');
  }
  const numericColumns = Object.keys(score)
    .filter((i) => score[i] > 0)
    .map((i) => headers[Number(i)]);

  // 全量解析
  const timestamps = [];
  const valuesByColumn = {};
  numericColumns.forEach((n) => {
    valuesByColumn[n] = [];
  });
  for (const row of rows) {
    const tv = parseDateTime(row[timeColumn]);
    if (tv == null) continue;
    timestamps.push(tv);
    for (const n of numericColumns) {
      valuesByColumn[n].push(parseDouble(row[n]));
    }
  }

  return {
    headers,
    timeColumn,
    numericColumns,
    timestamps,
    valuesByColumn,
    minTime,
    maxTime,
    rowCount: timestamps.length,
  };
}
