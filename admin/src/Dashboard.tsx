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
import { kpiColors } from './theme';

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

// KPI card con fondo gradiente estilo Dompet:
// icono blanco + numero blanco + etiqueta blanca semi-transparente.
const GradientKpi = ({
  label,
  value,
  icon,
  gradient,
}: {
  label: string;
  value: string | number;
  icon: ReactNode;
  gradient: { from: string; to: string };
}) => (
  <Card
    sx={{
      flex: '1 1 200px',
      minWidth: 200,
      background: `linear-gradient(135deg, ${gradient.from} 0%, ${gradient.to} 100%)`,
      border: 'none',
      color: '#fff',
    }}
  >
    <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, py: 2.5 }}>
      <Box
        sx={{
          width: 50,
          height: 50,
          borderRadius: '50%',
          flexShrink: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: 'rgba(255,255,255,0.25)',
          color: '#fff',
          fontSize: 28,
        }}
      >
        {icon}
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, lineHeight: 1.1, color: '#fff' }}>
          {value}
        </Typography>
        <Typography
          variant="body2"
          sx={{ color: 'rgba(255,255,255,0.85)', mt: 0.25, fontWeight: 500 }}
          noWrap
        >
          {label}
        </Typography>
      </Box>
    </CardContent>
  </Card>
);

const ChartCard = ({ title, children }: { title: string; children: any }) => (
  <Card sx={{ flex: '1 1 420px', minWidth: 320 }}>
    <CardContent>
      <Typography variant="subtitle1" gutterBottom sx={{ color: '#333' }}>
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

  const gridColor = theme.palette.mode === 'light' ? '#e9ecef' : '#334155';
  const axisColor = theme.palette.text.secondary;

  return (
    <Box sx={{ p: { xs: 1, sm: 2 } }}>
      <Title title="Servit · Panel de administración" />

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2.5 }}>
        <Typography variant="h6" sx={{ fontWeight: 600 }}>Resumen</Typography>
        <Button
          variant="contained"
          size="small"
          startIcon={<DownloadIcon />}
          sx={{
            background: 'linear-gradient(135deg, #4fd1c5 0%, #38b2ac 100%)',
            '&:hover': { background: 'linear-gradient(135deg, #38b2ac 0%, #2c9a8f 100%)' },
            textTransform: 'none',
            borderRadius: 2,
            px: 2.5,
          }}
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
      {!s && !error && <Typography color="text.secondary">Cargando…</Typography>}

      {s && (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
          <GradientKpi label="Usuarios" value={s.totalUsers} icon={<PeopleIcon />} gradient={kpiColors.teal} />
          <GradientKpi label="Nuevos (30 d)" value={s.newUsersLast30Days} icon={<PersonAddIcon />} gradient={kpiColors.blue} />
          <GradientKpi label="Proveedores" value={s.totalProviders} icon={<HandymanIcon />} gradient={kpiColors.purple} />
          <GradientKpi label="Solicitudes" value={s.totalRequests} icon={<AssignmentIcon />} gradient={kpiColors.coral} />
          <GradientKpi label="En cola" value={s.pendingRequests} icon={<HourglassEmptyIcon />} gradient={kpiColors.orange} />
          <GradientKpi label="En curso" value={s.assignedRequests} icon={<AutorenewIcon />} gradient={kpiColors.blue} />
          <GradientKpi label="Completadas" value={s.completedRequests} icon={<CheckCircleIcon />} gradient={kpiColors.green} />
          <GradientKpi label="Canceladas" value={s.cancelledRequests} icon={<CancelIcon />} gradient={kpiColors.coral} />
          <GradientKpi label="Tasa completado" value={`${Math.round(s.completionRate * 100)}%`} icon={<PercentIcon />} gradient={kpiColors.green} />
          <GradientKpi label="GMV" value={s.gmv.toLocaleString('es-PE')} icon={<PaidIcon />} gradient={kpiColors.teal} />
        </Box>
      )}

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mt: 2.5 }}>
        <ChartCard title="Solicitudes por día">
          <LineChart data={series}>
            <CartesianGrid strokeDasharray="3 3" stroke={gridColor} />
            <XAxis dataKey="date" stroke={axisColor} fontSize={12} />
            <YAxis allowDecimals={false} stroke={axisColor} fontSize={12} />
            <Tooltip
              contentStyle={{
                backgroundColor: '#fff',
                border: '1px solid #e9ecef',
                borderRadius: 8,
                boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
              }}
            />
            <Line type="monotone" dataKey="count" stroke="#4fd1c5" strokeWidth={2.5} dot={false} />
          </LineChart>
        </ChartCard>
        <ChartCard title="Top proveedores (GMV)">
          <BarChart data={topProviders}>
            <CartesianGrid strokeDasharray="3 3" stroke={gridColor} />
            <XAxis dataKey="name" hide />
            <YAxis stroke={axisColor} fontSize={12} />
            <Tooltip
              contentStyle={{
                backgroundColor: '#fff',
                border: '1px solid #e9ecef',
                borderRadius: 8,
                boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
              }}
            />
            <Bar dataKey="value" fill="#845ef7" radius={[6, 6, 0, 0]} />
          </BarChart>
        </ChartCard>
      </Box>
    </Box>
  );
};
