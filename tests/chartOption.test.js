import { describe, it, expect } from 'vitest';
import { buildOption, PALETTE } from '../src/lib/chartOption.js';

const seriesData = {
  A: { xs: [1000, 2000, 3000], ys: [1, 2, 3] },
  B: { xs: [1000, 2000, 3000], ys: [4, 5, 6] },
};

describe('buildOption（spec 5.7 / 5.8）', () => {
  it('无 Y2 时仅一个 Y 轴', () => {
    const opt = buildOption(
      [{ name: 'A', axis: 'Y1', chartType: '折线图' }],
      seriesData,
      { start: 1000, end: 3000, title: 't' }
    );
    expect(opt.yAxis).toHaveLength(1);
    expect(opt.yAxis[0].name).toBe('Y1');
  });

  it('含 Y2 时右轴存在且 series 轴索引正确', () => {
    const sels = [
      { name: 'A', axis: 'Y1', chartType: '折线图' },
      { name: 'B', axis: 'Y2', chartType: '散点图' },
    ];
    const opt = buildOption(sels, seriesData, { start: 1000, end: 3000, title: 't' });
    expect(opt.yAxis).toHaveLength(2);
    expect(opt.yAxis[1].position).toBe('right');
    expect(opt.series[0].yAxisIndex).toBe(0);
    expect(opt.series[1].yAxisIndex).toBe(1);
  });

  it('曲线颜色按调色板索引循环', () => {
    const sels = [
      { name: 'A', axis: 'Y1', chartType: '折线图' },
      { name: 'B', axis: 'Y2', chartType: '散点图' },
    ];
    const opt = buildOption(sels, seriesData, { start: 1000, end: 3000, title: 't' });
    expect(opt.series[0].itemStyle.color).toBe(PALETTE[0]);
    expect(opt.series[1].itemStyle.color).toBe(PALETTE[1]);
  });

  it('图表类型映射（spec 5.7 线宽/标记）', () => {
    const sels = [
      { name: 'A', axis: 'Y1', chartType: '折线图' },
      { name: 'B', axis: 'Y1', chartType: '点线图' },
      { name: 'A2', axis: 'Y1', chartType: '散点图' },
    ];
    const opt = buildOption(sels, seriesData, { start: 1000, end: 3000, title: 't' });
    const m = Object.fromEntries(opt.series.map((s) => [s.name, s]));
    // 折线图：无标记、线宽 2
    expect(m.A.showSymbol).toBe(false);
    expect(m.A.lineStyle.width).toBe(2);
    // 点线图：有标记、线宽 2
    expect(m.B.showSymbol).toBe(true);
    expect(m.B.lineStyle.width).toBe(2);
    // 散点图：线宽 0、有标记
    expect(m.A2.lineStyle.width).toBe(0);
    expect(m.A2.showSymbol).toBe(true);
  });

  it('标题与图例位置（spec 5.7 左上）', () => {
    const opt = buildOption(
      [{ name: 'A', axis: 'Y1', chartType: '折线图' }],
      seriesData,
      { start: 1000, end: 3000, title: '2023-01-01 00:00 至 2023-01-01 01:00' }
    );
    expect(opt.title.text).toBe('2023-01-01 00:00 至 2023-01-01 01:00');
    expect(opt.legend.left).toBe('left');
  });

  it('🆕 dataZoom 提供缩放/区间刷选', () => {
    const opt = buildOption(
      [{ name: 'A', axis: 'Y1', chartType: '折线图' }],
      seriesData,
      { start: 1000, end: 3000, title: 't' }
    );
    expect(opt.dataZoom).toEqual([
      { type: 'inside' },
      { type: 'slider', bottom: 20 },
    ]);
  });

  it('x 轴为时间类型并取时间范围 min/max', () => {
    const opt = buildOption(
      [{ name: 'A', axis: 'Y1', chartType: '折线图' }],
      seriesData,
      { start: 1000, end: 3000, title: 't' }
    );
    expect(opt.xAxis.type).toBe('time');
    expect(opt.xAxis.min).toBe(1000);
    expect(opt.xAxis.max).toBe(3000);
  });
});
