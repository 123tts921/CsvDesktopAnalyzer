import { defineStore } from 'pinia';
import { decodeBuffer } from '../lib/csvParse.js';
import { buildSeries } from '../lib/sampling.js';
import { buildOption } from '../lib/chartOption.js';
import { Message } from 'tdesign-vue-next';

const INTERVAL_MS = {
  '1分钟': 60000,
  '5分钟': 300000,
  '10分钟': 600000,
  '30分钟': 1800000,
  '1小时': 3600000,
};

function basename(p) {
  return p ? p.split(/[\\/]/).pop() : '';
}
function fmt(ms) {
  const d = new Date(ms);
  const p = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`;
}

export const useAnalyzerStore = defineStore('analyzer', {
  state: () => ({
    filePath: null,
    headers: [],
    timeColumn: null,
    numericColumns: [],
    timestamps: [],
    valuesByColumn: {},
    minTime: null,
    maxTime: null,
    rowCount: 0,
    config: {},
    loaded: false,
    timeRange: [],
    dataMinTime: null,
    dataMaxTime: null,
    searchText: '',
    defaultChartType: '折线图',
    displayMode: '固定点数',
    maxPoints: 6000,
    interval: '10分钟',
    seriesData: {},
    lastSpecs: [],
    drawVersion: 0,
    loading: false,
    status: '准备就绪',
    plotSummary: '未加载文件 | 0 条曲线 | 等待绘图',
  }),
  getters: {
    visibleColumns(state) {
      const q = state.searchText.trim().toLowerCase();
      return state.numericColumns.filter((n) => !q || n.toLowerCase().includes(q));
    },
    selectedSpecs(state) {
      return state.numericColumns
        .filter((n) => state.config[n] && state.config[n].selected)
        .map((n) => ({
          name: n,
          axis: state.config[n].axis,
          chartType: state.config[n].chartType,
        }));
    },
  },
  actions: {
    async loadFile(path) {
      this.filePath = path;
      this.loading = true;
      this.status = '正在加载 CSV 到内存...';
      try {
        const buf = await window.api.readBuffer(path);
        const text = decodeBuffer(new Uint8Array(buf));
        const worker = new Worker(new URL('../workers/csv.worker.js', import.meta.url), {
          type: 'module',
        });
        await new Promise((resolve, reject) => {
          worker.onmessage = (ev) => {
            const { ok, payload, error } = ev.data;
            if (!ok) {
              reject(new Error(error));
              return;
            }
            this._applyPayload(payload, path);
            resolve();
          };
          worker.onerror = (e) => reject(new Error(e.message));
          worker.postMessage({ text, requestedTimeColumn: this.timeColumn });
        });
        worker.terminate();
      } catch (err) {
        this.status = `加载失败：${err.message}`;
        Message.error(`加载失败：${err.message}`);
      } finally {
        this.loading = false;
      }
    },
    _applyPayload(p, path) {
      this.headers = p.headers;
      this.timeColumn = p.timeColumn;
      this.numericColumns = p.numericColumns;
      this.timestamps = p.timestamps;
      this.valuesByColumn = p.valuesByColumn;
      this.minTime = p.minTime;
      this.maxTime = p.maxTime;
      this.dataMinTime = p.minTime;
      this.dataMaxTime = p.maxTime;
      this.rowCount = p.rowCount;
      this.filePath = path;
      const cfg = {};
      p.numericColumns.forEach((n) => {
        cfg[n] = { selected: false, axis: 'Y1', chartType: this.defaultChartType };
      });
      this.config = cfg;
      this.timeRange = [p.minTime, p.maxTime];
      this.loaded = true;
      this.status = `${basename(path)} | 共 ${p.rowCount.toLocaleString()} 行 | ${p.numericColumns.length} 个可绘图列`;
      this.plotSummary = '等待绘图';
    },
    setTimeColumn(name) {
      this.timeColumn = name;
      if (this.filePath) this.loadFile(this.filePath);
    },
    setDefaultChartType(t) {
      this.defaultChartType = t;
    },
    setDisplayMode(m) {
      this.displayMode = m;
    },
    setInterval(v) {
      this.interval = v;
    },
    setMaxPoints(v) {
      this.maxPoints = Number(v);
    },
    setSearch(q) {
      this.searchText = q;
    },
    toggleSelect(name, val) {
      if (this.config[name]) this.config[name].selected = val;
    },
    setAxis(name, val) {
      if (this.config[name]) this.config[name].axis = val;
    },
    setChartType(name, val) {
      if (this.config[name]) this.config[name].chartType = val;
    },
    selectAllVisible() {
      this.visibleColumns.forEach((n) => {
        if (this.config[n]) this.config[n].selected = true;
      });
    },
    clearAll() {
      this.numericColumns.forEach((n) => {
        if (this.config[n]) this.config[n].selected = false;
      });
    },
    draw() {
      if (!this.loaded) return;
      const specs = this.selectedSpecs;
      if (specs.length === 0) {
        Message.warning('请至少选择一个指标列。');
        return;
      }
      const [s, e] = this.timeRange;
      if (s >= e) {
        Message.warning('开始时间必须早于结束时间。');
        return;
      }
      const seriesData = buildSeries(
        { timestamps: this.timestamps, valuesByColumn: this.valuesByColumn },
        {
          selections: specs,
          start: s,
          end: e,
          displayMode: this.displayMode,
          maxPoints: this.maxPoints,
          intervalMs: INTERVAL_MS[this.interval],
        }
      );
      this.seriesData = seriesData;
      this.lastSpecs = specs;
      this.drawVersion += 1;

      const maxPts = Math.max(0, ...Object.values(seriesData).map((d) => d.xs.length));
      const sampling =
        this.displayMode === '固定时间间隔'
          ? `固定时间间隔 ${this.interval}`
          : `固定点数 ${this.maxPoints}`;
      this.status = `${basename(this.filePath)} | ${sampling} | 当前最多 ${maxPts} 点/列 | ${specs.length} 条曲线`;
      this.plotSummary = `${basename(this.filePath)} | ${specs.length} 条曲线 | ${sampling} | 最多 ${maxPts} 点/列`;
    },
    getOption() {
      return buildOption(this.lastSpecs, this.seriesData, {
        start: this.timeRange[0],
        end: this.timeRange[1],
        title: `${fmt(this.timeRange[0])} 至 ${fmt(this.timeRange[1])}`,
      });
    },
  },
});
