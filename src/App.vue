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
html,
body,
#app {
  height: 100%;
  margin: 0;
}
.app {
  display: flex;
  flex-direction: column;
  height: 100vh;
  font-family: 'Microsoft YaHei UI', system-ui, sans-serif;
  background: #eef3f7;
  color: #192331;
}
.app-header {
  background: #fff;
  border-bottom: 1px solid #d8e1ea;
}
.app-body {
  flex: 1;
  display: flex;
  min-height: 0;
}
.app-left {
  width: 460px;
  min-width: 460px;
  padding: 12px;
  overflow: auto;
  background: #fff;
  border-right: 1px solid #d8e1ea;
}
.app-right {
  flex: 1;
  padding: 12px;
  min-width: 0;
}
</style>
