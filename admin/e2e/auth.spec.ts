import { test, expect } from '@playwright/test';
import {
  ADMIN_EMAIL, ADMIN_PASSWORD,
  NONADMIN_EMAIL, NONADMIN_PASSWORD,
  loginViaUi, expectLoggedIn,
} from './helpers';

test.describe('Autenticación del panel', () => {
  test('el admin inicia sesión y ve el panel', async ({ page }) => {
    await loginViaUi(page, ADMIN_EMAIL, ADMIN_PASSWORD);
    await expectLoggedIn(page);
    // El dashboard es la ruta por defecto: debe mostrar el botón de exportar CSV.
    await expect(page.getByRole('button', { name: /Exportar CSV/i })).toBeVisible();
  });

  test('un usuario no-admin es rechazado', async ({ page }) => {
    await loginViaUi(page, NONADMIN_EMAIL, NONADMIN_PASSWORD);
    // Debe permanecer en el login (no entra al panel).
    await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Usuarios' })).toHaveCount(0);
    // Y mostrar el error de acceso.
    await expect(page.getByText(/no tiene acceso de administrador/i)).toBeVisible();
  });

  test('credenciales inválidas no entran', async ({ page }) => {
    await loginViaUi(page, ADMIN_EMAIL, 'clave-incorrecta-123');
    await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Usuarios' })).toHaveCount(0);
  });
});
