# Servit — Backlog: Panel Administrativo Web

> Planificación del panel admin. Generado 2026-09-10 (rev. 2).
> **Decisiones:** Frontend = **React-Admin (SPA)** · HTTPS/acceso = **Cloudflare Tunnel** ·
> **Precios = edición de cotizaciones DESCARTADA** (los precios negociados no se editan desde el panel) ·
> **Verificación de proveedores = DIFERIDA** (pendiente de investigar cómo lo hacen otras plataformas).
> Infra: VM Oracle Ampere A1 (2 vCPU, 11 GB RAM, ~26 GB libre) — capacidad de sobra.

## Convenciones (todas las tareas)
- Endpoints admin bajo `/api/admin/...` con `[Authorize(Roles = "Admin")]`.
- Paginación, filtros y orden estándar en listados (formato compatible con el dataProvider de React-Admin).
- Toda mutación admin queda **auditada** (AP-03).
- DTOs en `Contracts/Admin`; nunca exponer entidades EF directas.
- CORS del API restringido al origen del panel (host de Cloudflare).

## Guía de diseño — qué es editable y qué no
- **Editable ✅:** categorías; estado de solicitudes (cancelar/marcar resuelta); estado de cuenta (suspender/restaurar/rol); moderación (eliminar reseñas abusivas); corrección puntual de datos de perfil por soporte (auditada).
- **NO editable ❌:** precio negociado de una cotización (si hay que corregir, anular/cancelar la solicitud); contenido de reseñas de terceros (solo eliminar); historial/timestamps.
- **Principio:** el admin edita catálogo/config, estado de cuenta y estado operativo, y **modera** contenido; no reescribe registros comerciales/transaccionales de terceros.

---

## Fase 0 — Fundaciones de seguridad (backend) · bloqueante

### AP-01 · Rol Admin + seed del primer administrador
- **Área:** backend
- Agregar `Admin` a `Roles.cs`. Sembrar un admin inicial desde config (`Admin:Email`, `Admin:Password`), creando el rol si falta. Idempotente.
- **Aceptación:** existe un usuario `Admin`; login estándar devuelve el rol; no duplica si ya existe.

### AP-02 · Autorización Admin + sesión
- **Área:** backend
- Política `[Authorize(Roles="Admin")]` reutilizable; proteger endpoints admin. `GET /api/admin/me` para validar sesión+rol desde el panel.
- **Aceptación:** no-Admin recibe 403 en `/api/admin/*`; `/admin/me` devuelve el admin autenticado.

### AP-03 · Log de auditoría
- **Área:** backend
- Entidad `AdminAuditLog` (adminUserId, action, entityType, entityId, metadata json, valorAnterior/Nuevo cuando aplique, createdAt) + migración + servicio. Registrar en cada mutación admin.
- **Aceptación:** cada acción admin genera una fila consultable.

---

## Fase 1 — API de administración (backend)

### AP-04 · Base: paginación/filtros/orden + errores
- **Área:** backend
- Helpers de paginado/orden/filtrado y formato `{ data, total }` (o `Content-Range`) que React-Admin consume.
- **Aceptación:** un listado responde en el formato del dataProvider.

### AP-05 · Usuarios (+ perfil 360)
- **Área:** backend
- `GET /admin/users` (buscar por nombre/email; filtrar rol/estado/fecha), `GET /admin/users/{id}` con **vista 360** (sus solicitudes, reseñas dadas/recibidas, actividad), `POST .../suspend|restore` (usa `DeletedAt`), `POST .../role`, `POST .../force-reset`.
- **Aceptación:** listar/buscar, ver 360, suspender/restaurar, cambiar rol, forzar reset; auditado.

### AP-06 · Proveedores (+ perfil 360) · sin verificación (diferida)
- **Área:** backend
- `GET /admin/providers` (rating, #categorías, #trabajos), `GET /admin/providers/{id}` con **360**: trabajos realizados, **tasa de aceptación**, **tiempo medio de respuesta**, rating, **GMV generado**. (La verificación se agrega cuando se retome — ver "Diferido".)
- **Aceptación:** listado + detalle 360 de proveedores.

### AP-07 · Categorías (CRUD)
- **Área:** backend
- `POST/PUT/DELETE` admin-guarded en Categories. Borrado seguro si hay asociaciones (bloquear con mensaje claro).
- **Aceptación:** CRUD completo con borrado seguro.

### AP-08 · Solicitudes + Historial/Búsqueda (núcleo operativo)
- **Área:** backend
- `GET /admin/service-requests` con **búsqueda global** y filtros (estado, categoría, rango de fechas, cliente, proveedor), y segmentos: **en cola** (Pending), **en curso** (Assigned), **realizadas** (Completed), **canceladas** (Cancelled). `GET /admin/service-requests/{id}` = **timeline completo** (creación → cotizaciones → asignación → cierre) + adjuntos + cotizaciones (solo lectura). `PATCH .../status`, `POST .../cancel`, `DELETE .../{id}`.
- **Aceptación:** buscar/filtrar todo el historial, ver detalle con timeline y adjuntos, cambiar estado/cancelar/eliminar; auditado.

### AP-09 · Reseñas (moderación)
- **Área:** backend
- `GET /admin/reviews` (filtrar proveedor/rating/fecha), `DELETE /admin/reviews/{id}` con recálculo de `AverageRating`/`RatingCount`.
- **Aceptación:** listar y eliminar reseñas; rating recalculado.

### AP-10 · Reportes / KPIs
- **Área:** backend
- `GET /admin/reports/summary` (usuarios totales/nuevos, proveedores, solicitudes por estado, tasa de completado, GMV = Σ `ProposedPrice` de completadas), `GET /admin/reports/timeseries`, `GET /admin/reports/top?dimension=providers|categories`, `GET /admin/reports/export.csv`.
- **Aceptación:** KPIs, series temporales, tops y CSV descargable.

### AP-11 · Analítica operativa (embudo · SLA · stale)
- **Área:** backend
- **Embudo:** solicitudes → cotizaciones → asignadas → completadas (con caída por etapa). **SLA/tiempos:** tiempo medio de 1ª respuesta y tiempo en cola. **Stale:** solicitudes Pending con 0 cotizaciones tras X horas.
- **Aceptación:** endpoints devuelven embudo, métricas de tiempo y lista de solicitudes sin respuesta.

### AP-12 · Vista geográfica (PostGIS)
- **Área:** backend
- `GET /admin/geo/requests` y `/admin/geo/providers` devolviendo lat/lon (desde los `Point` SRID 4326) con filtros por estado/categoría/fecha, para alimentar un mapa/heatmap de demanda.
- **Aceptación:** endpoints devuelven puntos geográficos filtrables.

---

## Fase 2 — Panel React-Admin (carpeta `admin/`)

### AP-13 · Scaffold + auth
- **Área:** frontend
- React-Admin (Vite + TS) en `admin/`. `dataProvider` a `/api/admin`; `authProvider` con login vía `/api/auth/login` que **rechaza si rol ≠ Admin**. Menú y textos en español, tema de marca.
- **Aceptación:** build genera estáticos; login admin ok; no-admin rechazado.

### AP-14 · Usuarios (+ perfil 360)
- **Área:** frontend — List/Show/Edit con filtros; acciones suspender/restaurar/rol/reset; pestaña 360.
- **Aceptación:** AP-05 disponible en UI, con vista 360.

### AP-15 · Proveedores (+ perfil 360)
- **Área:** frontend — List/Show con rating, categorías, métricas 360 (trabajos, aceptación, tiempo de respuesta, GMV).
- **Aceptación:** AP-06 en UI.

### AP-16 · Categorías (CRUD)
- **Área:** frontend — List/Create/Edit/Delete con borrado seguro.
- **Aceptación:** AP-07 en UI.

### AP-17 · Tablero de Historial/Operaciones (solicitudes)
- **Área:** frontend
- List con **búsqueda** y filtros; **segmentos**: en cola / en curso / realizadas / canceladas (tabs o filtros guardados). Show con **timeline**, visor de adjuntos (foto/video/audio), cotizaciones (solo lectura) y acciones estado/cancelar/eliminar.
- **Aceptación:** AP-08 en UI, con segmentos y timeline.

### AP-18 · Reseñas (moderación)
- **Área:** frontend — List con filtros + eliminar.
- **Aceptación:** AP-09 en UI.

### AP-19 · Dashboard de KPIs
- **Área:** frontend — tarjetas KPI + gráficos (recharts) + export CSV.
- **Aceptación:** AP-10 en UI.

### AP-20 · Analítica operativa (UI)
- **Área:** frontend — embudo, SLA/tiempos y lista de solicitudes stale (AP-11).
- **Aceptación:** vistas cargan datos reales.

### AP-21 · Mapa de demanda
- **Área:** frontend — mapa (react-leaflet u similar) con marcadores/heatmap desde AP-12; filtros por estado/categoría/fecha.
- **Aceptación:** mapa muestra solicitudes/proveedores filtrables.

### AP-22 · Visor de log de auditoría
- **Área:** frontend — List/Show del `AdminAuditLog` (AP-03): quién, qué, cuándo, valor anterior/nuevo.
- **Aceptación:** se puede rastrear cualquier acción admin.

### AP-23 · Pruebas E2E del panel (Playwright)
- **Área:** frontend/QA
- `@playwright/test` en `admin/` con flujos críticos: login admin (+ rechazo de no-admin), buscar/filtrar recursos, detalle de solicitud, cambio de estado, moderar reseña, dashboard. Screenshots/traces en fallo.
- **Aceptación:** `npx playwright test` corre en headless y cubre los flujos críticos.

---

## Fase 3 — Deploy en la VM Oracle · lo ejecuta el agente **release** (con tu aprobación)

### AP-24 · Build + contenedor nginx del panel
- **Área:** devops — Dockerfile multi-stage que compila `admin/` y lo sirve con nginx; `proxy_pass /api` al contenedor `api`. Añadir servicio `admin` al compose.
- **Aceptación:** `docker-compose up` sirve panel y `/api` vía nginx.

### AP-25 · Cloudflare Tunnel (HTTPS)
- **Área:** devops — contenedor `cloudflared` con token, hostname (dominio Cloudflare) → nginx; DNS; documentar secreto en `.env`.
- **Aceptación:** panel accesible por `https://<host>` con certificado válido; IP de la VM no expuesta.

### AP-26 · Endurecer CORS + secretos
- **Área:** backend/devops — CORS solo al origin del panel; credenciales admin y token Cloudflare en `.env`/user-secrets. (Opcional recomendado) **Cloudflare Access** delante del panel como 2ª capa.
- **Aceptación:** CORS restringido; secretos fuera de git; (si se elige) Access exige identidad.

### AP-27 · Runbook + PROJECT_STATUS
- **Área:** devops/docs — pasos de deploy/rollback + actualizar PROJECT_STATUS.
- **Aceptación:** runbook reproducible; PROJECT_STATUS al día.

---

## Diferido — pendiente de investigación
### Verificación de proveedores (KYC)
Antes de diseñar, **investigar cómo lo hacen InDrive/Uber/otras**: qué documentos piden (ID, antecedentes, selfie, certificaciones), el flujo (subida → cola de revisión → aprobado/rechazado → notificación), quién revisa y qué estados existen. Implica: campo(s) de verificación en `Provider` (`IsVerified`, `VerifiedAt`, estado), subida/almacenamiento de documentos, cola de revisión en el panel, y notificación al proveedor. **Retomar cuando esté la investigación.**

## Fase 4 — Opcional / post-MVP
- 2FA admin + expiración de sesión.
- Tests de integración de `/api/admin` (backend).
- Notificaciones/emails masivos desde el panel (reutiliza `IEmailSender`/SignalR).
- Tablero en tiempo real (SignalR): solicitudes activas, proveedores en línea, cotizaciones pendientes.

---

## Cómo pasarlo a los agentes
- Ejecutar **`servit-dev-qa-loop`** por **fase** (tope 8 tareas/corrida). Ej.: _"Implementar la Fase 0 del ADMIN_PANEL_BACKLOG.md (AP-01..AP-03)"_.
- El agente **QA** ya prueba **mobile (simulador iOS)** y **web (Playwright)**.
- **Deploy (Fase 3):** agente **release**, solo con tu aprobación explícita; nunca dentro del loop.
- **Orden:** Fase 0 → Fase 1 → Fase 2 → (aprobación) Fase 3. Verificación de proveedores: cuando termine la investigación.
