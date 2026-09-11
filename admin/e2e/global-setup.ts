import { request, FullConfig } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'fs';
import { dirname } from 'path';
import {
  ADMIN_EMAIL, ADMIN_PASSWORD,
  NONADMIN_EMAIL, NONADMIN_PASSWORD,
  AUTH_FILE,
} from './helpers';

// Corre una vez antes de la suite: (1) obtiene el token del admin sembrado y
// guarda un storageState reutilizable; (2) registra un usuario NO-admin para el
// test de rechazo de acceso. Requiere backend arriba (proxy /api del vite dev).
export default async function globalSetup(config: FullConfig) {
  const baseURL =
    config.projects.find((p) => p.use?.baseURL)?.use.baseURL ||
    process.env.E2E_BASE_URL ||
    'http://localhost:5173';

  const api = await request.newContext({ baseURL });

  // 1) Login del admin sembrado.
  const loginRes = await api.post('/api/auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  if (!loginRes.ok()) {
    throw new Error(
      `No pude iniciar sesión como admin (${ADMIN_EMAIL}) -> ${loginRes.status()}. ` +
        `Asegúrate de correr el backend con Admin__Email/Admin__Password sembrados y Postgres arriba.`,
    );
  }
  const auth = await loginRes.json();

  const meRes = await api.get('/api/admin/me', {
    headers: { Authorization: `Bearer ${auth.token}` },
  });
  if (meRes.status() !== 200) {
    throw new Error(`El usuario ${ADMIN_EMAIL} no tiene rol Admin (/api/admin/me -> ${meRes.status()}).`);
  }
  const identity = await meRes.json();

  // 2) Registrar un usuario no-admin (idempotente: 400 si ya existe).
  const regRes = await api.post('/api/auth/register', {
    data: {
      fullName: 'E2E NoAdmin',
      email: NONADMIN_EMAIL,
      password: NONADMIN_PASSWORD,
      role: 'Customer',
    },
  });
  if (!regRes.ok() && regRes.status() !== 400) {
    throw new Error(`No pude registrar el usuario no-admin (${regRes.status()}).`);
  }

  await api.dispose();

  // 3) storageState con el localStorage que lee react-admin al cargar.
  const state = {
    cookies: [],
    origins: [
      {
        origin: baseURL,
        localStorage: [
          { name: 'servit_token', value: auth.token },
          {
            name: 'servit_identity',
            value: JSON.stringify({
              id: identity.id,
              fullName: identity.fullName,
              email: identity.email,
              roles: identity.roles || [],
            }),
          },
        ],
      },
    ],
  };
  mkdirSync(dirname(AUTH_FILE), { recursive: true });
  writeFileSync(AUTH_FILE, JSON.stringify(state, null, 2));
}
