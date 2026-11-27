# CI no GitHub Actions

Este repositório usa o [GitHub Actions](https://docs.github.com/en/actions) com foco em **poucos minutos faturados**: jobs condicionais, cancelamento de execuções obsoletas e caches agressivos.

## Quando o CI corre

| Gatilho | Condição |
|---------|----------|
| `pull_request` | Para `main`, tipos `opened`, `synchronize`, `reopened`, `ready_for_review`, **e** o PR não está em **draft** |
| `push` | Apenas no branch `main` (pushes a outros branches **não** disparam o CI) |

Alterações **só** em ficheiros `*.md` ou em `docs/**` **não** disparam o workflow (`paths-ignore`).

O workflow **iOS** (`ios-simulator.yml`) é **manual** (`workflow_dispatch`) e não entra no CI habitual.

## Concorrência

Todos os workflows usam:

```yaml
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

Uma nova execução no mesmo PR ou branch **cancela** a anterior, evitando minutos em filas redundantes.

## Jobs e filtros de caminhos

O job `Deteção de alterações` (`dorny/paths-filter`) define o que corre:

| Job | Corre quando |
|-----|----------------|
| **Backend (.NET 10)** | Alterações em `backend/**` (build + todos os testes, incluindo integração com Postgres) |
| **Backend (integração API)** | Alterações em `web/**` **sem** alterações em `backend/**` (só o projeto de integração — evita repetir o job completo do backend) |
| **Web (React + Vite)** | Alterações em `web/**` |
| **Web E2E (Playwright)** | Alterações em `backend/**` **ou** `web/**` |
| **Android (assembleDebug)** | Alterações em `mobile/**` |

Alterações só em `.github/workflows/` (sem código de app) podem fazer correr apenas o job de deteção, sem build — útil para afinar o YAML com custo mínimo.

## Playwright

- Um único browser: **Chromium** (`npx playwright install --with-deps chromium`).
- API em `127.0.0.1:5183`, preview Vite em `127.0.0.1:4173`.
- Especificações em `web/e2e/`.

Antes de abrir PR, corre localmente:

```bash
# API + Postgres (ver README / scripts locais)
cd web && npm ci && npm run build
VITE_API_BASE_URL=http://127.0.0.1:5183 npm run preview -- --host 127.0.0.1 --port 4173 &
PLAYWRIGHT_BASE_URL=http://127.0.0.1:4173 npm run test:e2e
```

## Caches e timeouts

| Stack | Mecanismo |
|-------|-----------|
| .NET | `actions/cache` em `~/.nuget/packages` |
| Node | `actions/setup-node` com `cache: npm` (`web/package-lock.json`) |
| Gradle (Android) | `actions/setup-java` com `cache: gradle` + `gradle/actions/setup-gradle` |

Cada job tem `timeout-minutes` (5–45 conforme o trabalho) para evitar runners presos.

## Deploy

Os workflows de deploy (`deploy-api`, `deploy-web`, `android-release`, `backup-postgres`) só correm com **workflow_dispatch**. Não há `schedule` nem disparo em push. O caminho que não gasta minutos é o deploy nativo do Render e do Cloudflare Pages a partir do Git. Ver [deploy.md](./deploy.md).

## iOS

Ver [ios.md](./ios.md). O workflow `iOS (simulador)` corre só quando alguém o dispara manualmente no GitHub (Actions → iOS (simulador) → Run workflow).

## PRs em draft

Enquanto o PR estiver em draft, **nenhum** job de build/teste corre. Ao marcar **Ready for review**, o evento `ready_for_review` dispara o CI na condição habitual.
