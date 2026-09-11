import { test, expect } from '@playwright/test';
import { expectLoggedIn, gotoResource } from './helpers';

// CRUD completo de categorías (ruta de escritura no destructiva sobre datos núcleo).
// Aprovechamos el flujo natural de react-admin: <Create> redirige a la vista edit del
// nuevo registro y <Edit> redirige al listado al guardar (sin navegación manual).
test('Categorías: crear, editar y eliminar', async ({ page }) => {
  const stamp = Date.now();
  const name = `E2E Cat ${stamp}`;
  const renamed = `${name} editada`;

  await page.goto('/');
  await expectLoggedIn(page);
  await gotoResource(page, 'Categorías');
  await expect(page.getByRole('table').first()).toBeVisible();

  // Crear -> react-admin redirige a la vista edit del nuevo registro.
  await page.getByRole('link', { name: /Crear/i }).first().click();
  await page.getByRole('textbox', { name: /Nombre/i }).fill(name);
  await page.getByRole('textbox', { name: /Descripción/i }).fill('Creada por E2E');
  await page.getByRole('button', { name: 'Guardar' }).click();

  // Esperar a que la vista edit haya montado (su toolbar tiene "Eliminar", el create no).
  await expect(page.getByRole('button', { name: 'Eliminar' })).toBeVisible();
  const nameInput = page.getByRole('textbox', { name: /Nombre/i });
  await expect(nameInput).toHaveValue(name); // persistido

  // Editar y guardar -> <Edit> redirige al listado.
  await nameInput.fill(renamed);
  await expect(nameInput).toHaveValue(renamed);
  await expect(page.getByRole('button', { name: 'Guardar' })).toBeEnabled();
  await page.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('table').first()).toBeVisible();
  await page.getByRole('textbox', { name: 'Buscar' }).fill(renamed);
  await expect(page.getByRole('cell', { name: renamed })).toBeVisible();

  // Eliminar (selección + acción masiva)
  const row = page.getByRole('row', { name: new RegExp(renamed) });
  await row.getByRole('checkbox').check();
  await page.getByRole('button', { name: /Eliminar/i }).click();
  const confirm = page.getByRole('button', { name: 'Confirmar' });
  if (await confirm.isVisible().catch(() => false)) {
    await confirm.click();
  }
  await expect(page.getByRole('cell', { name: renamed })).toHaveCount(0);
});
