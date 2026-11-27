import { expect, test, type Page } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const DEMO_PASSWORD = 'DemoFlorist1!';
const CUSTOMER_EMAIL = 'cliente@demo.floristhub.pt';
const FLORIST_EMAIL = 'florista@demo.floristhub.pt';
const ADMIN_EMAIL = 'admin@demo.floristhub.pt';
const LINA_TENANT_ID = '11111111-1111-1111-1111-111111111101';
const SHOTS = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../docs/screenshots');

const viewports = [
  { id: 'desktop', width: 1280, height: 800 },
  { id: 'telemovel', width: 390, height: 844 },
] as const;

function localIso(daysAhead: number) {
  const date = new Date();
  date.setHours(12, 0, 0, 0);
  date.setDate(date.getDate() + daysAhead);
  while (date.getDay() === 0) date.setDate(date.getDate() + 1);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function watchConsole(page: Page) {
  const problems: string[] = [];
  page.on('console', (message) => {
    if (message.type() !== 'error') return;
    const text = message.text();
    if (/favicon|Download the React DevTools|THREE\.|WebGL/i.test(text)) return;
    problems.push(text);
  });
  page.on('pageerror', (error) => problems.push(error.message));
  return problems;
}

async function shot(page: Page, viewport: string, name: string) {
  mkdirSync(SHOTS, { recursive: true });
  await page.screenshot({
    path: path.join(SHOTS, `qa-2026-11-${name}-${viewport}.png`),
    fullPage: true,
  });
}

async function acceptCookies(page: Page) {
  const banner = page.getByRole('dialog', { name: 'Consentimento de cookies' });
  if (await banner.isVisible().catch(() => false)) {
    await banner.getByRole('button', { name: 'Apenas essenciais' }).click();
    await expect(banner).toBeHidden();
  }
}

async function resetSession(page: Page) {
  await page.goto('/login?tenant=flores-lina');
  await page.evaluate(() => localStorage.clear());
  await page.goto('/login?tenant=flores-lina');
  await acceptCookies(page);
}

async function selectDemoStore(page: Page) {
  const store = page.getByLabel('Loja');
  await expect(store).toBeVisible({ timeout: 30_000 });
  await expect.poll(async () => store.locator('option').count()).toBeGreaterThan(1);
  await store.selectOption(LINA_TENANT_ID);
}

async function login(page: Page, email: string, role: 'cliente' | 'florista' | 'superadmin') {
  await resetSession(page);
  if (role === 'superadmin') {
    await page.getByLabel('Entrar como SuperAdmin (plataforma)').check();
  } else {
    await selectDemoStore(page);
  }
  await page.getByLabel('Correio eletrónico').fill(email);
  await page.getByLabel('Palavra-passe').fill(DEMO_PASSWORD);
  await page.getByRole('button', { name: 'Entrar' }).click();
  if (role === 'superadmin') await expect(page).toHaveURL(/\/superadmin/, { timeout: 60_000 });
  else if (role === 'florista') await expect(page).toHaveURL(/\/(admin|florista)/, { timeout: 60_000 });
  else await expect(page).toHaveURL(/\/portfolio/, { timeout: 60_000 });
}

async function pickOpenSlot(page: Page) {
  const dateSelect = page.getByRole('group', { name: 'Data e hora pretendidas' }).getByLabel('Data');
  const count = await dateSelect.locator('option:not([disabled])').count();
  for (let index = 0; index < count; index++) {
    const value = await dateSelect.locator('option:not([disabled])').nth(index).getAttribute('value');
    if (!value) continue;
    await dateSelect.selectOption(value);
    const time = page.getByRole('group', { name: 'Data e hora pretendidas' }).getByLabel(/Hora/);
    if ((await time.locator('option').count()) > 1) {
      await time.selectOption({ index: 1 });
      return;
    }
  }
  throw new Error('Não há horário com vaga.');
}

test.describe('Fluxos novos da loja', () => {
  for (const viewport of viewports) {
    test(`cliente, florista e SuperAdmin em ${viewport.id}`, async ({ page }) => {
      test.setTimeout(240_000);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      const problems = watchConsole(page);
      const coupon = `QA${Date.now().toString().slice(-6)}`;
      const closedDay = localIso(18);
      const reviewText = `Arranjo impecável ${coupon}`;

      await page.goto('/?tenant=flores-lina');
      await expect(page.getByRole('dialog', { name: 'Consentimento de cookies' })).toBeVisible();
      await shot(page, viewport.id, 'cookies');
      await page.getByRole('button', { name: 'Apenas essenciais' }).click();
      await expect(page.getByRole('heading', { name: /Flores da Lina|Lina/i })).toBeVisible({ timeout: 30_000 });
      await shot(page, viewport.id, 'inicio');

      await page.goto('/flores-da-epoca?tenant=flores-lina');
      await expect(page.getByRole('heading', { name: 'Flores da época' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Rosa vermelha' })).toBeVisible();
      await shot(page, viewport.id, 'flores-da-epoca');

      await page.goto('/portfolio?tenant=flores-lina');
      await expect(page.getByText(/Buquê primaveril/i)).toBeVisible();
      await page.getByRole('link', { name: /Buquê primaveril/i }).first().click();
      await expect(page).toHaveURL(/portfolio\/buque-primaveril/);
      await shot(page, viewport.id, 'portfolio');

      for (const doc of ['termos', 'privacidade', 'cookies'] as const) {
        await page.goto(`/legal/${doc}?tenant=flores-lina`);
        await expect(page.locator('article, .page').first()).toBeVisible();
        await expect(page.getByText(/não encontrámos|não encontrado/i)).toHaveCount(0);
        await shot(page, viewport.id, `legal-${doc}`);
      }

      await login(page, FLORIST_EMAIL, 'florista');
      await page.goto('/admin');
      await expect(page.getByRole('heading', { name: 'Painel' })).toBeVisible();
      await shot(page, viewport.id, 'painel-florista');

      await page.goto('/admin/catalogo');
      await expect(page.getByRole('heading', { name: 'Catálogo' })).toBeVisible();
      await page.getByPlaceholder('Pesquisar…').fill('Rosa vermelha');
      const roseRow = page.getByRole('row', { name: /Rosa vermelha/ });
      await expect(roseRow).toBeVisible();
      const stockBefore = Number((await roseRow.getByRole('cell').nth(4).innerText()).replace(/[^\d]/g, ''));
      await roseRow.getByRole('button', { name: 'Editar' }).click();
      await shot(page, viewport.id, 'stock');

      await page.goto('/admin/capacidade');
      await expect(page.getByRole('heading', { name: 'Capacidade de entrega' })).toBeVisible();
      await page.getByLabel('Data', { exact: true }).fill(closedDay);
      await page.getByLabel('Fechado (folga ou feriado)').check();
      await page.getByLabel('Nota').fill('Sem vaga de teste');
      await page.getByRole('button', { name: 'Guardar exceção' }).click();
      await expect(page.getByText('Exceção guardada.')).toBeVisible();
      await shot(page, viewport.id, 'capacidade');

      await page.goto('/admin/cupoes');
      await page.getByLabel('Código').fill(coupon);
      await page.getByLabel('Desconto').fill('10');
      await page.getByRole('button', { name: 'Criar cupão' }).click();
      await expect(page.getByText('Cupão guardado')).toBeVisible();
      await expect(page.getByRole('cell', { name: coupon })).toBeVisible();
      await shot(page, viewport.id, 'cupoes');

      await page.goto('/admin/campanhas');
      await page.getByLabel('Nome').fill(`Campanha ${coupon}`);
      await page.getByLabel('Descrição').fill('Rosas da época com preço especial.');
      await page.getByLabel('Início').fill(localIso(0));
      await page.getByLabel('Fim').fill(localIso(20));
      await page.getByLabel('Flor em destaque').selectOption({ label: 'Rosa vermelha' });
      await page.getByLabel('Preço especial (€)').fill('1.50');
      await page.getByRole('button', { name: 'Criar campanha' }).click();
      await expect(page.getByText('Campanha guardada.')).toBeVisible();
      await shot(page, viewport.id, 'campanhas');

      await page.goto('/admin/faturacao');
      await page.getByLabel('Nome legal').fill('Flores da Lina Lda');
      await page.getByLabel('NIF').fill('999999990');
      await page.getByLabel('Morada').fill('Rua das Flores 1');
      await page.getByLabel('Localidade').fill('Lisboa');
      await page.getByLabel('Código postal').fill('1200-018');
      await page.getByLabel('Fornecedor').selectOption('0');
      await page.getByLabel('Emitir automaticamente quando o pagamento é confirmado').check();
      await page.getByRole('button', { name: 'Guardar' }).click();
      await expect(page.getByText('Dados de faturação guardados')).toBeVisible();
      await shot(page, viewport.id, 'faturacao');

      await page.goto('/admin/recorrencias');
      await expect(page.getByRole('heading', { name: 'Recorrências' })).toBeVisible();
      await shot(page, viewport.id, 'recorrencias-florista');
      await page.goto('/admin/rgpd');
      await expect(page.getByRole('heading').first()).toBeVisible();
      await shot(page, viewport.id, 'rgpd-florista');

      await login(page, CUSTOMER_EMAIL, 'cliente');
      await page.goto('/?tenant=flores-lina');
      await expect(page.getByText('Campanha em destaque')).toBeVisible({ timeout: 30_000 });
      await page.goto(`/campanha/campanha-${coupon.toLowerCase()}?tenant=flores-lina`);
      await expect(page.getByRole('heading', { name: `Campanha ${coupon}` })).toBeVisible();
      await expect(page.getByText('Rosa vermelha')).toBeVisible();
      await shot(page, viewport.id, 'campanha-publica');

      await page.goto('/montar?tenant=flores-lina');
      await expect(page.getByRole('heading', { name: 'Montar arranjo' })).toBeVisible();
      await expect(page.getByLabel('Pré-visualização 3D do arranjo').or(page.getByLabel('Pré-visualização 2D do arranjo'))).toBeVisible({ timeout: 30_000 });
      const closedOption = page.locator(`option[value="${closedDay}"]`);
      await expect(closedOption).toBeDisabled();
      await expect(closedOption).toContainText('sem vaga');
      await page
        .getByRole('listitem')
        .filter({ hasText: 'Rosa vermelha' })
        .getByRole('button', { name: 'Aumentar quantidade' })
        .click();
      await pickOpenSlot(page);
      await page.getByLabel('Cupão').fill(coupon);
      await page.getByRole('checkbox', { name: /Termos e Condições/ }).check();
      await shot(page, viewport.id, 'montar');
      await page.getByRole('button', { name: 'Submeter encomenda' }).click();
      const submitted = page.getByText(/Encomenda submetida\. Referência #/);
      await expect(submitted).toBeVisible({ timeout: 60_000 });
      const reference = ((await submitted.textContent()) ?? '').match(/#([0-9a-f]{8})/i)?.[1];
      expect(reference).toBeTruthy();

      await page.goto('/encomendas');
      await page.getByRole('link', { name: `#${reference}` }).click();
      await expect(page.getByText(new RegExp(`Desconto \\(${coupon}\\)`))).toBeVisible();
      await shot(page, viewport.id, 'encomenda-desconto');

      await login(page, FLORIST_EMAIL, 'florista');
      await page.goto('/admin/encomendas');
      const orderCard = page.locator('article', { has: page.getByRole('heading', { name: `#${reference}` }) });
      await orderCard.getByRole('button', { name: 'Confirmar' }).click();
      await expect(orderCard.getByRole('button', { name: 'Marcar em preparação' })).toBeVisible({ timeout: 30_000 });

      await page.goto('/admin/catalogo');
      await page.getByPlaceholder('Pesquisar…').fill('Rosa vermelha');
      const roseAfter = page.getByRole('row', { name: /Rosa vermelha/ });
      await expect.poll(async () => Number((await roseAfter.getByRole('cell').nth(4).innerText()).replace(/[^\d]/g, ''))).toBe(stockBefore - 1);
      await roseAfter.getByRole('button', { name: 'Editar' }).click();
      await expect(page.getByText(/-1 →/).first()).toBeVisible();

      await login(page, CUSTOMER_EMAIL, 'cliente');
      await page.goto('/encomendas');
      await page.getByRole('link', { name: `#${reference}` }).click();
      await page.getByRole('checkbox', { name: /Termos e Condições/ }).check();
      await page.getByRole('button', { name: 'Pagar' }).click();
      await expect(page.getByRole('heading', { name: 'Pagamento recebido' })).toBeVisible({ timeout: 60_000 });
      await shot(page, viewport.id, 'pagamento');
      await page.getByRole('link', { name: 'Voltar à encomenda' }).click();
      await expect(page.getByRole('heading', { name: 'Faturas' })).toBeVisible({ timeout: 30_000 });

      await login(page, FLORIST_EMAIL, 'florista');
      await page.goto('/admin/encomendas');
      const paidCard = page.locator('article', { has: page.getByRole('heading', { name: `#${reference}` }) });
      for (const label of ['Marcar em preparação', 'Marcar pronta', 'Marcar entregue']) {
        await paidCard.getByRole('button', { name: label }).click();
        await expect(paidCard).toBeVisible();
      }
      await expect(paidCard.getByText(/Entregue/)).toBeVisible({ timeout: 30_000 });

      await login(page, CUSTOMER_EMAIL, 'cliente');
      await page.goto('/encomendas');
      await page.getByRole('link', { name: `#${reference}` }).click();
      await expect(page.getByRole('heading', { name: 'Avaliar encomenda' })).toBeVisible();
      await page.getByLabel('Comentário').fill(reviewText);
      await page.getByRole('button', { name: 'Enviar avaliação' }).click();
      await expect(page.getByText(reviewText)).toBeVisible();
      await shot(page, viewport.id, 'avaliacao');

      await page.goto('/recorrencias');
      await page.getByRole('button', { name: 'Nova recorrência' }).click();
      const when = `${localIso(3)}T10:00`;
      await page.getByLabel('Primeira entrega').fill(when);
      await page.getByRole('checkbox', { name: /Termos e Condições/ }).check();
      await page.getByRole('button', { name: 'Pedir subscrição' }).click();
      await expect(page.getByText('Semanal').first()).toBeVisible({ timeout: 30_000 });
      await shot(page, viewport.id, 'recorrencias-cliente');

      await page.goto('/privacidade/dados');
      await expect(page.getByRole('heading', { name: 'Privacidade e dados' })).toBeVisible();
      const download = page.waitForEvent('download');
      await page.getByRole('button', { name: 'Exportar os meus dados (JSON)' }).click();
      expect((await download).suggestedFilename()).toMatch(/floristhub-dados-.*\.json/);
      await expect(page.getByText('Exportação concluída.')).toBeVisible();
      await shot(page, viewport.id, 'exportacao-dados');

      await login(page, FLORIST_EMAIL, 'florista');
      await page.goto('/admin/avaliacoes');
      await expect(page.getByText(reviewText)).toBeVisible();
      const reviewCard = page.locator('article', { hasText: reviewText });
      await reviewCard.getByLabel('Responder').fill('Obrigado pela visita.');
      await reviewCard.getByRole('button', { name: 'Publicar resposta' }).click();
      await expect(reviewCard.getByText('Resposta: Obrigado pela visita.')).toBeVisible();
      await shot(page, viewport.id, 'avaliacoes-florista');

      await login(page, ADMIN_EMAIL, 'superadmin');
      await page.goto('/superadmin/metricas');
      await expect(page.getByRole('heading', { name: 'Métricas da plataforma' })).toBeVisible();
      await expect(page.getByText('Lojas ativas')).toBeVisible();
      await shot(page, viewport.id, 'metricas');
      await page.goto('/superadmin/rgpd');
      await expect(page.getByRole('heading', { name: 'Auditoria RGPD (plataforma)' })).toBeVisible();
      await shot(page, viewport.id, 'rgpd-plataforma');

      expect(problems, problems.join('\n')).toEqual([]);
    });
  }
});
