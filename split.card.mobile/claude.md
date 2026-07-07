# CLAUDE.md — split.card.mobile

Project instructions for Claude Code when working in this repository. Part of the `split.card.app` monorepo (sibling: `split.card.service/`, .NET 9 + MongoDB on Railway) — see that repo's `claude.md` for the domain contract.

## What this project is

SplitCard mobile app: register purchases at the moment they happen, track installments, view a debt dashboard by person/card/currency, with Owner/Contributor/RestrictedViewer roles.

## Stack (actual installed versions — check before assuming)

- Expo SDK `^54`, Expo Router `~6.0.24`
- React `19.1.0`, React Native `0.81.5`
- TypeScript strict (`strict: true`, `noUncheckedIndexedAccess: true`)
- Zustand for global state
- `expo-secure-store ~15.0.8` for the JWT
- **`react-native-paper ^5.15.3`** — Material Design 3 UI kit, now installed and wired via `PaperProvider` in `app/_layout.tsx` with a custom theme (`src/constants/theme.ts`) and `@expo/vector-icons` (`MaterialCommunityIcons`) for icons.

Run `npx expo install --fix` after any dependency change — do not hand-edit version numbers in `package.json` and assume they're SDK-compatible.

## Structure

```
app/                         Routes (Expo Router) — THIN files only, see pattern below
  _layout.tsx                   Wraps everything in PaperProvider + theme
  index.tsx                     Redirect based on auth state
  (auth)/login.tsx              export { default } from '@/screens/LoginScreen'
  (app)/                        Protected group — redirects to login if no session
    index.tsx                   Dashboard (NOT yet migrated to src/screens/, plain RN components)
    cards/[cardId].tsx          Placeholder screen (NOT yet migrated)
    transactions/new.tsx        Purchase form (NOT yet migrated)
    settings.tsx                (NOT yet migrated)

src/
  screens/
    LoginScreen.tsx             Only screen migrated so far to react-native-paper
  api/
    client.ts                   fetch wrapper: auth header, 15s timeout, ApiResult<T>
    endpoints/                  One file per backend resource
  store/                        authStore, cardsStore, offlineQueueStore
  types/                        Mirror of the backend entities
  hooks/useAuth.ts
  constants/
    config.ts                   Reads EXPO_PUBLIC_API_URL
    theme.ts                    react-native-paper MD3 theme
  utils/                        currency.ts, date.ts
```

## IMPORTANT — inconsistent UI migration in progress

Only `LoginScreen` was moved to `src/screens/` + `react-native-paper` (`TextInput`, `Button`, `Surface`, `HelperText` from the library, `KeyboardAvoidingView`/`ScrollView` wrapper, MD3 theme). **Dashboard, Settings, Card detail, and New Transaction are still raw `react-native` components (`View`/`Text`/`Pressable`/`StyleSheet.create`) directly inside `app/(app)/`.**

This is a real inconsistency, not a design decision to preserve. When touching any of those four screens:
- Follow the `LoginScreen` pattern: move the component to `src/screens/<Name>Screen.tsx`, leave `app/(app)/...` as a one-line re-export (`export { default } from '@/screens/<Name>Screen';`), rebuild the UI with `react-native-paper` components and the shared `theme`.
- Don't do a partial rewrite (e.g. swap only buttons to Paper but leave the rest raw RN) — migrate the whole screen in one pass or don't touch it.
- Confirm with the user which screen to migrate next rather than doing all four unprompted — it's a real amount of UI work per screen.

## Code conventions

- **STRICT ANY BAN**: never use `any`. If a third-party library forces a loose type, isolate it at the boundary (e.g. inside `api/client.ts`) and never let it propagate into components or stores.
- If `any` is ever unavoidable to fulfill a request, still deliver the full solution, and at the end of the response list exactly which lines contain `any` and how to properly type them — never silently patch it without explicit approval.
- Every screen component uses `StyleSheet.create`, not loose inline styles (this still applies to `react-native-paper` screens too — see `LoginScreen.tsx`'s `styles` object for the pattern: layout/spacing in `StyleSheet`, visual theming delegated to Paper's `theme`).
- Types in `src/types/` must exactly mirror the JSON the backend returns — if the backend changes a contract, the type is updated in the same change, not afterward.
- No `// TODO` placeholders in code. Anything missing is documented in this file or the README, not in a loose comment.

## Contract with the backend — known friction points

- **Enums**: `src/types/enums.ts` assumes the backend serializes enums as `string`. By default `System.Text.Json` serializes them as numbers — confirm against `split.card.service` (`JsonStringEnumConverter` registered or not) before trusting those union types.
- **Dates**: the backend uses `DateOnly` (serialized as `yyyy-MM-dd` once the Controller exposes it). Mobile types already assume `string` in that format — do not switch to a JS `Date` without coordinating.
- **Auth**: `POST /api/auth/login` and `GET /api/auth/me` are the only auth endpoints assumed by `authStore`. Neither exists on the backend yet. If the backend defines a different contract, update `src/api/endpoints/auth.ts` first.

## CI

`.github/workflows/mobile-ci.yml` at the monorepo root runs `npm ci && npm run lint && npm run typecheck` on any push/PR touching `split.card.mobile/**`. Keep both green.

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
- UI migration to `react-native-paper` for Dashboard/Settings/Card detail/New Transaction (see section above).

## Interaction rules for Claude Code in this repo

- Explanations and technical reasoning: Spanish. Code (names, variables, components): English.
- Prefix technical statements with `[Seguro]` / `[Probable]` / `[Suponiendo]`. If `[Suponiendo]`, stop and list what's missing before writing code.
- On an unavoidable `any`: still deliver the full solution, and at the end of the response list the affected lines and how to type them — never silence it or apply it without an explicit flag.
