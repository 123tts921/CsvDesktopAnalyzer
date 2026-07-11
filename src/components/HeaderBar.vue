<template>
  <div class="header">
    <div class="brand">
      <span class="brand-bar"></span>
      <div class="title">
        CSV 桌面分析器
        <div class="hint">多列对比、多轴映射、时间范围筛选</div>
      </div>
    </div>
    <div class="file-row">
      <t-input :value="path" placeholder="当前文件" readonly class="file-input" />
      <t-button @click="choose">选择文件</t-button>
      <t-button theme="primary" @click="load">加载</t-button>
      <t-button
        variant="outline"
        shape="square"
        :title="dark ? '切换到浅色' : '切换到深色'"
        @click="toggleTheme"
      >
        <template #icon>
          <t-icon :name="dark ? 'sunny' : 'moon'" />
        </template>
      </t-button>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue';
import { MessagePlugin } from 'tdesign-vue-next';
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
const path = computed({
  get: () => store.filePath || '',
  set: (v) => {
    store.filePath = v;
  },
});
async function choose() {
  if (!window.api || !window.api.openCsv) {
    MessagePlugin.warning('文件选择需在 Electron 桌面环境中运行（请用 npm run dev 启动）');
    return;
  }
  const p = await window.api.openCsv();
  if (p) store.filePath = p;
}
function load() {
  if (store.filePath) store.loadFile(store.filePath);
}

/* ---------- 深色模式 ---------- */
const dark = ref(false);
function applyTheme(isDark) {
  dark.value = isDark;
  document.body.setAttribute('data-theme', isDark ? 'dark' : 'light');
  document.documentElement.setAttribute('theme-mode', isDark ? 'dark' : 'light');
  localStorage.setItem('theme', isDark ? 'dark' : 'light');
}
function toggleTheme() {
  applyTheme(!dark.value);
}
onMounted(() => {
  const saved = localStorage.getItem('theme');
  const prefersDark =
    saved === 'dark' ||
    (!saved && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
  applyTheme(prefersDark);
});
</script>

<style scoped>
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 88px;
  padding: 0 var(--s-5);
}
.brand {
  display: flex;
  align-items: center;
  gap: var(--s-3);
}
.brand-bar {
  width: 4px;
  height: 40px;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--c-primary), var(--c-primary-a));
}
.title {
  font-family: var(--font-head);
  font-size: 20px;
  font-weight: 700;
  color: var(--c-text);
}
.hint {
  font-size: 11px;
  color: var(--c-text-3);
  font-weight: normal;
}
.file-row {
  display: flex;
  align-items: center;
  gap: var(--s-3);
}
.file-input {
  width: 480px;
}
</style>
