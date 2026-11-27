# Testar a app iOS sem Mac (GitHub Actions)

Este guia é para quem desenvolve no **Windows** (ou Linux) e quer **compilar e ver** a app iOS do Florist Hub sem Xcode local. O repositório tem um workflow dedicado em runner **macOS** (custo de minutos **×10** no plano gratuito do GitHub).

**Importante:** o workflow **só corre manualmente** (`workflow_dispatch`). Não dispara em pull requests nem em pushes — para não gastar minutos macOS automaticamente.

Documentação complementar para desenvolvimento com Mac: [ios.md](./ios.md).

## O que o workflow faz

O ficheiro [`.github/workflows/ios-simulator.yml`](../.github/workflows/ios-simulator.yml):

1. Compila o framework Kotlin (`iosSimulatorArm64`) e corre `commonTest` via `:shared:iosSimulatorArm64Test`.
2. Compila a app `iosApp` para o **simulador** (sem assinatura de código).
3. Instala no simulador, arranca a app e tira **capturas de ecrã** dos ecrãs principais.
4. Publica artefactos: PNGs, `.app` do simulador e ZIP.

Por defeito activa `FLORIST_HUB_UI_PREVIEW=YES` (dados **fixture** em `PreviewApiClient`) para as capturas não dependerem da API. O ecrã de **login** aparece nos primeiros segundos antes do tour automático.

## Como lançar o workflow (única forma)

Precisa de permissão para executar Actions no repositório (colaborador com write ou owner).

1. GitHub → repositório **florist-hub** → separador **Actions**.
2. Na barra lateral, escolha o workflow **iOS (simulador)**.
3. Clique **Run workflow** (canto superior direito).
4. **Use workflow from**: escolha o ramo que quer testar (ex. `main` ou `cursor/ios-ci-simulator-1d51` do PR #16).
5. **Usar dados fixture (FLORIST_HUB_UI_PREVIEW) para capturas**:
   - **Marcado** (recomendado): tour automático com dados mock e capturas dos ecrãs principais.
   - **Desmarcado**: app sem tour fixture (útil sobretudo para ver só o ecrã de login sem API).
6. Confirme com **Run workflow**.
7. Abra a execução na lista e aguarde o job **iOS Simulator (KMP + capturas)** (~15–25 min com cache, mais na 1.ª vez).

O [CI normal](../.github/workflows/ci.yml) (Backend, Web, Android em Linux) continua a correr nos PRs e **não** inclui macOS.

## Onde ver capturas e binários

1. Na execução concluída, desça a **「Artifacts」**.
2. Descarregue `ios-simulator-<número-do-run>`.
3. Conteúdo típico:
   - `screenshots/01-simulator.png` … sequência alinhada ao tour (login → home → portfolio → montar arranjo/checkout → encomendas → dashboard florista → definições).
   - `app/iosApp.app` — bundle do **simulador** (não instala num iPhone físico).
   - `app/iosApp-simulator.zip` — o mesmo, comprimido.

## Minutos GitHub (plano gratuito)

| Fase | Duração típica (minutos reais) | Contabilização |
|------|-------------------------------|----------------|
| 1.ª execução (sem cache Konan/Gradle) | 25–45 | ×10 no macOS |
| Execuções seguintes (com cache) | 15–25 | ×10 |

**Exemplo:** 20 minutos reais no runner ≈ **200 minutos** debitados da quota mensal.

Para poupar:

- O workflow **não** corre sozinho — só quando alguém clica **Run workflow**.
- O `concurrency` cancela, na mesma ref, uma execução manual antiga se iniciar outra.
- Validação móvel «barata» nos PRs: job Android em `ci.yml` (runner Linux).

## Instalar num iPhone real

| Objetivo | Possível sem 99 USD/ano? |
|----------|---------------------------|
| Simulador + capturas no GitHub | **Sim** |
| `.app` do simulador no artefacto | **Sim** |
| TestFlight / App Store | **Não** — [Apple Developer Program](https://developer.apple.com/programs/) |
| Instalar no seu iPhone por 7 dias com conta Apple gratuita | Só com **Mac + Xcode** (ver [ios.md](./ios.md)) |

Caminho futuro **TestFlight** (desactivado): `mobile/iosApp/fastlane/Fastfile` — requer secrets e certificados de distribuição.

## Configurar a URL da API (build local no Mac)

Em `mobile/iosApp/Configuration/Config.xcconfig`:

```text
FLORIST_HUB_API_BASE_URL = http://127.0.0.1:5183
```

No simulador, `127.0.0.1` é o Mac anfitrião. No CI com preview, esta URL não é usada.

## Alternativas sem Mac

- **GitHub Actions** (este repo) — minutos macOS ×10, mas zero hardware.
- **Mac na nuvem pago à hora** (MacStadium, AWS EC2 Mac, Scaleway, etc.) — útil para depuração interactiva; custo por hora, sem multiplicador GitHub.
- **Codemagic / Bitrise** — tiers gratuitos limitados; mesmo requisito de runners Apple para builds iOS.

## Limitações conhecidas

- Não há testes `iosTest` dedicados além do que corre em `iosSimulatorArm64Test` / `commonTest`.
- Capturas com fixture **não** substituem testes contra a API real; para isso seria preciso subir PostgreSQL + API no job (mais minutos).
- `iosX64` não é suportado (removido); o alvo é **iosSimulatorArm64** (Apple Silicon nos runners `macos-latest`).

## Comandos locais (só em Mac)

```bash
cd mobile
./scripts/ios-simulator-ci.sh
```

Requer Xcode e JDK 21. Gera `ios-simulator-artifacts/` na raiz do repo.
