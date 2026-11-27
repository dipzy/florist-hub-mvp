import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = process.argv[2] || 'http://127.0.0.1:4173';
const outDir = process.argv[3] || '/opt/cursor/artifacts/screenshots';

const pages = [
  { name: 'portfolio', path: '/portfolio' },
  { name: 'login', path: '/login' },
  { name: 'montar', path: '/montar' },
  { name: 'admin', path: '/admin' },
];

await mkdir(outDir, { recursive: true });
const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });

for (const { name, path: p } of pages) {
  const page = await context.newPage();
  await page.goto(`${base}${p}`, { waitUntil: 'networkidle', timeout: 15000 }).catch(() => {});
  await page.screenshot({ path: path.join(outDir, `${name}.png`), fullPage: true });
  await page.close();
}

await browser.close();
console.log('Saved to', outDir);
