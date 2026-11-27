# Capturas de ecrã (web)

Imagens geradas com API de desenvolvimento, seed de demonstração e Vite (`web/scripts/capture-demo-screenshots.mjs`).

## Loja (cliente)

| Ficheiro | Descrição |
|----------|-----------|
| [01-loja-portfolio.png](./01-loja-portfolio.png) | Portfolio público com foto real |
| [02-montar-checkout.png](./02-montar-checkout.png) | Montar arranjo / fluxo de checkout |
| [03-encomendas-cliente.png](./03-encomendas-cliente.png) | Lista de encomendas do cliente |
| [04-encomenda-detalhe.png](./04-encomenda-detalhe.png) | Detalhe com proposta de alteração |
| [05-login.png](./05-login.png) | Ecrã de login |
| [30-configurador-esponja-vaso.png](./30-configurador-esponja-vaso.png) | Configurador 3D: formato Esponja, com vaso |
| [31-configurador-comprido-embrulho.png](./31-configurador-comprido-embrulho.png) | Formato Comprido, sem vaso (papel de embrulho), depois de «Gerar outra variação» |
| [32-configurador-curto.png](./32-configurador-curto.png) | Formato Curto |
| [33-configurador-telemovel.png](./33-configurador-telemovel.png) | O mesmo configurador numa largura de telemóvel |
| [34-configurador-2d.png](./34-configurador-2d.png) | Pré-visualização 2D quando não há WebGL (`webgl=0`) |

As capturas 30–33 vêm de `web/scripts/capture-configurator.mjs` na rota `/embed/configurador?demo=1` (catálogo de exemplo, sem API).

## Gestão (florista)

| Ficheiro | Descrição |
|----------|-----------|
| [10-admin-dashboard.png](./10-admin-dashboard.png) | Dashboard com gráficos interativos e filtro 7/30/90 dias |
| [11-admin-encomendas.png](./11-admin-encomendas.png) | Gestão de encomendas |
| [12-admin-catalogo.png](./12-admin-catalogo.png) | Catálogo com imagem por flor |
| [13-admin-definicoes.png](./13-admin-definicoes.png) | Definições da loja e tema |

## Plataforma

| Ficheiro | Descrição |
|----------|-----------|
| [20-superadmin-lojas.png](./20-superadmin-lojas.png) | Painel SuperAdmin — lista de lojas |

## QA de novembro de 2026

Capturas `qa-2026-11-*-desktop.png` e `qa-2026-11-*-telemovel.png`, geradas pelo Playwright (`web/e2e/novos-fluxos.spec.ts`) em 1280×800 e 390×844. Cobrem a loja, o configurador, cupão, pagamento, fatura de teste, avaliação, recorrência, capacidade, existências, páginas legais, cookies, exportação de dados e métricas do SuperAdmin.

## Android (Roborazzi + Robolectric, JVM)

Geradas com Roborazzi (sem emulador). O CI executa os testes com `-Proborazzi.test.verify=false` (diferenças de rasterização entre runners); antes de commitar PNG novos, correr localmente:

```bash
./gradlew :shared:recordRoborazziDebug -Pkotlin.native.ignoreDisabledTargets=true
./gradlew :shared:testDebugUnitTest -Proborazzi.test.verify=true
```

| Ficheiro | Descrição |
|----------|-----------|
| [android/01-portfolio-loja.png](./android/01-portfolio-loja.png) | Portfolio (loja) |
| [android/02-encomenda-detalhe.png](./android/02-encomenda-detalhe.png) | Detalhe de encomenda com proposta |
| [android/03-montar-checkout.png](./android/03-montar-checkout.png) | Montar arranjo / checkout |
| [android/04-florist-dashboard.png](./android/04-florist-dashboard.png) | Dashboard da florista com métricas |

### Regenerar Android

```bash
cd mobile
./gradlew :shared:recordRoborazziDebug -Pkotlin.native.ignoreDisabledTargets=true
```

As imagens são gravadas em `docs/screenshots/android/` (configurado no módulo `shared`).

## Regenerar web

```bash
# Postgres + API Development (porta 5183) + seed
# cd web && npm run dev
OUT_DIR=docs/screenshots API_BASE=http://127.0.0.1:5183 WEB_BASE=http://127.0.0.1:5173 node web/scripts/capture-demo-screenshots.mjs
```
