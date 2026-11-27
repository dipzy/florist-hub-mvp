import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = 'http://127.0.0.1:5173';
const outDir = path.resolve('../docs/screenshots');
const floristEmail = 'florista@demo.floristhub.pt';
const password = 'DemoFlorist1!';
const superEmail = 'admin@demo.floristhub.pt';

await mkdir(outDir, { recursive: true });
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });

await page.goto(`${base}/login?tenant=flores-lina`);
await page.waitForSelector('select');
await page.selectOption('select', '11111111-1111-1111-1111-111111111101');
await page.getByLabel(/^Email$/i).fill(floristEmail);
await page.getByLabel(/^Palavra-passe$/i).fill(password);
await page.getByRole('button', { name: /^Entrar$/i }).click();
await page.waitForURL(/\/(admin|florista)/, { timeout: 60_000 });
await page.goto(`${base}/admin/configuracao-inicial?tenant=flores-lina`);
await page.waitForTimeout(800);
await page.screenshot({ path: path.join(outDir, 'onboarding-wizard.png'), fullPage: true });

await page.goto(`${base}/admin?tenant=flores-lina`);
await page.waitForTimeout(600);
await page.screenshot({ path: path.join(outDir, 'onboarding-checklist.png'), fullPage: true });

await page.goto(`${base}/login`);
await page.getByLabel('Entrar como SuperAdmin (plataforma)', { exact: true }).check();
await page.getByLabel(/^Email$/i).fill(superEmail);
await page.getByLabel(/^Palavra-passe$/i).fill(password);
await page.getByRole('button', { name: /entrar/i }).click();
await page.waitForURL(/superadmin/);
await page.goto(`${base}/superadmin/lojas/11111111-1111-1111-1111-111111111101`);
await page.waitForTimeout(800);
await page.screenshot({ path: path.join(outDir, 'onboarding-superadmin.png'), fullPage: true });

await browser.close();
