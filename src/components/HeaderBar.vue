<template>
  <div class="header">
    <div class="title">
      CSV 桌面分析器
      <div class="hint">多列对比、多轴映射、时间范围筛选</div>
    </div>
    <div class="file-row">
      <t-input :value="path" placeholder="当前文件" readonly class="file-input" />
      <t-button @click="choose">选择文件</t-button>
      <t-button theme="primary" @click="load">加载</t-button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
const path = computed({
  get: () => store.filePath || '',
  set: (v) => {
    store.filePath = v;
  },
});
async function choose() {
  const p = await window.api.openCsv();
  if (p) store.filePath = p;
}
function load() {
  if (store.filePath) store.loadFile(store.filePath);
}
</script>

<style scoped>
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 88px;
  padding: 0 24px;
}
.title {
  font-size: 20px;
  font-weight: bold;
  color: #192331;
}
.hint {
  font-size: 11px;
  color: #6c7b8c;
  font-weight: normal;
}
.file-row {
  display: flex;
  align-items: center;
  gap: 12px;
}
.file-input {
  width: 480px;
}
</style>
