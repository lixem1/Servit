import { test, expect } from '@playwright/test';
import { expectLoggedIn, gotoResource } from './helpers';

// Detalle de solicitud: timeline + acción de cambio de estado (flujo operativo núcleo).
test('Solicitudes: detalle con timeline y cambio de estado', async ({ page }) => {
  await page.goto('/');
  await expectLoggedIn(page);
  await gotoResource(page, 'Solicitudes');
  await expect(page.getByRole('table').first()).toBeVisible();

  const dataRows = page.getByRole('row');
  const count = await dataRows.count();
  // La primera fila es el encabezado; necesitamos al menos una solicitud.
  test.skip(count < 2, 'No hay solicitudes en la BD de prueba');

  // rowClick="show": abrir la primera solicitud.
  await dataRows.nth(1).click();

  // Detalle: la línea de tiempo y las acciones deben estar presentes.
  await expect(page.getByText('Línea de tiempo')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Cambiar estado' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Cancelar' })).toBeVisible();

  // Ejecutar el cambio de estado (usa window.prompt con default "Completed").
  page.once('dialog', (d) => d.accept('Completed'));
  await page.getByRole('button', { name: 'Cambiar estado' }).click();
  await expect(page.getByText(/Estado cambiado a Completed/i)).toBeVisible();
});
