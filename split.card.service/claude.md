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
- QuestPDF for reconciliation PDF generation (`Community` license — free tier, revisit if that stops applying)

## Architecture — Clean Architecture, non-negotiable

```
src/
  Split.Card.Domain/          Entities, enums, value objects. ZERO external dependencies.
  Split.Card.Application/     Commands/Queries/Handlers + repository/service interfaces. Depends only on Domain.
  Split.Card.Infrastructure/  MongoDB implementation, password hashing, JWT issuance, PDF generation, DI.
  Split.Card.Api/             Program.cs, Minimal API endpoints, contracts, JWT validation, Swagger, health check.
tests/
  Split.Card.Domain.Tests/       xUnit. Covers Card.GetStatementPeriodFor() edge cases.
  Split.Card.Application.Tests/  xUnit. Hand-rolled in-memory fakes (Fakes/) — no Moq.
                                  Every handler that calls HouseholdAccessGuard has a
                                  same-household happy-path test + a cross-household
                                  ForbiddenException test. See "Auth" section below for
                                  the full list of test files.
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
- **Every household-scoped Command/Query must call `HouseholdAccessGuard.EnsureMember(actingUser, householdId)` right after loading the acting user** (or right after loading a Card/other entity that carries a `HouseholdId`, if the request is keyed by that entity's id instead), **and must have both a same-household success test and a cross-household `ForbiddenException` test** — this is now the established pattern across 10 handlers, not optional for new ones.

## Auth

- `POST /api/auth/login` (anonymous) — `LoginCommand(Email, Password)` → `LoginResult(Token, UserId)`. Same error message ("Invalid email or password") whether the email doesn't exist or the password is wrong — don't change this to distinguish the two, it enables account enumeration.
- `GET /api/auth/me` (`[RequireAuthorization]`) — returns the authenticated user's profile via `GetCurrentUserQuery`.
- `ITokenGenerator` (Application abstraction) → `JwtTokenGenerator` (Infrastructure), HS256, symmetric key from `JWT_SECRET`. Claims issued: `sub` (user id, `JwtRegisteredClaimNames.Sub`), `email`, `householdId` (custom), `ClaimTypes.Role`. Default expiration 7 days (`JWT_EXPIRATION_MINUTES` env var overrides it, optional).
- **`options.MapInboundClaims = false` is set in `Program.cs`'s `JwtBearerOptions` — do not remove it.** Without it, ASP.NET Core silently remaps short claim names ("sub") to long `ClaimTypes` URIs on the validation side, and `ClaimsPrincipalExtensions.GetUserId()` (`Api/Auth/`) — which reads the literal `"sub"` claim — starts returning null. If you ever need to change this, update `GetUserId()` in the same change.
- Every write endpoint (and every `actingUserId`-dependent read endpoint) pulls the identity from `ClaimsPrincipal` (`actingUser.GetUserId()`), never from the request body/query string. Don't reintroduce a client-supplied `ActingUserId`/`actingUserId` field on any new endpoint.
- Passwords: `IPasswordHasher` (Application abstraction) → `Pbkdf2PasswordHasher` (Infrastructure), PBKDF2-SHA256 via `System.Security.Cryptography` only — no third-party hashing package.
- **`HouseholdAccessGuard` (`Application/Common/HouseholdAccessGuard.cs`)**: authenticating a request only proves *who* someone is, not *which household's data they're allowed to touch* — `HouseholdId` (or a `CardId` that resolves to one) is client-supplied in every request, so without this guard any authenticated user could read or write another household's data just by passing its id. This was a real, exploitable gap found originally in 9 handlers (a 10th, `GenerateStatementPdfQuery`, was built with it from day one). Every household-scoped handler calls `HouseholdAccessGuard.EnsureMember(actingUser, householdId)` before doing anything else — `ForbiddenException` ("You do not have access to this household.") if it doesn't match.

  Full regression test coverage, one file per handler in `tests/Split.Card.Application.Tests/`: `CreateCardCommandHandlerTests`, `GetCardsForHouseholdQueryHandlerTests`, `InviteUserCommandHandlerTests`, `GetHouseholdMembersQueryHandlerTests`, `CreateSplitRuleCommandHandlerTests`, `GetSplitRulesForHouseholdQueryHandlerTests`, `RegisterTransactionCommandHandlerTests` (`Handle_CardBelongsToDifferentHousehold_ThrowsForbidden`), `GetVisibleTransactionsQueryHandlerTests`, `GetTransactionsForCardPeriodQueryHandlerTests`, `GenerateStatementPdfQueryHandlerTests`. `Fakes/InMemoryRepositories.cs` has `InMemoryHouseholdRepository`/`InMemorySplitRuleRepository` to support these. `SuggestSplitForMerchantQueryHandler` also calls the guard but doesn't have a dedicated test yet — lowest risk of the set (read-only, no financial data).

## API layer — Minimal API + Swagger

No Controllers with attributes. One `Map*Endpoints(this IEndpointRouteBuilder app)` extension method per feature in `Api/Endpoints/`, called from `Program.cs`. Request/response contracts live in `Api/Contracts/<Feature>/`, with `FromDomain()`/`ToDomain()` mapping — e.g. `UserResponse` deliberately excludes `PasswordHash`.

- **Swagger UI at `/swagger` — enabled in every environment, including Railway**, not gated to Development. Deliberate choice to allow verifying the deployed API directly; revisit once real user data exists in production. The JWT bearer scheme is wired into the Swagger security definition — after calling `/api/auth/login`, paste the token into the "Authorize" button to call protected endpoints from the Swagger UI.
- `ConfigureHttpJsonOptions` registers `JsonStringEnumConverter` globally in `Program.cs` — enums serialize as strings (`"Credit"`, not `1`). `split.card.mobile`'s `src/types/enums.ts` already assumed this.
- Central exception handling via `GlobalExceptionHandler : IExceptionHandler` (`Api/ErrorHandling/`): `UnauthorizedException`→401, `NotFoundException`→404, `ForbiddenException`→403, `ApplicationValidationException`/`DomainException`→400, anything else→500 (logged, not leaked to the client). Endpoints never catch these themselves — let the handler throw.
- Full request/response JSON examples for every endpoint: `RequestSamples.md` at the repo root. Field-by-field data dictionary: `JsonGlossary.md`. Keep both in sync with `Api/Contracts/` whenever a contract shape changes — they drift silently otherwise.
- Every `GET /api/households/{householdId}/...` and `GET /api/cards/{cardId}/...` endpoint takes a `ClaimsPrincipal` parameter and passes `actingUser.GetUserId()` into its query, specifically so `HouseholdAccessGuard` can run. If you add a new household-scoped endpoint, follow this same wiring — it's not optional.

Endpoints implemented: `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/households`, `POST /api/users/invite`, `GET /api/households/{householdId}/members`, `POST /api/cards`, `GET /api/households/{householdId}/cards`, `POST /api/transactions`, `GET /api/households/{householdId}/transactions`, `GET /api/cards/{cardId}/transactions`, `GET /api/cards/{cardId}/statement-pdf`, `POST /api/split-rules`, `GET /api/households/{householdId}/split-rules`, `GET /api/households/{householdId}/split-rules/suggest`. Everything requires auth except `POST /api/auth/login` and `POST /api/households` (household bootstrap/signup).

## Reconciliation (PDF)

`GET /api/cards/{cardId}/statement-pdf?date=yyyy-MM-dd` — generates a PDF for the statement period containing `date`, meant to be checked against the real bank statement (product feature 7 in `SplitCard.md`: "conciliación contra estado de cuenta real").

- `IStatementPdfGenerator` (Application abstraction, `Abstractions/IStatementPdfGenerator.cs`, alongside the `StatementPdfModel` record it consumes) → `QuestPdfStatementGenerator` (Infrastructure, `Documents/QuestPdfStatementGenerator.cs`). Same pattern as `IPasswordHasher`/`ITokenGenerator`/`ITokenGenerator` — Application defines the contract, Infrastructure implements it with the actual third-party library.
- `GenerateStatementPdfQueryHandler` (`Application/Reconciliation/GenerateStatementPdf.cs`) duplicates the exact guard + visibility logic of `GetTransactionsForCardPeriodQueryHandler` (household check via the loaded Card, then `Split[].PersonId` filtering for non-Owners) before building the PDF model — if you change one, check whether the other needs the same change.
- The PDF resolves `PersonId → Name` for every household member (via `IUserRepository.GetByHouseholdIdAsync`) so it shows names, not raw ids — a reconciliation document a person actually reads needs to be legible.
- `QuestPDF.Settings.License = LicenseType.Community` is set once in a static constructor on `QuestPdfStatementGenerator` — this only covers the Community license tier (free under QuestPDF's own revenue threshold); if that stops applying, this is the one line to change (to a purchased license key) plus wherever that key gets loaded from (should be an env var, not hardcoded, consistent with everything else in this project).
- **`[Probable]`, not `[Seguro]`**: the exact QuestPDF NuGet version pinned in `Split.Card.Infrastructure.csproj` (`2025.1.3`) and the fluent API calls in `QuestPdfStatementGenerator` (`Document.Create`, `table.Cell().Text(...)`, etc.) are based on the QuestPDF API as of this assistant's training data, not verified against a live build — QuestPDF may have shifted its fluent API surface since. If `dotnet build` fails inside this file specifically, check QuestPDF's current docs for the exact method signatures before assuming the logic is wrong.
- Amount formatting is currency-aware but manual (`₡` for CRC with 0 decimals, `$` for USD with 2) — not using `System.Globalization` culture-based formatting, since neither `es-CR` nor `en-US` `NumberFormatInfo` reliably matches "₡45,000" / "$120.00" exactly the way a Costa Rican bank statement would show it. If this needs to match a specific bank's exact formatting, that's a follow-up, not assumed here.

## Application layer — CQRS without a mediator

No MediatR. Each Command/Query is a concrete `sealed class` with a single `Handle(...)` method, injected directly via primary-constructor DI. Command/Result records and their Handler live in the same file, one file per use case, grouped by feature folder (`Auth/`, `Households/`, `Users/`, `Cards/`, `Transactions/`, `SplitRules/`, `Reconciliation/`). Don't introduce MediatR incidentally while adding one more handler.

Exceptions used by handlers (`Application/Common/`): `UnauthorizedException` (→401, login failures), `NotFoundException` (→404), `ForbiddenException` (→403, role check failed **or** `HouseholdAccessGuard` failed), `ApplicationValidationException` (→400, application-level input problems: duplicate email, inviting a second Owner, installments on a Debit card) — distinct from `SplitCard.Domain.Exceptions.DomainException` (→400, entity invariant violations, e.g. split not summing to 100).

Authorization pattern used throughout: handlers load the acting `User` via `IUserRepository.GetByIdAsync(command.ActingUserId, ...)`, call `HouseholdAccessGuard.EnsureMember(actingUser, householdId)`, then check `.Role`/`.CanWrite()`/`.CanReadAll()` server-side as needed — never trust a role passed in directly, and never skip the household check even for Owner. `ActingUserId` itself always originates from the JWT (`ClaimsPrincipal`) at the Api layer, not from client-supplied data. Current role rules (on top of the household check, which applies to all of these): Owner-only for `CreateCard`, `InviteUser`, `CreateSplitRule`; Owner+Contributor (`CanWrite()`) for `RegisterTransaction`; visibility filtered through `Split[].PersonId` for reads once household membership is confirmed.

Implemented: **Auth** (`LoginCommand`, `GetCurrentUserQuery`), **Households** (`RegisterHouseholdCommand` — bootstraps Household + first Owner atomically, no guard applicable since there's no existing household yet), **Users** (`InviteUserCommand`, `GetHouseholdMembersQuery`), **Cards** (`CreateCardCommand`, `GetCardsForHouseholdQuery`), **Transactions** (`RegisterTransactionCommand`, `GetVisibleTransactionsQuery`, `GetTransactionsForCardPeriodQuery`), **SplitRules** (`CreateSplitRuleCommand`, `GetSplitRulesForHouseholdQuery`, `SuggestSplitForMerchantQuery`), **Reconciliation** (`GenerateStatementPdfQuery`). All of these except `RegisterHouseholdCommand` call `HouseholdAccessGuard`.

`RegisterTransactionCommandHandler` is the piece worth understanding before touching anything else in Transactions/:
1. Loads acting user, checks `CanWrite()`.
2. Loads the Card, then `HouseholdAccessGuard.EnsureMember(actingUser, card.HouseholdId)` — this query is keyed by `CardId`, not `HouseholdId`, so the guard needs the Card loaded first.
3. Rejects with `ApplicationValidationException` if `card.Type == CardType.Debit && Installments is not null` — a debit/cash purchase cannot be paid in installments. Verified by `Handle_DebitCardWithInstallments_ThrowsApplicationValidationException`.
4. If `Installments` is present (and the card is Credit, per the guard above), creates ONE `InstallmentPlan` (never a Transaction per month).
5. If the Card is Credit, ensures a `StatementPeriod` exists for `PurchaseDate` — reuses it if one already covers that date, creates it via `Card.GetStatementPeriodFor()` otherwise. Debit cards skip this entirely.
6. Constructs the `Transaction` (split-sum-to-100 validation happens inside the Domain constructor).

`GetTransactionsForCardPeriodQueryHandler` and `GenerateStatementPdfQueryHandler` both follow the same "load Card first for the guard" pattern, and share the same visibility-filtering logic (Owner sees all, others only `Split[].PersonId` matches) — currently duplicated in two places, not extracted into a shared helper. Worth revisiting if a third handler needs the same logic.

## Code conventions

- C# 13 / .NET 9: primary constructors in repositories, handlers, and endpoint delegates; collection expressions (`= [];`); `sealed` by default on classes not designed for inheritance.
- `Nullable` enabled in every `.csproj`. Do not use `!` (null-forgiving) unless it is genuinely impossible to be null given an already-validated domain invariant.
- Domain entities: public constructor with validation (throws `DomainException`), private parameterless constructor only for Infrastructure reconstruction.
- Never use `// TODO` placeholders — anything missing is documented as an explicit pending item in the README/CLAUDE.md, not as a loose code comment.
- New Domain logic with non-trivial branching needs xUnit tests in `tests/Split.Card.Domain.Tests`.
- New Application handlers with real branching need xUnit tests in `tests/Split.Card.Application.Tests` using the in-memory fakes in `Fakes/` — do not add Moq or another mocking library without discussing it first. `FakePasswordHasher`/`FakeTokenGenerator` in `Fakes/FakeAuthServices.cs` are NOT real crypto, test-only. Any handler that calls `HouseholdAccessGuard` has real branching by definition and needs both a same-household and a cross-household test — see the full list in the "Auth" section above.

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
- Authorization: filtered by `Split[].PersonId contains userId` at the Mongo query level (`ElemMatch`) for who-sees-what-within-a-household, and by `HouseholdAccessGuard` (Application layer, in-memory check on the loaded `User`) for which-household-at-all. Two different, complementary checks — don't conflate them or assume one covers the other.
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

- No refresh-token flow — JWT just expires after `JWT_EXPIRATION_MINUTES`, user re-logs in. Fine for now, revisit if 7-day sessions turn out to be too short/long in practice.
- `SuggestSplitForMerchantQueryHandler` calls `HouseholdAccessGuard` but has no dedicated regression test yet (see "Auth" section) — lowest priority of the set, but still open.
- QuestPDF's exact fluent API in `QuestPdfStatementGenerator` is unverified against a live build (see "Reconciliation" section) — first thing to check if this specific file fails to compile.
- The Owner-vs-visible-only transaction filtering logic is duplicated between `GetTransactionsForCardPeriodQueryHandler` and `GenerateStatementPdfQueryHandler` — candidate for extraction into a shared helper if a third consumer shows up.

## Interaction rules for Claude Code in this repo

- Explanations and technical reasoning: Spanish. Code names, variables, components: English.
- Prefix technical statements with `[Seguro]` / `[Probable]` / `[Suponiendo]`. If `[Suponiendo]`, stop and list what's missing before writing code.
- If there is a genuine architectural disagreement, state it directly in the first line, no hedging or unnecessary validation.
