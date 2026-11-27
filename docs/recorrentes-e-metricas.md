# Encomendas recorrentes e métricas SuperAdmin

## Encomendas recorrentes

### Modelo

- `RecurringSubscription` — pedido do cliente com frequência (semanal, quinzenal, mensal), âncora de entrega, morada e composição ou **escolha da florista** com orçamento.
- `RecurringOccurrence` — ocorrência prevista; chave `IdempotencyKey` única por subscrição e data.
- `Order.RecurringSubscriptionId` / `RecurringOccurrenceId` — ligação à encomenda gerada.

Estados da subscrição: `PendingFloristApproval`, `Active`, `Paused`, `Cancelled`, `Rejected`.

### API (JWT + tenant)

| Papel | Endpoints |
|-------|-----------|
| Cliente | `POST /api/recurring-subscriptions`, `GET …/mine`, `POST …/{id}/pause\|resume\|skip-next\|cancel` |
| Florista | `GET …/manage`, `GET …/upcoming`, `POST …/{id}/accept\|reject` |

### Geração automática

- `RecurringOrderGenerationHostedService` — poll configurável (`RecurringOrders:PollIntervalSeconds`).
- `RecurringOrderGenerationService` — `IgnoreQueryFilters` para varrer todas as lojas; lease `GenerationLeaseUntil` para várias instâncias.
- Gera encomenda em `PendingConfirmation` com antecedência `GenerationAdvanceDays` (default 3).
- Notificações: `IRecurringEmailNotifications` + `IRecurringPushNotifications` (preferência `RecurringSubscriptions`).

### Web / app

- Web: `/recorrencias` (cliente), `/admin/recorrencias` (florista).
- App: ecrã **Recorrências** no menu do cliente; florista vê próximas entregas na web (admin).

## Métricas da plataforma (SuperAdmin)

- `GET /api/platform/metrics?days=7|30|90`
- Serviço `PlatformMetricsService` usa **`IgnoreQueryFilters()`** explicitamente em `Orders`, `Payments` e `Tenants`.
- Métricas: lojas ativas, encomendas e GMV no período, comissão (soma de `ApplicationFeeAmountCents` em pagamentos `Succeeded`), lojas com Stripe, novas lojas no mês, séries diárias, top 10 lojas, novas lojas por mês.
- UI: `/superadmin/metricas`.

### Autorização

Apenas role `SuperAdmin`. Testes de integração em `PlatformMetricsAuthorizationTests` (403 para florista e cliente).

## Testes

- `RecurringScheduleCalculatorTests` — datas, avanço, antecedência de geração, idempotência da chave.
- `PlatformMetricsServiceTests` — agregação multi-tenant e isolamento do filtro EF por tenant.
- `PlatformMetricsAuthorizationTests` — acesso HTTP.

## Configuração

```json
"RecurringOrders": {
  "GenerationAdvanceDays": 3,
  "PollIntervalSeconds": 60,
  "GenerationLeaseSeconds": 120
}
```

## Migração EF

`RecurringOrdersAndPlatformMetrics` — tabelas de recorrência, FKs em `Orders`, coluna `NotificationPreferences.RecurringSubscriptions`.
