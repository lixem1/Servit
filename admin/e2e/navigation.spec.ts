import { test, expect } from '@playwright/test';
import { expectLoggedIn, gotoResource } from './helpers';

// Recorre cada entrada del menú y verifica que el recurso carga sin romperse.
test.describe('Navegación de recursos', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await expectLoggedIn(page);
  });

  // label del menú -> fragmento de ruta esperado
  const resources: Array<[string, string]> = [
    ['Usuarios', 'users'],
    ['Proveedores', 'providers'],
    ['Solicitudes', 'service-requests'],
    ['Reseñas', 'reviews'],
    ['Categorías', 'categories'],
    ['Auditoría', 'audit-logs'],
  ];
  for (const [label, path] of resources) {
    test(`${label}: el listado carga`, async ({ page }) => {
      await gotoResource(page, label);
      // La ruta cambió al recurso...
      await expect(page).toHaveURL(new RegExp(path));
      // ...y no explotó (sin pantalla de error de react-admin).
      await expect(page.getByText(/Algo salió mal|Something went wrong/i)).toHaveCount(0);
    });
  }

  test('Analítica: carga el embudo', async ({ page }) => {
    await gotoResource(page, 'Analítica');
    await expect(page.getByRole('heading', { name: /Embudo de conversión/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: /Tiempos \(SLA\)/i })).toBeVisible();
  });

  test('Mapa: renderiza el contenedor de leaflet', async ({ page }) => {
    await gotoResource(page, 'Mapa');
    await expect(page.locator('.leaflet-container')).toBeVisible();
  });

  test('Usuarios: la búsqueda filtra sin errores', async ({ page }) => {
    await gotoResource(page, 'Usuarios');
    await expect(page.getByRole('table').first()).toBeVisible();
    const search = page.getByRole('textbox', { name: 'Buscar' }).first();
    await search.fill('zzz-no-existe-e2e');
    // El listado sigue vivo (no crashea) tras aplicar el filtro.
    await expect(page.getByText(/Algo salió mal|Something went wrong/i)).toHaveCount(0);
  });
});
