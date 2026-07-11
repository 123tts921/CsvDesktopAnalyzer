<template>
  <t-card title="变量筛选" :bordered="true" class="panel">
    <t-input
      :value="search"
      placeholder="例如：电能、温度、压力"
      @change="store.setSearch"
      style="margin-bottom: 8px"
    />
    <t-space style="margin-bottom: 8px">
      <t-button @click="store.selectAllVisible">全选当前结果</t-button>
      <t-button @click="store.clearAll">清空选择</t-button>
      <t-button theme="primary" @click="store.draw">绘制图表</t-button>
    </t-space>
    <t-table
      :data="rows"
      :columns="columns"
      row-key="name"
      size="small"
      :pagination="false"
      :max-height="360"
    >
      <template #selected="{ row }">
        <t-checkbox :checked="row.selected" @change="(v) => store.toggleSelect(row.name, v)" />
      </template>
      <template #axis="{ row }">
        <t-select
          :value="row.axis"
          :options="axisOpts"
          @change="(v) => store.setAxis(row.name, v)"
          size="small"
          style="width: 70px"
        />
      </template>
      <template #chartType="{ row }">
        <t-select
          :value="row.chartType"
          :options="typeOpts"
          @change="(v) => store.setChartType(row.name, v)"
          size="small"
          style="width: 110px"
        />
      </template>
    </t-table>
  </t-card>
</template>

<script setup>
import { computed } from 'vue';
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
const search = computed({
  get: () => store.searchText,
  set: (v) => store.setSearch(v),
});
const axisOpts = [
  { label: 'Y1', value: 'Y1' },
  { label: 'Y2', value: 'Y2' },
];
const typeOpts = [
  { label: '折线图', value: '折线图' },
  { label: '点线图', value: '点线图' },
  { label: '散点图', value: '散点图' },
];
const columns = [
  { colKey: 'selected', title: '选择', width: 60 },
  { colKey: 'name', title: '数据列' },
  { colKey: 'axis', title: 'Y轴', width: 90 },
  { colKey: 'chartType', title: '图表类型', width: 130 },
];
const rows = computed(() => store.visibleColumns.map((n) => ({ name: n, ...store.config[n] })));
</script>
