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

// Fetch a binary admin resource (e.g. an attachment) and open it in a new tab.
// Needed because attachment endpoints require the Bearer token, so a plain
// <a href> can't be used.
export const openAdminBlob = async (path: string) => {
  const token = localStorage.getItem('servit_token');
  const res = await fetch(`${API_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!res.ok) throw new Error(`Error ${res.status}`);
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  window.open(url, '_blank', 'noopener');
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
};

// Download a binary admin resource (e.g. reports/export.csv) as a file.
export const downloadAdminBlob = async (path: string, filename: string) => {
  const token = localStorage.getItem('servit_token');
  const res = await fetch(`${API_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!res.ok) throw new Error(`Error ${res.status}`);
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
};
