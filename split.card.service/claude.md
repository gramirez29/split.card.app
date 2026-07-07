# CLAUDE.md — split.card.service (Backend)

Project instructions for Claude Code when working in this repository. Part of the `split.card.app` monorepo (sibling: `split.card.mobile/`). See `SplitCard.md` at the monorepo root or in this repo for the full product context (domain, permissions, features).

## What this project is

SplitCard backend: register card purchases (credit/debit) at the moment they happen, automatically calculate installments and statement periods, and split the payment among household members (Owner / Contributor / RestrictedViewer).

## Stack

- .NET 9, C# 13 (primary constructors, collection expressions)
- MongoDB (Atlas in production, local Docker container in dev) via `MongoDB.Driver`, strongly typed
- JWT for authentication (issuance + validation both implemented)
- Minimal API + Swashbuckle (Swagger UI at `/swagger`) — no attribute-routed Controllers
- Deployed on Railway, Dockerfile at the repo root
- xUnit for testing (`tests/Split.Card.Domain.Tests`, `tests/Split.Card.Application.Tests`)
- QuestPDF for PDF generation (reconciliation) — pending, not added yet

## Architecture — Clean Architecture, non-negotiable

```
src/
  Split.Card.Domain/          Entities, enums, value objects. ZERO external dependencies.
  Split.Card.Application/     Commands/Queries/Handlers + repository/service interfaces. Depends only on Domain.
  Split.Card.Infrastructure/  MongoDB implementation, password hashing, JWT issuance, DI.
  Split.Card.Api/             Program.cs, Minimal API endpoints, contracts, JWT validation, Swagger, health check.
tests/
  Split.Card.Domain.Tests/       xUnit. Covers Card.GetStatementPeriodFor() edge cases.
  Split.Card.Application.Tests/  xUnit. Hand-rolled in-memory fakes (Fakes/) — no Moq.
                                  Covers RegisterTransactionCommandHandler, LoginCommandHandler.
```

**Naming note**: project folders and `.csproj` files use dotted names (`Split.Card.Domain`), but every `RootNamespace` is set to the non-dotted form (`SplitCard.Domain`, `SplitCard.Application`, etc.) — C# namespaces stay `SplitCard.*` throughout the codebase. Don't "fix" this to match the folder name; it's intentional.

**Test projects are the exception**: `Split.Card.Domain.Tests` and `Split.Card.Application.Tests` do NOT override `RootNamespace`, so their C# namespace is the literal dotted folder name. The namespace segment `Card` collides with the bare type `Card` (`SplitCard.Domain.Entities.Card`) — the compiler resolves the bare identifier `Card` to the namespace segment, not the type (CS0118). **Inside any test project, always fully qualify `SplitCard.Domain.Entities.Card`** — never write bare `Card`. See `CardTests.cs` and `Fakes/InMemoryRepositories.cs` for the pattern. (`Split.Card.Api`'s own namespace is `SplitCard.Api`, no dots, so this doesn't apply there.)

**`Split.Card.Infrastructure.csproj` explicitly sets `<AssemblyName>SplitCard.Infrastructure</AssemblyName>`.** Required so `InternalsVisibleTo Include="SplitCard.Infrastructure"` in `Split.Card.Domain.csproj` keeps working (used for `StatementPeriod.RestoreStatus()`). If you rename any project, verify this still matches.

**`Split.Card.Api.csproj` only has a `ProjectReference` to `Split.Card.Infrastructure`** — Domain/Application come in transitively. Don't add redundant direct references.

**Application self-registers its own DI**: `SplitCard.Application.DependencyInjection.AddSplitCardApplication()` registers every Command/Query handler as `Scoped`. `Program.cs` calls both `AddSplitCardInfrastructure()` and `AddSplitCardApplication()`.

Strict rules:
- **Never** put MongoDB-specific logic or attributes (`BsonObjectId`, `MongoDB.Driver` attributes) in `Domain` or `Application`. That belongs only in `Infrastructure`.
- `Infrastructure` repositories accept and return `Domain` entities, never leak Mongo `Document`s outside that layer.
- Input DTOs are kept separate at every boundary: Api `Contracts/` ≠ Application Commands ≠ Infrastructure `Document`s ≠ Domain entities. Each layer maps explicitly (`FromDomain()`, `ToDomain()`, `ToDocument()`) — never serialize a Domain entity or a Mongo Document directly as an HTTP response.
- IDs: `Guid.NewGuid().ToString("N")` (`SplitCard.Domain.Common.IdGenerator`), never a Mongo `ObjectId` exposed outside `Infrastructure`.

## Auth

- `POST /api/auth/login` (anonymous) — `LoginCommand(Email, Password)` → `LoginResult(Token, UserId)`. Same error message ("Invalid email or password") whether the email doesn't exist or the password is wrong — don't change this to distinguish the two, it enables account enumeration.
- `GET /api/auth/me` (`[RequireAuthorization]`) — returns the authenticated user's profile via `GetCurrentUserQuery`.
- `ITokenGenerator` (Application abstraction) → `JwtTokenGenerator` (Infrastructure), HS256, symmetric key from `JWT_SECRET`. Claims issued: `sub` (user id, `JwtRegisteredClaimNames.Sub`), `email`, `householdId` (custom), `ClaimTypes.Role`. Default expiration 7 days (`JWT_EXPIRATION_MINUTES` env var overrides it, optional).
- **`options.MapInboundClaims = false` is set in `Program.cs`'s `JwtBearerOptions` — do not remove it.** Without it, ASP.NET Core silently remaps short claim names ("sub") to long `ClaimTypes` URIs on the validation side, and `ClaimsPrincipalExtensions.GetUserId()` (`Api/Auth/`) — which reads the literal `"sub"` claim — starts returning null. If you ever need to change this, update `GetUserId()` in the same change.
- Every write endpoint (and the two `actingUserId`-dependent read endpoints) now pulls the identity from `ClaimsPrincipal` (`actingUser.GetUserId()`), never from the request body/query string. This was the gap flagged earlier — it's closed. Don't reintroduce a client-supplied `ActingUserId`/`actingUserId` field on any new endpoint.
- Passwords: `IPasswordHasher` (Application abstraction) → `Pbkdf2PasswordHasher` (Infrastructure), PBKDF2-SHA256 via `System.Security.Cryptography` only — no third-party hashing package.

## API layer — Minimal API + Swagger

No Controllers with attributes. One `Map*Endpoints(this IEndpointRouteBuilder app)` extension method per feature in `Api/Endpoints/`, called from `Program.cs`. Request/response contracts live in `Api/Contracts/<Feature>/`, with `FromDomain()`/`ToDomain()` mapping — e.g. `UserResponse` deliberately excludes `PasswordHash`.

- **Swagger UI at `/swagger` — enabled in every environment, including Railway**, not gated to Development. Deliberate choice to allow verifying the deployed API directly; revisit once real user data exists in production. The JWT bearer scheme is wired into the Swagger security definition — after calling `/api/auth/login`, paste the token into the "Authorize" button to call protected endpoints from the Swagger UI.
- `ConfigureHttpJsonOptions` registers `JsonStringEnumConverter` globally in `Program.cs` — enums serialize as strings (`"Credit"`, not `1`). `split.card.mobile`'s `src/types/enums.ts` already assumed this.
- Central exception handling via `GlobalExceptionHandler : IExceptionHandler` (`Api/ErrorHandling/`): `UnauthorizedException`→401, `NotFoundException`→404, `ForbiddenException`→403, `ApplicationValidationException`/`DomainException`→400, anything else→500 (logged, not leaked to the client). Endpoints never catch these themselves — let the handler throw.
- Full request/response JSON examples for every endpoint: `RequestSamples.md` at the repo root. Field-by-field data dictionary: `JsonGlossary.md`. Keep both in sync with `Api/Contracts/` whenever a contract shape changes — they drift silently otherwise.

Endpoints implemented: `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/households`, `POST /api/users/invite`, `POST /api/cards`, `GET /api/households/{householdId}/cards`, `POST /api/transactions`, `GET /api/households/{householdId}/transactions`, `GET /api/cards/{cardId}/transactions`, `POST /api/split-rules`, `GET /api/households/{householdId}/split-rules`, `GET /api/households/{householdId}/split-rules/suggest`. Everything requires auth except `POST /api/auth/login` and `POST /api/households` (household bootstrap/signup).

## Application layer — CQRS without a mediator

No MediatR. Each Command/Query is a concrete `sealed class` with a single `Handle(...)` method, injected directly via primary-constructor DI. Command/Result records and their Handler live in the same file, one file per use case, grouped by feature folder (`Auth/`, `Households/`, `Users/`, `Cards/`, `Transactions/`, `SplitRules/`). Don't introduce MediatR incidentally while adding one more handler.

Exceptions used by handlers (`Application/Common/`): `UnauthorizedException` (→401, login failures), `NotFoundException` (→404), `ForbiddenException` (→403, role check failed), `ApplicationValidationException` (→400, application-level input problems: duplicate email, inviting a second Owner, installments on a Debit card) — distinct from `SplitCard.Domain.Exceptions.DomainException` (→400, entity invariant violations, e.g. split not summing to 100).

Authorization pattern used throughout: handlers load the acting `User` via `IUserRepository.GetByIdAsync(command.ActingUserId, ...)` and check `.Role`/`.CanWrite()`/`.CanReadAll()` server-side — never trust a role passed in directly. `ActingUserId` itself now always originates from the JWT (`ClaimsPrincipal`) at the Api layer, not from client-supplied data. Current rules: Owner-only for `CreateCard`, `InviteUser`, `CreateSplitRule`; Owner+Contributor (`CanWrite()`) for `RegisterTransaction`; visibility always filtered through `Split[].PersonId` for reads.

Implemented: **Auth** (`LoginCommand`, `GetCurrentUserQuery`), **Households** (`RegisterHouseholdCommand` — bootstraps Household + first Owner atomically), **Users** (`InviteUserCommand`), **Cards** (`CreateCardCommand`, `GetCardsForHouseholdQuery`), **Transactions** (`RegisterTransactionCommand`, `GetVisibleTransactionsQuery`, `GetTransactionsForCardPeriodQuery`), **SplitRules** (`CreateSplitRuleCommand`, `GetSplitRulesForHouseholdQuery`, `SuggestSplitForMerchantQuery`).

`RegisterTransactionCommandHandler` is the piece worth understanding before touching anything else in Transactions/:
1. Loads acting user, checks `CanWrite()`.
2. Loads the Card.
3. **Rejects with `ApplicationValidationException` if `card.Type == CardType.Debit && Installments is not null`** — a debit/cash purchase cannot be paid in installments. This check happens before any InstallmentPlan/Transaction is created (verified by `Handle_DebitCardWithInstallments_ThrowsApplicationValidationException`, which also asserts nothing was persisted).
4. If `Installments` is present (and the card is Credit, per the guard above), creates ONE `InstallmentPlan` (never a Transaction per month).
5. If the Card is Credit, ensures a `StatementPeriod` exists for `PurchaseDate` — reuses it if one already covers that date, creates it via `Card.GetStatementPeriodFor()` otherwise. Debit cards skip this entirely.
6. Constructs the `Transaction` (split-sum-to-100 validation happens inside the Domain constructor).

## Code conventions

- C# 13 / .NET 9: primary constructors in repositories, handlers, and endpoint delegates; collection expressions (`= [];`); `sealed` by default on classes not designed for inheritance.
- `Nullable` enabled in every `.csproj`. Do not use `!` (null-forgiving) unless it is genuinely impossible to be null given an already-validated domain invariant.
- Domain entities: public constructor with validation (throws `DomainException`), private parameterless constructor only for Infrastructure reconstruction.
- Never use `// TODO` placeholders — anything missing is documented as an explicit pending item in the README/CLAUDE.md, not as a loose code comment.
- New Domain logic with non-trivial branching needs xUnit tests in `tests/Split.Card.Domain.Tests`.
- New Application handlers with real branching need xUnit tests in `tests/Split.Card.Application.Tests` using the in-memory fakes in `Fakes/` — do not add Moq or another mocking library without discussing it first. `FakePasswordHasher`/`FakeTokenGenerator` in `Fakes/FakeAuthServices.cs` are NOT real crypto, test-only.

## Infrastructure / Railway

- All configuration via `Environment.GetEnvironmentVariable`, never hardcoded or in `appsettings.json` (except `Logging`/`AllowedHosts`, which are not secrets).
- Required variables: `MONGODB_CONNECTION_STRING`, `MONGODB_DATABASE_NAME`, `JWT_SECRET`. Optional: `JWT_EXPIRATION_MINUTES` (default 7 days).
- The health check (`GET /health`) must verify a real Mongo connection (ping), not just that the process is alive — see `MongoDbContext.PingAsync`.
- `MongoClientSettings` with `ServerSelectionTimeout`, `ConnectTimeout`, `RetryWrites`/`RetryReads` configured — do not use `new MongoClient(connectionString)` directly without those timeouts.
- Multi-stage Dockerfile — lives at this repo's root, not inside `src/Split.Card.Api/`, because the build context needs all 4 `src/` projects.
- CI: `.github/workflows/service-ci.yml` at the monorepo root runs `dotnet restore && dotnet build && dotnet test` on any push/PR touching `split.card.service/**`. Keep both test projects green or CI blocks the PR.
- **Local dev only**: `Program.cs` loads `src/Split.Card.Api/.env` (via `DotNetEnv`) into process env vars before anything reads them — see `.env.example` in that folder. No-op on Railway (no `.env` file in the deployed container; real env vars are injected by the platform). If `MONGODB_CONNECTION_STRING`/`JWT_SECRET` errors show up locally, the fix is creating that `.env` file from the example, not hardcoding values anywhere.
- **Local Mongo**: `docker-compose.yml` at the repo root runs a Mongo container dedicated to this project — port `27018` (not the default `27017`), deliberately, to avoid colliding with any other local Mongo container from an unrelated project. Don't repoint `MONGODB_CONNECTION_STRING` at another project's local Mongo instance/credentials, even if convenient — each repo owns its own local infra. `docker compose up -d` before `dotnet run`.

## Domain model (summary — see SplitCard.md for full detail)

- `Household`, `User` (with `UserRole`: Owner/Contributor/RestrictedViewer), `Card` (Credit/Debit), `InstallmentPlan`, `Transaction`, `StatementPeriod`, `SplitRule`, `PersonShare` (value object, `PersonId` + `Percentage`, no `FixedAmount`).
- Installments: **never** generate a new record per month. `InstallmentPlan.GetInstallmentNumberFor()` calculates it on-the-fly from `FirstChargeDate`. Installments only apply to Credit cards — enforced in `RegisterTransactionCommandHandler`, not in the `InstallmentPlan`/`Card` entities themselves (it's a cross-entity rule, doesn't belong to either one's own invariants).
- Authorization: filtered by `Split[].PersonId contains userId` at the Mongo query level (`ElemMatch`), never in memory in the application layer.
- `Card.GetStatementPeriodFor()`: statement period heuristic, covered by unit tests (`tests/Split.Card.Domain.Tests/CardTests.cs`). Still not validated against real bank data for your 4 cards — if a bug surfaces in production, add a regression test first, then fix.

## Commands

```bash
docker compose up -d
dotnet restore
dotnet build
dotnet run --project src/Split.Card.Api
dotnet test
```

Swagger UI once running: `http://localhost:{port}/swagger`.

## Pending (do not assume it exists)

- `GET /api/households/{id}/members` — no handler yet (closest existing pattern: `GetCardsForHouseholdQueryHandler`). Blocks the split picker on mobile.
- Reconciliation PDF generation (QuestPDF) — package not added yet.
- No refresh-token flow — JWT just expires after `JWT_EXPIRATION_MINUTES`, user re-logs in. Fine for now, revisit if 7-day sessions turn out to be too short/long in practice.

## Interaction rules for Claude Code in this repo

- Explanations and technical reasoning: Spanish. Code names, variables, components: English.
- Prefix technical statements with `[Seguro]` / `[Probable]` / `[Suponiendo]`. If `[Suponiendo]`, stop and list what's missing before writing code.
- If there is a genuine architectural disagreement, state it directly in the first line, no hedging or unnecessary validation.
