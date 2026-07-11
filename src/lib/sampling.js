// 采样算法（严格对齐 spec 5.6）

export function downSample(points, limit) {
  if (points.length === 0) return [];
  if (limit <= 0 || points.length <= limit) return points.slice();

  const out = [];
  let prev = -1;
  const scale = (points.length - 1) / (limit - 1);
  for (let i = 0; i < limit; i += 1) {
    let idx = Math.round(i * scale);
    if (idx >= points.length) idx = points.length - 1;
    if (idx === prev) continue;
    out.push(points[idx]);
    prev = idx;
  }

  if (out.length === 0 || out[out.length - 1] !== points[points.length - 1]) {
    if (out.length === limit) {
      out[limit - 1] = points[points.length - 1];
    } else {
      out.push(points[points.length - 1]);
    }
  }
  return out;
}

export function resampleByInterval(points, intervalMs) {
  if (points.length === 0) return [];
  if (intervalMs <= 0) return points.slice();

  const out = [];
  const seen = new Set();
  for (const p of points) {
    const bucket = Math.floor(p.t / intervalMs);
    if (seen.has(bucket)) continue;
    seen.add(bucket);
    out.push(p);
  }
  return out;
}

export function buildSeries(cache, req) {
  const { timestamps, valuesByColumn } = cache;
  const result = {};
  for (const sel of req.selections) {
    const vals = valuesByColumn[sel.name];
    if (!vals) {
      result[sel.name] = { xs: [], ys: [] };
      continue;
    }
    const pts = [];
    for (let i = 0; i < timestamps.length; i += 1) {
      const t = timestamps[i];
      if (t < req.start || t > req.end) continue;
      const v = vals[i];
      if (v == null || Number.isNaN(v)) continue;
      pts.push({ t, v });
    }
    // 满足 spec 5.6 前提：采样前按时间升序
    pts.sort((a, b) => a.t - b.t);
    const sampled =
      req.displayMode === '固定时间间隔'
        ? resampleByInterval(pts, req.intervalMs)
        : downSample(pts, req.maxPoints);
    result[sel.name] = {
      xs: sampled.map((p) => p.t),
      ys: sampled.map((p) => p.v),
    };
  }
  return result;
}
