import { AuthProvider } from 'react-admin';

const API_URL = import.meta.env.VITE_API_URL || '/api/admin';
const AUTH_URL = import.meta.env.VITE_AUTH_URL || '/api/auth';

// Login via the standard /api/auth/login, then confirm Admin role by hitting
// /api/admin/me (200 only for admins). Non-admins are rejected.
export const authProvider: AuthProvider = {
  login: async ({ username, password }) => {
    const res = await fetch(`${AUTH_URL}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: username, password }),
    });
    if (!res.ok) throw new Error('Credenciales inválidas');
    const auth = await res.json();

    const me = await fetch(`${API_URL}/me`, {
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    if (me.status !== 200) throw new Error('Esta cuenta no tiene acceso de administrador');
    const identity = await me.json();

    localStorage.setItem('servit_token', auth.token);
    localStorage.setItem(
      'servit_identity',
      JSON.stringify({
        id: identity.id,
        fullName: identity.fullName,
        email: identity.email,
        roles: identity.roles || [],
      }),
    );
  },

  logout: () => {
    localStorage.removeItem('servit_token');
    localStorage.removeItem('servit_identity');
    return Promise.resolve();
  },

  checkAuth: () =>
    localStorage.getItem('servit_token') ? Promise.resolve() : Promise.reject(),

  checkError: (error) => {
    const status = error?.status;
    if (status === 401 || status === 403) {
      localStorage.removeItem('servit_token');
      localStorage.removeItem('servit_identity');
      return Promise.reject();
    }
    return Promise.resolve();
  },

  getIdentity: () => {
    const raw = localStorage.getItem('servit_identity');
    if (!raw) return Promise.reject();
    const id = JSON.parse(raw);
    return Promise.resolve({ id: id.id, fullName: id.fullName });
  },

  getPermissions: () => {
    const raw = localStorage.getItem('servit_identity');
    return Promise.resolve(raw ? JSON.parse(raw).roles || [] : []);
  },
};
