<template>
  <t-card title="图表配置" :bordered="true" class="panel">
    <t-form layout="vertical" class="cfg-form">
      <t-form-item label="时间列">
        <t-select
          :value="store.timeColumn"
          :options="headerOptions"
          @change="store.setTimeColumn"
          :disabled="!store.loaded"
        />
      </t-form-item>
      <t-form-item label="默认图表类型">
        <t-select
          :value="store.defaultChartType"
          :options="types"
          @change="store.setDefaultChartType"
        />
      </t-form-item>
      <t-form-item label="每条曲线最多点数">
        <t-select
          :value="store.maxPoints"
          :options="points"
          @change="store.setMaxPoints"
          :disabled="store.displayMode !== '固定点数'"
        />
      </t-form-item>
      <t-form-item label="显示模式">
        <t-select
          :value="store.displayMode"
          :options="modes"
          @change="store.setDisplayMode"
        />
      </t-form-item>
      <t-form-item label="固定时间间隔">
        <t-select
          :value="store.interval"
          :options="intervals"
          @change="store.setInterval"
          :disabled="store.displayMode !== '固定时间间隔'"
        />
      </t-form-item>
    </t-form>
  </t-card>
</template>

<script setup>
import { computed } from 'vue';
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
const headerOptions = computed(() => store.headers.map((h) => ({ label: h, value: h })));
const types = [
  { label: '折线图', value: '折线图' },
  { label: '点线图', value: '点线图' },
  { label: '散点图', value: '散点图' },
];
const points = [3000, 6000, 10000, 20000].map((v) => ({ label: String(v), value: v }));
const modes = [
  { label: '固定点数', value: '固定点数' },
  { label: '固定时间间隔', value: '固定时间间隔' },
];
const intervals = ['1分钟', '5分钟', '10分钟', '30分钟', '1小时'].map((v) => ({ label: v, value: v }));
</script>

<style scoped>
.cfg-form :deep(.t-form__item) {
  margin-bottom: 12px;
}
.cfg-form :deep(.t-form__label) {
  min-width: 96px;
  white-space: nowrap;
}
.cfg-form :deep(.t-select) {
  width: 100%;
}
</style>
