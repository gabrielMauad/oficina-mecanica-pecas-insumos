# Arquitetura do serviço PecasInsumos

Complementa o [README](../README.md) com a estrutura de projetos, os fluxos de mensageria em detalhe e
as decisões de projeto do serviço.

## Estrutura de projetos

```
src/Bootstrap/Api/                       host: Program.cs, autenticação, OpenAPI, health checks, observabilidade
src/Modules/PecasInsumos/
  PecasInsumos.Domain/                   PecaInsumo (agregado), Dinheiro, UnidadeDeMedida
  PecasInsumos.Application/              Commands, Queries, IntegrationEventHandlers, port IPecaInsumoGateway
  PecasInsumos.Adapters/                 PecaInsumoController (CA), Gateway, Presenter, Requests/ViewModels, IPecaInsumoRepository
  PecasInsumos.Infrastructure/           DbContext + migrations, repositório EF, consumers e bus MassTransit
  PecasInsumos.Web/                      PecaInsumoApiController (rotas HTTP e papéis)
src/SharedKernel/                        Result/Error, behaviors do MediatR, IIntegrationEventBus
src/Contracts/OficinaMecanica.Contracts  eventos de integração (cópia idêntica em todos os serviços)
```

Direção das dependências: `Web → Adapters → Application → Domain`; `Infrastructure` implementa os ports
declarados em `Adapters` e `Application`. `Domain` e `Application` não referenciam MassTransit, EF Core
nem ASP.NET.

## Pipeline do MediatR

Todo command passa por `LoggingBehavior` → `ValidationBehavior` → `TransactionBehavior`.

- A validação (FluentValidation) devolve uma falha `Result`, não lança exceção; no consumer isso significa
  que um item inválido do evento (id vazio, quantidade ≤ 0) vira falha de negócio.
- O `TransactionBehavior` só persiste (`SaveChanges`) quando o resultado é sucesso, e publica os eventos
  pendentes **depois** do commit. Um command que falha não grava nada.

## Fluxo 1 — baixa do orçamento (`OrcamentoGerado`)

1. A OS publica `OrcamentoGerado` com `OrdemServicoId`, `OrcamentoId` e a lista `{PecaInsumoId, Quantidade}`.
2. O `OrcamentoGeradoConsumer` (fila `pecas-insumos-orcamento-gerado`) delega ao
   `DecrementarEstoqueQuandoOrcamentoGerado`, que envia `BaixarEstoqueDoOrcamentoCommand`.
3. `AjusteDeEstoqueEmLote` soma as quantidades por peça, carrega cada peça, valida (existe, está ativa, o
   saldo cobre a quantidade) e aplica `Decrementar` em memória. Acumula os problemas de **todas** as
   peças em vez de parar na primeira.
4. Sem problemas: entrega as peças alteradas ao gateway e o `TransactionBehavior` faz **um único
   `SaveChanges`** — a transação do banco torna a baixa atômica.
5. Com problemas: o command falha, nada é gravado e o handler publica `EstoqueBaixaFalhou` com o motivo.
   Como o `TransactionBehavior` não publica eventos quando o command falha, a publicação é feita
   diretamente no `IIntegrationEventBus` pelo handler do evento.

## Fluxo 2 — estorno (`OrcamentoRejeitado`)

Mesmo mecanismo em lote com `Incrementar`. Todas as peças são devolvidas numa transação ou nenhuma. Peça
inexistente ou desativada impede o estorno inteiro, que fica registrado em log de erro.

## Fluxo 3 — consulta de disponibilidade (HTTP)

`GET /api/v1/pecas-insumos/{id}/disponibilidade?quantidade={n}` devolve `disponivel` (saldo ≥ `n`) e o
`precoUnitario` atual. É uma leitura, sem reserva: a reserva efetiva acontece só quando o orçamento é
gerado e o evento é consumido (fluxo 1).

## Decisões de projeto

| Decisão | Razão |
|---|---|
| Baixa e estorno do orçamento por **evento**, não por chamada HTTP da OS | a OS não precisa aguardar o estoque nem conhecer a API de saída de estoque; o serviço decide e comunica só a falha |
| Operação **em lote, tudo ou nada** | um orçamento parcialmente baixado deixaria o estoque inconsistente sem ter como compensar peça a peça |
| Falha de negócio vira **evento** (`EstoqueBaixaFalhou`), falha de infraestrutura vira **exceção** | o evento alimenta a compensação da Saga (OS rejeita o orçamento; pagamento cancela o pendente); a exceção aciona o retry do consumer |
| Banco **PostgreSQL** próprio | dados relacionais simples e transacionais (saldo de estoque exige gravação atômica de várias linhas) |
| Consumers MassTransit finos | mantêm a Application independente do transporte; trocar o broker não altera Domain nem Application |
| Sem chamadas HTTP de saída | o evento já carrega peças e quantidades, então o consumo de fila não depende de outro serviço estar no ar |

## Limitações e evolução futura

Descritas no [README](../README.md#limitações-conhecidas): sem outbox, sem inbox (reentrega), sem token de
concorrência na peça e estorno sem compensação.
