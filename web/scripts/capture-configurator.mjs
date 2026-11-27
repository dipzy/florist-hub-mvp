import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const webBase = process.env.WEB_BASE ?? 'http://127.0.0.1:5173';
const outDir = process.env.OUT_DIR ?? path.resolve(process.cwd(), '../docs/screenshots');

async function shot(page, name) {
  await page.waitForSelector('canvas', { timeout: 20000 });
  await page.waitForTimeout(1200);
  await page.screenshot({ path: path.join(outDir, `${name}.png`), fullPage: true });
}

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch({
  args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist'],
});

const desktop = await browser.newContext({ viewport: { width: 1280, height: 900 } });
const page = await desktop.newPage();
await page.goto(`${webBase}/embed/configurador?demo=1&shape=0&vase=1&seed=17483021`, { waitUntil: 'networkidle' });
await shot(page, '30-configurador-esponja-vaso');

await page.locator('label', { hasText: 'Com vaso' }).locator('input').uncheck();
await page.locator('select').selectOption('1');
await page.getByRole('button', { name: 'Gerar outra variação' }).click();
await page.waitForTimeout(800);
await shot(page, '31-configurador-comprido-embrulho');

await page.locator('select').selectOption('2');
await shot(page, '32-configurador-curto');
await desktop.close();

const mobile = await browser.newContext({ viewport: { width: 390, height: 844 } });
const phone = await mobile.newPage();
await phone.goto(`${webBase}/embed/configurador?demo=1&shape=0&vase=0&seed=48291`, { waitUntil: 'networkidle' });
await shot(phone, '33-configurador-telemovel');

await phone.goto(`${webBase}/embed/configurador?demo=1&shape=0&vase=1&seed=17483021&webgl=0`, { waitUntil: 'networkidle' });
await shot(phone, '34-configurador-2d');
await browser.close();
