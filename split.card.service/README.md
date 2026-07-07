# split.card.service

Backend .NET 9 (Clean Architecture) + MongoDB + Minimal API para SplitCard. Ver `claude.md` en este mismo repo para arquitectura, convenciones y estado real del proyecto — es la fuente de verdad, este README solo cubre el setup local.

## Estructura

```
split.card.service/
  SplitCard.sln
  Dockerfile
  docker-compose.yml           Mongo local dedicado a este proyecto (puerto 27018).
  railway.json
  src/
    Split.Card.Domain/          Entidades, enums, value objects. Cero dependencias externas.
    Split.Card.Application/     Commands/Queries/Handlers + interfaces de repositorio.
    Split.Card.Infrastructure/  MongoDB (documentos, mappings, repositorios, DI, password hashing).
    Split.Card.Api/             Program.cs, Minimal API endpoints, Swagger, JWT, health check.
  tests/
    Split.Card.Domain.Tests/
    Split.Card.Application.Tests/
```

## Base de datos local (Docker)

`docker-compose.yml` en la raíz de este repo levanta un Mongo **dedicado a SplitCard**, separado de cualquier otro Mongo local que tengas para otros proyectos (puerto `27018`, no `27017`, justamente para no chocar). No reutilices el Mongo de otro proyecto acá — cada repo debe poder levantar su propia infraestructura sin depender de que otro repo esté corriendo.

```bash
cd split.card.service
docker compose up -d
```

La base `split-card-dev-db` se crea sola al primer insert (comportamiento normal de Mongo, no hace falta crearla a mano). Para pararlo: `docker compose down` (agregá `-v` si además querés borrar el volumen y arrancar de cero).

## Variables de entorno requeridas

| Variable | Descripción |
|---|---|
| `MONGODB_CONNECTION_STRING` | Local: `mongodb://splitcard:splitcard_dev_only@localhost:27018` (ver `docker-compose.yml`). Producción: connection string de Mongo Atlas. |
| `MONGODB_DATABASE_NAME` | `split-card-dev-db` en local |
| `JWT_SECRET` | Clave simétrica para firmar/validar tokens JWT (mín. 32 caracteres) |

**En local**: copiá `src/Split.Card.Api/.env.example` a `src/Split.Card.Api/.env` y completá los valores reales. `Program.cs` carga ese archivo automáticamente al arrancar (vía `DotNetEnv`) — no hace falta exportarlas a mano en la terminal. `.env` está en `.gitignore` (patrón `*.env`), nunca se commitea.

**En Railway**: se configuran como variables del servicio directamente — no hay archivo `.env` en el contenedor, por lo que ese paso de carga es un no-op ahí.

## Cómo correrlo localmente

```bash
cd split.card.service
docker compose up -d                                          # Mongo local
cp src/Split.Card.Api/.env.example src/Split.Card.Api/.env    # y completar valores reales
dotnet restore
dotnet build
dotnet run --project src/Split.Card.Api
```

Swagger UI: `http://localhost:{puerto}/swagger`
Health check (ping real a Mongo, no solo el proceso vivo): `GET http://localhost:{puerto}/health`

## Tests

```bash
dotnet test
```
