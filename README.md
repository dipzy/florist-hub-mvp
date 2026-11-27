# Florist Hub

[![CI](https://github.com/dipzy/florist-hub-mvp/actions/workflows/ci.yml/badge.svg)](https://github.com/dipzy/florist-hub-mvp/actions/workflows/ci.yml)

> Versão pública do projeto (novembro de 2026). O desenvolvimento continua num repositório privado.

Plataforma multi-tenant **Beta/MVP** para floristas: clientes montam arranjos e submetem encomendas; a florista confirma, propõe alterações ou recusa **antes** do pagamento. Inclui portfolio de arranjos realizados (com upload de fotos), API REST, frontend web e app móvel (KMP + Compose).

## Stack

| Camada | Tecnologia |
|--------|------------|
| Backend | ASP.NET Core 10 (LTS) — `src/FloristHub.Api` → `src/FloristHub.Services` → `src/FloristHub.DAL` |
| Base de dados | PostgreSQL + Entity Framework Core (Npgsql) |
| Auth | ASP.NET Core Identity + JWT (claims `tenant_id`, roles `SuperAdmin`, `Florista`, `Cliente`) |
| Ficheiros | `IFileStorage` — disco local (dev) ou **S3/R2** (`Storage__Provider`) |
| Logging | Serilog (consola; ficheiro só em Development) + `AuditLog` na BD |
| Web | React 19 + Vite + TypeScript |
| Mobile | Kotlin Multiplatform + Compose Multiplatform (`shared`, `androidApp`, `iosApp`) |

**Fora do MVP (pontos de extensão):** pagamentos reais (`IPaymentGateway`), visualizador 3D (`IArrangement3DService`).

**Email transacional:** `IEmailSender` com fila em memória (`Channel` + `BackgroundService`). Em produção use **Resend** via API HTTP (`Email__Provider=Resend`); sem chave API, `LogEmailSender` escreve no Serilog. O campo **Email de contacto** nas definições da loja (`ContactEmail`) recebe notificações de novas encomendas.

## Alojamento gratuito (beta online)

Passo a passo de deploy: **[docs/deploy.md](docs/deploy.md)**. Limites dos planos gratuitos: **[docs/alojamento-gratuito.md](docs/alojamento-gratuito.md)**.

Variáveis de ambiente documentadas em **[.env.example](.env.example)** (sem segredos). Imagem Docker: `backend/Dockerfile`. Health check: `GET /health`.

## Estrutura do monorepo

```
backend/
  FloristHub.sln
  src/FloristHub.Api/        # Controllers, Middleware, Auth/JWT, Errors, Serilog
  src/FloristHub.Services/   # Services, DTOs, Validation, Payments/Storage
  src/FloristHub.DAL/        # Ver árvore DAL abaixo (DbContext, repositórios, UoW)
  tests/FloristHub.Api.Tests/
  tests/FloristHub.Services.Tests/
web/              # Frontend React
mobile/           # KMP + Compose
docker-compose.yml
uploads/          # Fotos carregadas em desenvolvimento (ignorado pelo Git)
```

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) 22 LTS (ver `web/.nvmrc`)
- [Docker](https://www.docker.com/) (PostgreSQL local)
- Android: JDK 21 + Android SDK 37 (compileSdk; para `mobile`)

## Base de dados (PostgreSQL)

```bash
docker compose up -d
```

Ligação por defeito (ver `backend/src/FloristHub.Api/appsettings.json`):

`Host=localhost;Port=5432;Database=floristhub;Username=floristhub;Password=floristhub`

### Camada DAL (`backend/src/FloristHub.DAL`)

```
FloristHub.DAL/
  Persistence/           # ApplicationDbContext, IApplicationDbContext, ConnectionStringFactory
  Entities/              # Modelos de domínio (incl. ApplicationUser)
  Enums/
  Common/                # BaseEntity, ITenantEntity
  MultiTenancy/          # ITenantContext, TenantContext
  Constants/             # Roles, ClaimTypes (JWT/Identity)
  Configurations/        # IEntityTypeConfiguration + TenantQueryFilterConfigurer
  Repositories/
    Interfaces/          # IRepository<T>, IFlowerRepository, IOrderRepository, …
    *.cs                 # Implementações
  UnitOfWork/            # IUnitOfWork, UnitOfWork (SaveChanges + transações)
  Migrations/
  Audit/                 # IAuditLogger, AuditLogger
  Seed/                  # DevelopmentDataSeeder
  DalServiceCollectionExtensions.cs
  WebApplicationExtensions.cs
```

Os **Services** dependem de `IUnitOfWork` e dos repositórios específicos, não do `DbContext` diretamente. A API continua a registar `IApplicationDbContext` para middleware/Identity; o `TenantsController` usa `ITenantRepository`.

Exemplo (serviço):

```csharp
public class FlowerService : IFlowerService
{
    private readonly IUnitOfWork _unitOfWork;

    public FlowerService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditLogger audit)
        => (_unitOfWork, _tenant, _audit) = (unitOfWork, tenant, audit);

    public async Task<FlowerDto> CreateAsync(CreateFlowerRequest request, Guid userId, CancellationToken ct)
    {
        var flower = new Flower { TenantId = RequireTenant(), Name = request.Name.Trim(), /* … */ };
        await _unitOfWork.Flowers.AddAsync(flower, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(flower);
    }
}
```

## Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet test
dotnet run --project src/FloristHub.Api/FloristHub.Api.csproj
```

- Swagger: `http://localhost:5183/swagger`
- Migrações aplicam-se automaticamente ao arrancar.
- Em **Development**, o `DevelopmentDataSeeder` cria um tenant e utilizadores de demonstração (idempotente).

### Dados de demonstração (Development)

| Campo | Valor |
|--------|--------|
| Tenant (slug) | `flores-lina` |
| Password (todos) | `DemoFlorist1!` |
| Florista | `florista@demo.floristhub.pt` |
| Cliente | `cliente@demo.floristhub.pt` |
| SuperAdmin (plataforma, sem tenant) | `admin@demo.floristhub.pt` |
| 2.ª loja (slug) | `flores-do-campo` — florista `florista.campo@demo.floristhub.pt` |

O seed inclui flores, um arranjo de portfolio, uma encomenda pendente e outra com **proposta de alteração pendente** (útil para testar aceitar/recusar no cliente).

### Upload de fotos do portfolio

- `POST /api/files/upload` (multipart, role `Florista`)
- **Local:** `GET /api/files/{*storagePath}` na API
- **R2/S3:** URLs públicas do bucket (`Storage__S3__PublicBaseUrl`); escolher provider com `Storage__Provider=Local|R2|S3`

### Propostas de alteração

1. Florista: `POST /api/orders/{id}/proposals`
2. Cliente: `POST /api/orders/{orderId}/proposals/{proposalId}/accept` ou `.../reject`
3. Aceitar aplica itens/total propostos e confirma a encomenda; recusar mantém a encomenda pendente. Histórico em `OrderStatusHistory` e estado em `OrderChangeProposal`.

### Multi-tenancy e query filters

Resolução por ordem: claim JWT `tenant_id` (utilizadores da loja), **domínio próprio** (`CustomDomain` no host), subdomínio de `App__BaseDomain`, headers `X-Tenant-Id` / `X-Tenant-Slug`. SuperAdmin não tem tenant e ignora o filtro apenas nos serviços `/api/platform/*` (repositórios com `IgnoreQueryFilters` para contagens). Filtros globais EF nas entidades com `TenantId`, **incluindo filhas** do portfolio e encomendas.

### SuperAdmin e domínios

| Papel | Âmbito |
|-------|--------|
| **SuperAdmin** | Gestão da plataforma (`/superadmin`, role `SuperAdmin`), sem `TenantId` no JWT |
| **Florista** | Administrador da sua loja (`/admin`), isolamento por tenant |

Endpoints (autenticados, role `SuperAdmin`):

- `GET /api/platform/tenants` — listar lojas (nome, slug, domínio, ativo/suspenso, n.º encomendas, criada)
- `GET /api/platform/tenants/{id}` — detalhe
- `POST /api/platform/tenants` — criar loja + convite por email à florista (`Email__Provider=Resend` ou `Log` em dev)
- `POST /api/platform/tenants/{id}/resend-invite` — reenviar convite
- `POST /api/auth/florist-invite/complete` — definir palavra-passe a partir do link (página web `/convite`)
- `PUT /api/platform/tenants/{id}` — editar nome, domínio, tema
- `POST /api/platform/tenants/{id}/suspend` — suspender/reativar (`{ "suspended": true }` bloqueia login da florista e encomendas)

Público: `GET /api/store/theme` — cores/tipografia da loja resolvida no pedido.

Variáveis: `App__BaseDomain` (subdomínios), `Cors__Origins` + domínios próprios registados.

Teste local com duas lojas: `?tenant=flores-lina` ou `?tenant=flores-do-campo` na web; SuperAdmin `admin@demo.floristhub.pt` / `DemoFlorist1!` (login sem selecionar loja).

## Web

```bash
cd web
cp .env.example .env   # opcional
npm install
npm run dev            # http://localhost:5173
npm run build
```

Defina `VITE_API_BASE_URL` (ver `web/.env.example`). Deploy estático: `web/public/_redirects` (SPA no Cloudflare Pages).

Ecrãs: login/registo, portfolio (com fotos e «usar como base»), montar arranjo, encomendas do cliente, **painel da florista** em `/admin`, e **SuperAdmin** em `/superadmin`. A loja pública aplica o tema via CSS variables (`--store-primary`, `--store-accent`). Resolução de tenant: `VITE_DEFAULT_TENANT_SLUG`, `?tenant=slug` ou subdomínio. A rota legada `/florista` redireciona para `/admin`.

### Painel da florista (API)

| Área | Endpoints |
|------|-----------|
| Definições | `GET/PUT /api/florist/store` (morada, horário, zonas de entrega, antecedência, tipos de entrega, aceitar encomendas on/off) |
| Dashboard | `GET /api/florist/dashboard` |
| Clientes | `GET /api/florist/customers?search=&page=` |
| Catálogo | `GET /api/flowers/manage?search=&page=` + CRUD existente (`DELETE` desativa se o item estiver em encomendas) |
| Portfolio | `PATCH /api/portfolio/{id}/status`, `PUT /api/portfolio/sort-order` |
| Encomendas | `GET /api/orders/manage/paged?status=&deliveryType=&customerSearch=&page=` + ações existentes |

As encomendas novas respeitam `MinimumOrderAdvanceMinutes`, tipos de entrega ativos, `OrdersAcceptingEnabled` e taxa de entrega por zona (`DeliveryZoneId` opcional no `POST /api/orders`).

## Mobile (Android)

No emulador Android, `10.0.2.2` aponta para o localhost da máquina host (API em `:5183`).

```bash
cd mobile
./gradlew :androidApp:assembleDebug
```

- URL da API: `floristHub.apiBaseUrl` em `mobile/gradle.properties` (ou `-PfloristHub.apiBaseUrl=https://...`).
- Slug da loja (build white-label): `floristHub.tenantSlug` no Gradle / `FLORIST_HUB_TENANT_SLUG` no Xcode (`mobile/iosApp/Configuration/Config.xcconfig`); omissão `sao-flores`.
- Cliente: portfolio → montar arranjo (incl. inspiração), encomendas com linha do tempo, cancelamento e aceitar/recusar propostas.
- Florista: dashboard (métricas da API), definições da loja (leitura), encomendas, confirmação/recusa/propostas e upload de foto.

**iOS:** Mac + Xcode localmente, ou **simulador no GitHub Actions** sem Mac — ver [`docs/ios-testes.md`](docs/ios-testes.md) e `mobile/iosApp/README.md`.

## Fluxo de encomenda

1. Cliente monta o arranjo (opcionalmente a partir do portfolio) e submete → `PendenteConfirmacao`
2. Florista confirma, recusa **com motivo** ou propõe alterações (JSON de itens validado)
3. Cliente aceita ou recusa a proposta; aceitar confirma a encomenda
4. Com a encomenda confirmada, o cliente paga por Stripe Checkout (cartão, MB Way e Multibanco, se a conta da florista os tiver ativos). Sem `Stripe__SecretKey` o pagamento fica desativado
5. Florista avança `EmPreparacao` → `Pronta` → `Entregue` (`PATCH /api/orders/{id}/status`)
6. Cliente pode cancelar em `PendenteConfirmacao` ou `Confirmada`. Se já pagou e ainda está confirmada, o cancelamento reembolsa o valor em aberto
7. Cada transição passa pela `OrderStateMachine` do domínio, com histórico em `OrderStatusHistory` e auditoria
8. Emails automáticos (pt-PT, branding da loja): boas-vindas no registo; encomenda recebida (cliente) e nova encomenda (florista); confirmação com link de pagamento (`App__WebBaseUrl`); proposta de alteração; recusa; pagamento recebido (cliente e florista, via Resend quando configurado); pronta; entregue; cancelada. Falhas de envio não bloqueiam o pedido — ficam no log e em `AuditLog` (`EmailDeliveryFailed`)

Pagamentos, Connect, comissão e webhooks: [`docs/pagamentos-stripe.md`](docs/pagamentos-stripe.md).

### Email (variáveis)

| Variável | Descrição |
|----------|-----------|
| `Email__Provider` | `Log` (predefinido) ou `Resend` |
| `Email__ResendApiKey` | Chave API Resend (obrigatória com `Resend`) |
| `Email__From` | Remetente (`Nome <email@dominio.pt>`) |
| `App__WebBaseUrl` | URL do frontend para links «Pagar encomenda» |
| `Stripe__SecretKey` | Chave secreta Stripe. Vazia desativa pagamentos |
| `Stripe__PublishableKey` | Chave publicável |
| `Stripe__WebhookSecret` | Segredo do webhook (`whsec_…`) |

API útil: `GET /api/orders/manage?status=` (florista), `GET /api/orders/{id}` (cliente ou florista), `GET /api/portfolio/{id}` (pré-preencher inspiração).

## CI (GitHub Actions)

O workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) corre em **pull requests** e em **push** para `main`. Três jobs em paralelo, com **filtros por pasta** (só corre o que mudou; alterações só ao workflow voltam a validar tudo):

| Job | O quê |
|-----|--------|
| **Backend** | `dotnet restore`, `build` e `test` em Release (NuGet em cache; relatório TRX no PR) |
| **Web** | `npm ci`, `npm run lint`, `npm run build` |
| **Android** | `./gradlew :androidApp:assembleDebug` (SDK 37, AGP 9.5, Java 21; sem build iOS/macOS) |

`concurrency` cancela execuções antigas da mesma ref. Não há PostgreSQL nem testes de integração no pipeline — alinhado com os testes unitários atuais do backend.

## Testes

```bash
cd backend && dotnet test
```

Testes unitários da `OrderStateMachine`, regras de encomendas por tenant (incl. loja suspensa), resolução de host (`TenantHostResolution`), slugs e validação de pedidos/propostas.

## Como foi construído

Desenvolvido com agentes de programação com IA (Claude Code, Cursor e outros). Defini o produto, a arquitetura e as
regras de negócio, orientei e revi cada alteração e testei-a.

## Licença

© 2026 Diogo Pereira. Todos os direitos reservados. O código está público para consulta, como portefólio; não pode ser copiado, alterado nem redistribuído sem autorização.
