# RequestSamples

Ejemplos de payloads JSON para cada endpoint de `split.card.service`. Los nombres de campo van en camelCase porque así los serializa Minimal API por default (`JsonNamingPolicy.CamelCase`), aunque los records de C# estén en PascalCase.

Base URL local: `http://localhost:{puerto}` (ver `/swagger` para el puerto real). Los endpoints marcados **Auth: Bearer** requieren el header `Authorization: Bearer {token}` obtenido de `/api/auth/login`.

---

## Households

### POST /api/households
**Auth:** ninguna (bootstrap de una cuenta nueva — Household + primer Owner atómico)

Request:
```json
{
  "householdName": "Familia Rodríguez",
  "ownerName": "Carlos Rodríguez",
  "ownerEmail": "carlos@example.com",
  "ownerPassword": "una-clave-segura"
}
```

Response `201 Created`:
```json
{
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "ownerUserId": "m3n4o5p6q7r8s9t0u1v2w3x4"
}
```

---

## Auth

### POST /api/auth/login
**Auth:** ninguna

Request:
```json
{
  "email": "carlos@example.com",
  "password": "una-clave-segura"
}
```

Response `200 OK`:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "m3n4o5p6q7r8s9t0u1v2w3x4"
}
```

Response `401 Unauthorized` (email o password incorrectos — mismo mensaje para ambos casos):
```json
{
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid email or password."
}
```

### GET /api/auth/me
**Auth: Bearer**

Sin body.

Response `200 OK`:
```json
{
  "id": "m3n4o5p6q7r8s9t0u1v2w3x4",
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "name": "Carlos Rodríguez",
  "email": "carlos@example.com",
  "role": "Owner"
}
```

---

## Users

### POST /api/users/invite
**Auth: Bearer** (solo Owner puede invitar; `role` no puede ser `"Owner"`)

Request:
```json
{
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "name": "María Rodríguez",
  "email": "maria@example.com",
  "password": "otra-clave-segura",
  "role": "Contributor"
}
```

`role` acepta: `"Contributor"` | `"RestrictedViewer"`.

Response `201 Created`:
```json
{
  "id": "y5z6a7b8c9d0e1f2g3h4i5j6",
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "name": "María Rodríguez",
  "email": "maria@example.com",
  "role": "Contributor"
}
```

### GET /api/households/{householdId}/members
**Auth: Bearer**

Sin body. Lista todos los miembros del hogar (Owner + Contributor + RestrictedViewer) — pensado para poblar el picker de split en mobile (elegir entre qué personas repartir una compra).

Response `200 OK`:
```json
[
  {
    "id": "m3n4o5p6q7r8s9t0u1v2w3x4",
    "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
    "name": "Carlos Rodríguez",
    "email": "carlos@example.com",
    "role": "Owner"
  },
  {
    "id": "y5z6a7b8c9d0e1f2g3h4i5j6",
    "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
    "name": "María Rodríguez",
    "email": "maria@example.com",
    "role": "Contributor"
  }
]
```

---

## Cards

### POST /api/cards
**Auth: Bearer** (solo Owner)

Request (tarjeta de crédito):
```json
{
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "name": "BAC Visa Signature",
  "bank": "BAC Credomatic",
  "type": "Credit",
  "cutoffDay": 20,
  "paymentDueDay": 5
}
```

Request (tarjeta de débito — `cutoffDay`/`paymentDueDay` deben ir `null`):
```json
{
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "name": "BAC Débito Colones",
  "bank": "BAC Credomatic",
  "type": "Debit",
  "cutoffDay": null,
  "paymentDueDay": null
}
```

`type` acepta: `"Credit"` | `"Debit"`.

Response `201 Created`:
```json
{
  "id": "k7l8m9n0o1p2q3r4s5t6u7v8",
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "name": "BAC Visa Signature",
  "bank": "BAC Credomatic",
  "type": "Credit",
  "cutoffDay": 20,
  "paymentDueDay": 5,
  "ownerUserId": "m3n4o5p6q7r8s9t0u1v2w3x4"
}
```

### GET /api/households/{householdId}/cards
**Auth: Bearer**

Sin body.

Response `200 OK`:
```json
[
  {
    "id": "k7l8m9n0o1p2q3r4s5t6u7v8",
    "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
    "name": "BAC Visa Signature",
    "bank": "BAC Credomatic",
    "type": "Credit",
    "cutoffDay": 20,
    "paymentDueDay": 5,
    "ownerUserId": "m3n4o5p6q7r8s9t0u1v2w3x4"
  }
]
```

---

## Transactions

### POST /api/transactions
**Auth: Bearer** (Owner o Contributor — `RestrictedViewer` recibe 403)

Request (compra de crédito sin cuotas — pago único, split 50/50):
```json
{
  "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8",
  "merchant": "Automercado",
  "purchaseDate": "2026-03-10",
  "amount": 45000,
  "currency": "CRC",
  "installments": null,
  "split": [
    { "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 50 },
    { "personId": "y5z6a7b8c9d0e1f2g3h4i5j6", "percentage": 50 }
  ]
}
```

`installments: null` en una tarjeta de Crédito significa "una sola cuota, se paga completo en el próximo corte" — no es una compra de contado.

Request (compra en cuotas, 100% a un solo usuario — solo válido en tarjetas Credit):
```json
{
  "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8",
  "merchant": "PriceSmart",
  "purchaseDate": "2026-03-10",
  "amount": 12000,
  "currency": "USD",
  "installments": {
    "totalInstallments": 6,
    "installmentAmount": 2000
  },
  "split": [
    { "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 100 }
  ]
}
```

`currency` acepta: `"CRC"` | `"USD"`. `split[].percentage` debe sumar exactamente `100` entre todos los elementos, o el request falla con `400` (`DomainException`).

**`installments` no nulo + `cardId` de una tarjeta `type: "Debit"` → `400` (`ApplicationValidationException`)**: una compra de contado se paga completa en el momento, no se puede fraccionar en cuotas. Ejemplo de request que falla:
```json
{
  "cardId": "id-de-una-tarjeta-debit",
  "merchant": "Supermercado",
  "purchaseDate": "2026-03-10",
  "amount": 15000,
  "currency": "CRC",
  "installments": { "totalInstallments": 3, "installmentAmount": 5000 },
  "split": [{ "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 100 }]
}
```
Response `400 Bad Request`:
```json
{
  "title": "Validation error",
  "status": 400,
  "detail": "Debit card purchases cannot be paid in installments — they are settled in cash at the moment of purchase."
}
```

`cardId` de una tarjeta que pertenece a **otro household** que no es el tuyo → `403` (ver `HouseholdAccessGuard` al final de este documento).

Response `201 Created` (para el request válido de arriba):
```json
{
  "id": "z9y8x7w6v5u4t3s2r1q0p9o8",
  "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8",
  "merchant": "Automercado",
  "purchaseDate": "2026-03-10",
  "amount": 45000,
  "currency": "CRC",
  "installmentPlanId": null,
  "createdByUserId": "m3n4o5p6q7r8s9t0u1v2w3x4",
  "split": [
    { "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 50 },
    { "personId": "y5z6a7b8c9d0e1f2g3h4i5j6", "percentage": 50 }
  ]
}
```

### GET /api/households/{householdId}/transactions
**Auth: Bearer** (Owner ve todo; Contributor/RestrictedViewer solo las transacciones donde aparecen en `split`)

Sin body.

Response `200 OK`: mismo shape que el array de `TransactionResponse` de arriba. **Nota**: este endpoint devuelve el `amount` total de cada compra sin resolver cuotas por periodo — es un listado plano de "todo lo que existe", no "lo que se debe ahora". Para eso usá el endpoint por periodo o el dashboard (ver abajo).

### GET /api/cards/{cardId}/transactions?date=2026-03-15
**Auth: Bearer**

Query param `date` (formato `yyyy-MM-dd`) — resuelve el `StatementPeriod` que contiene esa fecha y devuelve las transacciones que aplican a ese periodo, con el mismo filtro de visibilidad que el endpoint anterior.

**Importante — shape distinto a `TransactionResponse`**: este endpoint devuelve `PeriodTransactionResponse`, no `TransactionResponse`. Para una compra en cuotas, `amount` sigue siendo el total original de la compra (contexto), pero **`periodAmount` es lo que corresponde pagar en ESTE periodo específico** — son el mismo valor para una compra sin cuotas, pero distintos para una compra en cuotas. Cualquier UI que muestre o sume "cuánto se debe este periodo" tiene que usar `periodAmount`, nunca `amount`. También incluye `installmentNumber`/`totalInstallments` (ambos `null` si la compra no es en cuotas) para poder mostrar "Cuota 2 de 3".

Una compra en cuotas aparece en **todos** los periodos donde tiene una cuota activa (según `InstallmentPlan.FirstChargeDate` + `TotalInstallments`), no solo en el periodo donde se hizo la compra originalmente — esto era justamente el bug que se corrigió: antes, solo aparecía (con el monto total, no la cuota) en el primer periodo, y desaparecía por completo en los siguientes.

Sin body.

Response `200 OK` (compra sin cuotas — `amount` y `periodAmount` iguales):
```json
[
  {
    "id": "z9y8x7w6v5u4t3s2r1q0p9o8",
    "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8",
    "merchant": "Automercado",
    "purchaseDate": "2026-03-10",
    "amount": 45000,
    "periodAmount": 45000,
    "currency": "CRC",
    "installmentPlanId": null,
    "installmentNumber": null,
    "totalInstallments": null,
    "createdByUserId": "m3n4o5p6q7r8s9t0u1v2w3x4",
    "split": [{ "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 100 }]
  }
]
```

Response `200 OK` (la misma compra en cuotas de PriceSmart, ₡12,000 a 3 meses, consultada en su **segundo** periodo — `amount` sigue en 12000, `periodAmount` es la cuota):
```json
[
  {
    "id": "abc123",
    "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8",
    "merchant": "PriceSmart",
    "purchaseDate": "2026-03-10",
    "amount": 12000,
    "periodAmount": 4000,
    "currency": "USD",
    "installmentPlanId": "plan-1",
    "installmentNumber": 2,
    "totalInstallments": 3,
    "createdByUserId": "m3n4o5p6q7r8s9t0u1v2w3x4",
    "split": [{ "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 100 }]
  }
]
```

Response `404 Not Found` si no existe un `StatementPeriod` que contenga esa fecha para esa tarjeta (todavía no se registró ninguna compra en ese periodo):
```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "No statement period found for card k7l8m9n0o1p2q3r4s5t6u7v8 on 2026-03-15."
}
```

---

## Reconciliation

### GET /api/cards/{cardId}/statement-pdf?date=2026-03-15
**Auth: Bearer**

Query param `date` (formato `yyyy-MM-dd`) — igual que `GET /api/cards/{cardId}/transactions`, resuelve el `StatementPeriod` que contiene esa fecha. Genera un PDF (QuestPDF) con el detalle de transacciones de ese periodo para conciliar contra el estado de cuenta real del banco.

Sin body.

Response `200 OK`: binario `application/pdf`, descarga como `statement-{cardId}-{date}.pdf`. Contenido: nombre/banco/tipo de tarjeta, fechas del periodo (corte y pago), tabla de transacciones (comercio, fecha, **número de cuota** si aplica, monto **de esa cuota** formateado por moneda — no el total de la compra —, split con **nombres** de personas), y el total sumado por moneda al pie (también calculado con montos por cuota, no totales).

Mismo filtro de visibilidad que las demás rutas de transacciones: Owner ve el periodo completo, Contributor/RestrictedViewer solo las líneas donde aparecen en `split`.

Response `404 Not Found` (mismo caso que `GET /api/cards/{cardId}/transactions` — no hay `StatementPeriod` para esa fecha):
```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "No statement period found for card k7l8m9n0o1p2q3r4s5t6u7v8 on 2026-03-15."
}
```

Response `403 Forbidden` si `cardId` pertenece a otro household (ver `HouseholdAccessGuard` al final de este documento).

---

## Reporting

### GET /api/households/{householdId}/dashboard
**Auth: Bearer**

Sin body. Totales consolidados de lo que se debe **ahora mismo** — solo tarjetas `Credit`, solo el periodo abierto actual (el que contiene la fecha de hoy en el servidor). Las tarjetas `Debit` quedan fuera a propósito: una compra de débito ya está pagada, no es una deuda pendiente. Si una tarjeta Credit todavía no tiene ninguna compra registrada en el periodo actual, aparece en `byCard` con `totalCrc`/`totalUsd` en `0` (no se omite). Las compras en cuotas suman su **monto de cuota**, no el total original — mismo fix que el endpoint por periodo.

Mismo filtro de visibilidad que el resto: Owner ve todas las transacciones del periodo; Contributor/RestrictedViewer solo las que tienen en su `split`, y `byPerson` se arma únicamente a partir de esas transacciones visibles — un Contributor nunca deduce montos de transacciones que no le tocan.

Response `200 OK`:
```json
{
  "grandTotalCrc": 125000,
  "grandTotalUsd": 340,
  "byPerson": [
    { "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "personName": "Carlos Rodríguez", "totalCrc": 75000, "totalUsd": 340 },
    { "personId": "y5z6a7b8c9d0e1f2g3h4i5j6", "personName": "María Rodríguez", "totalCrc": 50000, "totalUsd": 0 }
  ],
  "byCard": [
    { "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8", "cardName": "BAC Visa Signature", "totalCrc": 125000, "totalUsd": 340 },
    { "cardId": "otra-tarjeta-credit-sin-compras", "cardName": "Scotiabank Clásica", "totalCrc": 0, "totalUsd": 0 }
  ]
}
```

Response `403 Forbidden` si `householdId` no es el tuyo (ver `HouseholdAccessGuard` abajo).

**Nota de diseño**: no hay conversión CRC↔USD en este endpoint — `grandTotalCrc`/`grandTotalUsd` van siempre separados. Si querés un total combinado con tipo de cambio de referencia, esa conversión se hace en el cliente (mobile), nunca en el backend — decisión de producto original en `SplitCard.md`: el tipo de cambio no se persiste, es solo para mostrar un cálculo de referencia.

---

## Alerts

### GET /api/households/{householdId}/alerts
**Auth: Bearer**

Sin body. Alertas **in-app** (no push todavía — ver `claude.md`, sección Alerts, para el plan de push) de dos tipos, solo tarjetas `Credit`:

- **`cardAlerts`**: una entrada por cada corte (`"CutoffSoon"`) o fecha de pago (`"PaymentDueSoon"`) que caiga dentro de los próximos 5 días. Se calculan siempre con `Card.GetStatementPeriodFor(hoy)` — no dependen de que exista un `StatementPeriod` guardado, así que una tarjeta sin compras todavía este periodo igual puede generar una alerta de corte/pago próximo.
- **`installmentAlerts`**: solo se calculan cuando `"PaymentDueSoon"` dispara para esa tarjeta — qué compras en cuotas forman parte de ese pago que se viene, con su `periodAmount` (la cuota, no el total) y `installmentNumber`/`totalInstallments`.

Mismo filtro de visibilidad que el resto: Owner ve todas las transacciones del periodo al armar `installmentAlerts`; Contributor/RestrictedViewer solo las que tienen en su `split`.

Response `200 OK` (ejemplo: corte en 2 días, pago en 4 días con una cuota activa):
```json
{
  "cardAlerts": [
    { "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8", "cardName": "BAC Visa Signature", "alertType": "CutoffSoon", "date": "2026-03-12", "daysUntil": 2 },
    { "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8", "cardName": "BAC Visa Signature", "alertType": "PaymentDueSoon", "date": "2026-03-14", "daysUntil": 4 }
  ],
  "installmentAlerts": [
    {
      "cardId": "k7l8m9n0o1p2q3r4s5t6u7v8",
      "cardName": "BAC Visa Signature",
      "transactionId": "tx-1",
      "merchant": "PriceSmart",
      "periodAmount": 4000,
      "currency": "CRC",
      "installmentNumber": 2,
      "totalInstallments": 3,
      "paymentDueDate": "2026-03-14",
      "daysUntilPaymentDue": 4
    }
  ]
}
```

Response `200 OK` (nada por vencer): `{ "cardAlerts": [], "installmentAlerts": [] }` — arrays vacíos, no `null`, no se omiten las claves.

Response `403 Forbidden` si `householdId` no es el tuyo (ver `HouseholdAccessGuard` abajo).

---

## SplitRules

### POST /api/split-rules
**Auth: Bearer** (solo Owner)

Request:
```json
{
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "descriptionPattern": "Netflix",
  "defaultSplit": [
    { "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 50 },
    { "personId": "y5z6a7b8c9d0e1f2g3h4i5j6", "percentage": 50 }
  ]
}
```

Response `201 Created`:
```json
{
  "id": "q1r2s3t4u5v6w7x8y9z0a1b2",
  "householdId": "a1b2c3d4e5f6g7h8i9j0k1l2",
  "descriptionPattern": "Netflix",
  "defaultSplit": [
    { "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 50 },
    { "personId": "y5z6a7b8c9d0e1f2g3h4i5j6", "percentage": 50 }
  ]
}
```

### GET /api/households/{householdId}/split-rules
**Auth: Bearer**

Sin body.

Response `200 OK`: array de `SplitRuleResponse` (mismo shape que arriba).

### GET /api/households/{householdId}/split-rules/suggest?merchant=Netflix
**Auth: Bearer**

Query param `merchant` — busca la primera `SplitRule` cuyo `descriptionPattern` haga match (contains, case-insensitive) con `merchant`.

Sin body.

Response `200 OK` (hay match):
```json
[
  { "personId": "m3n4o5p6q7r8s9t0u1v2w3x4", "percentage": 50 },
  { "personId": "y5z6a7b8c9d0e1f2g3h4i5j6", "percentage": 50 }
]
```

Response `204 No Content` (no hay ninguna regla que matchee — body vacío).

---

## Errores comunes (todos los endpoints)

`GlobalExceptionHandler` devuelve siempre esta forma para cualquier excepción no capturada por el endpoint:

```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "Only the household Owner can register cards."
}
```

| Excepción | status | Cuándo pasa |
|---|---|---|
| `UnauthorizedException` | 401 | Login con credenciales inválidas |
| `NotFoundException` | 404 | Household/User/Card/StatementPeriod referenciado no existe |
| `ForbiddenException` | 403 | El usuario autenticado no tiene el rol requerido para la acción, **o** el recurso (`householdId`/`cardId`) no pertenece a su propio household (`HouseholdAccessGuard` — ver nota abajo) |
| `ApplicationValidationException` | 400 | Email duplicado, invitar a un segundo Owner, cuotas en tarjeta Debit, etc. |
| `DomainException` | 400 | Invariante de dominio violado (ej. `split` no suma 100) |
| (sin manejar) | 500 | Bug — se loguea server-side, no se expone el detalle real al cliente |

### HouseholdAccessGuard

Todo endpoint con alcance de household (`{householdId}` en la ruta, o un `cardId` que resuelve a uno vía la tarjeta) verifica que el usuario autenticado pertenezca a **ese** household exacto — no solo que tenga el rol correcto. Si no coincide, `403`:

```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "You do not have access to this household."
}
```

Esto aplica **aunque el usuario sea Owner** — de su propio household, no de cualquiera. Antes de este chequeo, un Owner podía leer/escribir datos de un household ajeno con solo mandar otro `householdId`/`cardId` — corregido en todos los endpoints que tocan `households`, `cards`, `transactions`, `split-rules`, `reconciliation`, `dashboard` y `alerts`.
