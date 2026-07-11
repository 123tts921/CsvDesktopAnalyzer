import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

const csvPath = path.resolve(__dirname, '../fixtures/sample.csv');
const csvBase64 = fs.readFileSync(csvPath).toString('base64');

// —— UI 冒烟测试（spec §7.5 / §11）——
// 在浏览器中加载构建产物，通过 mock window.api 绕过 Electron IPC，
// 覆盖“选择文件 → 加载 → 选择列 → 绘制 → 导出”完整链路。
test.beforeEach(async ({ page }) => {
  await page.addInitScript((b64) => {
    const bytes = Uint8Array.from(atob(b64), (c) => c.charCodeAt(0));
    window.api = {
      openCsv: () => 'sample.csv',
      readBuffer: async () => bytes.buffer,
      savePng: () => null,
      writeBuffer: async () => true,
    };
  }, csvBase64);
});

test('加载 CSV 并绘制图表', async ({ page }) => {
  const pageErrors = [];
  page.on('pageerror', (e) => pageErrors.push(e.message));

  await page.goto('/');
  await expect(page.getByText('CSV 桌面分析器')).toBeVisible();

  // 选择文件 → 填充路径
  await page.getByRole('button', { name: '选择文件' }).click();
  // 加载 → 触发 worker 解析
  await page.getByRole('button', { name: '加载' }).click();

  // 等待加载完成：状态栏出现“可绘图列”（spec §5.9 / §7.5）
  await expect(page.getByText(/可绘图列/)).toBeVisible({ timeout: 20000 });
  // 数值列识别应排除文本列“备注”，保留 电能kW/温度/压力
  await expect(page.getByText('电能kW')).toBeVisible();
  await expect(page.getByText('温度')).toBeVisible();
  await expect(page.getByText('压力')).toBeVisible();

  // 全选可见列后绘制
  await page.getByRole('button', { name: '全选当前结果' }).click();
  await page.getByRole('button', { name: '绘制图表' }).click();

  // ECharts 画布渲染且有尺寸（spec §5.7）
  const canvas = page.locator('.chart-canvas canvas');
  await expect(canvas).toBeVisible({ timeout: 20000 });
  const box = await canvas.boundingBox();
  expect(box.width).toBeGreaterThan(0);
  expect(box.height).toBeGreaterThan(0);

  // 导出图片按钮可点击且不抛错（spec §5.10）
  await page.getByRole('button', { name: '导出图片' }).click();

  expect(pageErrors).toEqual([]);
});

test('清除选择后绘制应被校验拦截（spec §5.9 至少 1 列）', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: '选择文件' }).click();
  await page.getByRole('button', { name: '加载' }).click();
  await expect(page.getByText(/可绘图列/)).toBeVisible({ timeout: 20000 });

  // 全选后再清空，绘制应被拦截，画布不应出现
  await page.getByRole('button', { name: '全选当前结果' }).click();
  await page.getByRole('button', { name: '清空选择' }).click();
  await page.getByRole('button', { name: '绘制图表' }).click();

  await expect(page.locator('.chart-canvas canvas')).toHaveCount(0);
});
