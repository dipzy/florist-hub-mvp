# Segurança — Florist Hub

Este documento resume as medidas de segurança aplicadas na API e na web, e como as validar.

## Isolamento multi-tenant

- O Entity Framework aplica **filtros globais** por `TenantId` em flores, portfolio, encomendas, propostas, histórico, zonas de entrega e horários (`ApplicationDbContext.CurrentTenantId`, reavaliado por consulta).
- O middleware `TenantResolutionMiddleware` resolve o tenant por JWT (prioridade), host/subdomínio e, só para pedidos anónimos ou SuperAdmin, cabeçalhos `X-Tenant-Id` / `X-Tenant-Slug`.
- Utilizadores autenticados (exceto SuperAdmin) **não podem** mudar de loja via cabeçalhos HTTP.
- Encomendas: floristas só acedem a encomendas da sua loja (`OrderService.EnsureCanAccess`).
- Ficheiros em `uploads/{tenantId}/…` exigem tenant resolvido coincidente; conteúdos `demo/` permanecem públicos.

Testes de regressão: projeto `FloristHub.Api.IntegrationTests` (PostgreSQL + `WebApplicationFactory`).

## Rate limiting

- Política `auth` na API: **20 pedidos/minuto por IP** nos endpoints `POST /api/auth/login`, `register` e `florist-invite/complete`.
- Resposta `429 Too Many Requests` quando o limite é excedido.

## Cabeçalhos HTTP

`SecurityHeadersMiddleware` envia:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy` restritiva
- `X-Permitted-Cross-Domain-Policies: none`

## CORS

- Origens permitidas: lista estática (`Cors:Origins` / `Cors__AllowedOrigins`) mais domínios personalizados registados nas lojas (cache 5 min), com validação em `TenantCorsOriginChecker`.
- Em desenvolvimento, `localhost` e `127.0.0.1` são aceites.
- Métodos limitados a GET, POST, PUT, PATCH, DELETE e OPTIONS.
- Cabeçalhos limitados a `Authorization`, `Content-Type`, `X-Tenant-Id` e `X-Tenant-Slug` (já não se usa `AllowAnyHeader`).

## Validação de propostas de alteração

- `CreateOrderChangeProposalRequest` valida mensagem, total revisado e JSON de itens (`OrderItemChangesJsonValidator` + FluentValidation).
- JSON malformado ou itens inválidos rejeitam a proposta antes de persistir.

## Monitorização (Sentry, opcional)

| Componente | Variável | Comportamento |
|------------|----------|----------------|
| API | `Sentry__Dsn` | Ativa `Sentry.AspNetCore`; **sem PII** (`SendDefaultPii = false`). |
| Web | `VITE_SENTRY_DSN` | Ativa `@sentry/react`; tag `tenant` com slug público apenas. |

**Source maps:** o upload para o Sentry está **desligado** por defeito (`vite build` não gera source maps para produção). Para ativar localmente, documente `SENTRY_AUTH_TOKEN` e `sentry-vite-plugin` no pipeline de release — não faz parte do CI atual.

## Variáveis sensíveis

- `Jwt__Key` obrigatório em produção (mín. 32 caracteres).
- Não commitar DSNs nem chaves; usar secrets do ambiente de deploy.

## Referências

- Testes E2E Playwright: `web/e2e/`
- CI: job `e2e-web` em `.github/workflows/ci.yml`
