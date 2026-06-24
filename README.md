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
| `OrderSystem.Models` | Domain: `Entity`/`EntityDomainEvent` base, aggregates (`Product`, `Order`), `DomainEvents/` |
| `OrderSystem.Dto` | DTOs returned by the API |
| `OrderSystem.Repositories` | `AppDbContext`, generic repository, unit of work, EF configs, outbox |
| `OrderSystem.Services` | External integrations (email) + MediatR pipeline behaviors |
| `OrderSystem.Commands` | CQRS write side: command + validator + handler, domain-event handlers |
| `OrderSystem.Queries` | CQRS read side: query + handler (`AsNoTracking`) |
| `OrderSystem.Composition` | Composition root (`AddCompositionSetup`) wiring MediatR, validators, DbContext |
| `OrderSystem.Api` | Thin controllers (`_mediator.Send`), middleware, host |
| `OrderSystem.UnitTests` | xUnit + Moq + MockQueryable |

## Tech stack

.NET 9 · MediatR 12 · FluentValidation 11 · EF Core 9 (SQLite) · Swashbuckle.

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

## Test

```bash
dotnet test
```
