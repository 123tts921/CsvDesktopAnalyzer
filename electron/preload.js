const { contextBridge, ipcRenderer } = require('electron');

// 仅暴露最小必要 API，严禁扩展（spec 第 4.2 节）
contextBridge.exposeInMainWorld('api', {
  openCsv: () => ipcRenderer.invoke('dialog:open-csv'),
  readBuffer: (p) => ipcRenderer.invoke('fs:read-buffer', p),
  savePng: (n) => ipcRenderer.invoke('dialog:save-png', n),
  writeBuffer: (p, b) => ipcRenderer.invoke('fs:write-buffer', p, b),
});
