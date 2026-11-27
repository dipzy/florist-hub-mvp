# Pagamentos Stripe

O cliente só paga depois de a florista confirmar a encomenda. O dinheiro entra na conta Stripe Connect (Express) da florista. A plataforma fica com uma comissão (`application_fee_amount`), em percentagem, configurada pelo SuperAdmin — globalmente e, se quiser, por loja. A mensalidade fica guardada (global e por loja) e **não é cobrada**.

Sem `Stripe__SecretKey`, os ecrãs mostram os pagamentos como desativados. Não há chamadas ao Stripe e o resto da encomenda continua a funcionar.

## Variáveis

Não commit de chaves. Em desenvolvimento e produção:

| Variável | Configuração | Uso |
|----------|----------------|-----|
| `Stripe__SecretKey` | `Stripe:SecretKey` | Chave secreta (`sk_test_…` ou `sk_live_…`) |
| `Stripe__PublishableKey` | `Stripe:PublishableKey` | Chave publicável, exposta só no estado da ligação |
| `Stripe__WebhookSecret` | `Stripe:WebhookSecret` | Segredo da assinatura do webhook |

O Checkout usa EUR. Os métodos são cartão, MB Way e Multibanco, apenas os que estiverem **ativos** na conta Connect da florista.

## O que acontece

1. A florista liga a conta em **Pagamentos** no painel. A API cria uma conta Express (país PT) e devolve um Account Link.
2. O cliente, com a encomenda em Confirmada, Em preparação ou Pronta, e ainda por pagar, escolhe **Pagar**. A API abre uma sessão de Checkout em cobrança directa na conta da florista, com a comissão da plataforma.
3. O webhook, com assinatura verificada, actualiza o pagamento e o histórico. O mesmo `event.id` não é processado duas vezes.
4. A florista reembolsa o total em aberto ou uma parte. O cancelamento pelo cliente, enquanto a encomenda está confirmada e já paga, faz o reembolso total do valor em aberto e só depois passa a Cancelada. Depois de entrar em preparação, o cliente já não cancela; a florista reembolsa sem mudar o estado da encomenda.
5. O email de pagamento recebido segue pelo serviço já existente (Resend quando `Email__Provider=Resend`).

Eventos tratados:

- `checkout.session.completed` (só marca pago se `payment_status` for `paid`; Multibanco pode ficar pendente)
- `checkout.session.async_payment_succeeded` e `checkout.session.async_payment_failed` (Multibanco)
- `payment_intent.payment_failed`
- `charge.refunded` (total ou parcial, pelo valor absoluto devolvido pelo Stripe)
- `account.updated` (cobranças, levantamentos e métodos da conta da loja)

Um pagamento já confirmado não volta a falhado se chegar um evento atrasado.

## Modo de teste

1. No [Dashboard Stripe](https://dashboard.stripe.com/test/apikeys), copie as chaves de **teste**.
2. Active Connect e, na conta de teste, os métodos cartão, MB Way e Multibanco para Portugal.
3. Exporte as variáveis e arranque a API (`http://localhost:5183`) e a web (`http://localhost:5173`).
4. Na loja, como florista, abra **Pagamentos** e conclua o onboarding Express em modo de teste.
5. Confirme uma encomenda e, como cliente, use **Pagar**. Cartão de teste: `4242 4242 4242 4242`, validade futura, CVC qualquer.

### Webhooks locais (Stripe CLI)

```bash
stripe listen --forward-to localhost:5183/api/stripe/webhook
```

O CLI mostra um `whsec_…`. Exporte-o como `Stripe__WebhookSecret` e reinicie a API. Para disparar um evento à mão:

```bash
stripe trigger checkout.session.completed
```

O endpoint recusa corpos sem assinatura válida (`Stripe-Signature`) e responde 503 se a chave secreta não estiver definida.

## Onde está no código

- `IStripeGateway` isola o Stripe.net. Os testes usam `FakeStripeGateway` e não chamam a rede. A verificação de assinatura testa o adaptador real com um payload assinado.
- Entidades `Payment`, `PaymentEvent`, `PlatformBillingSettings` e `StripeWebhookReceipt`, com filtro de tenant nos pagamentos.
- Web: botão Pagar, `/encomendas/:id/pagamento/sucesso` e `…/cancelado`, painel da florista e comissão do SuperAdmin.
- App: estado do pagamento e Checkout no browser do sistema (Custom Tabs no Android, SFSafariViewController no iOS), com regresso por `floristhub://pagamento/…`.
