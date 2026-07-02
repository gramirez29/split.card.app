# SplitCard Mobile

App Expo Router + TypeScript strict + Zustand. Consume la API de `SplitCard` (backend .NET/Mongo — ver `../split.card.service/README.md`).

## Ubicación

```
D:\Repositories\Applications\split.card\split.card.mobile
```

## Setup

```bash
npm install
npx expo install --fix   # ajusta versiones exactas al SDK instalado
cp .env.example .env     # y editá EXPO_PUBLIC_API_URL
npx expo start
```

**No se corrió `npm install` en el entorno donde se generó este código** (sandbox sin red). Las versiones en `package.json` son de referencia — `expo install --fix` las corrige automáticamente contra el SDK real.

## Estructura

```
app/                        -> Rutas (Expo Router, file-based)
  _layout.tsx                  Stack raíz
  index.tsx                    Redirect según auth
  (auth)/
    login.tsx
  (app)/                       Grupo protegido (redirige a login si no hay sesión)
    index.tsx                  Dashboard: lista de tarjetas
    cards/[cardId].tsx         Detalle de tarjeta (parcial, ver Pendientes)
    transactions/new.tsx       Registro de compra
    settings.tsx                Logout

src/
  api/
    client.ts                  fetch wrapper: auth header, timeout 15s, manejo de errores
    endpoints/                 Un archivo por recurso del backend
  store/                       Zustand: authStore, cardsStore, offlineQueueStore
  types/                       Tipos espejo de las entidades del backend
  hooks/useAuth.ts
  constants/config.ts          Lee EXPO_PUBLIC_API_URL
  utils/                       currency.ts, date.ts
```

## Decisiones de diseño

- **Alias `@/*`**: apunta a `src/`, configurado en `tsconfig.json`. Expo SDK 50+ soporta `paths` de tsconfig nativamente vía Metro sin plugin adicional — si no resuelve al correr, agregar `babel-plugin-module-resolver` como fallback.
- **Auth token**: `expo-secure-store`, nunca en Zustand ni en memoria persistida sin cifrar.
- **Enums**: los tipos en `src/types/enums.ts` asumen que el backend serializa enums de C# como *string*. Por defecto `System.Text.Json` los serializa como número — hay que registrar `JsonStringEnumConverter` en `Program.cs` del backend cuando se construyan los Controllers, o cambiar estos tipos a `number`.

## Pendiente (no incluido en este esqueleto)

- **Picker de split entre miembros del household** (esposa/hija): la pantalla de nueva compra hoy asigna 100% al usuario que registra. Requiere que el backend exponga `GET /api/households/{id}/members`.
- **Persistencia del offline queue**: `offlineQueueStore` es en memoria; se pierde si la app se cierra sin conexión. Falta decidir `expo-sqlite` vs `AsyncStorage` (ver README del backend, misma pendiente anotada ahí) e implementar la sincronización real.
- **Detalle de tarjeta**: `cards/[cardId].tsx` es una pantalla placeholder — falta el endpoint de transacciones por periodo en el backend para completarla.
- **Dashboard consolidado** (por persona/tarjeta/moneda, con tipo de cambio de referencia): no implementado, depende de endpoints de agregación que el backend todavía no tiene.
- **Alertas de cuotas/corte, conciliación, presupuesto**: funcionalidades 3, 4, 7 y 9 de `SplitCard.md`, sin pantalla todavía.
