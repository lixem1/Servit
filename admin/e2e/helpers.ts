import { Page, expect } from '@playwright/test';

export const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL || 'e2e-admin@servit.local';
export const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD || 'E2eAdmin!2026';

// Usuario no-admin registrado por global-setup para probar el rechazo de acceso.
export const NONADMIN_EMAIL = process.env.E2E_NONADMIN_EMAIL || 'e2e-nonadmin@servit.local';
export const NONADMIN_PASSWORD = process.env.E2E_NONADMIN_PASSWORD || 'E2eNonAdmin!2026';

export const AUTH_FILE = 'e2e/.auth/admin.json';

// Login por la UI real (formulario de react-admin). El label de usuario es "Email"
// y el de contraseña "Contraseña"; el botón dice "Entrar" (ver i18nProvider).
export async function loginViaUi(page: Page, email: string, password: string) {
  await page.goto('/');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Contraseña').fill(password);
  await page.getByRole('button', { name: 'Entrar' }).click();
}

// Espera a que el layout autenticado esté visible (menú lateral con "Usuarios").
export async function expectLoggedIn(page: Page) {
  await expect(page.getByRole('menuitem', { name: 'Usuarios' })).toBeVisible();
}

// Navega a un recurso por su etiqueta del menú lateral.
export async function gotoResource(page: Page, menuLabel: string) {
  await page.getByRole('menuitem', { name: menuLabel }).click();
}

// Navega directo a un listado por su ruta (hash router). Evita depender del click
// del menú lateral cuando una notificación transitoria podría interceptarlo.
export async function gotoList(page: Page, resourcePath: string) {
  await page.goto(`/#/${resourcePath}`);
  await expectLoggedIn(page);
}
