import { useEffect, useState } from 'react';
import { Card, CardContent, Typography, Box } from '@mui/material';
import { Title } from 'react-admin';
import { adminFetch } from './api';

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

export const Dashboard = () => {
  const [s, setS] = useState<Summary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminFetch('/reports/summary')
      .then(setS)
      .catch((e) => setError(e.message));
  }, []);

  return (
    <Card>
      <Title title="Servit · Panel de administración" />
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Resumen
        </Typography>
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
      </CardContent>
    </Card>
  );
};
