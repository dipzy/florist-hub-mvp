# Faturação

Cada encomenda paga pela cliente gera uma **fatura-recibo em nome da florista**, que é a vendedora. A Florist Hub não numera, não assina e não comunica documentos à Autoridade Tributária. Quem o faz é o software certificado ligado à loja.

## Fornecedor escolhido

**InvoiceXpress** (certificado pela AT com o n.º 192).

Foi escolhido à frente do Moloni e do Vendus porque:

- a API REST está em **todos** os planos, incluindo o de entrada;
- autentica com uma chave de API e cobre fatura-recibo e nota de crédito, que é o que esta plataforma precisa;
- o Moloni e o Vendus só abrem a API no plano Flex (cerca de 10,90 € e 10,83 € por mês em faturação anual, sem IVA) e estão mais virados para stocks e POS, que a Florist Hub não gere.

A interface `IInvoicingProvider` isola o fornecedor. Trocar de software é um adaptador novo, sem mudar encomendas nem pagamentos. Em desenvolvimento e testes usa-se o fornecedor `Fake` (valor `0`), que emite um PDF sem valor fiscal. Em produção (`ASPNETCORE_ENVIRONMENT=Production`) esse fornecedor fica desligado, salvo se `Invoicing:AllowFakeProvider` for explicitamente `true`.

## Custos

Preços de tabela do InvoiceXpress, **sem IVA**, lidos na página de planos em outubro de 2026. Confirmem na [página oficial](https://invoicexpress.com/planos-precos/) antes de subscrever. Cada florista paga a sua própria conta: a fatura tem de sair da vendedora, não de uma conta partilhada da plataforma.

| Plano | Documentos / mês | Anual | Mensal |
| --- | --- | --- | --- |
| X3 | 3 | 3 € | não disponível |
| X10 | 10 | 7 € | 9 € |
| X100 | 100 | 20 € | 24 € |
| X500 | 500 | 24 € | 29 € |

Há planos maiores, até 100 000 documentos/mês. Uma encomenda paga conta como um documento; cada reembolso ou cancelamento com nota de crédito conta outro. O X3 chega para ensaios, não para uma loja aberta. A API está incluída. Há período experimental de 30 dias.

O Moloni, para comparação, pede o plano Flex (10,90 €/mês no anual, 20,99 € no mensal, sem IVA) para ter API. O Vendus pede o Flex (10,83 €/mês no anual, 17 € no mensal, sem IVA), por ponto de venda.

## O que o Diogo (ou cada florista) tem de criar

1. Uma conta InvoiceXpress **por florista**, com o nome legal e o NIF dessa florista. A conta é a emitente legal do documento.
2. Um plano com folga para faturas-recibo e notas de crédito.
3. A chave da API em Conta → Integrações → API. Não entra no repositório nem em ficheiros de configuração. Introduz-se no ecrã Faturação da loja; a API guarda-a cifrada com o Data Protection já usado pela aplicação e **nunca a devolve**.
4. O nome da conta: é o subdomínio, `nome.app.invoicexpress.com`.
5. Uma série comunicada à AT (ATCUD), criada dentro do InvoiceXpress. Se não for a série por defeito, copia-se o identificador da série para o campo Série.
6. Os impostos de origem (IVA23, IVA22, IVA16, IVA13, IVA6, IVA0). Uma conta nova já os traz. Sem o nome certo, a emissão falha e fica para repetir.
7. Se vender com IVA a 0%, o código de isenção (por exemplo `M10`) na loja.
8. Os mesmos dados fiscais (NIF, nome legal, morada, código postal) no ecrã Faturação, e a opção de emissão automática ligada.
9. O webhook Stripe a chegar à API. A emissão dispara quando o pagamento fica confirmado.
10. Cópias de segurança da tabela `DataProtectionKeys`. Sem essas chaves não se recuperam as credenciais cifradas.

## Como configurar na Florist Hub

- Florista: **Faturação** (`/admin/faturacao`). Fornecedor InvoiceXpress, conta, chave, série, IVA por defeito e isenção.
- Catálogo: cada produto pode ter a sua taxa (23, 22, 16, 13, 6 ou 0). Em branco, usa a taxa da loja. Os preços do catálogo **incluem IVA**.
- Cliente: NIF opcional ao montar a encomenda e outra vez no checkout, antes de pagar. É validado (dígito de controlo). Vazio significa consumidor final.
- Quando o webhook `checkout.session.completed` ou `checkout.session.async_payment_succeeded` confirma o pagamento, a API pede a fatura-recibo. Reembolsos e cancelamentos de encomendas pagas geram nota de crédito pelo valor ainda não creditado.
- A mesma encomenda não produz duas faturas-recibo para o mesmo pagamento: a chave de idempotência é o pagamento. Falhas ficam com estado Falhada e o processo em fundo repete, com espera crescente, até 8 tentativas. A florista também pode carregar em Repetir.
- Cliente e florista descarregam o PDF nos ecrãs da encomenda e de Faturação. O ficheiro não fica público: o pedido exige sessão e a encomenda tem de ser da mesma loja e, no caso do cliente, sua.
- Em desenvolvimento, escolhe-se o fornecedor **Teste**. Não comunica nada à AT.

Variável útil, sem segredos:

```json
"Invoicing": { "RetryIntervalSeconds": 60 }
```

## Limites

- Não há numeração, hash, ATCUD nem SAF-T próprios. Isso fica no InvoiceXpress, que também comunica os documentos à AT conforme o plano.
- A chave da API vai na query string, porque é assim que o InvoiceXpress autentica. A ligação é HTTPS, a chave não é escrita em logs deste cliente HTTP, e em repouso está cifrada.
- Os planos limitam documentos por mês. Esgotada a quota, a emissão falha e fica para repetir quando houver quota.
- O preço enviado ao InvoiceXpress é o líquido, com quatro casas decimais, calculado a partir do preço com IVA. Pode haver diferença de um cêntimo por arredondamento.
- Uma nota de crédito parcial sai numa linha única, à taxa de IVA por defeito da loja. O reembolso total replica as linhas da encomenda.
- Se o processo morrer depois de o InvoiceXpress aceitar o documento e antes de gravarmos o identificador, uma repetição pode criar um segundo rascunho. A janela é a do pedido HTTP.
- O fornecedor de teste não serve para produção.
- A app móvel ainda não pede o NIF nem descarrega o PDF. O checkout da web fá-lo. A API aceita o NIF e a app continua a pagar sem o campo.
- Isolamento: definições e documentos têm `TenantId` e filtro global. Um pedido autenticado de outra loja não lê o documento.

## Fora deste bloco

A **fatura da comissão da plataforma** à florista não é emitida. A comissão continua a ser a `application fee` do Stripe Connect. Para a faturar é preciso a pessoa coletiva da plataforma, com a sua conta certificada (não a da florista), e uma regra para o período e para a taxa. Fica como passo seguinte, com o mesmo adaptador, mas com credenciais da plataforma e não da loja.
