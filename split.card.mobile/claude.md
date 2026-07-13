# CLAUDE.md — split.card.mobile

Project instructions for Claude Code when working in this repository. Part of the `split.card.app` monorepo (sibling: `split.card.service/`, .NET 9 + MongoDB on Railway) — see that repo's `claude.md` for the domain contract. The backend has 15 working endpoints (auth, households, users/members, cards, transactions, split-rules, reconciliation PDF, dashboard, alerts) — don't assume anything is still missing on that side without checking `split.card.service/RequestSamples.md` first. See also `/PUSH_NOTIFICATIONS_PLAN.md` and `/DEPLOYMENT.md` at the monorepo root.

## What this project is

SplitCard mobile app: register purchases at the moment they happen, track installments, view a debt dashboard by person/card/currency, with Owner/Contributor/RestrictedViewer roles.

## Stack (actual installed versions — check before assuming)

- Expo SDK `^54`, Expo Router `~6.0.24`
- React `19.1.0`, React Native `0.81.5`
- TypeScript strict (`strict: true`, `noUncheckedIndexedAccess: true`)
- Zustand for global state
- `expo-secure-store ~15.0.8` for the JWT
- `react-native-paper ^5.15.3` — Material Design 3 UI kit, `PaperProvider` in `app/_layout.tsx`, dark theme (`src/constants/theme.ts`)
- `expo-file-system`, `expo-sharing` — installed via `npx expo install`. **`expo-file-system`'s classic API (`cacheDirectory`, `downloadAsync`) is imported from the `expo-file-system/legacy` subpath** — SDK 54's root export is a new `File`/`Directory` class-based API. See `client.ts`'s `apiDownloadFile`.
- `react-native-safe-area-context ~5.6.0` — was already a dependency, wasn't actually wired up (`SafeAreaProvider` missing from root layout) until the safe-area fix below.
- `eas.json` (root of this package) — build profiles for EAS Build, not yet run (needs interactive `eas login`/`eas init`). See `/DEPLOYMENT.md`.

Run `npx expo install --fix` after any dependency change.

## Visual direction — dark, card-based (reference: Avast One dashboard screenshot)

Deep navy background (`theme.colors.background`, `#0B1220`), slightly-lighter navy surfaces (`theme.colors.surface`, `#131B2C`), bright blue accent (`theme.colors.primary`, `#4C8DFF`). `theme.ts` extends `MD3DarkTheme`. Shared `spacing`/`radius` tokens also in `theme.ts`. `alertColors` (`warning`/`onWarningContainer`/`warningContainer`, amber) is a **separate export**, not part of `theme.colors` — Paper's `MD3Theme.colors` type has a fixed set of typed color roles, so a "warning" role doesn't fit there without fighting TypeScript; used directly in screen styles instead (see Alerts section below).

Every `Stack`/`Tabs` `screenOptions` must set `contentStyle`/background to `theme.colors.background` explicitly — nested navigators do **not** inherit the root Stack's `contentStyle` automatically.

## IMPORTANT — `TopAppBar` + `SideDrawer`: floating chrome, mounted once in `(tabs)/_layout.tsx`

The 4 tab screens have a persistent top bar (`src/components/TopAppBar.tsx`, hamburger icon + "SplitCard" title) and a slide-in-from-left drawer (`src/components/SideDrawer.tsx`, user info + nav shortcuts + logout), both mounted **once** in `(app)/(tabs)/_layout.tsx` (not duplicated per-screen) — `TabsLayout` wraps `<Tabs>` in a plain `<View style={{flex:1}}>` alongside `<TopAppBar>`/`<SideDrawer>` as siblings, so the bar/drawer are the same regardless of which tab is active.

- **Both use `Portal` + custom absolute positioning, not a native header or react-native-paper's `Drawer`** — same reasoning as `Dropdown`'s bottom sheet (see below): full control over the "floats over content" look the design explicitly asked for (a native header reserves layout space and pushes content down; this bar is `position: 'absolute'`, `zIndex: 10`, and screen content scrolls underneath it), and no anchor-measurement quirks.
- `TopAppBar` matches the bottom tab bar's exact elevation/shadow values (`elevation: 8`, `shadowOpacity: 0.3`, `shadowRadius: 12`) so both read as the same floating-chrome system, not two different components that happen to be near each other.
- `SideDrawer` slides in via `Animated.timing` on `translateX` (`-DRAWER_WIDTH` → `0`), with the backdrop's opacity animated in parallel — both stay mounted (not conditionally unmounted) so the close animation can actually play; `pointerEvents` toggles between `'auto'`/`'none'` based on `visible` instead.
- Drawer menu items are **real, working navigation targets only** (Reglas de split, Invitar miembro/Agregar tarjeta for Owner, Cerrar sesión) — no placeholder "coming soon" entries. Add more items to the `items` array in `SideDrawer.tsx` as new screens are actually built, not preemptively.
- Open/close state lives in `src/store/uiStore.ts` (`isDrawerOpen`, `openDrawer`, `closeDrawer`) — a Zustand store specifically because the trigger (`TopAppBar`'s hamburger) and the consumer (`SideDrawer`) are siblings, not parent/child, so plain `useState` would need prop-drilling through `TabsLayout` anyway; a tiny global store is simpler here than threading callbacks.
- **Every tab screen's top padding must account for `APP_BAR_HEIGHT`** (exported from `TopAppBar.tsx`, `56`), not just the safe-area inset — see the safe-area section below, now updated to include this.

## IMPORTANT — safe area: the 4 tab screens must account for the status bar AND the floating TopAppBar

`app/_layout.tsx` wraps everything in `SafeAreaProvider` (from `react-native-safe-area-context`, was already a dependency but never actually mounted). The 4 tab screens (`(tabs)/index.tsx`, `cards.tsx`, `household.tsx`, `account.tsx`) have **no native header** (`headerShown: false` on the `Tabs` navigator) — nothing was accounting for the status bar / notification area, so the screen title rendered underneath it, overlapping the clock/battery icons. Fixed: each of the 4 tab screens calls `useSafeAreaInsets()` and sets the container's top padding to `insets.top + APP_BAR_HEIGHT + spacing.md` (the `APP_BAR_HEIGHT` term was added when the floating `TopAppBar` was introduced — without it, content starts underneath the bar instead of below it).

**Pushed screens (`cards/add.tsx`, `household/invite.tsx`, `cards/[cardId].tsx`, `transactions/new.tsx`, split-rules screens) do NOT need this** — they have `headerShown: true` in `(app)/_layout.tsx`'s `Stack.Screen` options, and React Navigation's native header already accounts for the safe area on its own, and there's no floating `TopAppBar` on those routes (it's tab-only). Only screens with no header need the manual `useSafeAreaInsets()` treatment. If a new tab-level screen (no header) is added later, it needs this same pattern — copy it from any of the 4 existing tab screens rather than reintroducing the bug.

## IMPORTANT — `alertColors` had a real contrast bug, fixed

`onWarningContainer` was originally `#4A3400` (dark brown) — the right choice if `warningContainer` were a light background, but `warningContainer` is `#3D2E0A` (dark, matching the dark theme), so it was dark text on a dark background, nearly unreadable. Fixed: `onWarningContainer` is now `#FFE1A8` (light warm cream) — same pattern as `primaryContainer`/`onPrimaryContainer` elsewhere in the theme (dark container background, light text). Also bumped the alert row's text from `bodyMedium` to `bodyLarge` and the icon from 20 to 24px, per explicit feedback that the alert text was too small. If you add another custom "container" color pair outside Paper's theme, double check the container/on-container pairing makes sense for a **dark** theme specifically — light-theme intuitions about which one should be dark vs. light are inverted here.

## `src/components/Dropdown.tsx` — shared "select" component

**Rebuilt once already** — the first version wrapped react-native-paper's `Menu` around a read-only `TextInput`. Two real problems with that: `Menu` anchors via an absolute-position measurement of the anchor element, which is unreliable inside a `ScrollView` (options rendered outside the visible content area), and the tappable area was effectively just the `TextInput`'s own touch target, not the whole field. Now it's a custom bottom sheet: `Portal` + `Modal` (both from `react-native-paper`) sliding up from the bottom, with the entire field row as one `Pressable`. No anchor math at all — a `Modal` is a full-screen overlay, so there's nothing to mismeasure. `SideDrawer` above follows the same `Portal`-based philosophy for the same reasons.

Props unchanged from the first version: `label`, `value`, `options: { label, value }[]`, `onSelect(value)`, optional `style`. **The 3 call sites didn't need any changes when this was rebuilt** — only the internals of `Dropdown.tsx` changed.

Styling matches the dark theme: field looks like an outlined input (border, rounded corners, label above value inside the box), sheet has a drag handle, title, and a scrollable `FlatList` of options with the selected one highlighted (`primaryContainer` background + check icon).

Used in three places, all replacing a previous free-text or pill-based input that user testing flagged as not user-friendly:
- `transactions/new.tsx` — card picker (was `SegmentedButtons`; long card names didn't fit legibly as pills).
- `transactions/new.tsx` — cantidad de cuotas (was a free-text `TextInput`; now a fixed list `[2, 3, 4, 6, 12, 18, 24]`, matching what CR banks actually offer).
- `cards/add.tsx` — día de corte / día de pago (was two free-text `TextInput`s; now two `Dropdown`s with options `1`–`31` each).

`transactions/new.tsx`'s currency toggle (CRC/USD) and `cards/add.tsx`'s tipo toggle (Credit/Debit) are intentionally still `SegmentedButtons` — only 2 options each, pills read fine at that width. `Dropdown` is for long lists / long labels, not a universal replacement.

## IMPORTANT — `transactions/new.tsx` was sending the full amount as `installmentAmount` (real bug, fixed)

`RegisterTransactionRequest.installments.installmentAmount` must be the amount of **one** installment (e.g. `5000` for a ₡15,000 purchase in 3 installments), not the full purchase total. The form was sending `numericAmount` (the full total) for both `amount` and `installmentAmount` — so every installment purchase had `InstallmentPlan.InstallmentAmount == the full total`. Fixed: `installmentAmountPreview = round((amount / totalInstallments) * 100) / 100`, shown to the user as a preview ("3 cuotas de ₡5,000 cada una") before submit, and that's the value actually sent. **Any existing installment transactions created before this fix have wrong data in Mongo** — there's no update endpoint, the only fix is deleting and re-registering them.

## IMPORTANT — `PeriodTransaction` vs `Transaction`: don't sum `amount` for installment purchases

`GET /api/cards/{cardId}/transactions?date=...` returns `PeriodTransaction` (`src/types/transaction.ts`), **not** `Transaction`. `amount` is still the full original purchase total; **`periodAmount`** is what's actually due in the queried period — same value for a one-time purchase, different for an installment purchase. **Any total/sum shown on screen must use `periodAmount`, never `amount`.** `installmentNumber`/`totalInstallments` (both `null` for non-installment purchases) are also on this type — used to render "Cuota 2/3" next to the merchant.

`getVisibleTransactions` (used for the Debit branch of Card Detail) still returns plain `Transaction` — normalized into a `PeriodTransaction`-shaped object at the call site (`periodAmount = amount`, `installmentNumber`/`totalInstallments = null`) purely so the rendering code can stay uniform.

## IMPORTANT — recurring bug pattern: mobile assumed endpoint shapes that don't match the real backend

This has happened four separate times in this codebase (plus the two installment-amount issues above). **Before wiring any new `api/endpoints/*.ts` call, check `split.card.service/RequestSamples.md` for the actual route, query params, and response shape.** Fixed so far:

1. `api/endpoints/cards.ts` called `GET /api/cards` and `GET /api/cards/{cardId}` — neither exists. Fixed to `getCardsForHousehold(householdId)`.
2. `api/endpoints/transactions.ts`'s `getTransactionsForCard` sent `?startDate=...&endDate=...` — backend takes a single `?date=...`. Fixed.
3. `api/endpoints/statementPeriods.ts` called an endpoint that never existed. File is now empty (`export {}`) with a comment.
4. **Real backend constraint, not a mismatch**: transactions/PDF-by-period endpoints 404 unconditionally for Debit cards (no `StatementPeriod` ever created for them). Fixed via `getVisibleTransactions(householdId)` + client-side filter for Debit.

## Structure

```
app/
  _layout.tsx                       SafeAreaProvider + PaperProvider + theme + StatusBar(style="light")
  index.tsx                         Redirect based on auth state
  (auth)/
    _layout.tsx                     Stack, contentStyle = dark background
    login.tsx                       export { default } from '@/screens/LoginScreen'
    register.tsx                    Create Household + auto-login
  (app)/                           Protected group
    _layout.tsx                     Stack: (tabs) + pushed screens, explicit Stack.Screen
                                     per pushed route (dark header, several as 'modal') —
                                     these get safe-area handling for free via the header
    (tabs)/                        Floating pill bottom bar + floating TopAppBar/SideDrawer,
                                     4 tabs, headerShown: false — each screen handles its
                                     own top padding (safe area + APP_BAR_HEIGHT)
      _layout.tsx                   Mounts TopAppBar + SideDrawer alongside <Tabs>
      index.tsx                     Home — banner CTA + in-app alerts + consolidated
                                     dashboard totals + 2-col grid of cards
      cards.tsx                     Cards — full list + FAB "add" (Owner only)
      household.tsx                 Household — link to Split Rules + members list + FAB "invite" (Owner only)
      account.tsx                   Profile + logout
    cards/
      [cardId].tsx                  REAL content, Credit vs Debit branch, uses
                                     PeriodTransaction/periodAmount (see IMPORTANT above)
      add.tsx                       Owner-only, POST /api/cards, day-of-month Dropdowns
    household/
      invite.tsx                    Owner-only, POST /api/users/invite
      split-rules/
        index.tsx                   List, resolves personId -> name via householdStore
        create.tsx                  Owner-only, same equal-split-chips pattern as
                                     transactions/new.tsx (duplicated on purpose)
    transactions/
      new.tsx                       Real split picker + SplitRule merchant autocomplete +
                                     installment amount preview + Dropdown for card/cuotas
                                     (see IMPORTANT above and Dropdown section above)

src/
  components/
    Dropdown.tsx                    Shared bottom-sheet "select" — see dedicated section
    TopAppBar.tsx                   Floating top bar, exports APP_BAR_HEIGHT
    SideDrawer.tsx                  Slide-in-from-left nav drawer
  screens/
    LoginScreen.tsx                 Only screen using the src/screens/ + re-export pattern
  api/
    client.ts                       apiRequest<T> (JSON) + apiDownloadFile (binary, for PDF)
    endpoints/                      auth, cards, transactions (period-based + household-wide),
                                     households (members + register), users (invite),
                                     splitRules (list/create/suggest), reconciliation (PDF),
                                     dashboard (consolidated totals), alerts (in-app alerts)
  store/                            authStore, cardsStore, householdStore, splitRulesStore,
                                     dashboardStore, alertsStore, uiStore (drawer open/close),
                                     offlineQueueStore
  types/                            Mirror of the backend entities — Transaction AND
                                     PeriodTransaction are separate types, not one with
                                     optional fields (see IMPORTANT above)
  hooks/useAuth.ts
  constants/theme.ts                Dark MD3 theme + spacing/radius tokens + alertColors
  utils/                            currency.ts, date.ts
```

## Onboarding (`(auth)/register.tsx`)

`POST /api/households` (`registerHousehold`, `requiresAuth: false`) creates the Household + first Owner atomically. On success, auto-calls `authStore.login(ownerEmail, ownerPassword)` then redirects to `/(app)`. `LoginScreen` links here. Still the *only* way to create a household — no invite-by-link flow.

## Dashboard (`(tabs)/index.tsx`, `store/dashboardStore.ts`)

`GET /api/households/{householdId}/dashboard` → `HouseholdDashboard { grandTotalCrc, grandTotalUsd, byPerson[], byCard[] }` — these totals are already resolved per-period server-side. Home tab shows, in order: banner CTA, in-app alerts (see below), the two grand totals (CRC/USD, never combined server-side), a `TextInput` for an **exchange rate the user types in** to see a combined CRC-equivalent — **pure client state, never sent to the backend or persisted**, recalculated via `useMemo`. Below that, a per-person breakdown, then the card grid — each Credit card tile shows its own `CardTotal`; Debit tiles show the bank name since Debit is excluded from the dashboard entirely.

A Credit card with zero purchases this period still shows up with `₡0`/`$0`, not omitted — don't "helpfully" filter those out in the UI.

## Alerts (`(tabs)/index.tsx`, `store/alertsStore.ts`) — in-app only, no push

`GET /api/households/{householdId}/alerts` → `HouseholdAlerts { cardAlerts[], installmentAlerts[] }`. Rendered right below the banner CTA on Home, **only when there's at least one alert** — the whole section is omitted (not shown empty/collapsed) when `cardAlerts.length === 0 && installmentAlerts.length === 0`.

- `describeCardAlert()` builds a one-line string per `CardAlert` — `"BAC Visa: corte en 2 días"` / `"BAC Visa: pago mañana"` / `"...hoy"` — using `alertType` to pick "corte"/"pago" and `daysUntil` for the phrasing (`describeDaysUntil()` handles 0/1/N specially: "hoy", "mañana", "en N días").
- Each `InstallmentAlert` renders as `"{merchant} — Cuota {installmentNumber}/{totalInstallments} — {formatCurrency(periodAmount, currency)} — vence {describeDaysUntil(daysUntilPaymentDue)}"`.
- Styled with `alertColors` (amber), `bodyLarge` text, 24px icon — see the contrast-bug IMPORTANT section above for why the colors are what they are.
- **This is explicitly a first phase, no push notifications** — the user only sees this when they open the app. **Full plan for real push: `/PUSH_NOTIFICATIONS_PLAN.md` at the monorepo root.**
- **Budget/límite (`SplitCard.md` feature 9) — scoped decision made: per-card.** Not built on either side yet; backend `claude.md` has the shape of what's needed once it's prioritized.

## Split picker + SplitRule autocomplete + installment preview (`transactions/new.tsx`, `household/split-rules/create.tsx`)

- Member selection: `Chip` per household member, toggled on press. Defaults to `[user.id]` (self, 100%).
- **Equal-split auto-calculation runs only when the set of selected members changes** (`useEffect` keyed on `selectedMemberIds`), first selected member absorbs the rounding remainder. Manual percentage edits are not overwritten unless a member is toggled again.
- **Merchant autocomplete** (`transactions/new.tsx` only): on blur of "Comercio", calls `suggestSplitForMerchant`. Uses a guard ref (`skipNextAutoSplitRef`) so the equal-split effect doesn't immediately overwrite the suggestion. Don't remove without understanding why.
- **Installment preview** (`transactions/new.tsx` only): when "¿Pago en cuotas?" is on, shows "`N` cuotas de `formatCurrency(amount/N)` cada una" and submits that computed, rounded value as `installmentAmount`.
- **Cuotas picker** is a `Dropdown` with fixed options `[2, 3, 4, 6, 12, 18, 24]`, not free text — see Dropdown section above.
- Same chip pattern duplicated in `household/split-rules/create.tsx` — two usages, not worth extracting yet.
- Submit disabled unless percentages sum to exactly 100.

## Card Detail (`cards/[cardId].tsx`) — real content, Credit/Debit branch

- Looks up the `Card` from `cardsStore.cards` (fetches if empty) rather than a per-card GET — there isn't one.
- **Credit**: period navigation via `selectedDate` state (prev/next shift by one calendar month — approximation, API doesn't return exact period boundaries). Calls `getTransactionsForCard(cardId, isoDate)` → `PeriodTransaction[]`. A `404` is treated as "empty period", not an error.
- **Debit**: no period concept, no date nav, no PDF button. Calls `getVisibleTransactions(householdId)` once, filters by `cardId`, normalizes to `PeriodTransaction` shape.
- Totals per currency computed client-side using `periodAmount` (not `amount`).
- Row subtitle shows "Cuota X/Y" when `installmentNumber`/`totalInstallments` are set.
- "Compartir PDF de conciliación" (Credit only): `downloadStatementPdf` → `apiDownloadFile` → `expo-sharing`'s `Sharing.shareAsync`.

## Code conventions

- **STRICT ANY BAN**: never use `any`. Isolate any unavoidable loose typing at a boundary and never let it propagate.
- If `any` is ever unavoidable, still deliver the full solution, and at the end of the response list exactly which lines contain `any` and how to properly type them.
- Every screen uses `StyleSheet.create` referencing `theme.colors.*`/`spacing`/`radius` — no hardcoded hex colors (`alertColors` from `theme.ts` is the one sanctioned exception, for the reason noted above).
- Types in `src/types/` must exactly mirror the JSON the backend returns — including when two endpoints return DIFFERENT shapes for what looks like "the same" resource (see `Transaction` vs `PeriodTransaction`).
- No `// TODO` placeholders. Document gaps here or in the README instead.
- **Check `RequestSamples.md` before wiring a new endpoint call**, and re-check it if a backend contract might have changed even for an endpoint you already integrated.
- Prefer `npx expo install <package>` over hand-editing `package.json` — Expo packages have had breaking API redesigns between SDKs.
- Any request field that's a computed derivative of another field the user typed (like `installmentAmount` from `amount`/`totalInstallments`) must be computed AND previewed on screen before submit.
- **A free-text `TextInput` for a value with a small, known set of valid options (day-of-month, a fixed installment count list, an enum) should be a `Dropdown`, not a `TextInput`** — established convention after the card/cuotas/day-of-month changes.
- **Don't use react-native-paper's `Menu` or `Drawer` for anything that needs precise positioning/animation control** — see the Dropdown rebuild note and the TopAppBar/SideDrawer section above. Reuse the `Portal`-based pattern instead.
- **Any new tab-level screen (no native header) needs `useSafeAreaInsets()` + `APP_BAR_HEIGHT` for its top padding** — see the safe-area IMPORTANT section above.
- **Don't add placeholder/non-functional entries to `SideDrawer`'s menu** — every item must navigate to a real, working screen. Add items when the screen exists, not before.

## CI

`.github/workflows/mobile-ci.yml` runs `npm ci && npm run lint && npm run typecheck` on push/PR touching `split.card.mobile/**`.

## Commands

```bash
npm install
npx expo install --fix
npx expo start -c
npx tsc --noEmit
npm run lint
```

EAS build (needs interactive `eas login`/`eas init` first — see `/DEPLOYMENT.md`):

```bash
npx eas-cli build --profile preview --platform android   # installable APK, points at Railway
```

## Local dev / running against the backend

`EXPO_PUBLIC_API_URL` must point at wherever `split.card.service` is actually reachable — not always `localhost`:
- Android emulator + Expo Go: `http://10.0.2.2:{port}`
- iOS simulator: `http://localhost:{port}`
- Physical device (Expo Go): `http://{machine-LAN-IP}:{port}` (same Wi-Fi)
- **Against Railway (from the emulator or Expo Go)**: the real Railway public URL, `https://{...}.up.railway.app` — no special networking needed, it's just a normal internet URL. See `/DEPLOYMENT.md` for the full checklist of what needs to be true on the Railway side first.

`{port}` for local comes from `split.card.service/src/Split.Card.Api/Properties/launchSettings.json` — use `http://`, not `https://`. After changing `.env`, restart with `npx expo start -c`.

**`eas.json`'s `preview`/`production` profiles bake `EXPO_PUBLIC_API_URL` in at build time**, pointing at Railway — currently a placeholder (`https://REPLACE-WITH-YOUR-RAILWAY-PUBLIC-URL`) that needs to be replaced with the real domain before running `eas build`. See `/DEPLOYMENT.md`.

## Pending (do not assume it exists)

- **Card Detail period label is approximate** — shows month/year, not exact cutoff-to-cutoff range (API doesn't return period boundaries). Cosmetic, not a data-correctness issue.
- **Offline queue persistence** — `offlineQueueStore` is in-memory only. Still need to decide `expo-sqlite` vs `AsyncStorage`.
- **Stale `members` list risk** in the split picker — no re-check that a selected `personId` still belongs to the household by submit time. Low risk in practice.
- Dashboard's `byPerson` list order depends on the backend's sort — mobile doesn't re-sort, just renders as received.
- **Existing installment transactions created before the `installmentAmount` fix have wrong data in Mongo** (installment amount == full purchase total). No update endpoint exists — the only fix is deleting the bad `Transaction`/`InstallmentPlan` documents and re-registering the purchase from the app.
- `household/split-rules/create.tsx` doesn't have an installment concept, so no preview needed there.
- `Dropdown`'s 31-day sheet is a plain scrollable list, not a calendar-style grid — fine, but a grid could feel more natural for day-of-month specifically. Not built; flag if it comes up again.
- **Alerts are in-app only** — see `/PUSH_NOTIFICATIONS_PLAN.md`.
- **Budget/límite** — scoped as per-card, nothing built on either side yet.
- **`eas.json`'s Railway URL is a placeholder, not filled in** — and `eas init` (which writes `extra.eas.projectId` into `app.json`) hasn't been run yet either. See `/DEPLOYMENT.md` for the exact remaining steps, all of which need an interactive Expo login.
- **`SideDrawer`'s menu is short** (Reglas de split, Invitar miembro, Agregar tarjeta, Cerrar sesión) — by design, only real screens are listed. Grows as new screens get built.

## Interaction rules for Claude Code in this repo

- Explanations and technical reasoning: Spanish. Code (names, variables, components): English.
- Prefix technical statements with `[Seguro]` / `[Probable]` / `[Suponiendo]`. If `[Suponiendo]`, stop and list what's missing before writing code.
- On an unavoidable `any`: still deliver the full solution, and at the end of the response list the affected lines and how to type them.
