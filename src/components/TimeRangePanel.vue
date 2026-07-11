<template>
  <t-card title="时间范围" :bordered="true" class="panel">
    <t-date-range-picker
      enable-time-picker
      value-type="timestamp"
      :value="store.timeRange"
      @change="onChange"
      :disabled="!store.loaded"
      format="YYYY-MM-DD HH:mm:ss"
      :clearable="false"
      style="width: 100%"
    />
  </t-card>
</template>

<script setup>
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
function onChange(v) {
  if (Array.isArray(v) && v.length === 2) store.timeRange = v;
}
</script>
