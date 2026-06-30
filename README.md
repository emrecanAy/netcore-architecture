# OrderSystem

A sample order-management backend that mirrors the **JetZen** reference architecture
(`PROJECT.md`): **Layered + CQRS + tactical DDD + Domain Events**, on **ASP.NET Core Web API**
with **EF Core (SQLite)**.

> Status: work in progress — built incrementally. See `PROJECT.md` for the architectural rationale
> behind every layer.

## Architecture at a glance

Dependency direction (outer → inner):

```
Api → Composition → {Commands, Queries} → {Dto, Services} → {Models, Repositories, Common}
```

| Project | Responsibility |
|---------|----------------|
| `OrderSystem.Common` | `ValueObject` base, `AppSettings`, shared utilities |
| `OrderSystem.Models` | Domain: `Entity`/`EntityDomainEvent` base, aggregates (`Product`, `Order`, `KnowledgeDocument`), `DomainEvents/` |
| `OrderSystem.Dto` | DTOs returned by the API |
| `OrderSystem.Repositories` | `AppDbContext`, generic repository, unit of work, EF configs, outbox, `SqliteVecStore` (vector search) |
| `OrderSystem.Services` | External integrations (email, Gemini chat/embeddings), RAG ingestion worker + MediatR pipeline behaviors |
| `OrderSystem.Commands` | CQRS write side: command + validator + handler, domain-event handlers |
| `OrderSystem.Queries` | CQRS read side: query + handler (`AsNoTracking`) |
| `OrderSystem.Composition` | Composition root (`AddCompositionSetup`) wiring MediatR, validators, DbContext |
| `OrderSystem.Api` | Thin controllers (`_mediator.Send`), middleware, host |
| `OrderSystem.UnitTests` | xUnit + Moq + MockQueryable |

## Tech stack

.NET 9 · MediatR 12 · FluentValidation 11 · EF Core 9 (SQLite) · Swashbuckle ·
[sqlite-vec](https://github.com/asg017/sqlite-vec) (native vector search) · Google Gemini (chat + embeddings).

> Mapping is done with hand-written extension methods in `OrderSystem.Dto` (no AutoMapper):
> the only free AutoMapper version (13.0.1) carries a high-severity advisory and the patched
> versions are commercially licensed.

## Run

```bash
# apply migrations, then start the API
dotnet run --project src/OrderSystem.Api -- --migrate
dotnet run --project src/OrderSystem.Api
# open Swagger UI at /swagger
```

The RAG help-desk module calls the Gemini API. Provide the key via user-secrets
(it is never committed):

```bash
dotnet user-secrets --project src/OrderSystem.Api set "App:Rag:ApiKey" "<your-gemini-api-key>"
```

RAG settings live under `App:Rag` in `appsettings.json` (models, embedding
dimensions, chunk size/overlap, `TopK`, `MinScore`, upload limit). The sqlite-vec
native extension (`native/vec0.dll`) is copied next to the app and loaded at
runtime by a connection interceptor; swap in the `.so`/`.dylib` for Linux/macOS.

## Test

```bash
dotnet test
```

## API

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/products` | Create a product |
| `GET` | `/api/products/{id}` | Get a product |
| `GET` | `/api/products?onlyActive=true` | List products |
| `POST` | `/api/orders` | Place an order (draft → placed) |
| `POST` | `/api/orders/{id}/pay` | Pay a placed order |
| `GET` | `/api/orders/{id}` | Get an order |
| `POST` | `/api/helpdesk/documents` | Upload a document into the knowledge base (async ingestion) |
| `GET` | `/api/helpdesk/documents` | List knowledge-base documents |
| `GET` | `/api/helpdesk/documents/{id}` | Get a document (with ingestion status) |
| `DELETE` | `/api/helpdesk/documents/{id}` | Delete a document and its chunks |
| `POST` | `/api/helpdesk/ask` | Answer a question strictly from the indexed knowledge base |

### Walkthrough

```bash
# create a product (stock 10)
curl -X POST http://localhost:5080/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Mechanical Keyboard","sku":"KB-100","price":149.90,"currency":"USD","initialStock":10}'

# place an order for 3 → stock drops to 7, confirmation email is dispatched via the outbox
curl -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"11111111-1111-1111-1111-111111111111","currency":"USD","items":[{"productId":"<id>","quantity":3}]}'
```

### RAG help-desk walkthrough

```bash
# upload a document → accepted as Pending, ingested in the background
curl -X POST http://localhost:5080/api/helpdesk/documents \
  -F "file=@iade-politikasi.md" -F "title=Return Policy"

# poll status until Ingested
curl http://localhost:5080/api/helpdesk/documents/<id>

# ask a question; the answer is grounded only in indexed chunks
curl -X POST http://localhost:5080/api/helpdesk/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"How many days do I have to return an item?"}'
```

## How the key patterns fit together

- **Rich domain model** — rules live in entity behavior (`Product.DecreaseStock`,
  `Order.Place`); state changes only through named methods, never public setters.
- **Domain events + cascade** — placing an order raises `OrderPlacedDomainEvent`; an
  in-process handler reduces stock, which raises `ProductStockDecreased`/`ProductOutOfStock`.
  `UnitOfWorkBehavior` dispatches events in a cascade loop, then commits once in a single
  transaction. Handlers never call `SaveChanges`.
- **Transactional outbox** — every dispatched event is written to `OutboxMessages` in the
  same transaction; `OutboxDispatcher` (a background service) relays them after commit, so
  external side-effects like email never fire for a rolled-back transaction.
- **CQRS** — writes go through `Commands` (+ FluentValidation), reads through `Queries`
  (`AsNoTracking`).
- **Assembly scanning** — new handlers/validators are discovered automatically; no manual DI.
- **RAG help-desk** — uploaded documents are accepted as `Pending`; a background
  `DocumentIngestionWorker` extracts text, chunks it (`TextChunker`), embeds each chunk
  via Gemini and stores vectors in `SqliteVecStore`. `ask` embeds the question, runs a
  top-K similarity search above `MinScore`, and asks Gemini to answer **only** from the
  retrieved chunks — so answers stay grounded in the indexed knowledge base.

> See `PROJECT.md` for the full rationale, and its §15 for the improvements applied here
> (outbox, single transaction, migration as a separate `--migrate` step).
