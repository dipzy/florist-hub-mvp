# Marketing da loja e SEO

Cupões, campanhas, flores da época, avaliações e páginas públicas. Tudo fica no tenant da loja. O cliente não calcula o desconto: a API valida o cupão e grava o valor a pagar.

## Cupões

A florista cria o código em **Cupões**. Pode ser uma percentagem ou um valor fixo, com início e fim, limite total e por cliente, e valor mínimo da encomenda (artigos mais entrega, antes do desconto). Sem flores nem categorias, o desconto incide em todos os artigos. Com abrangência, só nessas linhas. A entrega não entra no desconto.

O código é normalizado em maiúsculas. O desconto arredonda a cêntimos, metade para cima (`MidpointRounding.AwayFromZero`), e nunca passa do valor elegível. O total da encomenda (`EstimatedTotal`) passa a ser o valor a pagar. O Stripe cobra esses cêntimos e a comissão da plataforma é essa percentagem sobre o mesmo valor. A fatura-recibo leva as linhas dos artigos, a entrega e uma linha negativa `Desconto CODIGO`, que somam o valor pago.

Um checkout pendente é cancelado quando o cupão muda, para a sessão seguinte não reutilizar o valor antigo. Cancelar a encomenda liberta a utilização. Um cupão já usado fica inativo em vez de ser apagado.

## Datas especiais e época

A campanha tem nome, datas (calendário de Lisboa), destaque na página inicial, flores e um preço especial opcional. Enquanto a campanha está ativa, a encomenda usa o menor preço especial dessa flor, mesmo que o cliente envie outro preço.

A flor pode ter meses de época (1 = janeiro). A loja pública mostra a campanha em destaque e as flores do mês corrente.

## Avaliações

Só o cliente da encomenda, e só depois de entregue, pode deixar 1 a 5 estrelas e um comentário, uma vez. A florista responde ou oculta. A média e a lista públicas ignoram avaliações ocultas.

## SEO

A aplicação web é uma SPA. O `index.html` é genérico e o JavaScript só actualiza o título no browser. Os crawlers que não executam JavaScript devem usar a API, no host da loja (domínio próprio ou subdomínio):

| Pedido | Conteúdo |
| --- | --- |
| `GET /robots.txt` | `Allow` e o endereço do sitemap dessa loja |
| `GET /sitemap.xml` | `/`, `/portfolio`, cada arranjo `/portfolio/{slug}`, `/flores-da-epoca` e campanhas activas `/campanha/{slug}` |
| `GET /api/seo/documento?caminho=/portfolio/buque-primaveril` | HTML com `title`, `description`, Open Graph e JSON-LD `Florist` + `LocalBusiness`. `aggregateRating` só entra quando há avaliações visíveis |

Não há um frontend gerado por florista. Se o domínio público for o da SPA, o proxy deve encaminhar `/robots.txt`, `/sitemap.xml` e, para user-agents de crawler, o HTML de `/api/seo/documento`.

## Onde está

- Regras de dinheiro: `CouponMath` e `OrderPricing`.
- Fatura: `InvoiceService.BuildLines`.
- Páginas: loja em `/`, portfolio em `/portfolio/{slug}`, painel em `/admin/cupoes`, `/admin/campanhas` e `/admin/avaliacoes`.
- App: cupão no checkout, campanha activa no início e avaliação na encomenda entregue.
