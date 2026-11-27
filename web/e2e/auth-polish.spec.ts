import { expect, test } from '@playwright/test';

test.describe('autenticação e sessão', () => {
  test('login com credenciais erradas mostra mensagem legível', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Correio eletrónico').fill('inexistente@example.com');
    await page.getByLabel('Palavra-passe').fill('palavra-passe-errada');
    await page.getByRole('button', { name: 'Entrar' }).click();
    const alert = page.getByRole('alert');
    await expect(alert).toBeVisible({ timeout: 30_000 });
    await expect(alert).toContainText(/Email ou palavra-passe incorretos/i);
    await expect(alert).not.toContainText('{"message"');
  });

  test('SuperAdmin pode sair e regressar ao login', async ({ page }) => {
    const adminEmail = process.env.E2E_SUPERADMIN_EMAIL ?? 'admin@demo.floristhub.pt';
    const password = process.env.E2E_DEMO_PASSWORD ?? 'DemoFlorist1!';

    await page.goto('/login');
    await page.getByLabel('Correio eletrónico').fill(adminEmail);
    await page.getByLabel('Palavra-passe').fill(password);
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page).toHaveURL(/\/superadmin/, { timeout: 60_000 });

    await page.getByRole('button', { name: 'Sair' }).click();
    await expect(page).toHaveURL(/\/login/, { timeout: 15_000 });
    await expect(page.getByRole('heading', { name: /^Entrar/ })).toBeVisible();
  });
});
