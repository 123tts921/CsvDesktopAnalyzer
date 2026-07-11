import { describe, it, expect } from 'vitest';
import {
  decodeBuffer,
  parseDateTime,
  parseDouble,
  resolveTimeColumn,
  parseCsv,
} from '../src/lib/csvParse.js';

// —— decodeBuffer（spec 5.2）——
describe('decodeBuffer', () => {
  it('UTF-8 BOM 被正确剥离并解码', () => {
    const bytes = new Uint8Array([0xef, 0xbb, 0xbf, ...new TextEncoder().encode('时间,温度')]);
    expect(decodeBuffer(bytes)).toBe('时间,温度');
  });

  it('无 BOM 的 UTF-8 中文可解码', () => {
    const bytes = new TextEncoder().encode('时间,温度,压力');
    expect(decodeBuffer(bytes)).toContain('时间');
  });

  it('ASCII 回退为 UTF-8 且不出错', () => {
    const bytes = new TextEncoder().encode('a,b,c');
    expect(decodeBuffer(bytes)).toBe('a,b,c');
  });
});

// —— parseDateTime（spec 5.1：11 种格式 + 宽松回退）——
describe('parseDateTime', () => {
  const ymd = (ms) => {
    const d = new Date(ms);
    return [d.getFullYear(), d.getMonth() + 1, d.getDate()];
  };
  it('yyyy/M/d H:mm:ss', () => {
    const ms = parseDateTime('2023/1/5 3:04:05');
    expect(ms).not.toBeNull();
    expect(ymd(ms)).toEqual([2023, 1, 5]);
  });
  it('yyyy-MM-dd HH:mm', () => {
    const ms = parseDateTime('2023-01-05 03:04');
    expect(ms).not.toBeNull();
    expect(ymd(ms)).toEqual([2023, 1, 5]);
  });
  it('yyyy-MM-dd HH:mm:ss 带秒', () => {
    const ms = parseDateTime('2023-12-31 23:59:59');
    expect(ms).not.toBeNull();
    expect(ymd(ms)).toEqual([2023, 12, 31]);
  });
  it('仅日期 yyyy-MM-dd', () => {
    const ms = parseDateTime('2023-06-15');
    expect(ms).not.toBeNull();
    expect(ymd(ms)).toEqual([2023, 6, 15]);
  });
  it('无效时间返回 null', () => {
    expect(parseDateTime('not-a-time')).toBeNull();
    expect(parseDateTime('')).toBeNull();
    expect(parseDateTime(null)).toBeNull();
  });
});

// —— parseDouble（spec 5.4：去逗号 + invariant 浮点）——
describe('parseDouble', () => {
  it('去逗号后解析', () => {
    expect(parseDouble('1,234.5')).toBe(1234.5);
  });
  it('普通数字', () => {
    expect(parseDouble('  -3.2 ')).toBe(-3.2);
  });
  it('非数字返回 null', () => {
    expect(parseDouble('abc')).toBeNull();
    expect(parseDouble('')).toBeNull();
  });
});

// —— resolveTimeColumn（spec 5.3）——
describe('resolveTimeColumn', () => {
  it('精确匹配「时间」', () => {
    expect(resolveTimeColumn(['时间', 'A', 'B'])).toBe('时间');
  });
  it('模糊匹配「日期」', () => {
    expect(resolveTimeColumn(['日期', 'A'])).toBe('日期');
  });
  it('模糊匹配英文 time', () => {
    expect(resolveTimeColumn(['timestamp', 'A'])).toBe('timestamp');
  });
  it('指定列名在表头中则优先使用', () => {
    expect(resolveTimeColumn(['col1', 'col2'], 'col2')).toBe('col2');
  });
  it('无任何匹配时取表头第一列', () => {
    expect(resolveTimeColumn(['col1', 'col2'])).toBe('col1');
  });
  it('模糊匹配取第一个包含时间/日期的列（spec 5.3 规则3）', () => {
    // 「A时间」先于「日期」，且包含「时间」，应为首匹配
    expect(resolveTimeColumn(['A时间', '日期'])).toBe('A时间');
  });
});

// —— parseCsv（spec 5.3 / 5.4 全链路）——
describe('parseCsv', () => {
  const csv = [
    '时间,温度,压力,备注',
    '2023-01-01 00:00,20.1,101,x',
    '2023-01-01 00:01,20.5,102,y',
    '2023-01-01 00:02,21.0,103,z',
    '2023-01-01 00:03,notnum,104,w',
  ].join('\n');

  it('识别时间列与数值列（排除时间列与非数值列）', () => {
    const r = parseCsv(csv);
    expect(r.timeColumn).toBe('时间');
    expect(r.numericColumns).toEqual(['温度', '压力']);
    expect(r.numericColumns).not.toContain('时间');
    expect(r.numericColumns).not.toContain('备注');
  });

  it('缓存行数与时间范围', () => {
    const r = parseCsv(csv);
    expect(r.rowCount).toBe(4);
    expect(r.timestamps.length).toBe(4);
    expect(r.minTime).toBeLessThanOrEqual(r.maxTime);
    // 首行时间
    const d = new Date(r.timestamps[0]);
    expect([d.getFullYear(), d.getMonth() + 1, d.getDate()]).toEqual([2023, 1, 1]);
  });

  it('数值列解析对 null（非数值）保留为 null', () => {
    const r = parseCsv(csv);
    // 第 4 行「温度」= notnum → null
    expect(r.valuesByColumn['温度'][3]).toBeNull();
    expect(r.valuesByColumn['压力'][3]).toBe(104);
  });

  it('无表头抛错', () => {
    expect(() => parseCsv('')).toThrow();
  });
});
