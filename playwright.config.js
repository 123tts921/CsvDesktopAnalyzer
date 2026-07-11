import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 60000,
  expect: { timeout: 20000 },
  use: {
    baseURL: 'http://localhost:4173',
    headless: true,
  },
  webServer: {
    // 先构建 web 产物再用 vite preview 托管（base: './' 适配静态托管）
    command: 'npm run build:web && npx vite preview --port 4173 --strictPort',
    url: 'http://localhost:4173',
    reuseExistingServer: false,
    timeout: 180000,
  },
});
