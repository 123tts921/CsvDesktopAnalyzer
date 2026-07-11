<template>
  <div class="status-bar">
    <span class="status-left">
      <span class="dot" :class="{ loading: store.loading }"></span>
      {{ store.status }}
    </span>
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
  align-items: center;
  padding: var(--s-1) var(--s-3);
  font-size: 12px;
  color: var(--c-text-2);
  border-top: 1px solid var(--c-border);
  background: var(--c-surface-2);
}
.status-left {
  display: flex;
  align-items: center;
  gap: var(--s-2);
}
.dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--c-success);
  box-shadow: 0 0 0 0 var(--c-success);
  animation: breathe 1.5s var(--ease) infinite;
}
.dot.loading {
  background: var(--c-primary-a);
  box-shadow: 0 0 0 0 var(--c-primary-a);
}
.summary {
  color: var(--c-text-3);
}
@keyframes breathe {
  0% {
    opacity: 0.6;
    box-shadow: 0 0 0 0 rgba(120, 140, 93, 0.4);
  }
  70% {
    opacity: 1;
    box-shadow: 0 0 0 5px rgba(120, 140, 93, 0);
  }
  100% {
    opacity: 0.6;
    box-shadow: 0 0 0 0 rgba(120, 140, 93, 0);
  }
}
</style>
