import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const webBase = process.env.WEB_BASE ?? 'http://127.0.0.1:5173';
const apiBase = process.env.VITE_API_BASE_URL ?? process.env.API_BASE ?? 'http://localhost:5183';
const outDir = process.env.OUT_DIR ?? path.resolve(process.cwd(), '../docs/screenshots');

const demoPassword = 'DemoFlorist1!';
const floristEmail = 'florista@demo.floristhub.pt';
const customerEmail = 'cliente@demo.floristhub.pt';
const superAdminEmail = 'admin@demo.floristhub.pt';

async function apiJson(url, init = {}) {
  const res = await fetch(url, init);
  if (!res.ok) throw new Error(`${url} → ${res.status}`);
  return res.json();
}

async function login(tenantId, email, password) {
  return apiJson(`${apiBase}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': tenantId },
    body: JSON.stringify({ email, password, tenantId }),
  });
}

async function loginSuperAdmin(email, password) {
  return apiJson(`${apiBase}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, tenantId: null }),
  });
}

async function seedSession(page, auth) {
  await page.addInitScript((session) => {
    localStorage.setItem('floristhub.session', JSON.stringify(session));
  }, {
    token: auth.token,
    tenantId: auth.tenantId,
    tenantSlug: auth.tenantSlug,
    role: auth.role,
    email: auth.email,
  });
}

async function shot(page, name) {
  await page.waitForTimeout(600);
  await page.screenshot({ path: path.join(outDir, `${name}.png`), fullPage: true });
}

await mkdir(outDir, { recursive: true });

const tenants = await apiJson(`${apiBase}/api/tenants`);
const tenant = tenants.find((t) => t.slug === 'flores-lina') ?? tenants[0];
if (!tenant) throw new Error('Sem tenants — API/seed em falta?');

const floristAuth = await login(tenant.id, floristEmail, demoPassword);
const customerAuth = await login(tenant.id, customerEmail, demoPassword);
const superAuth = await loginSuperAdmin(superAdminEmail, demoPassword);

const demoOrderWithTimeline = '55555555-5555-5555-5555-555555555502';

const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });

{
  const page = await context.newPage();
  await seedSession(page, customerAuth);
  await page.goto(`${webBase}/portfolio`, { waitUntil: 'networkidle', timeout: 60000 });
  await shot(page, '01-loja-portfolio');
  await page.goto(`${webBase}/montar`, { waitUntil: 'networkidle', timeout: 60000 });
  await shot(page, '02-montar-checkout');
  await page.goto(`${webBase}/encomendas`, { waitUntil: 'networkidle', timeout: 60000 });
  await shot(page, '03-encomendas-cliente');
  await page.goto(`${webBase}/encomendas/${demoOrderWithTimeline}`, { waitUntil: 'networkidle', timeout: 60000 });
  await shot(page, '04-encomenda-detalhe');
  await page.goto(`${webBase}/login`, { waitUntil: 'networkidle' });
  await page.evaluate(() => localStorage.removeItem('floristhub.session'));
  await page.reload({ waitUntil: 'networkidle' });
  await shot(page, '05-login');
  await page.close();
}

{
  const page = await context.newPage();
  await seedSession(page, floristAuth);
  const adminPages = [
    ['10-admin-dashboard', '/admin'],
    ['11-admin-encomendas', '/admin/encomendas'],
    ['12-admin-catalogo', '/admin/catalogo'],
    ['13-admin-definicoes', '/admin/definicoes'],
  ];
  for (const [name, route] of adminPages) {
    await page.goto(`${webBase}${route}`, { waitUntil: 'networkidle', timeout: 60000 });
    await shot(page, name);
  }
  await page.close();
}

{
  const page = await context.newPage();
  await seedSession(page, superAuth);
  await page.goto(`${webBase}/superadmin`, { waitUntil: 'networkidle', timeout: 60000 });
  await shot(page, '20-superadmin-lojas');
  await page.close();
}

await browser.close();
console.log('Capturas em', outDir);
