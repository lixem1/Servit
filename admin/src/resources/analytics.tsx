import { useEffect, useState } from 'react';
import { Card, CardContent, Typography, Box } from '@mui/material';
import { Title } from 'react-admin';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';
import { adminFetch } from '../api';

type Funnel = { requests: number; withQuotes: number; assigned: number; completed: number };
type Times = {
  averageFirstResponseSeconds: number | null;
  requestsWithResponse: number;
  averageTimeInQueueSeconds: number | null;
  assignedRequestsMeasured: number;
};
type Stale = {
  id: string;
  description: string;
  categoryName: string;
  customerName: string;
  createdAt: string;
  ageHours: number;
};

const fmt = (v: number | null) =>
  v == null ? '—' : v >= 3600 ? `${(v / 3600).toFixed(1)} h` : `${Math.round(v / 60)} min`;

const Kpi = ({ label, value }: { label: string; value: string | number }) => (
  <Card sx={{ flex: '1 1 220px', minWidth: 200 }}>
    <CardContent>
      <Typography variant="body2" color="textSecondary">
        {label}
      </Typography>
      <Typography variant="h5">{value}</Typography>
    </CardContent>
  </Card>
);

export const AnalyticsPage = () => {
  const [funnel, setFunnel] = useState<Funnel | null>(null);
  const [times, setTimes] = useState<Times | null>(null);
  const [stale, setStale] = useState<Stale[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminFetch('/analytics/funnel').then(setFunnel).catch((e) => setError(e.message));
    adminFetch('/analytics/response-times').then(setTimes).catch(() => {});
    adminFetch('/analytics/stale?hours=24').then(setStale).catch(() => {});
  }, []);

  const funnelData = funnel
    ? [
        { stage: 'Solicitudes', value: funnel.requests },
        { stage: 'Con cotización', value: funnel.withQuotes },
        { stage: 'Asignadas', value: funnel.assigned },
        { stage: 'Completadas', value: funnel.completed },
      ]
    : [];

  return (
    <Card>
      <Title title="Servit · Analítica operativa" />
      <CardContent>
        {error && <Typography color="error">{error}</Typography>}

        <Typography variant="h6" gutterBottom>
          Embudo de conversión
        </Typography>
        <Box sx={{ height: 300 }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={funnelData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="stage" />
              <YAxis allowDecimals={false} />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#fff',
                  border: '1px solid #e9ecef',
                  borderRadius: 8,
                  boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
                }}
              />
              <Bar dataKey="value" fill="#4fd1c5" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Box>

        <Typography variant="h6" gutterBottom sx={{ mt: 3 }}>
          Tiempos (SLA)
        </Typography>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
          <Kpi label="1ª respuesta (media)" value={fmt(times?.averageFirstResponseSeconds ?? null)} />
          <Kpi label="Tiempo en cola (media)" value={fmt(times?.averageTimeInQueueSeconds ?? null)} />
          <Kpi label="Con respuesta" value={times?.requestsWithResponse ?? '—'} />
          <Kpi label="Asignadas medidas" value={times?.assignedRequestsMeasured ?? '—'} />
        </Box>

        <Typography variant="h6" gutterBottom sx={{ mt: 3 }}>
          Solicitudes sin respuesta ({stale.length})
        </Typography>
        <Box component="table" sx={{ width: '100%', borderCollapse: 'collapse' }}>
          <Box component="thead">
            <Box component="tr" sx={{ textAlign: 'left', borderBottom: '1px solid #ddd' }}>
              <Box component="th" sx={{ p: 1 }}>Categoría</Box>
              <Box component="th" sx={{ p: 1 }}>Cliente</Box>
              <Box component="th" sx={{ p: 1 }}>Descripción</Box>
              <Box component="th" sx={{ p: 1 }}>Antigüedad</Box>
            </Box>
          </Box>
          <Box component="tbody">
            {stale.map((r) => (
              <Box component="tr" key={r.id} sx={{ borderBottom: '1px solid #f0f0f0' }}>
                <Box component="td" sx={{ p: 1 }}>{r.categoryName}</Box>
                <Box component="td" sx={{ p: 1 }}>{r.customerName}</Box>
                <Box component="td" sx={{ p: 1 }}>{r.description}</Box>
                <Box component="td" sx={{ p: 1 }}>{r.ageHours} h</Box>
              </Box>
            ))}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
};
