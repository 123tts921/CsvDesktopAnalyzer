import { parseCsv } from '../lib/csvParse.js';

self.onmessage = (e) => {
  const { text, requestedTimeColumn } = e.data;
  try {
    const payload = parseCsv(text, requestedTimeColumn);
    self.postMessage({ ok: true, payload });
  } catch (err) {
    self.postMessage({ ok: false, error: String((err && err.message) || err) });
  }
};
