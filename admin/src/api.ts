// Authenticated fetch for admin endpoints that aren't plain REST resources
// (custom actions like suspend/restore, reports, analytics, geo).
const API_URL = import.meta.env.VITE_API_URL || '/api/admin';

export const adminFetch = async (path: string, options: RequestInit = {}) => {
  const token = localStorage.getItem('servit_token');
  const headers = new Headers(options.headers);
  if (!headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  if (token) headers.set('Authorization', `Bearer ${token}`);
  const res = await fetch(`${API_URL}${path}`, { ...options, headers });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `Error ${res.status}`);
  }
  return res.status === 204 ? null : res.json();
};
