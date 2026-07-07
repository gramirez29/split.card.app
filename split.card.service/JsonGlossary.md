# JsonGlossary

Diccionario de datos: cada propiedad JSON usada en los requests/responses de `split.card.service` (ver `RequestSamples.md` para ejemplos completos por endpoint), en orden alfabético. Nombres en camelCase — así los serializa Minimal API por default.

| Campo | Tipo JSON | Nullable | Dónde aparece | Descripción |
|---|---|---|---|---|
| `amount` | number | no | `RegisterTransactionRequest`, `TransactionResponse` | Monto total de la transacción, en la moneda indicada por `currency`. Si la compra es en cuotas, es el monto total de la compra, no el de una cuota individual (eso es `installmentAmount`). |
| `bank` | string | no | `CreateCardRequest`, `CardResponse` | Nombre del banco emisor de la tarjeta (ej. `"BAC Credomatic"`). Texto libre, sin validación de catálogo. |
| `cardId` | string | no | `RegisterTransactionRequest`, `TransactionResponse` | Id de la tarjeta (`Card.Id`) a la que pertenece la transacción. |
| `createdByUserId` | string | no | `TransactionResponse` | Id del usuario que registró la transacción (`ActingUserId` al momento de crearla, extraído del JWT). No es necesariamente uno de los `split[].personId` — alguien puede registrar una compra y asignársela 100% a otra persona. |
| `currency` | string (enum) | no | `RegisterTransactionRequest`, `TransactionResponse`, `InstallmentPlanRequestInput` (implícito, ver nota) | `"CRC"` \| `"USD"`. Moneda de la transacción. |
| `cutoffDay` | number | **sí**, si `type` es `"Debit"` | `CreateCardRequest`, `CardResponse` | Día del mes (1–31) en que corta el estado de cuenta. Obligatorio si `type` es `"Credit"`; debe ir `null` si es `"Debit"` — el backend rechaza la combinación contraria con 400. |
| `defaultSplit` | array de `PersonShare` | no | `CreateSplitRuleRequest`, `SplitRuleResponse` | Split por default que se sugiere cuando `descriptionPattern` matchea el comercio de una compra nueva. Mismo shape que `split` (ver abajo), misma regla de suma a 100. |
| `descriptionPattern` | string | no | `CreateSplitRuleRequest`, `SplitRuleResponse` | Texto contra el que se compara `merchant` (contains, case-insensitive) para sugerir `defaultSplit` automáticamente. Ej: `"Netflix"` matchea `"NETFLIX.COM"`. |
| `detail` | string | no | Respuestas de error (`GlobalExceptionHandler`) | Mensaje específico de la excepción — texto libre, no apto para mostrar tal cual al usuario final sin traducir/formatear. |
| `email` | string | no | `LoginRequest`, `InviteUserRequest`, `UserResponse` | Email del usuario. Único por instalación (`IUserRepository.GetByEmailAsync` lo valida al invitar/registrar). |
| `householdId` | string | no | Casi todos los contratos con alcance de hogar | Id del `Household` al que pertenece el recurso. En los endpoints de escritura vas por body; en los de lectura por segmento de ruta (`/api/households/{householdId}/...`). |
| `householdName` | string | no | `RegisterHouseholdRequest` | Nombre visible del hogar (ej. `"Familia Rodríguez"`). Solo se define una vez, al bootstrap — no hay endpoint para renombrarlo todavía. |
| `id` | string | no | Todas las Response de una entidad individual | Id generado por el backend (`Guid.NewGuid().ToString("N")`, no `ObjectId` de Mongo). Nunca lo generás vos del lado cliente. |
| `installmentAmount` | number | no (dentro de `installments`) | `InstallmentPlanRequestInput` | Monto de **cada** cuota individual. `amount` (de la transacción) sigue siendo el monto total de la compra — el backend no valida que `amount == installmentAmount * totalInstallments`, es responsabilidad del cliente calcularlo bien. |
| `installmentPlanId` | string | **sí** | `TransactionResponse` | Id del `InstallmentPlan` creado, o `null` si la compra no fue en cuotas (`installments` venía `null` en el request). |
| `installments` | object \| null | **sí** | `RegisterTransactionRequest` | `null` = compra sin cuotas (pago único). Si no es `null`, debe traer `totalInstallments` + `installmentAmount`. **Solo válido en tarjetas `type: "Credit"`** — si `cardId` apunta a una tarjeta `"Debit"` y `installments` no es `null`, el backend rechaza con 400 (`ApplicationValidationException`, mensaje: "Debit card purchases cannot be paid in installments..."). Una compra de contado no se paga en cuotas. |
| `merchant` | string | no | `RegisterTransactionRequest`, `TransactionResponse`; también query param en `/split-rules/suggest` | Nombre del comercio donde se hizo la compra, tal como aparece en el estado de cuenta. Es el campo contra el que matchea `descriptionPattern`. |
| `name` | string | no | `InviteUserRequest`, `UserResponse`, `CreateCardRequest`, `CardResponse` | Nombre visible — de la persona (`User.Name`) o de la tarjeta (`Card.Name`), según el contrato. |
| `ownerEmail` | string | no | `RegisterHouseholdRequest` | Email del primer Owner del hogar, creado atómicamente junto con el Household. |
| `ownerName` | string | no | `RegisterHouseholdRequest` | Nombre del primer Owner del hogar. |
| `ownerPassword` | string | no | `RegisterHouseholdRequest` | Password en texto plano — **solo en tránsito**, el backend lo hashea (PBKDF2) antes de guardarlo. Nunca se devuelve en ninguna response. |
| `ownerUserId` | string | no | `RegisterHouseholdResponse`, `CardResponse` | En `RegisterHouseholdResponse`: id del Owner recién creado. En `CardResponse`: id del usuario que registró la tarjeta (siempre un Owner, por la regla de autorización de `CreateCardCommandHandler`) — el nombre es un poco engañoso, no implica "dueño personal de la tarjeta física", es quien la administra en el sistema. |
| `password` | string | no | `LoginRequest`, `InviteUserRequest` | Password en texto plano, solo en tránsito (ver `ownerPassword`). |
| `paymentDueDay` | number | **sí**, si `type` es `"Debit"` | `CreateCardRequest`, `CardResponse` | Día del mes en que vence el pago del estado de cuenta. Misma regla de nullability que `cutoffDay`. |
| `percentage` | number | no | `PersonShareRequest`/`PersonShareResponse`, dentro de `split`/`defaultSplit` | Porcentaje (0 exclusivo–100 inclusivo) que le corresponde a `personId` de ese monto. La suma de todos los `percentage` dentro de un mismo array `split` debe dar exactamente `100`, o falla con 400. |
| `personId` | string | no | `PersonShareRequest`/`PersonShareResponse` | Id del `User` al que se le asigna ese `percentage` del split. No tiene que ser quien registró la transacción (`createdByUserId`). |
| `purchaseDate` | string (`yyyy-MM-dd`) | no | `RegisterTransactionRequest`, `TransactionResponse`; también query param `date` en `/cards/{cardId}/transactions` | Fecha en que se hizo la compra. El backend la usa para calcular a qué `StatementPeriod` pertenece (solo en tarjetas Credit) — mandar una fecha incorrecta corre la transacción de periodo silenciosamente, no hay validación contra "fecha futura" ni similar. |
| `role` | string (enum) | no | `InviteUserRequest`, `UserResponse` | `"Owner"` \| `"Contributor"` \| `"RestrictedViewer"`. En `InviteUserRequest` el backend rechaza `"Owner"` con 400 (`ApplicationValidationException`) — un segundo Owner no se puede crear por esta vía. |
| `split` | array de `PersonShare` | no | `RegisterTransactionRequest`, `TransactionResponse` | Cómo se reparte `amount` entre los miembros del hogar. Ver `percentage`/`personId`. Mínimo 1 elemento, sin `personId` duplicados. |
| `status` | number | no | Respuestas de error | Código HTTP numérico, redundante con el status code real de la respuesta — está en el body también para que el cliente no dependa de leer headers. |
| `title` | string | no | Respuestas de error | Categoría corta del error (`"Forbidden"`, `"Resource not found"`, etc.) — ver tabla de excepciones en `RequestSamples.md`. |
| `token` | string | no | `LoginResponse` | JWT firmado (HS256). Se manda como `Authorization: Bearer {token}` en cada request subsiguiente que requiera auth. Expira según `JWT_EXPIRATION_MINUTES` (default 7 días). |
| `totalInstallments` | number | no (dentro de `installments`) | `InstallmentPlanRequestInput` | Cantidad total de cuotas de la compra (ej. `6` para "6 cuotas"). |
| `type` | string (enum) | no | `CreateCardRequest`, `CardResponse` | `"Credit"` \| `"Debit"`. Determina si la tarjeta maneja `StatementPeriod`/cortes (`Credit`) y si acepta `installments` (`Credit` sí, `Debit` no — ver `installments`). |
| `userId` | string | no | `LoginResponse` | Id del usuario autenticado — mismo valor que devolvería `GET /api/auth/me` en su campo `id`. |

## Notas generales

- Los campos marcados **Nullable: sí** son los únicos que podés/debés mandar como `null` explícito en el JSON — omitir el campo por completo no es equivalente en todos los casos (depende del binding de Minimal API, pero mandar `null` explícito es siempre seguro).
- Ningún contrato de request incluye `actingUserId` — esa identidad sale del JWT (`Authorization: Bearer`), nunca del body. Si ves un ejemplo viejo (de antes de Auth) con ese campo, está desactualizado.
- Ningún contrato de response incluye `passwordHash` — verificado explícitamente en `UserContracts.cs` (`FromDomain` no lo mapea).
