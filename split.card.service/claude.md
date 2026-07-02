# CLAUDE.md — SplitCard (Backend)

Project instructions for Claude Code when working in this repository. See `SplitCard.md` in this same repo for the full product context (domain, permissions, features).

## What this project is

SplitCard backend: register card purchases (credit/debit) at the moment they happen, automatically calculate installments and statement periods, and split the payment among household members (Owner / Contributor / RestrictedViewer).

## Stack

- .NET 9, C# 13 (primary constructors, collection expressions)
- MongoDB (Atlas free tier) via `MongoDB.Driver`, strongly typed
- JWT for authentication
- Deployed on Railway, Dockerfile at the repo root
- QuestPDF for PDF generation (reconciliation) — pending, to be added when that feature is implemented

## Architecture — Clean Architecture, non-negotiable

```
src/
  Split.Card.Domain/          Entities, enums, value objects. ZERO external dependencies.
  Split.Card.Application/     Repository interfaces and (pending) Commands/Queries/Handlers. Depends only on Domain.
  Split.Card.Infrastructure/  MongoDB implementation: BSON documents, mappings, repositories, DI.
  Split.Card.Api/             Program.cs, controllers, JWT, health check.
```

Strict rules:
- **Never** put MongoDB-specific logic or attributes (`BsonObjectId`, `MongoDB.Driver` attributes) in `Domain` or `Application`. That belongs only in `Infrastructure`.
- `Infrastructure` repositories accept and return `Domain` entities, never leak Mongo `Document`s outside that layer.
- Input DTOs (Application/Api) are kept separate from Mongo Data Models (Infrastructure), mapped via extension methods (`ToDocument()` / `ToDomain()`), not AutoMapper or generic reflection.
- IDs: `Guid.NewGuid().ToString("N")` (`Split.Card.Domain.Common.IdGenerator`), never a Mongo `ObjectId` exposed outside `Infrastructure`.

## Code conventions

- C# 13 / .NET 9: primary constructors in repositories (`public sealed class MongoCardRepository(MongoDbContext context) : ICardRepository`), collection expressions (`= [];`), `sealed` by default on classes not designed for inheritance.
- `Nullable` enabled in every `.csproj`. Do not use `!` (null-forgiving) unless it is genuinely impossible to be null given an already-validated domain invariant.
- Domain entities: public constructor with validation (throws `DomainException`), private parameterless constructor only for Infrastructure reconstruction.
- Never use `// TODO` placeholders — anything missing is documented as an explicit pending item in the README/CLAUDE.md, not as a loose code comment.

## Infrastructure / Railway

- All configuration via `Environment.GetEnvironmentVariable`, never hardcoded or in `appsettings.json` (except `Logging`/`AllowedHosts`, which are not secrets).
- Required variables: `MONGODB_CONNECTION_STRING`, `MONGODB_DATABASE_NAME`, `JWT_SECRET`.
- The health check (`GET /health`) must verify a real Mongo connection (ping), not just that the process is alive — see `MongoDbContext.PingAsync`.
- `MongoClientSettings` with `ServerSelectionTimeout`, `ConnectTimeout`, `RetryWrites`/`RetryReads` configured — do not use `new MongoClient(connectionString)` directly without those timeouts.
- Multi-stage Dockerfile (SDK for build, ASP.NET for runtime) — lives at the repo root, not inside `SplitCard.Api/`, because the build context needs all 4 projects.

## Domain model (summary — see SplitCard.md for full detail)

- `Household`, `User` (with `UserRole`: Owner/Contributor/RestrictedViewer), `Card` (Credit/Debit), `InstallmentPlan`, `Transaction`, `StatementPeriod`, `SplitRule`, `PersonShare` (value object, `PersonId` + `Percentage`, no `FixedAmount`).
- Installments: **never** generate a new record per month. `InstallmentPlan.GetInstallmentNumberFor()` calculates it on-the-fly from `FirstChargeDate`.
- Authorization: filtered by `Split[].PersonId contains userId` at the Mongo query level (`ElemMatch`), never by fetching everything and filtering in memory in the application layer.
- `Card.GetStatementPeriodFor()`: a heuristic for computing the statement period, **not validated against real card cutoff dates** — any change there requires unit tests covering edge cases (end of month, cutoff day 31 in February, etc.) before merging.

## Commands

```bash
dotnet restore
dotnet build
dotnet run --project src/SplitCard.Api
dotnet test              # once the test project exists
```

## Pending (do not assume it exists)

- Real Application layer: Commands/Queries/Handlers — today only repository interfaces exist in `Abstractions/`.
- API Controllers — today only `/health` exists.
- Login/JWT issuance endpoint (`/api/auth/login`, `/api/auth/me`).
- `JsonStringEnumConverter` — not registered yet. Decide whether enums are serialized as string or number before building the Controllers (this directly affects the TypeScript types in the mobile repo).
- Reconciliation PDF generation (QuestPDF) — package not added yet.
- Household members endpoint (`GET /api/households/{id}/members`) — blocking for the split picker on mobile.

## Interaction rules for Claude Code in this repo

- Explanations and technical reasoning: Spanish. Code names, variables, components: English.
- Prefix technical statements with `[Seguro]` / `[Probable]` / `[Suponiendo]`. If `[Suponiendo]`, stop and list what's missing before writing code.
- If there is a genuine architectural disagreement, state it directly in the first line, no hedging or unnecessary validation.
