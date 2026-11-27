# Relatório — design system Florist Hub

## Follow-up pós-merge (#11) — polimento

### Dashboard admin (web)
- Secções «Encomendas por estado» e «Itens mais pedidos» com chips de estado (cores do tema) e barras horizontais simples — sem novos endpoints.
- Estilos em `web/src/styles/components.css` (`.dashboard-chart`, `.dashboard-bar-*`).

### Plurais (PT)
- Utilitário `web/src/lib/pluralPt.ts` (ex.: «1 linha» em vez de «1 linhas»).
- `MyOrdersPage`: «+N item/itens».
- App móvel: `PluralPt.kt` e contagens «N encomenda(s) hoje/amanhã» no dashboard da florista.

### Imagens do seed (Pexels, no repositório)
- JPEG em `backend/src/FloristHub.Api/demo-assets/`.
- `DevelopmentDemoAssetsInstaller` copia para `uploads/demo/` em Development.
- Seed e patch do tenant `flores-lina` apontam para `/api/files/demo/flores-lina-logo.jpg` e `bouquet-primaveril.jpg`.
- Licenças e atribuição: [docs/demo-image-licenses.md](./demo-image-licenses.md).

### Capturas web

Gerar com API + seed + Vite:

```bash
# Terminal 1: postgres (docker compose up -d) e API Development
# Terminal 2: cd web && npm run dev
OUT_DIR=docs/screenshots-demo node web/scripts/capture-demo-screenshots.mjs
```

| Ficheiro | Conteúdo |
|----------|----------|
| `01-portfolio-loja.png` | Loja com foto real do portfolio |
| `02-montar-arranjo.png` | Montar arranjo |
| `03-encomendas-cliente.png` | Lista de encomendas (cliente) |
| `04-encomenda-timeline.png` | Detalhe com proposta |
| `05-login.png` | Login sem sessão |
| `10-admin-dashboard.png` | Dashboard com gráficos de barras |
| `11-admin-encomendas.png` | Gestão de encomendas |
| `12-admin-catalogo.png` | Catálogo |
| `13-admin-definicoes.png` | Definições da loja |

As PNG ficam em `docs/screenshots-demo/` (versionadas após correr o script localmente).

App Android: emulador indisponível nesta execução Cloud Agent (sem capturas mobile).

## Histórico — PR #11 (merged)

### Logo no header
- `PlatformLogoMark`: SVG inline; `Logo.tsx` com `onError` para fallback.
- CORS dev: origens `127.0.0.1` para o Vite.

### API / capturas
- JWT por defeito em `/api/*`; query do dashboard (top itens) corrigida para EF.
- Script `web/scripts/capture-demo-screenshots.mjs` com sessão injetada.

## Build / testes

- `npm run build` e `npm run lint` (web)
- `dotnet test backend/FloristHub.sln` (backend)
- `./gradlew :androidApp:assembleDebug` (mobile, requer Android SDK)
