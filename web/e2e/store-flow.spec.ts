import { expect, test } from '@playwright/test';

const DEMO_PASSWORD = 'DemoFlorist1!';
const CUSTOMER_EMAIL = 'cliente@demo.floristhub.pt';
const FLORIST_EMAIL = 'florista@demo.floristhub.pt';
const LINA_TENANT_ID = '11111111-1111-1111-1111-111111111101';

async function selectDemoStore(page: import('@playwright/test').Page) {
  const store = page.getByLabel('Loja');
  await expect(store).toBeVisible({ timeout: 30_000 });
  await expect.poll(async () => store.locator('option').count()).toBeGreaterThan(1);
  await store.selectOption(LINA_TENANT_ID);
}

async function loginAsCustomer(page: import('@playwright/test').Page) {
  await page.goto('/login?tenant=flores-lina');
  await selectDemoStore(page);
  await page.getByLabel('Correio eletrónico').fill(CUSTOMER_EMAIL);
  await page.getByLabel('Palavra-passe').fill(DEMO_PASSWORD);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/portfolio/, { timeout: 60_000 });
}

async function loginAsFlorist(page: import('@playwright/test').Page) {
  await page.goto('/login?tenant=flores-lina');
  await selectDemoStore(page);
  await page.getByLabel('Correio eletrónico').fill(FLORIST_EMAIL);
  await page.getByLabel('Palavra-passe').fill(DEMO_PASSWORD);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/(admin|florista)/, { timeout: 60_000 });
}

test.describe('Fluxo loja pública e encomendas', () => {
  test('cliente autentica e submete uma encomenda', async ({ page }) => {
    await loginAsCustomer(page);

    await page.goto('/montar?tenant=flores-lina');
    await expect(page.getByRole('heading', { name: 'Montar arranjo' })).toBeVisible({ timeout: 30_000 });
    await page.getByRole('button', { name: 'Aumentar quantidade' }).first().click();
    await page.getByLabel(/aceito os.*Termos/i).check();
    await page.getByRole('button', { name: 'Submeter encomenda' }).click();
    await expect(page.getByText(/Encomenda submetida/)).toBeVisible({ timeout: 60_000 });
  });

  test('florista acede ao painel de encomendas', async ({ page }) => {
    await loginAsFlorist(page);
    await page.goto('/admin/encomendas');
    await expect(page.getByRole('heading', { name: /Encomendas/i })).toBeVisible({ timeout: 30_000 });
  });

  test('resolução da loja por parâmetro tenant', async ({ page }) => {
    await page.goto('/portfolio?tenant=flores-lina');
    await expect(page.getByText(/Buquê primaveril/i)).toBeVisible({ timeout: 30_000 });
  });
});
