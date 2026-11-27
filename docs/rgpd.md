# RGPD e termos legais (Bloco A)

> **Aviso:** Os textos legais incluídos na base de dados são **modelos** e devem ser revistos por advogado antes de produção.

## Âmbito

- Textos-base da plataforma FloristHub, versionados em `LegalDocumentVersions`.
- Dados de responsável pelo tratamento e NIF da loja vivem em **`FloristInvoicingSettings`** (faturação) — reutilizados nos textos legais RGPD.
- Isolamento por `TenantId` em consentimentos, exportação, anonimização e auditoria.

## Versionamento

| Tipo | Enum | Versão inicial |
|------|------|----------------|
| Termos e Condições | `Terms` | `2026.1` |
| Política de Privacidade | `Privacy` | `2026.1` |
| Política de Cookies | `Cookies` | `2026.1` |

Apenas um registo `IsCurrent = true` por tipo. Novas versões: criar migração ou script administrativo (UI de publicação fica pendente).

## Consentimentos

| Momento | Obrigatório | Registo |
|---------|-------------|---------|
| Registo de cliente | Termos + Privacidade; marketing opcional | Campos em `AspNetUsers` + `AuditLogs` |
| Submissão de encomenda | Termos + Privacidade (versão atual) | Campos em `Orders` + auditoria `Legal.OrderConsent` |
| Pagamento Stripe | Reconfirmação Termos + Privacidade | Atualiza `Orders` + `Legal.PaymentConsent` |

API valida que as versões enviadas coincidem com as atuais (`GET /api/legal/versions`).

## Cookies (web)

- Banner na primeira visita; escolha em `localStorage` (`floristhub.cookieConsent`).
- **Essenciais:** sessão, tenant.
- **Opcionais:** Sentry — `initSentryIfAllowed()` só após consentimento «Aceitar todos».

## Direitos do titular (cliente)

- `GET /api/privacy/export` — JSON com perfil, consentimentos e encomendas **do tenant atual**.
- `POST /api/privacy/anonymize` — anonimiza conta (email técnico `anonymized-{id}@anon.floristhub.invalid`); encomendas mantidas com morada/notas anonimizadas (obrigações fiscais).

## Auditoria

Ações em `ComplianceAuditActions` (ex. `Legal.DataExport`, `Legal.AccountAnonymized`). Consulta:

- Florista: `GET /api/compliance-audit`
- SuperAdmin: `GET /api/compliance-audit/platform?tenantId=`

## UI

- Web: `/legal/{termos|privacidade|cookies}`, `/privacidade/dados`, painel `/admin/rgpd`, SuperAdmin `/superadmin/rgpd`.
- Mobile: ecrãs `legal/{kind}` e aceitação em montar encomenda / pagar.

## Migração

`20261110163438_GdprLegalCompliance` — colunas RGPD, tabela legal e seed dos três documentos v1.

## Pendente / melhorias

- Revisão jurídica dos textos e adaptação por mercado.
- UI SuperAdmin para publicar novas versões sem migração.
- Registo de consentimento de cookies no servidor (opcional).
- DPO e contacto dedicado RGPD na plataforma.
