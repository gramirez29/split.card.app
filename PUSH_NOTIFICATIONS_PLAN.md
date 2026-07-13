# Push Notifications — plan (no construido todavía)

Estado actual: `GET /api/households/{householdId}/alerts` (`split.card.service`) calcula qué está por vencer (corte/pago próximo, cuotas activas en el pago que se viene) pero es **puramente in-app** — el usuario solo se entera si abre la app. Este documento es la lista concreta de lo que falta para que eso llegue como notificación push real al celular, sin necesidad de abrir la app.

No hay nada de esto implementado todavía. Se armó este plan para no tener que rediseñar desde cero cuando se priorice, y para no construirlo a medias repartido entre pedidos sueltos.

## 1. Mobile — registrar el dispositivo

- Agregar `expo-notifications` (`npx expo install expo-notifications`).
- Pedir permiso de notificaciones al usuario (`Notifications.requestPermissionsAsync()`) — en algún punto del onboarding o la primera vez que hay algo que notificar, no al abrir la app por primera vez sin contexto.
- Obtener el push token del dispositivo (`Notifications.getExpoPushTokenAsync({ projectId })` — el `projectId` sale de `app.json`'s `extra.eas.projectId`, que se genera al correr `eas init`, ver `DEPLOYMENT.md`).
- Mandar ese token al backend (endpoint nuevo, ver punto 2) cada vez que cambie o al loguearse.
- **Nota real de Expo Go vs. build standalone**: en Expo Go, `getExpoPushTokenAsync` funciona para pruebas locales, pero un push real y confiable requiere un build standalone (EAS Build) con las credenciales de FCM (Android) configuradas — ver `DEPLOYMENT.md`.

## 2. Backend — guardar el token

- Nuevo campo/colección: lo más simple es un array de `PushToken` (string) por `User`, ya que una persona puede tener el token de más de un dispositivo (celular nuevo, reinstaló la app, etc.) — no diseñado todavía, decidir si va en el mismo documento de `User` (Mongo permite arrays sin problema) o en una colección separada `push_tokens` con `userId` + `token` + `updatedAt` (mejor si se necesita hacer cleanup de tokens viejos/inválidos más adelante).
- Nuevo endpoint: `POST /api/users/me/push-token` (autenticado, `ActingUserId` sale del JWT como siempre) — `{ "token": "ExponentPushToken[...]" }`. Idempotente: si el token ya está registrado para ese usuario, no duplicar.
- Posiblemente un `DELETE /api/users/me/push-token` para logout (dejar de recibir push en ese dispositivo).

## 3. Backend — el disparador periódico

ASP.NET no tiene cron nativo. Dos caminos reales, no hay una opción "gratis":

**Opción A — `BackgroundService` dentro del mismo proceso**
- Un `IHostedService`/`BackgroundService` con un `PeriodicTimer` (ej. cada 6 horas) que recorre todos los households, llama a `GetHouseholdAlertsQueryHandler` para cada uno, y para lo que esté "nuevo" desde la última corrida, dispara el push.
- Simple de implementar, cero infraestructura nueva.
- **Riesgo real**: si Railway duerme el servicio por inactividad (depende del plan) o lo reinicia, el timer se resetea y se puede perder una ventana. Para un household hogareño con pocos usuarios esto probablemente no importa, pero es honesto decirlo.
- Necesita lógica de "no mandar el mismo push dos veces" — guardar cuándo fue la última alerta enviada por transacción/tarjeta (otro campo/colección nuevo).

**Opción B — Cron Job separado + endpoint interno**
- Railway soporta servicios tipo "Cron Job" (se configuran en el dashboard de Railway, corren un comando en un schedule, no es parte del servicio web).
- Ese cron pega a un endpoint nuevo tipo `POST /api/internal/run-alerts`, protegido con un secreto compartido (header `X-Internal-Secret` o similar, **no** JWT normal, ya que no hay un usuario logueado disparando esto) — variable de entorno nueva en Railway.
- Más robusto que la Opción A (no depende de que el proceso web esté despierto en el momento exacto), pero es un servicio más para mantener.

No hay una recomendación fuerte todavía — depende de cuánta confiabilidad se necesita. Para un MVP con pocos usuarios, Opción A alcanza.

## 4. Backend — mandar el push

- Una llamada POST simple a la API de Expo: `https://exp.host/--/api/v2/push/send`, body con `to` (el push token), `title`, `body`, y opcionalmente `data` (para que la app abra una pantalla específica al tocar la notificación).
- Gratis, sin necesidad de cuenta paga de Expo para el volumen que este proyecto va a tener.
- **Pero solo confiable en un build standalone** con credenciales de Firebase Cloud Messaging (Android) configuradas en el proyecto de EAS — en Expo Go el push puede fallar o no llegar de forma consistente, es una herramienta de desarrollo, no de producción.

## 5. Decisiones de producto pendientes (no técnicas, pero bloquean el diseño final)

- ¿Con qué frecuencia se manda un push por la misma alerta? (ej. "corte en 5 días" y de nuevo "corte en 1 día", o solo una vez).
- ¿Hay alguna preferencia de horario para no mandar notificaciones de madrugada?
- ¿El usuario puede desactivar las notificaciones por tipo (cuotas vs. corte/pago) o es todo o nada?

## Orden sugerido si se prioriza

1. Backend: endpoint de registro de token (punto 2) — sin esto no hay nada que hacer del lado mobile tampoco.
2. Mobile: `expo-notifications` + registro del token (punto 1).
3. Backend: Opción A (`BackgroundService`) para no bloquear en decidir infraestructura de cron — se puede migrar a Opción B después sin romper nada del lado mobile.
4. Mobile: manejar el tap en la notificación (deep link a la tarjeta o compra relevante) — no mencionado arriba, pero es lo que hace que la notificación sea útil y no solo un aviso genérico.
