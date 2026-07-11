import { createApp } from 'vue';
import { createPinia } from 'pinia';
import TDesign from 'tdesign-vue-next';
import 'tdesign-vue-next/dist/tdesign.css';
import App from './App.vue';

const app = createApp(App);
app.use(createPinia());
app.use(TDesign);
app.mount('#app');
