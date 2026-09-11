import { DataProvider, fetchUtils } from 'react-admin';

const API_URL = import.meta.env.VITE_API_URL || '/api/admin';

// Authenticated JSON client. fetchJson throws HttpError on non-2xx, which
// react-admin turns into notifications and feeds to authProvider.checkError.
const httpClient = (url: string, options: fetchUtils.Options = {}) => {
  const headers = (options.headers as Headers) || new Headers({ Accept: 'application/json' });
  const token = localStorage.getItem('servit_token');
  if (token) headers.set('Authorization', `Bearer ${token}`);
  return fetchUtils.fetchJson(url, { ...options, headers });
};

const listQuery = (params: any): string => {
  const query = new URLSearchParams();
  const { page = 1, perPage = 25 } = params.pagination || {};
  const { field, order } = params.sort || {};
  query.set('page', String(page));
  query.set('perPage', String(perPage));
  if (field) query.set('sort', field);
  if (order) query.set('order', order);
  Object.entries(params.filter || {}).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') query.set(k, String(v));
  });
  return query.toString();
};

// Maps react-admin to the Servit backend: listings return { data, total };
// getOne/create/update return the DTO directly.
export const dataProvider: DataProvider = {
  getList: async (resource, params) => {
    const { json } = await httpClient(`${API_URL}/${resource}?${listQuery(params)}`);
    return { data: json.data, total: json.total };
  },

  getOne: async (resource, params) => {
    const { json } = await httpClient(`${API_URL}/${resource}/${params.id}`);
    return { data: json };
  },

  getMany: async (resource, params) => {
    const data = await Promise.all(
      params.ids.map((id) => httpClient(`${API_URL}/${resource}/${id}`).then((r) => r.json)),
    );
    return { data };
  },

  getManyReference: async (resource, params) => {
    const merged = { ...params, filter: { ...params.filter, [params.target]: params.id } };
    const { json } = await httpClient(`${API_URL}/${resource}?${listQuery(merged)}`);
    return { data: json.data, total: json.total };
  },

  create: async (resource, params) => {
    const { json } = await httpClient(`${API_URL}/${resource}`, {
      method: 'POST',
      body: JSON.stringify(params.data),
    });
    return { data: json };
  },

  update: async (resource, params) => {
    const { json } = await httpClient(`${API_URL}/${resource}/${params.id}`, {
      method: 'PUT',
      body: JSON.stringify(params.data),
    });
    return { data: json ?? { ...params.data, id: params.id } };
  },

  updateMany: async (resource, params) => {
    await Promise.all(
      params.ids.map((id) =>
        httpClient(`${API_URL}/${resource}/${id}`, { method: 'PUT', body: JSON.stringify(params.data) }),
      ),
    );
    return { data: params.ids };
  },

  delete: async (resource, params) => {
    await httpClient(`${API_URL}/${resource}/${params.id}`, { method: 'DELETE' });
    return { data: params.previousData as any };
  },

  deleteMany: async (resource, params) => {
    await Promise.all(
      params.ids.map((id) => httpClient(`${API_URL}/${resource}/${id}`, { method: 'DELETE' })),
    );
    return { data: params.ids };
  },
};
