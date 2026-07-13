# Deployment — checklist Railway (backend) + EAS (mobile APK de testing)

Este documento es un checklist de lo que tiene que estar verdaderamente configurado para que:
1. El backend quede publicado en Railway y accesible desde internet.
2. Se pueda generar un APK instalable en tu celular (sin Play Store) que consuma ese backend de Railway.
3. Puedas correr la app desde el emulador de Android Studio contra Railway **o** contra tu backend local, según necesites.

Nada de esto se puede verificar ni ejecutar completamente desde acá — varios pasos requieren login interactivo (OAuth de Expo, dashboard de Railway, dashboard de GitHub) que solo vos podés hacer. Lo que sigue es exactamente qué falta confirmar/hacer en cada uno.

---

## 1. Railway (backend) — checklist

El mecanismo ya existe en código (verificado en el repo):
- `split.card.service/Dockerfile` — build multi-stage, escucha en `:8080`.
- `split.card.service/railway.json` — le dice a Railway que use ese Dockerfile, healthcheck en `/health`.
- `.github/workflows/service-ci.yml` — job `deploy` que corre `railway up` después de que build+test pasen, **pero solo en push a la rama `develop`** (no `master`/`main`).

Lo que necesitás confirmar/hacer vos, en el dashboard de Railway y de GitHub (no accesible desde acá):

- [ ] **¿Existe el servicio en Railway?** Si no, creá un proyecto nuevo → "Deploy from GitHub repo" → apuntá a este repo, con el root directory en `split.card.service` (o dejá que Railway lo detecte por `railway.json`).
- [ ] **Variables de entorno del servicio en Railway** (Settings → Variables): `MONGODB_CONNECTION_STRING` (connection string real de Mongo Atlas — el Docker local de `docker-compose.yml` **no** sirve para producción), `MONGODB_DATABASE_NAME`, `JWT_SECRET` (mínimo 32 caracteres, generá uno nuevo, no reutilices el de local). Opcional: `JWT_EXPIRATION_MINUTES`.
- [ ] **¿Hay un cluster de Mongo Atlas creado?** Si todavía no, es necesario antes que nada — Railway no provee Mongo administrado nativo, hay que usar Atlas (tier gratuito alcanza para esto) o un plugin de Mongo de Railway si preferís todo en la misma plataforma.
- [ ] **Dominio público**: Settings → Networking → "Generate Domain" si todavía no tiene uno. Este es el `https://...up.railway.app` que necesitás para el paso de EAS más abajo.
- [ ] **GitHub — secret `RAILWAY_TOKEN`**: Settings del repo → Environments → `develop` → Secrets → `RAILWAY_TOKEN` (se genera en Railway: Account Settings → Tokens, o `railway login` + `railway whoami` para confirmar que tu CLI está autenticado, el token de CI es uno *project-scoped*, no tu token personal).
- [ ] **GitHub — variable `RAILWAY_SERVICE_NAME`**: mismo Environment `develop` → Variables → el nombre exacto del servicio tal como aparece en Railway.
- [ ] **Confirmá que estás pusheando a la rama `develop`** para que el deploy dispare — si tu flujo de trabajo usa otra rama como principal, hay que ajustar el `if:` del workflow (`.github/workflows/service-ci.yml`).
- [ ] Una vez desplegado, probá `GET https://{tu-dominio-railway}/health` — debe devolver `{ "status": "healthy", "mongo": "connected", ... }`. Si da 503, es casi seguro `MONGODB_CONNECTION_STRING` mal configurado.

---

## 2. EAS (mobile) — generar un APK de testing

`split.card.mobile/eas.json` ya existe con 3 perfiles (`development`, `preview`, `production`). El que te interesa para instalar en tu celular sin pasar por Play Store es **`preview`** (`distribution: internal`, `buildType: apk`).

**Pasos que tenés que correr vos** (requieren tu cuenta de Expo, login interactivo):

```bash
cd split.card.mobile
npx eas-cli login                    # tu cuenta de Expo (creála en expo.dev si no tenés)
npx eas-cli init                     # crea el proyecto en EAS, escribe extra.eas.projectId en app.json automáticamente
```

- [ ] **Reemplazar el placeholder en `eas.json`**: los perfiles `preview` y `production` tienen `"EXPO_PUBLIC_API_URL": "https://REPLACE-WITH-YOUR-RAILWAY-PUBLIC-URL"` — cambiá eso por el dominio real de Railway del paso 1 (`https://{tu-dominio-railway}`, sin barra al final).
- [ ] **Android package**: ya está seteado en `app.json` (`com.splitcard.mobile`) — no hace falta tocarlo para un build interno de testing.

Con eso, generar el APK:

```bash
npx eas-cli build --profile preview --platform android
```

Esto sube el build a los servidores de EAS (gratis dentro de límites razonables de uso), y al terminar te da un link para descargar el `.apk` directo (también podés escanear el QR que te muestra la terminal). Instalalo en tu celular — Android puede pedir habilitar "instalar apps de fuentes desconocidas" la primera vez, es normal para un APK que no viene de Play Store.

**Este APK va a pegarle a Railway, no a tu backend local** — es justamente para lo que sirve el perfil `preview` con el `EXPO_PUBLIC_API_URL` de Railway ya "horneado" en el build.

---

## 3. Emulador de Android Studio — contra Railway o contra local

No hace falta un build de EAS para esto — alcanza con `npx expo start` + Expo Go (o un dev client si en algún momento se agrega un módulo nativo que Expo Go no soporte, no es el caso hoy).

- **Contra tu backend local**: `split.card.mobile/.env` con `EXPO_PUBLIC_API_URL=http://10.0.2.2:{puerto}` (`10.0.2.2` es el alias que el emulador de Android usa para "la máquina host", no `localhost`) — ya documentado en `split.card.mobile/claude.md`.
- **Contra Railway**: mismo `.env`, `EXPO_PUBLIC_API_URL=https://{tu-dominio-railway}` — sin necesidad de tocar nada de red especial, es una URL pública normal, el emulador tiene acceso a internet igual que el celular.

Después de cambiar `.env`, siempre:

```bash
npx expo start -c
```

El `-c` limpia el caché de Metro — los `EXPO_PUBLIC_*` se inyectan al bundle en build time, un reload normal no siempre los vuelve a leer.

---

## Resumen — qué falta que decidas/confirmes vos (no lo puedo hacer desde acá)

1. Nombre/existencia real del servicio y proyecto en Railway.
2. Si ya existe un cluster de Mongo Atlas para producción, o si hay que crear uno.
3. Los 3 valores de env var reales en Railway (`MONGODB_CONNECTION_STRING`, `MONGODB_DATABASE_NAME`, `JWT_SECRET`).
4. Los secrets/vars de GitHub (`RAILWAY_TOKEN`, `RAILWAY_SERVICE_NAME`) en el Environment `develop`.
5. El dominio público real de Railway, para pegarlo en `eas.json` (reemplazando el placeholder) y en `.env` de mobile.
6. Login de EAS con tu cuenta de Expo + `eas init` (interactivo, no lo puedo correr por vos).
