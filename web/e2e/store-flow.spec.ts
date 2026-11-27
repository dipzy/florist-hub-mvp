import { expect, test } from '@playwright/test';

const DEMO_PASSWORD = 'DemoFlorist1!';
const CUSTOMER_EMAIL = 'cliente@demo.floristhub.pt';
const FLORIST_EMAIL = 'florista@demo.floristhub.pt';

async function loginAsCustomer(page: import('@playwright/test').Page) {
  // A loja vem do ?tenant= do endereço: o login já não tem seletor de loja.
  await page.goto('/login?tenant=flores-lina');
  await page.getByLabel('Correio eletrónico').fill(CUSTOMER_EMAIL);
  await page.getByLabel('Palavra-passe').fill(DEMO_PASSWORD);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/portfolio/, { timeout: 60_000 });
}

async function loginAsFlorist(page: import('@playwright/test').Page) {
  // A loja vem do ?tenant= do endereço: o login já não tem seletor de loja.
  await page.goto('/login?tenant=flores-lina');
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
    // Passo 1: o arranjo. Passo 2: a entrega. Passo 3: confirmar e submeter.
    await page.getByRole('button', { name: /^Aumentar quantidade/ }).first().click();
    await page.getByRole('button', { name: 'Continuar para entrega' }).first().click();
    await page.getByRole('button', { name: 'Continuar para confirmação' }).click();
    await page.getByRole('checkbox', { name: /aceito os.*Termos/i }).check();
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
