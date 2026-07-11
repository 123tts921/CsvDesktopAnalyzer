const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const fs = require('fs');
const http = require('http');
const path = require('path');

function createWindow() {
  const win = new BrowserWindow({
    width: 1464,
    height: 921,
    minWidth: 1440,
    minHeight: 920,
    center: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      sandbox: true,
      nodeIntegration: false,
    },
  });

  const dev = process.env.VITE_DEV_SERVER_URL;
  if (dev) {
    const url = new URL(dev);
    // 兜底：页面加载完成但 #app 仍为空（白屏）时自动重载一次
    win.webContents.on('did-finish-load', () => {
      setTimeout(() => {
        win.webContents
          .executeJavaScript(
            "document.getElementById('app') && document.getElementById('app').childElementCount > 0"
          )
          .then((ok) => {
            if (!ok) win.reload();
          })
          .catch(() => {});
      }, 2000);
    });
    // 探测入口模块：Vite 必须先完成依赖预构建才会返回 200，避免首屏白屏
    const probe = () => {
      const req = http.get({ host: url.hostname, port: url.port, path: '/src/main.js' }, (res) => {
        res.destroy();
        if (res.statusCode === 200) win.loadURL(dev);
        else setTimeout(probe, 300);
      });
      req.on('error', () => setTimeout(probe, 300));
      req.setTimeout(3000, () => {
        req.destroy();
        setTimeout(probe, 300);
      });
    };
    probe();
  } else {
    win.loadFile(path.join(__dirname, '../dist/index.html'));
  }
}

function registerIpc() {
  ipcMain.handle('dialog:open-csv', async () => {
    const r = await dialog.showOpenDialog({
      properties: ['openFile'],
      filters: [{ name: 'CSV', extensions: ['csv', 'txt'] }],
    });
    return r.canceled ? null : r.filePaths[0];
  });

  ipcMain.handle('fs:read-buffer', async (_e, p) => {
    const stat = fs.statSync(p);
    const MAX = 500 * 1024 * 1024; // 500MB 上限（spec 7.1 / 12）
    if (stat.size > MAX) {
      throw new Error(`文件过大（${Math.round(stat.size / 1024 / 1024)}MB），已超过 500MB 限制，请拆分后加载。`);
    }
    const buf = fs.readFileSync(p);
    return buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength);
  });

  ipcMain.handle('dialog:save-png', async (_e, defaultName) => {
    const r = await dialog.showSaveDialog({
      defaultPath: defaultName,
      filters: [{ name: 'PNG', extensions: ['png'] }],
    });
    return r.canceled ? null : r.filePath;
  });

  ipcMain.handle('fs:write-buffer', async (_e, p, buf) => {
    fs.writeFileSync(p, Buffer.from(buf));
    return true;
  });
}

app.whenReady().then(() => {
  registerIpc();
  createWindow();
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});
