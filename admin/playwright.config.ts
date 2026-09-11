import { defineConfig, devices } from '@playwright/test';

// E2E del panel admin (AP-23).
// Requisitos para correr: backend en http://localhost:5202 con un admin sembrado
// (Admin__Email / Admin__Password) y Postgres arriba. El frontend (vite dev) lo
// levanta este config automáticamente. Credenciales configurables por env:
//   E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD  (admin sembrado en el backend)
// El global-setup obtiene el token del admin y registra un usuario NO-admin
// para probar el rechazo de acceso.
const BASE_URL = process.env.E2E_BASE_URL || 'http://localhost:5173';

export default defineConfig({
  testDir: './e2e',
  // Serie: comparten la BD de desarrollo y el backend aplica rate-limit al login.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: 0,
  timeout: 30_000,
  expect: { timeout: 10_000 },
  globalSetup: './e2e/global-setup.ts',
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: BASE_URL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [
    {
      // Flujos sin sesión: login admin real + rechazo de no-admin.
      name: 'guest',
      testMatch: /auth\.spec\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    {
      // Flujos autenticados: reutilizan el storageState del admin (sin re-login UI).
      name: 'authed',
      testMatch: /(navigation|categories|requests|dashboard)\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], storageState: 'e2e/.auth/admin.json' },
    },
  ],
  webServer: {
    command: 'npm run dev',
    url: BASE_URL,
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
