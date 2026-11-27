# Florist Hub: alojamento gratuito (Beta/MVP)

> O passo a passo para pôr a loja online está em [deploy.md](./deploy.md). Este ficheiro regista os limites dos planos.

> Documento para agentes de código. Pesquisa feita a 7 nov 2026 nas páginas oficiais de preços/limites.
> Os limites mudam com frequência: **volta a confirmar antes de implementar**. "Não confirmado" quer dizer que não foi verificado numa página oficial nesta pesquisa.
> Isto é informação, não uma ordem para fazer deploy. Não alterar infraestrutura nem o repositório sem pedido explícito do Diogo.

Stack: ASP.NET Core .NET 10 LTS (REST, Identity + JWT, Serilog, EF Core) · PostgreSQL · fotos atrás de `IFileStorage` (hoje disco local) · web React + Vite + TS (SPA estática) · app Kotlin Multiplatform + Compose (Android/iOS) · repo privado `dipzy/florist-hub`.

**Conclusão:** dá para pôr a API, a BD, as imagens e a web online a custo zero, com limitações (cold start, quotas pequenas). **A loja iOS não é gratuita** (99 USD/ano). A Google Play custa 25 USD, pagos uma vez. Há alternativas gratuitas para testes fechados no Android. No iOS, sem pagar, só dá para instalar no teu próprio dispositivo.

---

## 1. API .NET (container Docker)

| Opção | Plano gratuito (confirmado) | Cartão | Preços |
|---|---|---|---|
| **Render (Free web service)** | 0,1 CPU / 512 MB RAM. Hiberna após **15 min** sem tráfego e o arranque demora **~1 min**. **750 h/mês** por workspace. Disco efémero (os uploads perdem-se). Sem disco persistente nem SSH. Pode reiniciar a qualquer momento. Domínio próprio e TLS incluídos. Bloqueia as portas SMTP de saída 25/465/587 (usar a API HTTP do fornecedor de email). Se a largura de banda incluída se esgotar sem cartão, os serviços ficam suspensos até ao fim do mês. | Não exigido (não confirmado explicitamente) | https://render.com/docs/free · https://render.com/pricing |
| **Koyeb (Free Instance)** | 1 instância por organização. 512 MB RAM, 0,1 vCPU, 2 GB SSD. Só Frankfurt ou Washington. **Escala para zero após 1 h** sem tráfego. Sem volumes. | Não confirmado | https://www.koyeb.com/docs/reference/instances · https://www.koyeb.com/pricing |
| **Google Cloud Run** | Por mês (us-central1, cobrança por pedido): 180 000 vCPU-s, 360 000 GiB-s e **2 milhões de pedidos**. Escala para zero (cold start de alguns segundos, valor não confirmado). Saída de dados grátis: só 1 GiB/mês dentro da América do Norte. Artifact Registry e Cloud Build cobrados à parte, cada um com quota grátis própria. | **Sim, conta de faturação** (não confirmado nesta página, mas é prática conhecida da GCP) | https://cloud.google.com/run/pricing |
| Oracle Cloud Always Free (VM) | Ampere A1: 2 OCPU + 12 GB RAM no total (ou 2 VMs AMD micro com 1 GB), 200 GB de block storage e 10 TB/mês de saída. Corres Docker + Postgres tu próprio. **VMs pouco usadas podem ser recuperadas** (CPU p95 < 20 % durante 7 dias). Erros "out of host capacity" são frequentes. | Sim, para o registo (não confirmado nesta página) | https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm |
| Azure App Service F1 | 60 min de CPU/dia, 1 GB RAM, 1 GB de armazenamento, sem SLA, "não suportado para produção". Fraco para .NET + EF. | Sim (conta Azure; não confirmado aqui) | https://azure.microsoft.com/en-us/pricing/details/app-service/linux/ |
| ~~Fly.io~~ | **Sem plano gratuito** para organizações novas: só um trial de 1 USD/7 dias, depois exige cartão. A máquina mais barata custa 2,19 USD/mês. | Sim | https://fly.io/docs/about/pricing/ |
| Railway, Azure Container Apps | **Não confirmado** nesta pesquisa. Railway não tinha plano gratuito permanente da última vez que se soube; verificar. | — | https://railway.com/pricing · https://azure.microsoft.com/pricing/details/container-apps/ |

**Escolha:** Render (o mais simples, com Docker direto do GitHub). Koyeb é a alternativa, com a vantagem de hibernar só após 1 h e ter região na Europa (Frankfurt).

## 2. PostgreSQL

| Opção | Plano gratuito (confirmado) | Cartão | Preços |
|---|---|---|---|
| **Neon Free** | Permanente, não é trial. **1 GB por projeto** (20 GB no total da conta), 100 CU-h/mês por projeto, 5 GB de saída/mês por projeto e até 100 projetos. Escala para zero após 5 min (não desativável), com retoma em "algumas centenas de ms". **Não pausa por inatividade**. Ao passar os limites: computação suspensa ou escritas bloqueadas, **sem apagar dados**. Histórico de restauro de 6 h. Inclui ainda 5 GB de Object Storage por projeto. | **Não** | https://neon.com/pricing |
| Supabase Free | 500 MB de BD, 5 GB de saída, 1 GB de ficheiros, 2 projetos ativos. **O projeto pausa após 1 semana de inatividade.** Sem backups automáticos. | Não (não confirmado explicitamente) | https://supabase.com/pricing |
| Render Postgres Free | 1 GB. **Expira 30 dias depois de criado** e é apagado ao fim de mais 14 dias. Sem backups. Só serve para demos. | — | https://render.com/docs/free |
| Koyeb Postgres Free | 0,25 vCPU, 1 GB RAM, 1 GB de armazenamento, **"5h"** (parece ser 5 h de computação/mês; detalhe não confirmado). | — | https://www.koyeb.com/pricing |
| Aiven Free PostgreSQL | **Não confirmado** nesta pesquisa. | — | https://aiven.io/pricing |

**Escolha:** Neon. Usar a connection string *pooled* e `sslmode=require`.

## 3. Armazenamento de imagens

| Opção | Plano gratuito (confirmado) | Preços |
|---|---|---|
| **Cloudflare R2** | 10 GB-mês, 1 M operações Classe A (escrita) e 10 M Classe B (leitura) por mês. **Saída de dados grátis.** API compatível com S3. O free tier só cobre a classe Standard. Requer conta Cloudflare; a ativação do R2 costuma pedir método de pagamento (**não confirmado**). | https://developers.cloudflare.com/r2/pricing/ |
| Supabase Storage | 1 GB, 5 GB de saída em cache, upload máximo de 50 MB, sem transformações de imagem no Free. Pausa juntamente com o projeto. | https://supabase.com/pricing |
| Cloudinary Free | 25 créditos/mês (1 crédito = 1 GB de armazenamento, 1 GB de largura de banda ou 1000 transformações). Inclui redimensionamento e CDN. | https://cloudinary.com/pricing |
| Neon Object Storage | 5 GB por projeto no Free, mas a saída conta para os 5 GB partilhados com a BD. | https://neon.com/pricing |

**Escolha:** R2 (sem custos de saída). Se for preciso evitar cartão, usar Cloudinary.

## 4. Web estática (React + Vite)

| Opção | Plano gratuito (confirmado) | Preços |
|---|---|---|
| **Cloudflare Pages** | 500 builds/mês, 1 build de cada vez, timeout de 20 min, 20 000 ficheiros, 25 MiB por ficheiro, 100 domínios por projeto. Largura de banda estática sem limite publicado nesta página. Uso comercial permitido. | https://developers.cloudflare.com/pages/platform/limits/ |
| Vercel Hobby | 100 GB de Fast Data Transfer, 1 M pedidos CDN e 100 deploys/dia. **Só uso pessoal e não comercial.** Ao exceder, espera-se 30 dias. | https://vercel.com/docs/plans/hobby |
| Render Static Site | Grátis, mas conta para a largura de banda e os minutos de pipeline do workspace (partilhados com a API). | https://render.com/docs/free |
| Netlify Free | **Não confirmado** nesta pesquisa (passou a um modelo de créditos). | https://www.netlify.com/pricing/ |
| GitHub Pages | Num repo **privado** exige plano pago (GitHub Pro); **não confirmado aqui**. Para uma SPA não há rewrites (truque do 404.html). | https://docs.github.com/pages |

**Escolha:** Cloudflare Pages com `_redirects` de SPA: `/* /index.html 200`.

## 5. CI/CD (GitHub Actions, repo privado)

- GitHub Free: **12 USD/mês de computação incluída** (≈ 2000 min em Linux 2-core a 0,006 USD/min), 500 MB de artefactos/Packages e 10 GB de cache por repo. Sem cartão, o uso fica **bloqueado** ao esgotar (não há cobrança surpresa).
- **macOS custa 0,062 USD/min**, ou seja, ~10× mais. Um build iOS de 20 min gasta ~1,24 USD da quota. Correr os builds iOS raramente ou manualmente.
- Os runners self-hosted são gratuitos.
- Fonte: https://docs.github.com/en/billing/concepts/product-billing/github-actions
- Alternativa: Render e Cloudflare Pages fazem o build a partir do GitHub, sem gastar minutos de Actions. Nesse caso, o Actions só corre testes.

## 6. Distribuição móvel

**Android**
- Google Play: **25 USD, uma vez**, mais verificação de identidade. Contas pessoais têm de cumprir o requisito de testes fechados antes de publicar em produção. https://support.google.com/googleplay/android-developer/answer/6112435
- **Gratuito:** o Android Developer Console tem um plano "**Limited distribution**" sem taxa, até **20 dispositivos** (ligado aos novos requisitos de verificação de programadores para sideloading). https://support.google.com/android-developer-console/answer/16640817
- **Gratuito:** Firebase App Distribution (Spark, "No-cost", sem cartão) para testers, ou APK direto (por exemplo num GitHub Release). Com as regras de verificação de programadores Android em vigor em 2026, apps de programadores não registados podem ter instalação restrita em dispositivos certificados; confirmar o estado atual. https://firebase.google.com/pricing

**iOS**
- Apple Developer Program: **99 USD/ano**. Necessário para TestFlight, App Store e App Store Connect. https://developer.apple.com/programs/whats-included/
- **Gratuito (limitado):** conta Apple grátis com "Personal Team" no Xcode, só nos teus dispositivos: máx. 3 dispositivos e 3 apps por dispositivo, com perfis que **expiram ao fim de 7 dias** (é preciso reinstalar). **TestFlight não está disponível sem conta paga.** Precisa de um Mac com Xcode. https://developer.apple.com/support/compare-memberships/
- Firebase App Distribution para iOS também exige builds assinados (ad hoc), o que na prática requer a conta paga.

## 7. Domínio e HTTPS

- Sem custo: usar os subdomínios das plataformas (`*.onrender.com`, `*.pages.dev`, `*.koyeb.app`), que já têm HTTPS incluído.
- Um domínio próprio **não é gratuito**: é preciso registá-lo (no Cloudflare Registrar ao preço de custo; valor não confirmado). Render e Cloudflare Pages dão TLS gratuito para domínios próprios.

### Domínios por loja (Florist Hub)

Cada tenant pode ter:

1. **Subdomínio** — `{slug}.{App__BaseDomain}` (ex.: `flores-lina.floristhub.pt`), configurado com registo DNS wildcard ou CNAME por subdomínio para a API e a web.
2. **Domínio próprio** — campo `CustomDomain` no tenant (ex.: `www.floresdalina.pt`). O SuperAdmin regista-o na plataforma; o middleware resolve o tenant pelo `Host` completo antes do subdomínio.

**Cloudflare Pages (web estática)**

- Adicionar o domínio no projeto Pages → *Custom domains*.
- CNAME do domínio (ou subdomínio) para `<projeto>.pages.dev`.
- Para várias lojas com domínios diferentes, o mesmo projeto Pages serve todas as rotas SPA; a resolução da loja é feita no browser (host ou `?tenant=slug`) e na API (host / `X-Tenant-Id`).

**Render / API**

- `App__BaseDomain=floristhub.pt` (ou o domínio canónico da plataforma).
- CORS: além de `Cors__AllowedOrigins`, a API aceita origens HTTPS cujo host coincide com um `CustomDomain` registado (cache de 5 min).

**Limitações do plano gratuito**

- Cloudflare Pages: 500 builds/mês, sem SLA; domínios personalizados suportados no plano gratuito.
- Render Free: cold start ~1 min; não usar dezenas de domínios com pings constantes só para “acordar” a API.
- Sem wildcard TLS automático em todos os fornecedores: cada domínio próprio tem de ser validado no Pages/Render.

**Localhost**

- Web: `http://localhost:5173/?tenant=flores-lina` ou `VITE_DEFAULT_TENANT_SLUG=flores-lina`.
- API: header `X-Tenant-Id` / subdomínio `flores-lina.localhost` (com hosts ficheiro ou DNS local).

## 8. Email transacional (confirmação de conta, encomendas, reset de password)

| Opção | Plano gratuito | Preços |
|---|---|---|
| **Resend Free** | **3000 emails/mês, máx. 100/dia**, 3 domínios, API HTTP e SMTP. | https://resend.com/pricing |
| OCI Email Delivery | 3000 emails/mês (Always Free). | (doc Oracle acima) |
| Brevo / outros | Não confirmado. | — |

Nota: o Render Free bloqueia SMTP de saída, por isso é preciso **usar a API HTTP** do Resend em vez de SMTP.

### Configurar Resend (grátis) para o Florist Hub

1. Criar conta em [resend.com](https://resend.com) (plano Free).
2. Em **Domains**, adicionar o domínio de envio (ex. `flores.pt`) e publicar os registos DNS (SPF/DKIM) que o Resend indicar. Sem domínio verificado, só é possível enviar para o email da própria conta (útil para smoke tests).
3. Em **API Keys**, gerar uma chave (`re_...`).
4. No **Render** (Web Service da API), definir variáveis de ambiente:
   - `Email__Provider=Resend`
   - `Email__ResendApiKey=re_...` (segredo)
   - `Email__From=Flores da Lina <ola@seudominio.pt>` (remetente no domínio verificado)
   - `App__WebBaseUrl=https://<projeto>.pages.dev` (links de pagamento nos emails)
5. Sem `Email__ResendApiKey`, a API usa `LogEmailSender` (mensagens só no Serilog) — adequado para desenvolvimento local.

O backend envia para `POST https://api.resend.com/emails` com `Authorization: Bearer`. Falhas de email **não** interrompem confirmações de encomenda; ficam em log e `AuditLog` (`EmailDeliveryFailed`).

---

## Combinação recomendada (100 % gratuita)

| Parte | Serviço |
|---|---|
| API .NET (Docker) | **Render Free Web Service** (alternativa: Koyeb Free, Frankfurt) |
| PostgreSQL | **Neon Free** (região UE) |
| Imagens | **Cloudflare R2** (alternativa sem cartão: Cloudinary Free) |
| Web | **Cloudflare Pages** |
| CI | GitHub Actions (testes Linux), com deploy pelas próprias plataformas |
| Android | APK, Firebase App Distribution ou Limited distribution (≤ 20 dispositivos). Play Store mais tarde (25 USD) |
| iOS | Só Xcode Personal Team no próprio iPhone. TestFlight/App Store mais tarde (99 USD/ano) |
| Email | Resend Free via API HTTP (só se houver domínio; senão adiar) |
| Domínio | Subdomínios grátis `onrender.com` / `pages.dev` |

### Estrutura do backend no repositório

- Solução: `backend/FloristHub.sln` com pastas de solução **`src`** e **`tests`**.
- Imagem Docker: `docker build -f backend/Dockerfile backend` (publica `src/FloristHub.Api/FloristHub.Api.csproj` → assembly `FloristHub.Api.dll`).
- Variáveis: `backend/src/FloristHub.Api/appsettings.json` (dev) + env `FloristHub__*` / `ConnectionStrings__*` em produção.

### O que mudar no código para suportar isto
1. **`IFileStorage` → implementação S3** (`AWSSDK.S3` com `ServiceURL = https://<account>.r2.cloudflarestorage.com`, `ForcePathStyle`). Servir as imagens por URL pública do bucket ou por URL pré-assinada. Manter a implementação em disco para desenvolvimento e escolher por configuração (`Storage:Provider`).
2. **Configuração por variáveis de ambiente**: `ConnectionStrings__Default`, `Jwt__Key`/`Issuer`/`Audience`, `Storage__*`, `Email__*` (`Provider`, `ResendApiKey`, `From`), `App__WebBaseUrl` e `Cors__AllowedOrigins`. Nada de segredos no `appsettings.json`.
3. **Converter `DATABASE_URL`** (formato `postgres://`) para connection string Npgsql, se a plataforma o der nesse formato, com `SSL Mode=Require`. Ativar `EnableRetryOnFailure` por causa do cold start do Neon.
4. **Porta**: ouvir em `http://0.0.0.0:${PORT}` (`ASPNETCORE_URLS` ou `ASPNETCORE_HTTP_PORTS`). O TLS termina no proxy, por isso usar `UseForwardedHeaders` e não forçar `UseHttpsRedirection` atrás do proxy.
5. **CORS** com a origem do Pages (`https://<proj>.pages.dev`) e o domínio final.
6. **Migrações EF no arranque** (`Database.Migrate()`, protegido por flag) ou num passo de pré-deploy. Seed de demonstração **desligado** em produção.
7. **Dockerfile multi-stage** (sdk → aspnet, de preferência Alpine para caber em 512 MB). Endpoint **`GET /health`** leve (sem acesso à BD) para health checks do Render/Koyeb e para **pings externos** — por exemplo um cron em [cron-job.org](https://cron-job.org) de 10 em 10 minutos contra `https://<api>/health`, para reduzir hibernação no Render Free (~15 min sem tráfego). Isto consome horas do plano grátis; usar só se o cold start for inaceitável na demo.
8. **Serilog** só para a consola (o filesystem é efémero), com o nível de log por variável de ambiente.
9. **Data Protection keys**: persistir na BD (`PersistKeysToDbContext`), porque o disco é efémero.
10. **Web**: `VITE_API_BASE_URL` por ambiente e `_redirects` de SPA. **App móvel**: URL base da API configurável por build.
11. **Email**: `IEmailSender` (Resend HTTP ou log), fila `Channel` + `BackgroundService`, templates HTML pt-PT com nome/logo do tenant.

### Limitações a aceitar
- **Cold start**: o primeiro pedido após 15 min parado demora **~1 min** no Render, mais o arranque do Neon. O endpoint `/health` + um cron externo (ex. cron-job.org) pode manter a API acordada, mas **gasta as 750 h/mês** do plano grátis — usar com moderação na demo.
- Recursos mínimos (0,1 CPU / 512 MB), sem SLA e com reinícios a qualquer momento. Não serve para produção real.
- BD de 1 GB, 100 CU-h e 5 GB de saída/mês. Restauro de apenas 6 h e **sem backups automáticos**: fazer `pg_dump` manual.
- Imagens: 10 GB no R2. Redimensionar e comprimir no upload.
- Email: 100/dia.
- iOS: sem TestFlight nem App Store sem 99 USD/ano. Android: sem Play Store sem 25 USD.
- Os planos gratuitos mudam sem aviso. Rever este ficheiro antes de cada deploy.
