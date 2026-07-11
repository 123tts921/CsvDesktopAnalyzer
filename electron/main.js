const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const fs = require('fs');
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
    win.loadURL(dev);
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
