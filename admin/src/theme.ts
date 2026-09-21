import { defaultTheme } from 'react-admin';
import type { RaThemeOptions } from 'react-admin';

// Paleta inspirada en Dompet: teal como primario, acentos coral/verde/púrpura.
// Superficies claras, tarjetas con sombra suave, tipografía legible.
const brand = { main: '#4fd1c5', dark: '#38b2ac', light: '#81e6d9', contrastText: '#ffffff' };

// Colores para KPI cards con gradiente.
export const kpiColors = {
  coral:   { from: '#ff6b6b', to: '#ee5a24' },
  green:   { from: '#51cf66', to: '#20c997' },
  purple:  { from: '#845ef7', to: '#5f3dc4' },
  teal:    { from: '#4fd1c5', to: '#38b2ac' },
  blue:    { from: '#4dabf7', to: '#228be6' },
  orange:  { from: '#ffa94d', to: '#fd7e14' },
};

// Sidebar claro (blanco con texto oscuro).
export const sidebar = {
  bg: '#ffffff',
  text: '#5a6474',
  icon: '#8e99a4',
  hoverBg: '#f0faf9',
  activeBg: 'rgba(79, 209, 197, 0.12)',
  activeText: '#38b2ac',
  borderRight: '#e9ecef',
};

const sharedComponents = (mode: 'light' | 'dark') => ({
  ...defaultTheme.components,
  MuiCard: {
    styleOverrides: {
      root: {
        border: 'none',
        borderRadius: 12,
        boxShadow:
          mode === 'light'
            ? '0 2px 8px rgba(0,0,0,0.06), 0 0 1px rgba(0,0,0,0.04)'
            : '0 2px 8px rgba(0,0,0,0.4)',
        backgroundImage: 'none',
      },
    },
  },
  MuiPaper: {
    styleOverrides: {
      root: {
        backgroundImage: 'none',
        borderRadius: 12,
      },
    },
  },
  MuiAppBar: {
    styleOverrides: {
      root: {
        boxShadow: 'none',
        borderBottom: `1px solid ${mode === 'light' ? '#e9ecef' : '#334155'}`,
      },
    },
  },
  MuiButton: { defaultProps: { disableElevation: true } },
  MuiTableHead: {
    styleOverrides: {
      root: {
        '& .MuiTableCell-head': {
          fontWeight: 600,
          color: mode === 'light' ? '#5a6474' : '#94a3b8',
          fontSize: '0.8125rem',
          textTransform: 'uppercase' as const,
          letterSpacing: '0.5px',
        },
      },
    },
  },
});

export const lightTheme: RaThemeOptions = {
  ...defaultTheme,
  palette: {
    mode: 'light',
    primary: brand,
    secondary: { main: '#845ef7' },
    success: { main: '#51cf66' },
    error: { main: '#ff6b6b' },
    warning: { main: '#ffa94d' },
    info: { main: '#4dabf7' },
    background: { default: '#f5f6fa', paper: '#ffffff' },
    text: { primary: '#333333', secondary: '#7e7e7e' },
  },
  shape: { borderRadius: 12 },
  typography: {
    fontFamily: '"Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", sans-serif',
    h5: { fontWeight: 700, color: '#333333' },
    h6: { fontWeight: 600, color: '#333333' },
    subtitle1: { fontWeight: 600 },
    body2: { color: '#5a6474' },
  },
  components: sharedComponents('light'),
};

export const darkTheme: RaThemeOptions = {
  ...defaultTheme,
  palette: {
    mode: 'dark',
    primary: brand,
    secondary: { main: '#a78bfa' },
    success: { main: '#51cf66' },
    error: { main: '#ff6b6b' },
    warning: { main: '#ffa94d' },
    info: { main: '#4dabf7' },
    background: { default: '#0f172a', paper: '#1e293b' },
  },
  shape: { borderRadius: 12 },
  typography: {
    fontFamily: '"Inter", "Segoe UI", "Roboto", "Helvetica", "Arial", sans-serif',
    h5: { fontWeight: 700 },
    h6: { fontWeight: 600 },
    subtitle1: { fontWeight: 600 },
  },
  components: sharedComponents('dark'),
};
