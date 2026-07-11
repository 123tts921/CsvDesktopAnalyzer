<template>
  <div class="app">
    <header class="app-header"><HeaderBar /></header>
    <div class="app-body">
      <aside class="app-left">
        <FilterPanel />
        <TimeRangePanel />
        <ColumnTable />
      </aside>
      <main class="app-right"><ChartView /></main>
    </div>
    <StatusBar />
  </div>
</template>

<script setup>
import HeaderBar from './components/HeaderBar.vue';
import FilterPanel from './components/FilterPanel.vue';
import TimeRangePanel from './components/TimeRangePanel.vue';
import ColumnTable from './components/ColumnTable.vue';
import ChartView from './components/ChartView.vue';
import StatusBar from './components/StatusBar.vue';
</script>

<style>
.app {
  display: grid;
  grid-template-rows: auto 1fr auto;
  min-height: 100vh;
  font-family: var(--font-body);
  background: var(--c-bg);
  color: var(--c-text);
}
.app-header {
  background: var(--c-surface);
  border-bottom: 1px solid var(--c-border);
}
.app-body {
  display: grid;
  grid-template-columns: 460px 1fr;
}
.app-left {
  min-width: 460px;
  padding: var(--s-3);
  overflow: visible;
  background: var(--c-surface);
  border-right: 1px solid var(--c-border);
}
.app-right {
  padding: var(--s-3);
  min-width: 0;
}
</style>
