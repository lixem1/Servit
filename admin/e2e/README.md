# Pruebas E2E del panel (AP-23 · Playwright)

Flujos críticos del panel admin cubiertos con `@playwright/test` (headless, Chromium):

- **auth.spec.ts** — login del admin (entra al panel), rechazo de un usuario no-admin, y rechazo de credenciales inválidas.
- **navigation.spec.ts** — cada recurso del menú carga sin romperse (Usuarios, Proveedores, Solicitudes, Reseñas, Categorías, Auditoría), el embudo de Analítica, el mapa (leaflet) y la búsqueda de Usuarios.
- **categories.spec.ts** — CRUD completo de categorías (crear → editar → eliminar).
- **requests.spec.ts** — detalle de solicitud con línea de tiempo y **cambio de estado** real.
- **dashboard.spec.ts** — KPIs, "Resumen" y **exportación CSV** (descarga).

## Requisitos para correr

1. **Postgres** arriba (misma BD de desarrollo del backend).
2. **Backend** en `http://localhost:5202` con un **admin sembrado** y, para evitar el
   rate-limit de login durante la suite, con el rate-limiting deshabilitado:

   ```bash
   cd backend/src/Servit.Api
   ASPNETCORE_ENVIRONMENT=Development \
   ASPNETCORE_URLS=http://localhost:5202 \
   Admin__Email=e2e-admin@servit.local \
   Admin__Password='E2eAdmin!2026' \
   IpRateLimit__EnableEndpointRateLimiting=false \
   dotnet run --no-launch-profile
   ```

3. El **frontend** lo levanta Playwright automáticamente (`npm run dev`, `reuseExistingServer`).

Luego, desde `admin/`:

```bash
E2E_ADMIN_EMAIL=e2e-admin@servit.local \
E2E_ADMIN_PASSWORD='E2eAdmin!2026' \
npm run test:e2e
```

`global-setup.ts` obtiene el token del admin (guarda `e2e/.auth/admin.json`) y registra
un usuario no-admin (`e2e-nonadmin@servit.local`) para el test de rechazo. Credenciales
configurables por env: `E2E_ADMIN_EMAIL/PASSWORD`, `E2E_NONADMIN_EMAIL/PASSWORD`,
`E2E_BASE_URL`.

## Notas

- Efectos sobre la BD: `categories.spec` crea/edita/elimina su propia categoría (limpio);
  `requests.spec` **cambia el estado** de una solicitud existente a `Completed` (idempotente
  al re-correr). Tras probar en la BD de desarrollo, borra los usuarios de prueba y cualquier
  `Categories` con nombre `E2E Cat%`.
- Artefactos (`playwright-report/`, `test-results/`, `e2e/.auth/`) están en `.gitignore`.
- El navegador se instala una vez con `npx playwright install chromium`.
