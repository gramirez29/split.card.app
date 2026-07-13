# CLAUDE.md — split.card.service (Backend)

Project instructions for Claude Code when working in this repository. Part of the `split.card.app` monorepo (sibling: `split.card.mobile/`). See `SplitCard.md` at the monorepo root or in this repo for the full product context (domain, permissions, features). See also `/PUSH_NOTIFICATIONS_PLAN.md` at the monorepo root for the full plan to go from today's in-app-only alerts to real push notifications, and `/DEPLOYMENT.md` for the Railway + EAS deployment checklist.

## What this project is

SplitCard backend: register card purchases (credit/debit) at the moment they happen, automatically calculate installments and statement periods, and split the payment among household members (Owner / Contributor / RestrictedViewer).

## Stack

- .NET 9, C# 13 (primary constructors, collection expressions)
- MongoDB (Atlas in production, local Docker container in dev) via `MongoDB.Driver`, strongly typed
- JWT for authentication (issuance + validation both implemented)
- Minimal API + Swashbuckle (Swagger UI at `/swagger`) — no attribute-routed Controllers
- Deployed on Railway, Dockerfile at the repo root
- xUnit for testing (`tests/Split.Card.Domain.Tests`, `tests/Split.Card.Application.Tests`)
- QuestPDF for reconciliation PDF generation (`Community` license — free tier, revisit if that stops applying). Confirmed working via `dotnet build`/`dotnet test`.

## IMPORTANT — installment amounts: a real bug, now fixed, don't reintroduce it

`Transaction.Amount` is **always the full original purchase total** (e.g. ₡12,000 for a 6-installment purchase) and **never changes**. For a long time, every place that computed "what's owed this period" (`GetTransactionsForCardPeriodQueryHandler`, `GenerateStatementPdfQueryHandler`, `GetHouseholdDashboardQueryHandler`) used `transaction.Amount` directly and filtered transactions by `PurchaseDate BETWEEN periodStart AND periodEnd` (`ITransactionRepository.GetByCardAndDateRangeAsync`). This is wrong for installment purchases in two ways at once:

1. **Wrong amount**: the full ₡12,000 showed up, not the ₡2,000/month installment.
2. **Wrong periods**: since `PurchaseDate` never changes, the transaction only ever matched the date-range filter for its FIRST period — months 2, 3, ... showed nothing at all for that purchase, even though the installment was still active.

Fixed with two new pieces that every period-aware handler must go through — **never query `ITransactionRepository` by date range directly for "what's owed this period" math again**:

- `Transaction.GetPeriodAmount(InstallmentPlan? plan)` (Domain) — resolves the correct amount for a period: `plan.InstallmentAmount` if a plan is given, `Amount` otherwise.
- `Transaction.GetShareAmount(string personId, decimal amountForPeriod)` (Domain) — replaces the old `GetAmountFor(personId)` (removed). Takes the amount explicitly so callers can't accidentally pass the wrong one.
- `SplitCard.Application.Transactions.PeriodTransactionResolver.Resolve(...)` — the actual fix. Fetches ALL of a card's transactions (`ITransactionRepository.GetByCardIdAsync`, new method, ignores `PurchaseDate` entirely) and, for each one with an `InstallmentPlanId`, checks `InstallmentPlan.HasActiveInstallmentIn(periodStart, periodEnd)` instead of comparing dates on the `Transaction` itself. Returns `ResolvedPeriodTransaction { Transaction, PeriodAmount, InstallmentNumber, TotalInstallments }`.

`GetTransactionsForCardPeriodQueryHandler`, `GenerateStatementPdfQueryHandler`, `GetHouseholdDashboardQueryHandler`, and `GetHouseholdAlertsQueryHandler` all go through `PeriodTransactionResolver` and use `.PeriodAmount`, never `.Transaction.Amount`, for any sum or displayed amount. `Api/Contracts/Transactions/PeriodTransactionResponse` is the wire contract for the first one — deliberately a different shape from `TransactionResponse` (which still shows the full `amount`, correctly, since `POST /api/transactions` and `GET /api/households/{id}/transactions` aren't period-scoped).

Regression tests proving the exact reported scenario (₡12,000 / 3 installments, showing ₡4,000 in each of 3 consecutive periods, then nothing in the 4th): `GetTransactionsForCardPeriodQueryHandlerTests.Handle_InstallmentPurchase_ShowsInstallmentAmountNotFullTotal_AcrossThreeConsecutivePeriods`, plus installment-specific tests in `GenerateStatementPdfQueryHandlerTests`, `GetHouseholdDashboardQueryHandlerTests`, and `GetHouseholdAlertsQueryHandlerTests`, plus Domain-level `Split.Card.Domain.Tests/TransactionTests.cs` for `GetPeriodAmount`/`GetShareAmount` directly.

## IClock — date-sensitive Application code should use this, not DateTime.UtcNow directly

`Application/Abstractions/IClock.cs` (`DateOnly Today { get; }`) → `Infrastructure/Time/SystemClock.cs`. Introduced for `GetHouseholdAlertsQueryHandler`, whose whole job is "is this date within N days of today" — without a clock abstraction, testing that deterministically means fighting with whatever day it happens to be when `dotnet test` runs. Test fake: `Fakes/FakeClock.cs` (`new FakeClock(new DateOnly(2026, 3, 10))`), gives every alert test a fixed, known "today".

**`GetHouseholdDashboardQueryHandler` was NOT retrofitted to use `IClock`** — it still calls `DateOnly.FromDateTime(DateTime.UtcNow)` directly, predating this abstraction. Not urgent (its tests don't need day-precision, just "does a period exist for some date near today"), but if you're touching that handler anyway, consider migrating it for consistency.

## Architecture — Clean Architecture, non-negotiable

```
src/
  Split.Card.Domain/          Entities, enums, value objects. ZERO external dependencies.
  Split.Card.Application/     Commands/Queries/Handlers + repository/service interfaces. Depends only on Domain.
  Split.Card.Infrastructure/  MongoDB implementation, password hashing, JWT issuance, PDF generation, clock, DI.
  Split.Card.Api/             Program.cs, Minimal API endpoints, contracts, JWT validation, Swagger, health check.
tests/
  Split.Card.Domain.Tests/       xUnit. Card.GetStatementPeriodFor() edge cases, Transaction
                                  GetPeriodAmount/GetShareAmount.
  Split.Card.Application.Tests/  xUnit. Hand-rolled in-memory fakes (Fakes/) — no Moq.
                                  Every handler that calls HouseholdAccessGuard has a
                                  same-household happy-path test + a cross-household
                                  ForbiddenException test. See "Auth" section below for
                                  the full list of test files.
```

**Naming note**: project folders and `.csproj` files use dotted names (`Split.Card.Domain`), but every `RootNamespace` is set to the non-dotted form (`SplitCard.Domain`, `SplitCard.Application`, etc.) — C# namespaces stay `SplitCard.*` throughout the codebase. Don't "fix" this to match the folder name; it's intentional.

**Test projects are the exception**: `Split.Card.Domain.Tests` and `Split.Card.Application.Tests` do NOT override `RootNamespace`, so their C# namespace is the literal dotted folder name. The namespace segment `Card` collides with the bare type `Card` (`SplitCard.Domain.Entities.Card`) — the compiler resolves the bare identifier `Card` to the namespace segment, not the type (CS0118). **Inside any test project, always fully qualify `SplitCard.Domain.Entities.Card`** — never write bare `Card`. See `CardTests.cs`, `TransactionTests.cs`, and `Fakes/InMemoryRepositories.cs` for the pattern.

**`Split.Card.Infrastructure.csproj` explicitly sets `<AssemblyName>SplitCard.Infrastructure</AssemblyName>`.** Required so `InternalsVisibleTo Include="SplitCard.Infrastructure"` in `Split.Card.Domain.csproj` keeps working (used for `StatementPeriod.RestoreStatus()`). If you rename any project, verify this still matches.

**`Split.Card.Api.csproj` only has a `ProjectReference` to `Split.Card.Infrastructure`** — Domain/Application come in transitively. Don't add redundant direct references.

**Application self-registers its own DI**: `SplitCard.Application.DependencyInjection.AddSplitCardApplication()` registers every Command/Query handler as `Scoped`. `Program.cs` calls both `AddSplitCardInfrastructure()` and `AddSplitCardApplication()`.

**Watch for named-tuple field-name loss.** `Dictionary<string, (decimal Crc, decimal Usd)>.GetValueOrDefault(key, (0m, 0m))` — the fallback literal `(0m, 0m)` has no field names, and in some inference paths (conditional expressions combining two unnamed-arithmetic tuple literals, in particular) the compiler ends up with an unnamed `(decimal, decimal)` instead of the declared named type, causing `'(decimal, decimal)' does not contain a definition for 'Crc'` at every `.Crc`/`.Usd` access. Always write the fallback with explicit names — `(Crc: 0m, Usd: 0m)` — when the dictionary's value type is a named tuple. This bit `GetHouseholdDashboardQueryHandler` once already.

Strict rules:
- **Never** put MongoDB-specific logic or attributes (`BsonObjectId`, `MongoDB.Driver` attributes) in `Domain` or `Application`. That belongs only in `Infrastructure`.
- `Infrastructure` repositories accept and return `Domain` entities, never leak Mongo `Document`s outside that layer.
- Input DTOs are kept separate at every boundary: Api `Contracts/` ≠ Application Commands ≠ Infrastructure `Document`s ≠ Domain entities. Each layer maps explicitly (`FromDomain()`, `ToDomain()`, `ToDocument()`) — never serialize a Domain entity or a Mongo Document directly as an HTTP response.
- IDs: `Guid.NewGuid().ToString("N")` (`SplitCard.Domain.Common.IdGenerator`), never a Mongo `ObjectId` exposed outside `Infrastructure`.
- **Every household-scoped Command/Query must call `HouseholdAccessGuard.EnsureMember(actingUser, householdId)` right after loading the acting user** (or right after loading a Card/other entity that carries a `HouseholdId`, if the request is keyed by that entity's id instead), **and must have both a same-household success test and a cross-household `ForbiddenException` test** — established pattern across 12 handlers, not optional for new ones.
- **Any handler computing "what's owed this period" must use `PeriodTransactionResolver`, never a raw `PurchaseDate`-range query** — see the section above.
- **Any new date-sensitive Application logic should take `IClock`, not call `DateTime.UtcNow` directly** — see the IClock section above.

## Auth

- `POST /api/auth/login` (anonymous) — `LoginCommand(Email, Password)` → `LoginResult(Token, UserId)`. Same error message ("Invalid email or password") whether the email doesn't exist or the password is wrong — don't change this, it enables account enumeration.
- `GET /api/auth/me` (`[RequireAuthorization]`) — returns the authenticated user's profile via `GetCurrentUserQuery`.
- `ITokenGenerator` (Application abstraction) → `JwtTokenGenerator` (Infrastructure), HS256, symmetric key from `JWT_SECRET`. Claims: `sub`, `email`, `householdId` (custom), `ClaimTypes.Role`. Default expiration 7 days (`JWT_EXPIRATION_MINUTES` optional).
- **`options.MapInboundClaims = false` in `Program.cs`'s `JwtBearerOptions` — do not remove it.** Without it, ASP.NET Core remaps `"sub"` to a long `ClaimTypes` URI and `ClaimsPrincipalExtensions.GetUserId()` silently returns null.
- Every write endpoint (and every `actingUserId`-dependent read endpoint) pulls identity from `ClaimsPrincipal`, never from the request body/query string.
- Passwords: `IPasswordHasher` (Application abstraction) → `Pbkdf2PasswordHasher` (Infrastructure), PBKDF2-SHA256, no third-party package.
- **`HouseholdAccessGuard` (`Application/Common/HouseholdAccessGuard.cs`)**: `HouseholdId`/`CardId` are client-supplied, so without this guard any authenticated user could touch another household's data by passing its id. Every household-scoped handler calls `HouseholdAccessGuard.EnsureMember(actingUser, householdId)` first — `ForbiddenException` if it doesn't match.

  Regression tests, one file per handler in `tests/Split.Card.Application.Tests/`: `CreateCardCommandHandlerTests`, `GetCardsForHouseholdQueryHandlerTests`, `InviteUserCommandHandlerTests`, `GetHouseholdMembersQueryHandlerTests`, `CreateSplitRuleCommandHandlerTests`, `GetSplitRulesForHouseholdQueryHandlerTests`, `RegisterTransactionCommandHandlerTests`, `GetVisibleTransactionsQueryHandlerTests`, `GetTransactionsForCardPeriodQueryHandlerTests`, `GenerateStatementPdfQueryHandlerTests`, `GetHouseholdDashboardQueryHandlerTests`, `GetHouseholdAlertsQueryHandlerTests`. `SuggestSplitForMerchantQueryHandler` also calls the guard but has no dedicated test yet — lowest risk (read-only, no financial data).

## API layer — Minimal API + Swagger

No Controllers with attributes. One `Map*Endpoints(this IEndpointRouteBuilder app)` extension method per feature in `Api/Endpoints/`, called from `Program.cs`. Request/response contracts live in `Api/Contracts/<Feature>/`.

- **Swagger UI at `/swagger` — enabled in every environment, including Railway**. JWT bearer scheme wired into the Swagger security definition.
- `ConfigureHttpJsonOptions` registers `JsonStringEnumConverter` globally — enums serialize as strings.
- Central exception handling via `GlobalExceptionHandler : IExceptionHandler`: `UnauthorizedException`→401, `NotFoundException`→404, `ForbiddenException`→403, `ApplicationValidationException`/`DomainException`→400, anything else→500.
- Full request/response JSON examples: `RequestSamples.md`. Field-by-field dictionary: `JsonGlossary.md`. Keep in sync with `Api/Contracts/`.
- Every household/card-scoped `GET` takes a `ClaimsPrincipal` and passes `actingUser.GetUserId()` into its query, for `HouseholdAccessGuard`.
- **`TransactionResponse` vs `PeriodTransactionResponse` are different contracts, not the same shape with extra fields** — see the installment-amount section above. `GET /api/cards/{cardId}/transactions` uses the latter (`amount` + `periodAmount` + `installmentNumber`/`totalInstallments`); everything else uses the former (`amount` only, the full total).

Endpoints implemented: `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/households`, `POST /api/users/invite`, `GET /api/households/{householdId}/members`, `POST /api/cards`, `GET /api/households/{householdId}/cards`, `POST /api/transactions`, `GET /api/households/{householdId}/transactions`, `GET /api/cards/{cardId}/transactions`, `GET /api/cards/{cardId}/statement-pdf`, `POST /api/split-rules`, `GET /api/households/{householdId}/split-rules`, `GET /api/households/{householdId}/split-rules/suggest`, `GET /api/households/{householdId}/dashboard`, `GET /api/households/{householdId}/alerts`. Everything requires auth except `POST /api/auth/login` and `POST /api/households`.

## Reconciliation (PDF)

`GET /api/cards/{cardId}/statement-pdf?date=yyyy-MM-dd` — PDF for the statement period containing `date` (product feature 7 in `SplitCard.md`).

- `IStatementPdfGenerator` (Application, `Abstractions/IStatementPdfGenerator.cs` — `StatementPdfModel` now carries `IReadOnlyList<ResolvedPeriodTransaction>`, not raw `Transaction`s) → `QuestPdfStatementGenerator` (Infrastructure).
- `GenerateStatementPdfQueryHandler` uses `PeriodTransactionResolver` — the PDF's amount column and footer total both use `PeriodAmount`, and now show an "Installment" column (`"2/3"` or `"—"`).
- Resolves `PersonId → Name` for every household member so the PDF shows names.
- `QuestPDF.Settings.License = LicenseType.Community` — free tier; revisit if that stops applying.
- Amount formatting is manual per-currency (`₡`/`$`), not culture-based `NumberFormatInfo`.

## Reporting (dashboard)

`GET /api/households/{householdId}/dashboard` — product feature 5 in `SplitCard.md`: consolidated totals owed right now, by person and by card.

- `GetHouseholdDashboardQueryHandler` scope: **Credit cards' current open StatementPeriod only** — Debit excluded (already-settled cash, not a pending balance). A Credit card with no purchases yet this period still appears with zero totals.
- Uses `PeriodTransactionResolver` + `Transaction.GetShareAmount(personId, item.PeriodAmount)` — installment purchases contribute their per-period installment amount, not the full total.
- `byPerson` built only from transactions the caller can already see (`IsVisibleTo`).
- No currency conversion — `grandTotalCrc`/`grandTotalUsd` always separate; combining with a reference rate is client-side-only in mobile.
- `Api/Contracts/Reporting/DashboardContracts.cs` mirrors the Application result types almost 1:1.

## Alerts (in-app only, no push yet)

`GET /api/households/{householdId}/alerts` — product features 3 and 4 in `SplitCard.md` ("alertas de cuotas por vencer" / "notificación de corte/pago próximo"), **in-app version only**. This is data for the mobile Home tab to render when the user opens the app — no push notifications, no scheduled job, no stored device tokens. **Full plan for what real push would need: `/PUSH_NOTIFICATIONS_PLAN.md` at the monorepo root.**

- `GetHouseholdAlertsQueryHandler` (`Application/Alerts/GetHouseholdAlerts.cs`), Credit cards only. Uses `Card.GetStatementPeriodFor(clock.Today)` **directly** — not the persisted `StatementPeriod` repository — so a cutoff/payment-due alert fires even if no purchase has been registered yet this period (there might be no `StatementPeriod` document at all, but the date is still approaching regardless of whether anyone's bought anything).
- `AlertWindowDays = 5` (hardcoded constant in the handler, not configurable yet). A card can produce both a `"CutoffSoon"` and a `"PaymentDueSoon"` `CardAlert` simultaneously if both dates happen to fall inside the window — these are independent checks, not mutually exclusive.
- `InstallmentAlert`s are only computed when `"PaymentDueSoon"` fires for that card — deliberately tied to the moment money actually needs to be paid (the cutoff itself doesn't need a per-transaction breakdown, just the "heads up, corte pronto" card-level alert). Uses the same `PeriodTransactionResolver` + visibility filter as everywhere else.
- Regression tests build deterministic scenarios by picking `cutoffDay`/`paymentDueDay` values that keep `Card.GetStatementPeriodFor` resolving to dates within the SAME month as the fixed `FakeClock` "today" — see the tests' comments for exactly why (the day-of-month domain math makes it easy to accidentally roll into next month if you're not careful about which values you pick relative to `today.Day`).

## Application layer — CQRS without a mediator

No MediatR. Each Command/Query is a concrete `sealed class` with a single `Handle(...)` method, primary-constructor DI. One file per use case, grouped by feature folder (`Auth/`, `Households/`, `Users/`, `Cards/`, `Transactions/`, `SplitRules/`, `Reconciliation/`, `Reporting/`, `Alerts/`).

Exceptions (`Application/Common/`): `UnauthorizedException` (→401), `NotFoundException` (→404), `ForbiddenException` (→403), `ApplicationValidationException` (→400, application-level) — distinct from `SplitCard.Domain.Exceptions.DomainException` (→400, entity invariants).

Authorization pattern: load acting `User`, `HouseholdAccessGuard.EnsureMember(...)`, then role checks (`.Role`/`.CanWrite()`/`.CanReadAll()`). `ActingUserId` always from the JWT. Owner-only: `CreateCard`, `InviteUser`, `CreateSplitRule`. Owner+Contributor: `RegisterTransaction`. Visibility filtered through `Split[].PersonId` for reads.

Implemented: **Auth**, **Households**, **Users**, **Cards**, **Transactions** (`RegisterTransactionCommand`, `GetVisibleTransactionsQuery`, `GetTransactionsForCardPeriodQuery`, `PeriodTransactionResolver`), **SplitRules**, **Reconciliation**, **Reporting**, **Alerts**.

`RegisterTransactionCommandHandler` — the piece worth understanding before touching Transactions/:
1. Loads acting user, checks `CanWrite()`.
2. Loads the Card, `HouseholdAccessGuard.EnsureMember(actingUser, card.HouseholdId)`.
3. Rejects `Debit + Installments` with `ApplicationValidationException`.
4. Creates ONE `InstallmentPlan` if `Installments` present (never a Transaction per month).
5. Ensures a `StatementPeriod` exists for Credit cards (reuses if one already covers the date).
6. Constructs the `Transaction`.

`GetTransactionsForCardPeriodQueryHandler`, `GenerateStatementPdfQueryHandler`, `GetHouseholdDashboardQueryHandler`, and `GetHouseholdAlertsQueryHandler` all call `PeriodTransactionResolver` then apply the same visibility filter (Owner sees all, others only `Split[].PersonId` matches) — this filter is duplicated four times now, not extracted into a shared helper yet.

## Code conventions

- C# 13 / .NET 9: primary constructors, collection expressions (`= [];`), `sealed` by default.
- `Nullable` enabled everywhere. Avoid `!` unless genuinely impossible to be null.
- Domain entities: public constructor with validation (`DomainException`), private parameterless constructor for Infrastructure reconstruction.
- Never use `// TODO` placeholders — document gaps here instead.
- New Domain logic with non-trivial branching (date math, financial calculations) needs xUnit tests in `tests/Split.Card.Domain.Tests`.
- New Application handlers with real branching need tests in `tests/Split.Card.Application.Tests` using `Fakes/` — no Moq without discussing it first.
- Any handler calling `HouseholdAccessGuard` needs both a same-household and cross-household test.
- Any code computing per-period amounts must go through `PeriodTransactionResolver`/`Transaction.GetPeriodAmount` — never re-derive it from `Transaction.Amount` + a date comparison.
- Any code checking "is this date within N days of now" must take `IClock`, not call `DateTime.UtcNow` directly, so it can be tested with a fixed date.

## Infrastructure / Railway

- Config via `Environment.GetEnvironmentVariable` only. Required: `MONGODB_CONNECTION_STRING`, `MONGODB_DATABASE_NAME`, `JWT_SECRET`. Optional: `JWT_EXPIRATION_MINUTES`.
- `GET /health` verifies a real Mongo connection (`MongoDbContext.PingAsync`), not just process liveness.
- `MongoClientSettings` with `ServerSelectionTimeout`/`ConnectTimeout`/`RetryWrites`/`RetryReads` configured.
- Multi-stage Dockerfile at repo root (build context needs all 4 `src/` projects). `ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080` — Railway auto-detects this from the Dockerfile; no `PORT` env var wiring needed on the app side. `railway.json` at the repo root points Railway at this Dockerfile and sets `healthcheckPath: /health`.
- CI: `.github/workflows/service-ci.yml` (lives at the monorepo root, not inside this repo) runs `dotnet restore && dotnet build && dotnet test` on push/PR touching `split.card.service/**`.
- **CD**: same workflow's `deploy` job runs only on `push` to **`develop`** (not `master`/`main`, not PRs — check the actual `if:` condition in the workflow file before assuming, this has been mis-stated here before) and only after `build-and-test` succeeds (`needs:`), so a failing test blocks the deploy. Installs `@railway/cli` and runs `railway up --service "<name>" --detach --ci` from the `split.card.service` working directory, authenticated via a project-scoped `RAILWAY_TOKEN` (GitHub secret, in the **`develop`** GitHub Environment — not `production`). `RAILWAY_SERVICE_NAME` is a repo variable (`vars.`), not a secret. Railway's own GitHub auto-deploy integration is deliberately **not** used, specifically so deploys are gated on tests passing — don't re-enable "Auto Deploy" on the Railway service without removing/adjusting this job. **Full checklist of what needs to be true on the Railway/GitHub side for this to actually work end-to-end: `/DEPLOYMENT.md` at the monorepo root** — this file only describes the mechanism, not whether it's currently configured correctly (that requires checking the live GitHub/Railway dashboards, which isn't possible from inside this repo).
- `.dockerignore` at the repo root excludes `bin/`, `obj/`, `tests/`, `.env*` (except `.env.example`), and dev-only files from the Docker build context.
- **Local dev**: `Program.cs` loads `src/Split.Card.Api/.env` via `DotNetEnv` — no-op on Railway.
- **Local Mongo**: `docker-compose.yml` at repo root, port `27018` (not `27017`, to avoid colliding with other local projects). `docker compose up -d` before `dotnet run`.

## Domain model (summary — see SplitCard.md for full detail)

- `Household`, `User` (`UserRole`: Owner/Contributor/RestrictedViewer), `Card` (Credit/Debit), `InstallmentPlan`, `Transaction`, `StatementPeriod`, `SplitRule`, `PersonShare`.
- Installments: never a new record per month. `InstallmentPlan.GetInstallmentNumberFor()`/`HasActiveInstallmentIn()` calculate on-the-fly from `FirstChargeDate`. Only Credit cards — enforced in `RegisterTransactionCommandHandler`.
- `Transaction.Amount` is always the full purchase total — see the dedicated section above for why this matters and what NOT to do with it.
- Authorization: `Split[].PersonId contains userId` at the Mongo query level for who-sees-what, `HouseholdAccessGuard` for which-household-at-all.
- `Card.GetStatementPeriodFor()`: heuristic, covered by unit tests, and now also the basis for `GetHouseholdAlertsQueryHandler` (computed on-the-fly, not from persisted `StatementPeriod`s). Not validated against real bank data yet.

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

- No refresh-token flow — JWT just expires, user re-logs in.
- `SuggestSplitForMerchantQueryHandler` has no dedicated `HouseholdAccessGuard` regression test yet — lowest risk of the set.
- The Owner-vs-visible-only filtering logic is duplicated across `GetTransactionsForCardPeriodQueryHandler`, `GenerateStatementPdfQueryHandler`, `GetHouseholdDashboardQueryHandler`, and `GetHouseholdAlertsQueryHandler` — four call sites now, strong candidate for extraction.
- `PeriodTransactionResolver` calls `IInstallmentPlanRepository.GetByIdAsync` once per installment transaction found (no batch lookup) — fine at current scale, revisit if a card ever has many concurrent installment plans.
- Dashboard makes one sequential resolver pass per Credit card in a loop — fine at current scale, not parallelized. `GetHouseholdAlertsQueryHandler` has the same shape (loop per Credit card).
- `GetHouseholdDashboardQueryHandler` not yet migrated to `IClock` (see IClock section above) — inconsistent with the newer `GetHouseholdAlertsQueryHandler`, worth fixing together if either is touched again.
- **Real push notifications are NOT built** — see `/PUSH_NOTIFICATIONS_PLAN.md` at the monorepo root for the full plan (mobile token registration, backend storage endpoint, scheduled trigger options, Expo push API). Don't half-build this across unrelated feature requests; do it once, deliberately, when actually prioritized.
- **Budget/límite (`SplitCard.md` feature 9) — scoped decision made: per-card, not per-household or per-person.** Not built yet. Needs: a new `Budget`-ish concept tied to `Card.Id` (a limit amount + currency, presumably editable only by Owner), and a comparison against `GetHouseholdDashboardQueryHandler`'s `byCard` totals (already computed) to show "% used" / over-limit — almost certainly informational only (doesn't block `RegisterTransactionCommand`), but confirm before assuming.
- **Railway/EAS deployment status is a checklist, not a confirmed-working pipeline** — see `/DEPLOYMENT.md` at the monorepo root. The CI/CD mechanism exists in code (workflow, Dockerfile, railway.json), but whether the actual Railway service, its env vars, and the GitHub secrets/variables are correctly configured has NOT been verified from inside this repo — that requires checking the live dashboards.

## Interaction rules for Claude Code in this repo

- Explanations and technical reasoning: Spanish. Code names, variables, components: English.
- Prefix technical statements with `[Seguro]` / `[Probable]` / `[Suponiendo]`. If `[Suponiendo]`, stop and list what's missing before writing code.
- If there is a genuine architectural disagreement, state it directly in the first line, no hedging or unnecessary validation.
