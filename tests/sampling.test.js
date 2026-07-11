import { describe, it, expect } from 'vitest';
import { downSample, resampleByInterval, buildSeries } from '../src/lib/sampling.js';

// —— downSample（spec 5.6）——
describe('downSample', () => {
  const pts = (n) => Array.from({ length: n }, (_, i) => ({ t: i, v: i }));

  it('空数组返回空', () => {
    expect(downSample([], 10)).toEqual([]);
  });

  it('count<=limit 原样返回', () => {
    const p = pts(5);
    expect(downSample(p, 10)).toHaveLength(5);
  });

  it('limit<=0 原样返回', () => {
    const p = pts(5);
    expect(downSample(p, 0)).toHaveLength(5);
  });

  it('等比抽样保留首尾且长度=limit', () => {
    const p = pts(10);
    const out = downSample(p, 4);
    expect(out).toHaveLength(4);
    expect(out[0]).toBe(p[0]);
    expect(out[3]).toBe(p[9]);
  });

  it('末尾点一定存在（limit=3,count=5）', () => {
    const p = pts(5);
    const out = downSample(p, 3);
    expect(out).toHaveLength(3);
    expect(out[out.length - 1]).toBe(p[4]);
  });

  it('抽样结果保持时间升序', () => {
    const p = pts(100);
    const out = downSample(p, 7);
    for (let i = 1; i < out.length; i += 1) {
      expect(out[i].t).toBeGreaterThan(out[i - 1].t);
    }
  });
});

// —— resampleByInterval（spec 5.6）——
describe('resampleByInterval', () => {
  const pts = (arr) => arr.map((t, i) => ({ t, v: i }));

  it('空数组返回空', () => {
    expect(resampleByInterval([], 1000)).toEqual([]);
  });

  it('interval<=0 原样返回', () => {
    const p = pts([1, 2, 3]);
    expect(resampleByInterval(p, 0)).toHaveLength(3);
  });

  it('每个桶只保留第一个点', () => {
    const p = pts([0, 100, 200, 700, 800]);
    const out = resampleByInterval(p, 500);
    expect(out.map((x) => x.t)).toEqual([0, 700]);
  });
});

// —— buildSeries（spec 5.6 前提：按时间升序 + 范围过滤 + 模式切换）——
describe('buildSeries', () => {
  const cache = {
    timestamps: [1000, 2000, 3000, 4000],
    valuesByColumn: {
      A: [1, 2, 3, 4],
      B: [5, 6, 7, 8],
    },
  };
  const selections = [
    { name: 'A', axis: 'Y1', chartType: '折线图' },
    { name: 'B', axis: 'Y2', chartType: '散点图' },
  ];

  it('按时间范围过滤', () => {
    const r = buildSeries(cache, {
      selections,
      start: 2000,
      end: 4000,
      displayMode: '固定点数',
      maxPoints: 2,
      intervalMs: 600000,
    });
    expect(r.A.xs).toEqual([2000, 4000]);
    expect(r.A.ys).toEqual([2, 4]);
  });

  it('固定点数模式调用 downSample', () => {
    const r = buildSeries(cache, {
      selections: [{ name: 'A', axis: 'Y1', chartType: '折线图' }],
      start: 1000,
      end: 4000,
      displayMode: '固定点数',
      maxPoints: 2,
      intervalMs: 600000,
    });
    // 4 点降到 2 点：等比取 [p0, p3]
    expect(r.A.xs).toEqual([1000, 4000]);
  });

  it('固定间隔模式调用 resampleByInterval（合并同桶）', () => {
    const r = buildSeries(cache, {
      selections: [{ name: 'A', axis: 'Y1', chartType: '折线图' }],
      start: 1000,
      end: 4000,
      displayMode: '固定时间间隔',
      maxPoints: 9999,
      intervalMs: 2000,
    });
    // 桶 floor(1000/2000)=0, 2000/2000=1, 3000/2000=1(合并), 4000/2000=2
    expect(r.A.xs).toEqual([1000, 2000, 4000]);
  });

  it('输出结果保持时间升序', () => {
    const shuffled = {
      timestamps: [4000, 1000, 3000, 2000],
      valuesByColumn: { A: [4, 1, 3, 2] },
    };
    const r = buildSeries(shuffled, {
      selections: [{ name: 'A', axis: 'Y1', chartType: '折线图' }],
      start: 0,
      end: 9999,
      displayMode: '固定点数',
      maxPoints: 100,
      intervalMs: 600000,
    });
    const xs = r.A.xs;
    for (let i = 1; i < xs.length; i += 1) {
      expect(xs[i]).toBeGreaterThan(xs[i - 1]);
    }
  });
});
