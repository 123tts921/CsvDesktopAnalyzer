# 前端美化方案（基于 frontend-dev + brand-guidelines 方法论）

> 适用项目：Electron + Vue3 + TDesign + ECharts 桌面 CSV 分析工具
> 制定日期：2026-07-11

## 0. 适配说明（哪些 skill 规则采纳 / 舍弃）

`frontend-dev` 与 `brand-guidelines` 原生面向"营销落地页 + React/Tailwind + AI 生成媒体"。
本项目是**桌面数据工具**，因此做如下裁剪：

| Skill 提供 | 采纳 | 舍弃原因 |
|---|---|---|
| brand-guidelines 配色令牌（暖灰中性 + 橙/蓝/绿强调） | ✅ 作为设计令牌基底 | — |
| frontend-dev 设计拨盘 / 设计规则 / Anti-Slop / 状态与微交互 | ✅ 提取框架无关原则 | — |
| frontend-dev 的 Tailwind / Framer Motion / GSAP / Three.js | ❌ | 依赖改动大，且桌面工具无需电影级动效 |
| frontend-dev 的 MINIMAX 图/视/音生成 | ❌ | 工具 UI 不需要营销素材，且需外部 API Key |
| brand-guidelines 的 **Lora 衬线正文** | ❌ | frontend-dev 明确规定 *dashboards NEVER use serif*，正文保持无衬线 |
| frontend-dev 的 "NEVER AI purple/blue" | ⚠️ 软约束 | 本项目主色选**专业蓝** `#6a9bcc`（低饱和，并非 AI 紫蓝），可接受 |

---

## 1. 设计拨盘（Design Dials，针对本工具调校）

| Dial | 取值 | 说明 |
|---|---|---|
| `DESIGN_VARIANCE` | 4 | 桌面工具偏规整，仅做轻微不对称（左栏密集 / 右栏开阔） |
| `MOTION_INTENSITY` | 2 | Subtle：仅 CSS transition 150–300ms，hover/进出场反馈 |
| `VISUAL_DENSITY` | 7 | 左栏 460px 控件密集 → 用分隔线（`divide-y`/边框）而非堆叠卡片 |

---

## 2. 设计令牌（Design Tokens）

建议新建 `src/styles/theme.css`，定义语义令牌并覆盖 TDesign 变量。

### 2.1 颜色（源自 brand-guidelines，转为语义令牌）

```css
:root {
  /* 中性暖灰 */
  --c-bg:        #faf9f5;  /* 应用底（取代 #eef3f7） */
  --c-surface:   #ffffff;  /* 卡片/面板 */
  --c-surface-2: #f5f3ee;  /* 次级表面：输入框底、悬停底 */
  --c-border:    #e3e0d8;  /* 分隔/边框 */
  --c-text:      #141413;  /* 主文字（Dark） */
  --c-text-2:    #57534e;  /* 次级文字 */
  --c-text-3:    #b0aea5;  /* 弱化/占位（Mid） */

  /* 强调色（饱和 < 80%） */
  --c-primary:   #6a9bcc;  /* 主操作 / 数据主色（专业蓝） */
  --c-primary-h: #5b8cbd;  /* hover */
  --c-primary-a: #d97757;  /* 强调 / 选中高亮 / 警告（橙） */
  --c-success:   #788c5d;  /* 成功 / 通过（绿） */
  --c-danger:    #c2554a;  /* 错误（低饱和红，非纯红） */
}
```

### 2.2 圆角 / 阴影 / 间距 / 动效

```css
:root {
  --r-sm: 6px;  --r-md: 10px;  --r-lg: 14px;
  --sh-card: 0 1px 2px rgba(20,20,19,.04), 0 4px 14px rgba(20,20,19,.05);
  --sh-pop:  0 6px 24px rgba(20,20,19,.12);
  --s-1: 4px; --s-2: 8px; --s-3: 12px; --s-4: 16px; --s-5: 24px;
  --ease: cubic-bezier(0.16, 1, 0.3, 1);
  --dur: 180ms;
}
```

### 2.3 字体（遵守"dashboard 不用 serif"）

```css
:root {
  /* 标题：优先 Poppins（仅影响拉丁字符），中文回退雅黑 */
  --font-head: 'Poppins', 'Microsoft YaHei UI', system-ui, sans-serif;
  /* 正文：保持无衬线（不引入 Lora 衬线） */
  --font-body: 'Microsoft YaHei UI', system-ui, -apple-system, sans-serif;
}
```

### 2.4 覆盖 TDesign 主题变量（零依赖）

```css
:root {
  --td-brand-color:        var(--c-primary);
  --td-brand-color-hover:  var(--c-primary-h);
  --td-bg-color-page:      var(--c-bg);
  --td-bg-color-container: var(--c-surface);
  --td-border-level-1-color: var(--c-border);
  --td-text-color-primary:   var(--c-text);
  --td-text-color-secondary: var(--c-text-2);
  --td-radius-default:    var(--r-md);
  --td-comp-size-m:       36px; /* 控件高度略增，更易点击 */
}
```

---

## 3. 分组件改造清单

| 文件 | 现状问题 | 改造 |
|---|---|---|
| `src/main.js` | 未引入主题样式 | `import './styles/theme.css'`（在 tdesign.css 之后） |
| `src/App.vue` | 硬编码 `#eef3f7/#192331/#fff/#d8e1ea` | 全部替换为令牌；`background:var(--c-bg)`；改用 CSS Grid 三段式；统一 `--s-3` 内边距 |
| `src/components/HeaderBar.vue` | 纯白 + 下边框，无品牌感 | 左侧主色色条/品牌块 + 标题用 `--font-head`；文件信息弱化（`--c-text-3`）；右侧加**深色模式切换**按钮 |
| `src/components/FilterPanel.vue` | 仅 `.panel{margin-bottom:16px}` | 统一为 `.panel`：白底 + `--r-md` + `--sh-card` + 标题左侧 3px 主色条；面板间用 `divide` 或 `--s-3` 间距 |
| `src/components/TimeRangePanel.vue` | 同上 | 同 FilterPanel 的 `.panel` 规范（抽成 `src/styles/panel.css` 复用） |
| `src/components/ColumnTable.vue` | 同上 | 同上；表格头用 `--c-surface-2` 底，行 hover 用 `--c-surface-2` |
| `src/components/ChartView.vue` | 仅工具栏按钮 + 画布，空旷 | 卡片包裹（`--c-surface` + `--sh-card` + `--r-lg`）；工具栏统一样式；**新增空态**（未加载 CSV 时插画/提示）、**加载态**（skeleton）；ECharts 容器圆角 |
| `src/components/StatusBar.vue` | 硬编码色 | 用令牌；状态点加 1.5s 呼吸动画（低强度 perpetual motion）；文字 `--c-text-2` |

---

## 4. 必做状态与微交互（来自 frontend-dev 强制项）

- **Loading**：图表加载时骨架屏（不阻塞左栏）。
- **Empty**：未导入 CSV 时 ChartView 中央空态提示 + 引导按钮。
- **Error**：解析失败时在 StatusBar / 弹层给出可理解文案。
- **Tactile**：按钮/可点元素 `transition: var(--dur) var(--ease)`，hover 微变；可点卡片 `:active{ transform: scale(.99) }`。
- **遵守禁忌**：不用纯黑 `#000`、不用过饱和强调、不用标题渐变文字、不用 emoji（TDesign 图标足够）、不引入自定义光标。

---

## 5. 深色模式（TDesign 原生支持）

- 在 `HeaderBar` 增加切换：通过 `ConfigProvider` 的 `theme-mode`，或在 `body[data-theme="dark"]` 下定义一套暗色令牌覆盖（复用 brand-guidelines 的 Dark `#141413` 作底，Light `#faf9f5` 作字）。
- 状态持久化到 `localStorage`，Electron 渲染进程可直接读写。

---

## 6. 实施阶段（建议顺序）

1. **P1 令牌与全局**：新增 `src/styles/theme.css` + `src/styles/panel.css`，改 `main.js`、`App.vue`。→ 全站配色/间距立刻统一。
2. **P2 左栏面板**：FilterPanel / TimeRangePanel / ColumnTable 套用 `.panel` 规范。
3. **P3 图表区**：ChartView 卡片化 + 空态/加载态。
4. **P4 顶栏品牌化 + 深色模式**：HeaderBar 品牌条 + 主题切换。
5. **P5 状态条微交互**：StatusBar 令牌化 + 呼吸点。

> 每一阶段独立可提交，便于 review。
