import { describe, it, expect, beforeAll, beforeEach } from 'vitest';

// —— 集成测试：验证 csv.worker 的消息契约（onmessage → postMessage）
// 以及 worker 内部 parseCsv 的端到端解析正确性（spec §4 / §5.3 / §5.4）。
// 在 node 环境下用 fake self 模拟 Worker 运行时。

const posted = [];
// 共享的 fake self：worker 模块加载时会赋值 self.onmessage
globalThis.self = {
  onmessage: null,
  postMessage: (m) => posted.push(m),
};

beforeAll(async () => {
  // 必须先设置 globalThis.self，再动态导入 worker（导入即挂载 onmessage）
  await import('../src/workers/csv.worker.js');
});

beforeEach(() => {
  posted.length = 0;
});

const SAMPLE = `时间,温度,压力
2024-01-01 00:00,20.5,101.2
2024-01-01 00:10,21.0,101.5
2024-01-01 00:20,19.8,100.9
2024-01-01 00:30,22.3,102.0`;

function dispatch(text, requestedTimeColumn) {
  expect(globalThis.self.onmessage).toBeTypeOf('function');
  globalThis.self.onmessage({ data: { text, requestedTimeColumn } });
  return posted[posted.length - 1];
}

describe('csv.worker 消息契约', () => {
  it('成功解析时返回 ok:true 且 payload 字段完整', () => {
    const msg = dispatch(SAMPLE);
    expect(msg.ok).toBe(true);
    const p = msg.payload;
    expect(p).toMatchObject({
      headers: ['时间', '温度', '压力'],
      timeColumn: '时间',
      numericColumns: ['温度', '压力'],
      rowCount: 4,
    });
    expect(p.timestamps).toHaveLength(4);
    expect(p.timestamps[0]).toBeLessThan(p.timestamps[3]);
    expect(p.valuesByColumn['温度']).toEqual([20.5, 21.0, 19.8, 22.3]);
    expect(p.minTime).toBe(p.timestamps[0]);
    expect(p.maxTime).toBe(p.timestamps[3]);
  });

  it('支持通过 requestedTimeColumn 指定时间列', () => {
    const alt = `日期,转速,流量
2024-02-01,1200,3.5
2024-02-02,1300,3.8`;
    const msg = dispatch(alt, '日期');
    expect(msg.ok).toBe(true);
    expect(msg.payload.timeColumn).toBe('日期');
    expect(msg.payload.numericColumns).toEqual(['转速', '流量']);
  });

  it('解析失败时返回 ok:false 并带 error 信息（spec §5.3 无法识别时间列）', () => {
    // 时间列内容为非日期文本 → 所有行 tv 为 null → minTime 为空 → 抛错
    const bad = `时间,值
abc,1.0
def,2.0`;
    const msg = dispatch(bad);
    expect(msg.ok).toBe(false);
    expect(typeof msg.error).toBe('string');
    expect(msg.error.length).toBeGreaterThan(0);
  });

  it('worker 仅通过 postMessage 回传，不污染返回值', () => {
    dispatch(SAMPLE);
    expect(posted).toHaveLength(1);
    expect(posted[0]).toHaveProperty('ok');
  });
});
