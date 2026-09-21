import { useEffect, useState, type ReactNode } from 'react';
import { Card, CardContent, Typography, Box, Button, useTheme } from '@mui/material';
import { Title } from 'react-admin';
import PeopleIcon from '@mui/icons-material/People';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import HandymanIcon from '@mui/icons-material/Handyman';
import AssignmentIcon from '@mui/icons-material/Assignment';
import HourglassEmptyIcon from '@mui/icons-material/HourglassEmpty';
import AutorenewIcon from '@mui/icons-material/Autorenew';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import PercentIcon from '@mui/icons-material/Percent';
import PaidIcon from '@mui/icons-material/Paid';
import DownloadIcon from '@mui/icons-material/Download';
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

// "info-box" estilo AdminLTE: chip de icono a color + numero + etiqueta.
// El color se refuerza con icono y texto (nunca identidad solo por color).
const InfoBox = ({
  label,
  value,
  icon,
  color,
}: {
  label: string;
  value: string | number;
  icon: ReactNode;
  color: string;
}) => (
  <Card sx={{ flex: '1 1 190px', minWidth: 190 }}>
    <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 1.75 }}>
      <Box
        sx={{
          width: 46,
          height: 46,
          borderRadius: 2,
          flexShrink: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: `${color}1f`,
          color,
        }}
      >
        {icon}
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, lineHeight: 1.1 }}>
          {value}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap>
          {label}
        </Typography>
      </Box>
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
  const theme = useTheme();
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

  const c = theme.palette;
  const gridColor = c.mode === 'light' ? '#e5e7eb' : '#334155';
  const axisColor = c.text.secondary;

  return (
    <Box sx={{ p: { xs: 1, sm: 2 } }}>
      <Title title="Servit · Panel de administración" />

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Resumen</Typography>
        <Button
          variant="contained"
          size="small"
          startIcon={<DownloadIcon />}
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
          <InfoBox label="Usuarios" value={s.totalUsers} icon={<PeopleIcon />} color={c.info.main} />
          <InfoBox label="Nuevos (30 d)" value={s.newUsersLast30Days} icon={<PersonAddIcon />} color={c.primary.main} />
          <InfoBox label="Proveedores" value={s.totalProviders} icon={<HandymanIcon />} color={c.secondary.main} />
          <InfoBox label="Solicitudes" value={s.totalRequests} icon={<AssignmentIcon />} color={c.primary.main} />
          <InfoBox label="En cola" value={s.pendingRequests} icon={<HourglassEmptyIcon />} color={c.warning.main} />
          <InfoBox label="En curso" value={s.assignedRequests} icon={<AutorenewIcon />} color={c.info.main} />
          <InfoBox label="Completadas" value={s.completedRequests} icon={<CheckCircleIcon />} color={c.success.main} />
          <InfoBox label="Canceladas" value={s.cancelledRequests} icon={<CancelIcon />} color={c.error.main} />
          <InfoBox label="Tasa de completado" value={`${Math.round(s.completionRate * 100)}%`} icon={<PercentIcon />} color={c.success.main} />
          <InfoBox label="GMV" value={s.gmv.toLocaleString('es-PE')} icon={<PaidIcon />} color={c.primary.main} />
        </Box>
      )}

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mt: 2 }}>
        <ChartCard title="Solicitudes por día">
          <LineChart data={series}>
            <CartesianGrid strokeDasharray="3 3" stroke={gridColor} />
            <XAxis dataKey="date" stroke={axisColor} fontSize={12} />
            <YAxis allowDecimals={false} stroke={axisColor} fontSize={12} />
            <Tooltip />
            <Line type="monotone" dataKey="count" stroke={c.primary.main} strokeWidth={2} dot={false} />
          </LineChart>
        </ChartCard>
        <ChartCard title="Top proveedores (GMV)">
          <BarChart data={topProviders}>
            <CartesianGrid strokeDasharray="3 3" stroke={gridColor} />
            <XAxis dataKey="name" hide />
            <YAxis stroke={axisColor} fontSize={12} />
            <Tooltip />
            <Bar dataKey="value" fill={c.primary.main} radius={[4, 4, 0, 0]} />
          </BarChart>
        </ChartCard>
      </Box>
    </Box>
  );
};
