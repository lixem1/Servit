import { useEffect, useState } from 'react';
import { Card, CardContent, Typography, Box, MenuItem, TextField, FormControlLabel, Switch } from '@mui/material';
import { Title } from 'react-admin';
import { MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import { adminFetch } from '../api';

type GeoRequest = {
  id: string;
  lat: number;
  lng: number;
  status: number | string;
  categoryId: number;
  categoryName: string;
  createdAt: string;
};
type GeoProvider = {
  id: string;
  lat: number;
  lng: number;
  fullName: string;
  averageRating: number;
  ratingCount: number;
};
type Category = { id: number; name: string };

// Backend serializes ServiceRequestStatus as its numeric enum value here.
const STATUS_COLOR: Record<string, string> = {
  '0': '#f9a825', // Pending
  '1': '#1976d2', // Assigned
  '2': '#2e7d32', // Completed
  '3': '#9e9e9e', // Cancelled
};
const STATUS_LABEL: Record<string, string> = {
  '0': 'En cola',
  '1': 'En curso',
  '2': 'Completada',
  '3': 'Cancelada',
};

const LIMA: [number, number] = [-12.05, -77.04];

export const GeoPage = () => {
  const [requests, setRequests] = useState<GeoRequest[]>([]);
  const [providers, setProviders] = useState<GeoProvider[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [status, setStatus] = useState<string>('');
  const [categoryId, setCategoryId] = useState<string>('');
  const [showProviders, setShowProviders] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminFetch('/categories?perPage=200')
      .then((r: any) => setCategories(r.data || []))
      .catch(() => {});
  }, []);

  useEffect(() => {
    const q = new URLSearchParams();
    if (status !== '') q.set('status', status);
    if (categoryId !== '') q.set('categoryId', categoryId);
    adminFetch(`/geo/requests?${q.toString()}`).then(setRequests).catch((e) => setError(e.message));
  }, [status, categoryId]);

  useEffect(() => {
    if (!showProviders) return;
    const q = new URLSearchParams();
    if (categoryId !== '') q.set('categoryId', categoryId);
    adminFetch(`/geo/providers?${q.toString()}`).then(setProviders).catch(() => {});
  }, [showProviders, categoryId]);

  return (
    <Card>
      <Title title="Servit · Mapa de demanda" />
      <CardContent>
        {error && <Typography color="error">{error}</Typography>}
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 2, alignItems: 'center' }}>
          <TextField
            select
            size="small"
            label="Estado"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">Todos</MenuItem>
            <MenuItem value="0">En cola</MenuItem>
            <MenuItem value="1">En curso</MenuItem>
            <MenuItem value="2">Completada</MenuItem>
            <MenuItem value="3">Cancelada</MenuItem>
          </TextField>
          <TextField
            select
            size="small"
            label="Categoría"
            value={categoryId}
            onChange={(e) => setCategoryId(e.target.value)}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">Todas</MenuItem>
            {categories.map((c) => (
              <MenuItem key={c.id} value={String(c.id)}>
                {c.name}
              </MenuItem>
            ))}
          </TextField>
          <FormControlLabel
            control={<Switch checked={showProviders} onChange={(e) => setShowProviders(e.target.checked)} />}
            label="Mostrar proveedores"
          />
          <Typography variant="body2" color="textSecondary">
            {requests.length} solicitudes{showProviders ? ` · ${providers.length} proveedores` : ''}
          </Typography>
        </Box>

        <Box sx={{ height: 600 }}>
          <MapContainer center={LIMA} zoom={11} style={{ height: '100%', width: '100%' }}>
            <TileLayer
              attribution='&copy; OpenStreetMap'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {requests.map((r) => {
              const key = String(r.status);
              return (
                <CircleMarker
                  key={`r-${r.id}`}
                  center={[r.lat, r.lng]}
                  radius={7}
                  pathOptions={{ color: STATUS_COLOR[key] || '#f9a825', fillOpacity: 0.7 }}
                >
                  <Popup>
                    <strong>{r.categoryName}</strong>
                    <br />
                    {STATUS_LABEL[key] || r.status}
                    <br />
                    {new Date(r.createdAt).toLocaleString('es-PE')}
                  </Popup>
                </CircleMarker>
              );
            })}
            {showProviders &&
              providers.map((p) => (
                <CircleMarker
                  key={`p-${p.id}`}
                  center={[p.lat, p.lng]}
                  radius={6}
                  pathOptions={{ color: '#6a1b9a', fillColor: '#8e24aa', fillOpacity: 0.6 }}
                >
                  <Popup>
                    <strong>{p.fullName}</strong>
                    <br />
                    ★ {p.averageRating.toFixed(1)} ({p.ratingCount})
                  </Popup>
                </CircleMarker>
              ))}
          </MapContainer>
        </Box>
      </CardContent>
    </Card>
  );
};
