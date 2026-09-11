import { test, expect } from '@playwright/test';
import { expectLoggedIn } from './helpers';

// Dashboard: KPIs + export CSV. El dashboard es la ruta por defecto tras entrar.
test('Dashboard: KPIs, resumen y exportación CSV', async ({ page }) => {
  await page.goto('/');
  await expectLoggedIn(page);

  await expect(page.getByRole('heading', { name: 'Resumen' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Exportar CSV' })).toBeVisible();

  // El export dispara una descarga.
  const [download] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: 'Exportar CSV' }).click(),
  ]);
  expect(download.suggestedFilename()).toBe('servit-solicitudes.csv');
});
