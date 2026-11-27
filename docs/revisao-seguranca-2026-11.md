# Revisão de segurança e isolamento — 10 de novembro de 2026

Âmbito: endpoints e serviços que entraram em `main` (`3d26a2d`) — faturação InvoiceXpress, RGPD, notificações push, encomendas recorrentes, métricas do SuperAdmin, cupões, campanhas, flores da época, avaliações, SEO e capacidade/stock. Não foram acrescentadas funcionalidades de produto.

O filtro global do Entity Framework deixa passar todas as linhas de tenant quando `CurrentTenantId` é nulo. Pedidos anónimos sem domínio nem cabeçalho de loja, o SuperAdmin (sem tenant no JWT) e os trabalhos em segundo plano estão nessa situação. Os serviços novos foram revistos à luz disso.

## O que foi verificado

- Autorização por papel nos controladores novos (florista, cliente, SuperAdmin e anónimo).
- Isolamento por `TenantId`, incluindo `IgnoreQueryFilters`, serviços alojados e a geração de recorrências.
- IDOR: identificadores de outra loja ou de outro cliente em encomendas, recorrências, avaliações, faturas e exportação de dados.
- Validação de entrada dos cupões (valor negativo, percentagem e casas decimais).
- Corridas no máximo de utilizações do cupão, na capacidade de entrega e no stock.
- Webhook Stripe (assinatura e idempotência) e o modo de pagamento simulado.
- Credenciais do InvoiceXpress e tokens FCM.
- Dados pessoais em logs, no Sentry e no HTML para crawlers.

## Achados

### Alta

**Catálogo e marketing públicos sem loja resolvida.** `FlowerService.ListAsync`, `PortfolioService` (listagem, id e slug), `ReviewService.ListPublicAsync` e `SummaryAsync`, `PublicStorefrontService.SeasonalAsync` e `CampaignBySlugAsync`, e `SeoService.SitemapAsync` não exigiam tenant. Com o filtro aberto, um pedido sem loja recebia flores, portfolio, avaliações, campanhas e o sitemap de todas as lojas. Correção: `RequireTenant` / `RequireTenantAsync` antes da leitura. Teste: `Public_catalog_without_tenant_does_not_leak_other_stores`.

**Corrida no máximo de utilizações do cupão.** A contagem de resgates e a inserção não estavam na mesma transação com bloqueio. Dois pedidos paralelos passavam o máximo. Correção: `pg_advisory_xact_lock` do cupão dentro de uma transação compatível com a estratégia de retentativa do Npgsql, na criação da encomenda, na aplicação do cupão e na reaplicação ao aceitar uma proposta. Teste: `Parallel_coupon_uses_cannot_pass_the_maximum`. Valores negativos já eram rejeitados por `CouponMath`; ficou coberto por `Parallel` não, mas por um pedido com desconto fixo `-5` no mesmo conjunto de testes.

**Stock só serializado no processo.** A confirmação e a reposição usavam um semáforo em memória. Duas instâncias da API vendiam a última unidade. Correção: `pg_advisory_xact_lock` por flor dentro da transação da base de dados. Na confirmação o bloqueio de capacidade continua a ser adquirido antes do de stock. Teste: `Parallel_confirms_on_two_processes_cannot_oversell_stock` (duas `WebApplicationFactory`).

**Chave do InvoiceXpress e destino do PDF.** A chave ia na query string e as respostas de erro eram relançadas sem redação, pelo que podiam aparecer em logs, em `LastError` e na resposta da API. O download do PDF aceitava qualquer nome DNS, incluindo endereços que resolvem para IPs privados ou de metadata. Correção: todas as falhas passam por redação da chave; o URL tem de ser HTTPS e o DNS não pode resolver para loopback, link-local, privado ou ULA. Testes: `InvoiceXpressSafetyTests`.

### Média

**Subscrição recorrente legível fora do cliente.** `EnsureCanAccess` devolvia de imediato para Florista e SuperAdmin. O SuperAdmin não tem tenant, por isso o filtro aberto devolvia a subscrição de qualquer loja (morada, notas, nome). Correção: o florista só acede se o tenant do pedido for o da subscrição; o cliente só se for o titular; o SuperAdmin é recusado neste recurso. Teste: `Recurring_subscription_is_not_readable_across_customers_or_by_superadmin`.

**Geração de recorrências em segundo plano.** A subscrição era lida com o filtro de tenant antes de `SetTenant`, dependendo do filtro aberto. Correção: ler o `TenantId` com `IgnoreQueryFilters`, fixar o tenant e só depois consultar.

**Anonimização incompleta.** O pedido do titular apagava morada e notas, mas deixava o NIF na encomenda e o texto da avaliação. Correção: limpar `CustomerTaxId` e o comentário. Os documentos fiscais já emitidos não são reescritos (obrigação de conservação). Teste: `Anonymize_clears_tax_id_and_review_comment`.

**Totais com a taxa de entrega a dobrar.** `EstimatedTotal` já inclui a taxa menos o desconto. As métricas do SuperAdmin, o painel da florista e o total gasto do cliente somavam outra vez `DeliveryFee`. Correção: somar só `EstimatedTotal`. O teste de métricas passou a esperar 130 em vez de 135.

**Testes de integração na base de desenvolvimento.** A connection string injectada na factory não ganhava a `appsettings.Development.json`, e os testes escreveram campanhas, cupões e o limite de capacidade na base `floristhub`. Correção: a fixture define `ConnectionStrings__DefaultConnection` e `UseSetting` antes de arrancar o host. Os resíduos deixados na base de demonstração (campanhas `segredo-*` e limite diário 400) foram retirados.

### Baixa

**Dados em logs e no Sentry.** O seed escrevia a palavra-passe de demonstração. O envio de email registava o endereço completo e a mensagem da exceção na auditoria. O push de log guardava os primeiros caracteres do token FCM. O Sentry podia anexar o corpo do pedido. Correção: deixar de registar a palavra-passe e o prefixo do token; o email na auditoria fica `***@domínio`; `SendDefaultPii` mantém-se falso e `MaxRequestBodySize` passa a `None`.

## Verificado sem falha nova

- Cupão com valor negativo, percentagem acima de 100 ou mais de duas casas decimais é recusado.
- As definições de faturação devolvem `credentialsConfigured`, não a chave.
- O webhook Stripe exige assinatura; o recibo é idempotente. O gateway simulado (`Stripe:Provider=Fake`, nunca em Production) recusa webhooks e não grava `acct_dev_fake` na loja.
- O HTML e o JSON-LD para crawlers escapam `<`.
- As métricas da plataforma estão limitadas ao SuperAdmin e o `IgnoreQueryFilters` é explícito.
- A capacidade de entrega já usava `pg_advisory_xact_lock` e um teste de corrida.
- Criar uma avaliação exige o cliente da encomenda e o estado entregue.
- O registo de push que move um token entre contas usa `IgnoreQueryFilters` de propósito: o token é um segredo do dispositivo. Fica como risco aceite.
- A retentativa de faturas e o webhook de pagamento usam `IgnoreQueryFilters` para localizar a linha e depois trabalham com o tenant dessa linha.

## Correções de ecrã encontradas no browser

Não são falhas de isolamento, mas impediam os fluxos novos: a rota `/flores-da-epoca` anunciada no sitemap não existia; a página mostrava «sem flores» enquanto carregava; o configurador mostrava o preço de catálogo quando a encomenda ia ser cobrada ao preço de campanha; o markdown legal não separava títulos de parágrafos; o banner de cookies tapava o botão no fundo do ecrã; «Dashboard» e «Stock» apareciam em inglês. O seed de demonstração passou a marcar a época das flores de exemplo.
