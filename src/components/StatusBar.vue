<template>
  <div class="status-bar">
    <span>{{ store.status }}</span>
    <span class="summary">{{ store.plotSummary }}</span>
  </div>
</template>

<script setup>
import { useAnalyzerStore } from '../stores/analyzer';
const store = useAnalyzerStore();
</script>

<style scoped>
.status-bar {
  display: flex;
  justify-content: space-between;
  padding: 4px 12px;
  font-size: 12px;
  color: #6c7b8c;
  border-top: 1px solid #d8e1ea;
  background: #f8fbfd;
}
</style>
