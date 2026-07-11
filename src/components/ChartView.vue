<template>
  <div class="chart-card">
    <div class="chart-toolbar">
      <t-space>
        <t-button theme="primary" @click="onRefresh" :disabled="!store.loaded">刷新</t-button>
        <t-button @click="onReset" :disabled="!store.loaded">重置视图</t-button>
        <t-button @click="onExport" :disabled="!store.loaded">导出图片</t-button>
      </t-space>
    </div>

    <div class="chart-body">
      <!-- 画布始终存在，供 ECharts 挂载 -->
      <div ref="el" class="chart-canvas" v-show="store.loaded && !store.loading"></div>

      <!-- 加载态：骨架屏 -->
      <div v-if="store.loading" class="chart-state">
        <div class="skeleton-toolbar"></div>
        <div class="skeleton-canvas"></div>
      </div>

      <!-- 空态：未加载 CSV -->
      <div v-else-if="!store.loaded" class="chart-state chart-empty">
        <svg class="empty-icon" viewBox="0 0 64 64" fill="none" aria-hidden="true">
          <rect x="8" y="10" width="48" height="44" rx="4" stroke="currentColor" stroke-width="2" />
          <path d="M16 40l10-12 8 8 8-12 6 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
          <circle cx="22" cy="22" r="3" fill="currentColor" />
        </svg>
        <div class="empty-title">尚未加载数据</div>
        <div class="empty-hint">请在顶部选择 CSV 文件并点击「加载」，随后在左侧选择指标绘图。</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick, onMounted, onBeforeUnmount, watch } from 'vue';
import * as echarts from 'echarts';
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
const el = ref(null);
let chart = null;

function ensureChart() {
  if (!chart && el.value) {
    chart = echarts.init(el.value);
  }
}
function render() {
  // 仅在「已加载 + 画布可见 + 实例存在」时绘制，避免对隐藏/空 DOM 操作
  if (store.loaded && el.value && chart && store.lastSpecs.length) {
    chart.resize();
    chart.setOption(store.getOption(), true);
  }
}
function resize() {
  if (chart && el.value) chart.resize();
}
onMounted(() => {
  window.addEventListener('resize', resize);
});
onBeforeUnmount(() => {
  window.removeEventListener('resize', resize);
  if (chart) {
    chart.dispose();
    chart = null;
  }
});

// 数据加载完成 → 画布可见后再创建实例并绘制（不再对 display:none 元素 init）
watch(
  () => store.loaded,
  (v) => {
    if (v) nextTick(() => { ensureChart(); render(); });
  },
  { immediate: true }
);
// 绘制版本变化 → 重新渲染
watch(() => store.drawVersion, render);

function onRefresh() {
  store.draw();
}
function onReset() {
  if (chart) chart.dispatchAction({ type: 'dataZoom', start: 0, end: 100 });
}
async function onExport() {
  if (!chart) return;
  // 导出尺寸保证 ≥ 1200×700（spec 5.10），pixelRatio 取满足条件的最小值
  const w = el.value.clientWidth;
  const h = el.value.clientHeight;
  const pr = Math.max(2, Math.ceil(1200 / w), Math.ceil(700 / h));
  const url = chart.getDataURL({ type: 'png', pixelRatio: pr, backgroundColor: '#fff' });
  const blob = await (await fetch(url)).blob();
  const arr = await blob.arrayBuffer();
  const name = `chart_${stamp()}.png`;
  const p = await window.api.savePng(name);
  if (p) {
    await window.api.writeBuffer(p, arr);
    store.status = `已导出图片：${p.split(/[\\/]/).pop()}`;
  }
}
function stamp() {
  const d = new Date();
  const p = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}_${p(d.getHours())}${p(
    d.getMinutes()
  )}${p(d.getSeconds())}`;
}
</script>

<style scoped>
.chart-card {
  display: flex;
  flex-direction: column;
  height: 100%;
  max-height: calc(100vh - 160px);
  background: var(--c-surface);
  border: 1px solid var(--c-border);
  border-radius: var(--r-lg);
  box-shadow: var(--sh-card);
  padding: var(--s-3);
  overflow: hidden;
}
.chart-toolbar {
  padding-bottom: var(--s-3);
  border-bottom: 1px solid var(--c-border);
  margin-bottom: var(--s-3);
}
.chart-body {
  position: relative;
  flex: 1;
  min-height: 420px;
}
.chart-canvas {
  width: 100%;
  height: 100%;
  min-height: 420px;
  border-radius: var(--r-md);
  overflow: hidden;
}

/* 空态 / 加载态共用容器 */
.chart-state {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
}

/* 空态 */
.chart-empty {
  align-items: center;
  justify-content: center;
  gap: var(--s-2);
  color: var(--c-text-3);
  text-align: center;
  padding: var(--s-5);
}
.empty-icon {
  width: 64px;
  height: 64px;
  color: var(--c-primary);
  opacity: 0.6;
}
.empty-title {
  font-family: var(--font-head);
  font-size: 16px;
  font-weight: 600;
  color: var(--c-text-2);
}
.empty-hint {
  font-size: 13px;
  max-width: 320px;
  line-height: 1.6;
}

/* 加载骨架屏 */
.skeleton-toolbar {
  height: 24px;
  width: 40%;
  border-radius: var(--r-sm);
  margin-bottom: var(--s-3);
  background: var(--c-surface-2);
}
.skeleton-canvas {
  flex: 1;
  border-radius: var(--r-md);
  background: linear-gradient(
    90deg,
    var(--c-surface-2) 25%,
    var(--c-border) 37%,
    var(--c-surface-2) 63%
  );
  background-size: 400% 100%;
  animation: shimmer 1.4s ease infinite;
}
@keyframes shimmer {
  0% {
    background-position: 100% 0;
  }
  100% {
    background-position: 0 0;
  }
}
</style>
