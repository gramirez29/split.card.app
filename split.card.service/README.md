# SplitCard

Backend .NET 9 (Clean Architecture) + MongoDB para SplitCard. Ver `SplitCard.md` (documento del proyecto) para el contexto completo de dominio, permisos y funcionalidades.

## Ubicación

Este proyecto vive en:

```
D:\Repositories\Applications\split.card\split.card.service
```

## Estructura

```
split.card.service/
  SplitCard.sln
  Dockerfile
  railway.json
  src/
    Split.Card.Domain/          -> Entidades, enums, value objects. Cero dependencias externas.
    Split.Card.Application/     -> Interfaces de repositorio (abstracciones). Depende solo de Domain.
    Split.Card.Infrastructure/  -> Implementación MongoDB (documentos, mappings, repositorios, DI).
    Split.Card.Api/             -> Program.cs, JWT, health check.
```

## Variables de entorno requeridas

| Variable | Descripción |
|---|---|
| `MONGODB_CONNECTION_STRING` | Connection string de Mongo Atlas (mismo cluster free tier, base de datos separada de la barbería) |
| `MONGODB_DATABASE_NAME` | Nombre de la base de datos para SplitCard |
| `JWT_SECRET` | Clave simétrica para firmar/validar tokens JWT |

En local, se pueden definir en un archivo `.env` (no versionado) o exportarlas antes de correr:

```bash
export MONGODB_CONNECTION_STRING="mongodb+srv://..."
export MONGODB_DATABASE_NAME="splitcard"
export JWT_SECRET="una-clave-larga-y-aleatoria"
```

En Railway, se configuran como variables del servicio.

## Cómo correrlo localmente

```bash
cd split.card.service
dotnet restore
dotnet build
dotnet run --project src/Split.Card.Api
```

Verificación de salud (incluye ping real a Mongo, no solo el proceso):

```
GET http://localhost:5000/health
```

## Nota importante — no compilado ni probado en este entorno

Este esqueleto se generó en un sandbox sin acceso a red, por lo tanto:

- No se ejecutó `dotnet restore` (no hay descarga de paquetes NuGet).
- No se ejecutó `dotnet build` para verificar que compila.

**Antes de dar por bueno el esqueleto, correr `dotnet build` localmente y corregir cualquier error de compilación que aparezca** (lo más probable: versiones exactas de paquetes NuGet que hayan cambiado).

## Decisiones de diseño relevantes (resumen)

- **IDs**: strings generados con `Guid.NewGuid().ToString("N")` (ver `SplitCard.Domain.Common.IdGenerator`), no `ObjectId` de Mongo. Mantiene Domain/Application 100% libres de dependencias de Mongo.
- **DateOnly en Mongo**: el driver no lo serializa nativamente. Se registra un `DateOnlySerializer` custom (`SplitCard.Infrastructure.Persistence.Serializers`) que lo guarda como `DateTime` UTC a medianoche, para que los filtros `>=`/`<=` funcionen.
- **Cuotas**: `InstallmentPlan` no genera un registro por mes. `GetInstallmentNumberFor()` calcula matemáticamente, a partir de `FirstChargeDate`, si una cuota está activa en un periodo dado.
- **Cálculo de periodo de corte** (`Card.GetStatementPeriodFor`): es una primera versión heurística basada en día fijo de corte por mes, con clamp para meses cortos (ej. corte día 31 en febrero → día 28/29). **No validado contra casos reales de bancos costarricenses** — recomiendo escribir tests unitarios con las fechas de corte reales de tus 4 tarjetas antes de confiar en este cálculo para producción.
- **Autorización por Split[].PersonId**: implementada en `MongoTransactionRepository.GetVisibleToUserAsync` vía `ElemMatch`, no a nivel de aplicación — así el filtro ocurre en la query de Mongo, nunca se trae de más para luego filtrar en memoria.

## Pendiente para la siguiente fase (no incluido en este esqueleto)

- Capa de Application "real": Commands/Queries/Handlers que orquesten los repositorios (hoy solo existen las interfaces).
- Controllers de la API (hoy solo existe `/health`).
- Autenticación: endpoint de login que emita el JWT (hoy solo está configurada la validación del token).
- Generación de PDF de conciliación (QuestPDF).
- Lógica de negocio para "sugerir split por SplitRule" al registrar una transacción.
- App móvil (React Native + Expo).
