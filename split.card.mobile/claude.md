# CLAUDE.md — SplitCard Mobile

Project instructions for Claude Code when working in this repository. Consumes the API from the `Split.Card.Service` backend repo (.NET 9 + MongoDB on Railway) — see that repo's `CLAUDE.md` for the domain contract.

## What this project is

SplitCard mobile app: register purchases at the moment they happen, track installments, view a debt dashboard by person/card/currency, with Owner/Contributor/RestrictedViewer roles.

## Stack

- Expo (SDK ~52) + Expo Router (file-based routing)
- Strict TypeScript (`strict: true`, `noUncheckedIndexedAccess: true`)
- Zustand for global state
- `expo-secure-store` for the JWT (never in persisted Zustand state or unencrypted AsyncStorage)

## Structure

```
app/                        Routes (Expo Router)
  _layout.tsx                  Root stack
  index.tsx                    Redirect based on auth state
  (auth)/login.tsx
  (app)/                       Protected group — redirects to login if no session
    index.tsx                  Dashboard
    cards/[cardId].tsx
    transactions/new.tsx
    settings.tsx

src/
  api/
    client.ts                  fetch wrapper: auth header, 15s timeout, ApiResult<T>
    endpoints/                 One file per backend resource
  store/                       authStore, cardsStore, offlineQueueStore
  types/                       Mirror of the backend entities
  hooks/useAuth.ts
  constants/config.ts          Reads EXPO_PUBLIC_API_URL
  utils/                       currency.ts, date.ts
```

## Code conventions

- **STRICT ANY BAN**: never use `any`. If a third-party library forces a loose type, isolate it at the boundary (e.g. inside `api/client.ts`) and never let it propagate into components or stores.
- If `any` is ever unavoidable to fulfill a request, still deliver the full solution, and at the end of the response list exactly which lines contain `any` and how to properly type them — never silently patch it without explicit approval.
- Every screen component uses `StyleSheet.create`, not loose inline styles.
- Types in `src/types/` must exactly mirror the JSON the backend returns — if the backend changes a contract, the type is updated in the same change, not afterward.
- No `// TODO` placeholders in code. Anything missing is documented in this file or the README, not in a loose comment.

## Contract with the backend — known friction points

- **Enums**: `src/types/enums.ts` assumes the backend serializes enums as `string`. By default `System.Text.Json` serializes them as numbers — confirm against the backend (`JsonStringEnumConverter` registered or not) before trusting those union types.
- **Dates**: the backend uses `DateOnly` (serialized as `yyyy-MM-dd` once the Controller exposes it). Mobile types already assume `string` in that format — do not switch to a JS `Date` without coordinating.
- **Auth**: `POST /api/auth/login` and `GET /api/auth/me` are the only auth endpoints assumed by `authStore`. If the backend defines a different contract, update `src/api/endpoints/auth.ts` first.

## Commands

```bash
npm install
npx expo install --fix
npx expo start
npx tsc --noEmit      # type check, run before any PR
npm run lint
```

## Pending (do not assume it exists)

- Split picker across household members — today `transactions/new.tsx` assigns 100% to whoever registers the purchase. Blocked by `GET /api/households/{id}/members` on the backend.
- Real offline queue persistence — `offlineQueueStore` is in-memory only. Still need to decide `expo-sqlite` vs `AsyncStorage`.
- `cards/[cardId].tsx` is a placeholder screen — needs the per-period transactions endpoint.
- Consolidated dashboard (by person/card/currency + reference exchange rate): not implemented.
- Installment/cutoff alerts, in-app + PDF reconciliation, optional budget: no screens yet.

## Interaction rules for Claude Code in this repo

- Explanations and technical reasoning: Spanish. Code (names, variables, components): English.
- Prefix technical statements with `[Seguro]` / `[Probable]` / `[Suponiendo]`. If `[Suponiendo]`, stop and list what's missing before writing code.
- On an unavoidable `any`: still deliver the full solution, and at the end of the response list the affected lines and how to type them — never silence it or apply it without an explicit flag.
