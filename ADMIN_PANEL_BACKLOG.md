# Servit — Backlog: Panel Administrativo Web

> Documento de planificación para el panel admin. Generado el 2026-09-10.
> Decisiones tomadas: **Frontend = React-Admin (SPA)** · **HTTPS/acceso = Cloudflare Tunnel** ·
> **Precios = editar cotizaciones individuales** (`ProviderResponse.ProposedPrice`; sin comisión de plataforma ni precio por categoría).
> Infra: VM Oracle Ampere A1 (2 vCPU, 11 GB RAM, ~26 GB disco libre) — capacidad de sobra.

## Convenciones (aplican a todas las tareas)
- Endpoints admin bajo `/api/admin/...`, todos con `[Authorize(Roles = "Admin")]`.
- Paginación, filtros y ordenamiento estándar en todos los listados (query params: `page`, `pageSize`, `sort`, filtros por campo).
- Toda mutación admin (borrar, cambiar estado, editar precio, cambiar rol, verificar) queda **auditada** (AP-03).
- DTOs dedicados en `Contracts/Admin`; nunca exponer entidades EF directas.
- CORS del API restringido al origen del panel (host de Cloudflare), no `AllowAnyOrigin`.

---

## Fase 0 — Fundaciones de seguridad (backend)
_Bloqueante del resto. Sin esto no hay admin._

### AP-01 · Rol Admin + seed del primer administrador
- **Área:** backend
- **Descripción:** Agregar `Admin` a `Roles.cs`. Sembrar (seed) en el arranque un usuario admin inicial tomando credenciales de configuración (`Admin:Email`, `Admin:Password` vía user-secrets/.env), creando el rol si no existe. Idempotente.
- **Aceptación:** al levantar el API con las vars, existe un usuario con rol `Admin`; login estándar devuelve el rol `Admin` en el token. No se crea nada si ya existe.

### AP-02 · Autorización Admin y guardas
- **Área:** backend
- **Descripción:** Política/atributo `[Authorize(Roles="Admin")]` reutilizable. Asegurar que los endpoints existentes (Categories write, etc.) que pasen a admin queden protegidos. Endpoint `GET /api/admin/me` para que el panel valide sesión+rol.
- **Aceptación:** un token no-Admin recibe 403 en cualquier `/api/admin/*`; `GET /api/admin/me` devuelve datos del admin autenticado.

### AP-03 · Log de auditoría
- **Área:** backend
- **Descripción:** Entidad `AdminAuditLog` (id, adminUserId, action, entityType, entityId, metadata json, createdAt) + migración + servicio para registrar acciones. Escribir un registro en cada mutación admin.
- **Aceptación:** cada acción admin (AP-06..AP-12) genera una fila de auditoría consultable.

### AP-04 · Campo de verificación en Provider
- **Área:** backend
- **Descripción:** Agregar `IsVerified` (bool, default false) y `VerifiedAt` (nullable) a `Provider` + migración.
- **Aceptación:** migración aplica sin romper datos; campos disponibles vía API.

---

## Fase 1 — API de administración (backend)
_Cada tarea = un grupo de endpoints bajo `/api/admin`._

### AP-05 · Base: paginación, filtros y manejo de errores admin
- **Área:** backend
- **Descripción:** Helpers de paginado/orden/filtrado y formato de respuesta (`{ data, total }`) que React-Admin espera (headers `Content-Range` o payload equivalente). Manejo de errores consistente.
- **Aceptación:** un listado de prueba responde en el formato que consume el dataProvider de React-Admin.

### AP-06 · Usuarios
- **Área:** backend
- **Descripción:** `GET /admin/users` (buscar por nombre/email, filtrar por rol/estado/fecha), `GET /admin/users/{id}`, `POST /admin/users/{id}/suspend` y `/restore` (usa `DeletedAt`), `POST /admin/users/{id}/role`, `POST /admin/users/{id}/force-reset`.
- **Aceptación:** se puede listar, ver, suspender/restaurar, cambiar rol y forzar reset; todo auditado.

### AP-07 · Proveedores
- **Área:** backend
- **Descripción:** `GET /admin/providers` (con rating, #categorías, verificado), `GET /admin/providers/{id}`, `POST /admin/providers/{id}/verify` y `/unverify` (setea AP-04).
- **Aceptación:** listado/detalle de proveedores + alternar verificación (auditado).

### AP-08 · Categorías (CRUD)
- **Área:** backend
- **Descripción:** Extender Categories con `POST/PUT/DELETE` admin-guarded. Impedir borrar categoría con proveedores/solicitudes asociadas (o soft-guard con mensaje claro).
- **Aceptación:** CRUD completo de categorías desde admin; borrado seguro.

### AP-09 · Solicitudes de servicio
- **Área:** backend
- **Descripción:** `GET /admin/service-requests` (filtrar por estado/categoría/fecha/cliente), `GET /admin/service-requests/{id}` (incluye adjuntos + respuestas), `PATCH .../status`, `POST .../cancel`, `DELETE .../{id}`.
- **Aceptación:** listar/filtrar, ver detalle con adjuntos y cotizaciones, cambiar estado, cancelar y eliminar; auditado.

### AP-10 · Cotizaciones / precios
- **Área:** backend
- **Descripción:** `GET /admin/service-requests/{id}/responses`, `PATCH /admin/responses/{id}` para editar `ProposedPrice` (y opcionalmente `Status`). Validación de monto ≥ 0.
- **Aceptación:** el admin puede ver y **editar el precio de una cotización** puntual; queda auditado con valor anterior/nuevo.

### AP-11 · Reseñas (moderación)
- **Área:** backend
- **Descripción:** `GET /admin/reviews` (filtrar por proveedor/rating/fecha), `DELETE /admin/reviews/{id}`. Al borrar, recalcular `AverageRating`/`RatingCount` del proveedor.
- **Aceptación:** se listan y eliminan reseñas; el rating del proveedor se recalcula correctamente.

### AP-12 · Reportes / analítica
- **Área:** backend
- **Descripción:** `GET /admin/reports/summary` (KPIs: usuarios totales/nuevos, proveedores, solicitudes por estado, tasa de completado, GMV = Σ `ProposedPrice` de solicitudes completadas), `GET /admin/reports/timeseries?metric=...&from=...&to=...`, `GET /admin/reports/top?dimension=providers|categories`, y export `GET /admin/reports/export.csv`.
- **Aceptación:** endpoints devuelven KPIs, series temporales, tops y CSV descargable.

---

## Fase 2 — Panel React-Admin (frontend, carpeta `admin/`)

### AP-13 · Scaffold del panel
- **Área:** frontend
- **Descripción:** App React-Admin con Vite + TypeScript en `admin/`. `dataProvider` REST apuntando a `/api/admin`, `authProvider` con login vía `/api/auth/login` que **rechaza si el rol ≠ Admin**. Menú en español, tema básico de marca.
- **Aceptación:** `npm run build` genera estáticos; login admin funciona; no-admin es rechazado.

### AP-14 · Recurso Usuarios
- **Área:** frontend
- **Descripción:** List (filtros por rol/estado/búsqueda), Show, Edit; acciones suspender/restaurar, cambiar rol, forzar reset.
- **Aceptación:** operaciones de AP-06 disponibles desde la UI.

### AP-15 · Recurso Proveedores
- **Área:** frontend
- **Descripción:** List/Show con rating y categorías; toggle Verificar/Quitar verificación.
- **Aceptación:** operaciones de AP-07 desde la UI.

### AP-16 · Recurso Categorías (CRUD)
- **Área:** frontend
- **Descripción:** List/Create/Edit/Delete de categorías.
- **Aceptación:** CRUD de AP-08 desde la UI, con mensajes de borrado seguro.

### AP-17 · Recurso Solicitudes
- **Área:** frontend
- **Descripción:** List con filtros por estado/categoría/fecha; Show con visor de adjuntos (foto/video/audio), lista de cotizaciones, y acciones cambiar estado/cancelar/eliminar.
- **Aceptación:** operaciones de AP-09 desde la UI, con visor de adjuntos.

### AP-18 · Edición de cotizaciones (precios)
- **Área:** frontend
- **Descripción:** Dentro del detalle de solicitud, editar `ProposedPrice` de una cotización (AP-10).
- **Aceptación:** el admin edita un precio y ve el cambio reflejado.

### AP-19 · Recurso Reseñas
- **Área:** frontend
- **Descripción:** List con filtros; acción eliminar reseña.
- **Aceptación:** moderación de AP-11 desde la UI.

### AP-20 · Dashboard de reportes
- **Área:** frontend
- **Descripción:** Página inicial con tarjetas de KPIs, gráficos (recharts) de series temporales y tops, y botones de export CSV (AP-12).
- **Aceptación:** dashboard carga KPIs y gráficos reales del backend; export descarga CSV.

---

## Fase 3 — Deploy en la VM Oracle (nginx + Cloudflare Tunnel)
_Ejecuta el **agente release**, siempre con tu aprobación explícita._

### AP-21 · Build + contenedor nginx del panel
- **Área:** devops
- **Descripción:** Dockerfile multi-stage que compila el SPA de `admin/` y lo sirve con nginx; nginx hace `proxy_pass /api` al contenedor `api`. Añadir servicio `admin` al `docker-compose.yml`.
- **Aceptación:** `docker-compose up` sirve el panel y el `/api` responde a través de nginx.

### AP-22 · Cloudflare Tunnel (HTTPS)
- **Área:** devops
- **Descripción:** Contenedor `cloudflared` con token, mapeando un hostname (dominio en Cloudflare) → nginx. DNS configurado. Documentar creación del túnel y secreto en `.env`.
- **Aceptación:** el panel es accesible por `https://<host>` con certificado válido; la IP de la VM no se expone directamente.

### AP-23 · Endurecer CORS + secretos
- **Área:** backend/devops
- **Descripción:** Restringir CORS del API al origin del panel. Mover credenciales admin y token de Cloudflare a `.env`/user-secrets. (Opcional recomendado) poner **Cloudflare Access** delante del panel como 2ª capa de login.
- **Aceptación:** CORS solo permite el host del panel; secretos fuera de git; (si se elige) Access exige identidad antes de llegar al panel.

### AP-24 · Runbook + PROJECT_STATUS
- **Área:** devops/docs
- **Descripción:** Documentar pasos de deploy/rollback del panel y actualizar `PROJECT_STATUS.md`.
- **Aceptación:** runbook reproducible; PROJECT_STATUS refleja el nuevo componente.

---

## Fase 4 — Endurecimiento (opcional, post-MVP)
- **AP-25 · 2FA para admin** + expiración de sesión.
- **AP-26 · Tests de integración** de los endpoints `/api/admin` (backend).
- **AP-27 · E2E del panel con Playwright** (nota: el agente QA hoy usa simulador iOS/Flutter; para web hay que habilitarle Playwright — ver nota abajo).
- **AP-28 · Notificaciones/emails masivos** desde el panel (reutiliza `IEmailSender`/SignalR).

---

## Cómo pasarlo a los agentes
- Ejecutar el workflow **`servit-dev-qa-loop`** por **fase** (el workflow topa en 8 tareas por corrida): primero Fase 0, luego Fase 1, etc. Ejemplo de objetivo: _"Implementar la Fase 0 del ADMIN_PANEL_BACKLOG.md (AP-01..AP-04)"_.
- **QA del panel web:** el agente QA está configurado para simulador iOS (Flutter). Para probar el panel React hay que ampliarlo a **Playwright** (browser E2E). Hasta entonces, QA del panel = build + checks + revisión manual.
- **Deploy (Fase 3):** lo hace el agente **release** únicamente con tu aprobación; nunca dentro del loop.
- **Orden recomendado:** Fase 0 → Fase 1 → Fase 2 → (aprobación) Fase 3.
