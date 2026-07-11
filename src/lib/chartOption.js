// ECharts option 构造（对齐 spec 5.7 / 5.8）
export const PALETTE = [
  '#2F80ED', '#20A39E', '#D48A1F', '#C95D73', '#6D73D9',
  '#0EA5E9', '#8B5CF6', '#4B5563', '#65A30D', '#C2410C',
];

export function buildOption(selections, seriesData, { start, end, title }) {
  const hasY2 = selections.some((s) => s.axis === 'Y2');
  const yAxis = [{ type: 'value', name: 'Y1', axisLine: { show: true } }];
  if (hasY2) {
    yAxis.push({ type: 'value', name: 'Y2', position: 'right', axisLine: { show: true } });
  }

  const y1Sel = selections.find((s) => s.axis !== 'Y2');
  const y2Sel = selections.find((s) => s.axis === 'Y2');
  if (y1Sel) {
    yAxis[0].axisLine.lineStyle = { color: PALETTE[selections.indexOf(y1Sel) % 10] };
  }
  if (y2Sel) {
    yAxis[1].axisLine.lineStyle = { color: PALETTE[selections.indexOf(y2Sel) % 10] };
  }

  const series = selections.map((s, i) => {
    const d = seriesData[s.name] || { xs: [], ys: [] };
    const color = PALETTE[i % PALETTE.length];
    const isScatter = s.chartType === '散点图';
    return {
      name: s.name,
      type: 'line',
      yAxisIndex: s.axis === 'Y2' ? 1 : 0,
      showSymbol: s.chartType !== '折线图',
      symbolSize: 5,
      lineStyle: { width: isScatter ? 0 : 2, color },
      itemStyle: { color },
      data: d.xs.map((x, idx) => [x, d.ys[idx]]),
    };
  });

  return {
    title: { text: title, left: 'left', textStyle: { fontSize: 14 } },
    tooltip: { trigger: 'axis' },
    legend: { left: 'left', top: 28, type: 'scroll' },
    grid: { top: 70, left: 60, right: hasY2 ? 60 : 30, bottom: 80, containLabel: true },
    xAxis: { type: 'time', min: start, max: end },
    yAxis,
    // 🆕 dataZoom 提供缩放/区间刷选（增强，不破坏原行为）
    dataZoom: [{ type: 'inside' }, { type: 'slider', bottom: 20 }],
    series,
  };
}
