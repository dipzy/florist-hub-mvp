# Deploy do Florist Hub

Do zero até à loja online, no alojamento gratuito. Não é preciso correr GitHub Actions: o Render e o Cloudflare Pages constroem a partir do Git. Os workflows manuais existem para quem quiser dispará-los à mão.

Limites e preços dos planos: [alojamento-gratuito.md](./alojamento-gratuito.md). Variáveis, sem segredos: [`.env.example`](../.env.example).

| Parte | Onde | Custo |
| --- | --- | --- |
| API | Render Free (alternativa: Koyeb Free, Frankfurt) | 0 |
| Postgres | Neon Free, região UE | 0 |
| Imagens e cópias | Cloudflare R2 | 0 dentro da quota |
| Web | Cloudflare Pages | 0 |
| Domínio `floristhub.pt` | Registrar (Cloudflare ou outro) | o preço do domínio |
| Google Play | Conta de programador | 25 USD, uma vez |
| App Store | Apple Developer Program | 99 USD/ano |

O plano gratuito da API hiberna. O primeiro pedido depois de ~15 min parado no Render demora cerca de um minuto, mais o tempo de o Neon acordar. Não há disco persistente: as chaves de Data Protection e as fotos não podem ficar no contentor.

## 1. Contas

1. [Neon](https://neon.tech) — projeto na UE (Frankfurt, se existir).
2. [Cloudflare](https://dash.cloudflare.com) — R2 e Pages. A activação do R2 pode pedir um método de pagamento; o uso dentro da quota gratuita não deve gerar factura. Confirme no ecrã antes de continuar.
3. [Render](https://render.com) — sem cartão, se a conta o permitir.
4. Stripe, Resend e Firebase quando for a altura (secções 7 a 9). InvoiceXpress é uma conta **por florista**, no ecrã da loja, não uma variável da API.

## 2. Neon

No projeto, copie as duas connection strings:

- **Pooled** (`-pooler` no host) → `DATABASE_URL`
- **Direct** (sem `-pooler`) → `DATABASE_URL_UNPOOLED`

As duas devem trazer `sslmode=require`. A API, se o host contiver `neon.tech` e o modo SSL for mais fraco do que `Require`, sobe-o para `Require`. No host `-pooler` desliga prepared statements automáticos e multiplexing, porque o PgBouncer em modo transação não os aguenta.

Não preencha `ConnectionStrings__DefaultConnection` em produção se usar `DATABASE_URL`: a connection string explícita ganha à URI.

As migrações usam `DATABASE_URL_UNPOOLED` (ou `ConnectionStrings__Migrations`). O `pg_dump` também. A app em funcionamento usa a pooled.

## 3. Cloudflare R2

1. Crie um bucket, por exemplo `florist-hub`, classe Standard.
2. Em R2 → Manage R2 API tokens, crie um token com leitura e escrita nesse bucket. Guarde o Access Key ID, o Secret e o endpoint `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`.
3. Active o acesso público de leitura (subdomínio `r2.dev` ou um domínio próprio) e copie esse URL para `Storage__S3__PublicBaseUrl`. A API grava o objeto e devolve esse URL. Sem URL pública, o site não mostra as fotos.
4. Na API: `Storage__Provider=R2`, `Storage__S3__Region=auto`, `Storage__S3__ForcePathStyle=true`, mais `ServiceUrl`, `Bucket` e as chaves. O cliente é o AWS SDK com a API S3; o checksum extra do SDK 4 fica desligado porque o R2 não o aceita.

O mesmo bucket pode guardar as cópias em `backups/floristhub/`. Pode usar outro token só com esse prefixo.

## 4. API no Render

O ficheiro [`render.yaml`](../render.yaml) é o blueprint.

1. Render → New → Blueprint → repositório `florist-hub`, branch `main`.
2. Se o plano gratuito recusar a região `frankfurt`, edite `region` para `oregon` e aceite a latência (ou use o Koyeb em Frankfurt). O Neon deve ficar o mais perto possível da API por causa dos 5 GB/mês de saída.
3. Preencha os valores `sync: false` com o conteúdo de `.env.example`. `Database__MigrateOnStartup` fica `false`: cada deploy corre antes `dotnet FloristHub.Api.dll --migrate`. Acordar do sono **não** repete as migrações.
4. O health check é `GET /health`. Responde sem abrir a base de dados, para o Render não marcar a instância como morta enquanto o Neon acorda noutro pedido.
5. `autoDeployTrigger: commit` faz o deploy em cada push a `main` e **não** espera pelos checks do GitHub Actions.

O contentor escuta `http://0.0.0.0:$PORT`. O TLS acaba no proxy. Em produção a app lê `X-Forwarded-Proto` e `X-Forwarded-Host` e não redirecciona para HTTPS dentro do contentor. Os logs são Serilog na consola (o disco é efémero). O nível é `Serilog__MinimumLevel__Default`.

As chaves de Data Protection (`PersistKeysToDbContext`) ficam na tabela `DataProtectionKeys`. Sem essa tabela no restauro, as chaves InvoiceXpress cifradas deixam de abrir. O seed de demonstração não corre com `ASPNETCORE_ENVIRONMENT=Production`.

Comando manual de migração, na imagem:

```bash
dotnet FloristHub.Api.dll --migrate
```

### Koyeb, se não usar o Render

Crie um Web Service a partir do mesmo Dockerfile (`backend/Dockerfile`, contexto `backend`), porto 8080, health check `/health`, região Frankfurt. Não há `preDeployCommand`: ponha `Database__MigrateOnStartup=true`. As outras variáveis são as mesmas. A instância escala para zero ao fim de cerca de uma hora sem tráfego.

### Arranque local da imagem

```bash
docker compose --profile api up -d --build
curl -fsS http://127.0.0.1:8080/health
```

Isto sobe a API e o Postgres. A password do compose é só para essa máquina. `docker compose up -d` sem o perfil continua a subir apenas a base.

## 5. Web no Cloudflare Pages

No Pages → Create → ligar o repositório Git:

| Campo | Valor |
| --- | --- |
| Root directory | `web` |
| Build command | `npm ci && npm run build` |
| Build output | `dist` |
| Node | 22 |

Variáveis de **build** (entram no JavaScript):

- `VITE_API_BASE_URL` = URL HTTPS da API, sem barra final
- `VITE_BASE_DOMAIN` = `floristhub.pt` (ou vazio enquanto só existir `pages.dev`)
- `VITE_SENTRY_DSN` só se quiser Sentry no browser, e mesmo assim só depois do consentimento de cookies

Variável de **runtime** (a Function de SEO não vê o valor que o Vite embutiu):

- `API_BASE_URL` = o mesmo URL da API

O `web/public/_redirects` envia rotas desconhecidas para `index.html` (SPA). O `web/public/_headers` evita cache longo do `index.html` e marca os ficheiros em `/assets/` como imutáveis.

### SEO

A API já publica, no host da loja:

- `GET /robots.txt` e `GET /sitemap.xml`
- `GET /api/seo/documento?caminho=/portfolio/...` com HTML, Open Graph e JSON-LD

A Function `web/functions/_middleware.js` corre no Pages **antes** do redirect da SPA e aponta para as rotas públicas da API:

- `GET /robots.txt` → `GET /robots.txt` (a API também aceita `/api/seo/robots.txt`)
- `GET /sitemap.xml` → `GET /sitemap.xml` (alias `/api/seo/sitemap.xml`)
- HTML de crawler, em GET de uma rota da SPA, → `GET /api/seo/documento?caminho=...`

O pedido leva `X-Forwarded-Host` com o host da loja, para o tenant e os URLs canónicos não ficarem com o host do Render. Se a API não responder (sono do plano gratuito, loja por resolver), `/robots.txt` fica um texto mínimo e a página HTML cai na SPA.

Um crawler logo a seguir à hibernação pode estourar o tempo da Function. O pedido seguinte, com a API acordada, recebe o HTML.

## 6. Domínios

Enquanto não houver domínio, use `https://<serviço>.onrender.com` e `https://<projeto>.pages.dev`. HTTPS já vem incluído. Ponha o host do Pages em `Cors__AllowedOrigins` e em `App__WebBaseUrl`.

Com `floristhub.pt` na Cloudflare:

1. **API.** No Render, Custom Domain `api.floristhub.pt`. O painel mostra o CNAME. Um único host chega: a loja no browser chama esta API e manda o tenant por header.
2. **Web, apex e www.** No Pages, Custom domains `floristhub.pt` e `www.floristhub.pt`. CNAME (ou ALIAS no apex) para `<projeto>.pages.dev`.
3. **Subdomínio de cada loja.** O browser em `https://flores-lina.floristhub.pt` resolve o slug `flores-lina` porque `VITE_BASE_DOMAIN=floristhub.pt`. A API aceita essa origem HTTPS porque o host é o `App__BaseDomain` ou um subdomínio dele.
   - No DNS, CNAME `flores-lina` → `<projeto>.pages.dev`.
   - No Pages, adicione o mesmo hostname em Custom domains, para existir certificado.
   - Se o painel aceitar o hostname `*.floristhub.pt`, um CNAME `*` chega para as lojas novas. Se recusar, registe cada subdomínio. Não conte com um wildcard que o produto não mostre.
4. **Domínio próprio de uma florista** (`www.floresdalina.pt`). CNAME para o projeto Pages, adicionar o hostname no Pages, e gravar esse host no campo de domínio da loja (SuperAdmin). A API passa a aceitar a origem depois de a ler da base (cache de cerca de 5 minutos) e o middleware resolve o tenant por esse host.

`App__BaseDomain` na API e `VITE_BASE_DOMAIN` na web têm de ser o mesmo apex. Sem isso, o subdomínio não escolhe a loja.

Em localhost continua `http://localhost:5173/?tenant=flores-lina` ou `VITE_DEFAULT_TENANT_SLUG`.

## 7. Stripe em produção

Sem `Stripe__SecretKey` a loja funciona e os pagamentos ficam desligados. Para cobrar:

1. No [Dashboard Stripe](https://dashboard.stripe.com), passe a **modo live**. Active o Connect (contas Express, país PT) e os métodos que quiser oferecer: cartão, MB Way, Multibanco. Cada florista completa o onboarding no painel da loja; a API cria a conta Express.
2. Copie a secret key `sk_live_…` para `Stripe__SecretKey` e a publishable `pk_live_…` para `Stripe__PublishableKey`.
3. Developers → Webhooks → Add endpoint.
   - URL: `https://api.floristhub.pt/api/stripe/webhook` (o host real da API).
   - Active **Listen to events on Connected accounts**. O dinheiro entra na conta da florista; sem isto os eventos `checkout.session.completed` da conta ligada não chegam.
   - Eventos: `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`, `payment_intent.payment_failed`, `charge.refunded`, `account.updated`.
4. O segredo `whsec_…` desse endpoint vai para `Stripe__WebhookSecret`. É diferente do `whsec_` do Stripe CLI local.
5. `App__WebBaseUrl` tem de ser a web pública. É o regresso do pagamento quando o cliente não manda um URL, e a base dos links nos emails. `Email__WebBaseUrl`, se estiver preenchido, substitui esse valor nos convites e nos outros emails.
6. Não defina `Stripe__Provider`. O valor `Fake` só existe em `appsettings.Development.json`. O `render.yaml` e o `.env.example` não o trazem, e com `ASPNETCORE_ENVIRONMENT=Production` a API ignora `Fake`: usa a secret key, ou deixa os pagamentos desligados se ela estiver vazia.
7. Reinicie o serviço depois de gravar as variáveis. Um pagamento de teste em live mode mexe em dinheiro real: use um valor pequeno na primeira encomenda.

O endpoint recusa corpos sem `Stripe-Signature` e responde 503 se a secret key não existir. O mesmo `event.id` não é aplicado duas vezes.

## 8. Resend, Firebase, InvoiceXpress, Sentry

**Resend.** O Render bloqueia SMTP (portos 25, 465, 587). Use a API HTTP: `Email__Provider=Resend`, `Email__ResendApiKey`, `Email__From` num domínio verificado no Resend (SPF e DKIM). Sem chave, a API escreve o email no log e, em Development, devolve o link do convite na resposta. Plano gratuito: 3000 emails/mês e 100/dia. O link do convite é `{base}/convite?userId=…&token=…`. A base é `Email__WebBaseUrl` ou, se estiver vazio, `App__WebBaseUrl`. O convite vale 72 horas e é de uso único.

**Primeira loja.** O seed de demonstração não corre em Production. A primeira loja é criada pelo SuperAdmin em `POST /api/platform/tenants` (ecrã `/superadmin`): nasce por publicar e o convite segue por email. A florista abre o link, define a palavra-passe e completa o assistente `/admin/configuracao-inicial`. A publicação é `POST /api/florist/onboarding/publish`, só quando os obrigatórios estão feitos (dados da loja, cores, catálogo, zona de entrega, termos e Stripe activo ou pagamentos desativados de propósito). Até lá, o catálogo público, o portfolio e as encomendas de clientes respondem que a loja ainda não está publicada. O detalhe dos passos está em [onboarding-florista.md](./onboarding-florista.md).

**Firebase / FCM.** Opcional. `Push__Provider=Fcm`, `Push__FcmProjectId` e o JSON da conta de serviço em `Push__FcmServiceAccountJson` (uma linha). O `google-services.json` fica fora do Git, na máquina que gera o AAB. Sem isto a app abre na mesma e o push fica em log. Ver [notificacoes.md](./notificacoes.md).

**InvoiceXpress.** Cada florista cria a conta e cola a chave no ecrã Faturação. A API cifra-a com as Data Protection keys da base. Não há variável global com essa chave. Ver [faturacao.md](./faturacao.md).

**Sentry.** Opcional. Sem DSN, o SDK não arranca. `SendDefaultPii` fica desligado.

| Onde | Variável | Notas |
| --- | --- | --- |
| API (Render) | `Sentry__Dsn` | Ativa `Sentry.AspNetCore` |
| API | `Sentry__Environment` | Opcional; por omissão `ASPNETCORE_ENVIRONMENT` |
| API | `Sentry__Release` | Opcional; versão/commit do deploy |
| API | `Sentry__TracesSampleRate` | Opcional (ex. `0.1`) |
| Cloudflare Pages (build) | `VITE_SENTRY_DSN` | Só após consentimento de cookies |
| Pages (build) | `VITE_SENTRY_ENVIRONMENT` | Opcional (ex. `production`) |
| Pages (build) | `VITE_SENTRY_RELEASE` | Opcional; alinhar com o deploy |

**Recorrências.** O worker usa `RecurringOrders__GenerationAdvanceDays` (3), `RecurringOrders__PollIntervalSeconds` (60) e `RecurringOrders__GenerationLeaseSeconds` (120). Sem estas variáveis aplicam-se os mesmos valores. Cupões, campanhas, avaliações, capacidade de entrega e stock não acrescentam variáveis: ficam na base, por loja.

## 9. Cópias de segurança

O Neon Free tem histórico de horas, não um arquivo mensal. O script [`scripts/backup-neon-to-r2.sh`](../scripts/backup-neon-to-r2.sh) faz `pg_dump` em SQL simples, comprime com gzip e envia para object storage compatível S3 (Neon Object Storage, Cloudflare R2, etc.), apagando cópias mais antigas do que `BACKUP_RETENTION_DAYS` (14 por omissão).

### GitHub Actions (diário, 03:00 UTC)

Workflow **Backup Postgres** (`.github/workflows/backup-postgres.yml`). Segredos do repositório:

| Segredo | Descrição |
| --- | --- |
| `DATABASE_URL_UNPOOLED` | URI **directa** do Neon (sem `-pooler`, `sslmode=require`) |
| `BACKUP_S3_ENDPOINT` | URL S3 (Neon Object Storage ou R2) |
| `BACKUP_S3_BUCKET` | Nome do bucket |
| `BACKUP_S3_ACCESS_KEY_ID` | Access key |
| `BACKUP_S3_SECRET_ACCESS_KEY` | Secret key |

O script aceita também os prefixos `NEON_S3_*` ou `R2_*` para execução local. O objecto fica em `backups/floristhub/floristhub-<timestamp>.sql.gz`.

### Manual / cron local

```bash
export DATABASE_URL="postgresql://...direct.../neondb?sslmode=require"
export BACKUP_S3_ENDPOINT="https://..."
export BACKUP_S3_BUCKET="florist-hub"
export BACKUP_S3_ACCESS_KEY_ID="..."
export BACKUP_S3_SECRET_ACCESS_KEY="..."
./scripts/backup-neon-to-r2.sh
```

Precisa de `pg_dump` e da AWS CLI.

### Restauro

```bash
export DATABASE_URL="postgresql://floristhub:floristhub@127.0.0.1:5432/floristhub"
./scripts/restore-postgres.sh /tmp/floristhub-....sql.gz
```

Se o destino não for `localhost` / `127.0.0.1`, o script pára. Para restaurar o Neon: `RESTORE_I_UNDERSTAND=yes` e a URI **directa**. Isso substitui o que o `psql` aplicar em cima da base actual; faça-o numa base vazia ou depois de decidir o que pode ser reescrito. Inclua `DataProtectionKeys` nessa cópia.

O procedimento local (dump, apagar uma tabela, restaurar, ler a linha) está coberto pelo script contra o Postgres do `docker compose`.

## 10. Deploy sem Actions, e os workflows manuais

Caminho normal, zero minutos:

1. Merge para `main`.
2. O Render constrói o Dockerfile e faz o pré-deploy das migrações.
3. O Pages constrói `web/` no próprio Cloudflare.

Workflows, todos só com **Run workflow**:

| Workflow | O que faz |
| --- | --- |
| Deploy API (manual) | POST ao Deploy Hook do Render (`RENDER_DEPLOY_HOOK_URL`) |
| Deploy web (manual) | `npm run build` e `wrangler pages deploy` (`CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `VITE_API_BASE_URL`) |
| Android release (manual) | AAB assinado. Segredos abaixo |
| Backup Postgres | `pg_dump` diário para object storage S3 (também manual) |
| iOS (simulador) | Já existia. Simulador e capturas, não um IPA de loja |

Não dispare estes workflows enquanto a conta GitHub não tiver minutos.

## 11. Google Play (25 USD, uma vez)

A app de debug usa HTTP para o emulador. O AAB de release recusa tráfego em claro e só deve apontar para HTTPS.

1. Pague a conta em [Play Console](https://play.google.com/console). Uma conta pessoal nova tem de passar por testes fechados antes de produção; siga o que o consola pedir.
2. Crie a app com o id `com.floristhub.android`.
3. Keystore **fora do repositório**:

   ```bash
   keytool -genkeypair -v -keystore floristhub-upload.jks -alias floristhub -keyalg RSA -keysize 2048 -validity 10000
   ```

   Active Play App Signing. Guarde o ficheiro e as passwords: sem eles não há actualizações.
4. Versão: `floristHub.versionCode` e `floristHub.versionName` em `mobile/gradle.properties`, ou `ANDROID_VERSION_CODE` / `ANDROID_VERSION_NAME`. O `versionCode` sobe em cada envio.
5. Bundle local:

   ```bash
   export ANDROID_KEYSTORE_PATH=/caminho/floristhub-upload.jks
   export ANDROID_KEYSTORE_PASSWORD=...
   export ANDROID_KEY_ALIAS=floristhub
   export ANDROID_KEY_PASSWORD=...
   cd mobile
   ./gradlew :androidApp:bundleRelease \
     -PfloristHub.apiBaseUrl=https://api.floristhub.pt \
     -PfloristHub.webBaseUrl=https://floristhub.pt
   ```

   O AAB fica em `mobile/androidApp/build/outputs/bundle/release/`.
6. No workflow manual, os segredos são `ANDROID_KEYSTORE_BASE64` (`base64 -w0 floristhub-upload.jks`), `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`. O artefacto descarrega-se do Actions e carrega-se no Play Console. O workflow não publica sozinho.
7. Ficha em pt-PT, textos já no repositório em `mobile/androidApp/fastlane/metadata/android/pt-PT/`:
   - título, descrição curta, descrição longa
   - ícone 512 px em `images/icon.png` (também há ícones adaptativos na app)
   - política de privacidade: `https://floristhub.pt/legal/privacidade` (a página `/legal/privacidade` da web). Se o domínio ainda for `pages.dev`, use esse host com o mesmo caminho.
8. Capturas de ecrã: o Play pede pelo menos duas. Não estão geradas aqui.

## 12. App Store (99 USD/ano)

Sem esta inscrição não há TestFlight nem App Store. Uma Apple ID gratuita só instala no próprio iPhone, durante 7 dias, e exige um Mac com Xcode. Esse caminho está em [ios.md](./ios.md).

Sem Mac:

1. Inscreva-se no [Apple Developer Program](https://developer.apple.com/programs/).
2. No App Store Connect, nova app: bundle id `com.floristhub.ios` (o de `mobile/iosApp/Configuration/Config.xcconfig`), idioma principal português (Portugal), SKU à escolha.
3. Ficha: nome Florist Hub, política de privacidade `https://floristhub.pt/legal/privacidade`, categoria compras ou estilo de vida, capturas quando as tiver.
4. URL da API de produção: `FLORIST_HUB_API_BASE_URL` e `FLORIST_HUB_WEB_BASE_URL` no xcconfig, antes de arquivar. O simulador local usa `127.0.0.1`.
5. Valide o projeto com o workflow **iOS (simulador)** quando houver minutos. Esse workflow produz um simulador, não um IPA assinado, e não fala com o App Store Connect.
6. O upload do binário (Transporter ou Xcode Organizer) faz-se num Mac, com o certificado de distribuição e o perfil App Store. Não há, neste repositório, um workflow que assine e envie o IPA.

Push no iOS continua dependente de uma chave APNs no Firebase. Ver [notificacoes.md](./notificacoes.md).

## 13. Depois de estar no ar

1. `curl -fsS https://<api>/health` → `{"status":"healthy",...}`.
2. Abra `https://<web>/?tenant=...` ou o subdomínio, faça login da florista e carregue uma foto. O URL da imagem tem de ser o `PublicBaseUrl` do R2, não `/api/files`.
3. Crie o webhook Stripe e uma encomenda de valor baixo em live mode, ou mantenha as chaves vazias até lá.
4. Agende o `pg_dump`. Faça um restauro de ensaio para uma base local pelo menos uma vez.
5. Quando o domínio existir, actualize `App__WebBaseUrl`, `Cors__AllowedOrigins`, `VITE_API_BASE_URL`, `VITE_BASE_DOMAIN` e a política de privacidade na ficha das lojas de apps.

O primeiro pedido depois do sono é lento. Isso é o plano gratuito, não uma falha do health check.
