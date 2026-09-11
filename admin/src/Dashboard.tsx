import { useEffect, useState } from 'react';
import { Card, CardContent, Typography, Box, Button } from '@mui/material';
import { Title } from 'react-admin';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';
import { adminFetch, downloadAdminBlob } from './api';

type Summary = {
  totalUsers: number;
  newUsersLast30Days: number;
  totalProviders: number;
  pendingRequests: number;
  assignedRequests: number;
  completedRequests: number;
  cancelledRequests: number;
  totalRequests: number;
  completionRate: number;
  gmv: number;
};

const Kpi = ({ label, value }: { label: string; value: string | number }) => (
  <Card sx={{ flex: '1 1 180px', minWidth: 180 }}>
    <CardContent>
      <Typography variant="body2" color="textSecondary">
        {label}
      </Typography>
      <Typography variant="h5">{value}</Typography>
    </CardContent>
  </Card>
);

const ChartCard = ({ title, children }: { title: string; children: any }) => (
  <Card sx={{ flex: '1 1 420px', minWidth: 320 }}>
    <CardContent>
      <Typography variant="subtitle1" gutterBottom>
        {title}
      </Typography>
      <Box sx={{ height: 260 }}>
        <ResponsiveContainer width="100%" height="100%">
          {children}
        </ResponsiveContainer>
      </Box>
    </CardContent>
  </Card>
);

export const Dashboard = () => {
  const [s, setS] = useState<Summary | null>(null);
  const [series, setSeries] = useState<{ date: string; count: number }[]>([]);
  const [topProviders, setTopProviders] = useState<{ name: string; value: number }[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminFetch('/reports/summary').then(setS).catch((e) => setError(e.message));
    adminFetch('/reports/timeseries?metric=requests')
      .then((rows: any[]) =>
        setSeries(rows.map((r) => ({ date: String(r.date).slice(0, 10), count: r.count }))),
      )
      .catch(() => {});
    adminFetch('/reports/top?dimension=providers')
      .then((rows: any[]) => setTopProviders(rows.map((r) => ({ name: r.name, value: Number(r.value) }))))
      .catch(() => {});
  }, []);

  return (
    <Card>
      <Title title="Servit · Panel de administración" />
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">Resumen</Typography>
          <Button
            variant="outlined"
            size="small"
            onClick={() =>
              downloadAdminBlob('/reports/export.csv', 'servit-solicitudes.csv').catch((e) =>
                setError(e.message),
              )
            }
          >
            Exportar CSV
          </Button>
        </Box>

        {error && <Typography color="error">{error}</Typography>}
        {!s && !error && <Typography>Cargando…</Typography>}

        {s && (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
            <Kpi label="Usuarios" value={s.totalUsers} />
            <Kpi label="Nuevos (30 d)" value={s.newUsersLast30Days} />
            <Kpi label="Proveedores" value={s.totalProviders} />
            <Kpi label="Solicitudes" value={s.totalRequests} />
            <Kpi label="En cola" value={s.pendingRequests} />
            <Kpi label="En curso" value={s.assignedRequests} />
            <Kpi label="Completadas" value={s.completedRequests} />
            <Kpi label="Canceladas" value={s.cancelledRequests} />
            <Kpi label="Tasa de completado" value={`${Math.round(s.completionRate * 100)}%`} />
            <Kpi label="GMV" value={s.gmv.toLocaleString('es-PE')} />
          </Box>
        )}

        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mt: 2 }}>
          <ChartCard title="Solicitudes por día">
            <LineChart data={series}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="date" />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Line type="monotone" dataKey="count" stroke="#1976d2" />
            </LineChart>
          </ChartCard>
          <ChartCard title="Top proveedores (GMV)">
            <BarChart data={topProviders}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" hide />
              <YAxis />
              <Tooltip />
              <Bar dataKey="value" fill="#2e7d32" />
            </BarChart>
          </ChartCard>
        </Box>
      </CardContent>
    </Card>
  );
};
