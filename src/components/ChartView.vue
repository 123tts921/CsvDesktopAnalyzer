<template>
  <div class="chart-wrap">
    <div class="chart-toolbar">
      <t-space>
        <t-button theme="primary" @click="onRefresh">刷新</t-button>
        <t-button @click="onReset">重置视图</t-button>
        <t-button @click="onExport">导出图片</t-button>
      </t-space>
    </div>
    <div ref="el" class="chart-canvas"></div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';
import * as echarts from 'echarts';
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
const el = ref(null);
let chart = null;

function resize() {
  if (chart) chart.resize();
}
onMounted(() => {
  chart = echarts.init(el.value);
  window.addEventListener('resize', resize);
});
onBeforeUnmount(() => {
  window.removeEventListener('resize', resize);
  if (chart) chart.dispose();
});

watch(
  () => store.drawVersion,
  () => {
    if (store.lastSpecs.length) {
      chart.setOption(store.getOption(), true);
    }
  }
);

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
.chart-wrap {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.chart-toolbar {
  padding: 8px 0;
}
.chart-canvas {
  flex: 1;
  min-height: 420px;
}
</style>
