import { defaultTheme } from 'react-admin';
import type { RaThemeOptions } from 'react-admin';

// Paleta estilo "AdminLTE moderno": azul de marca, superficies claras,
// estados semanticos reservados (success/error/warning/info).
const brand = { main: '#2563eb', dark: '#1d4ed8', light: '#3b82f6', contrastText: '#ffffff' };

// Colores del sidebar oscuro (se referencian tambien desde Layout.tsx).
export const sidebar = {
  bg: '#1f2937',
  text: '#c2c7d0',
  icon: '#9aa4b2',
  hoverBg: 'rgba(255,255,255,0.08)',
  activeBg: '#2563eb',
  activeText: '#ffffff',
};

const sharedComponents = (mode: 'light' | 'dark') => ({
  ...defaultTheme.components,
  MuiCard: {
    styleOverrides: {
      root: {
        border: `1px solid ${mode === 'light' ? '#e5e7eb' : '#334155'}`,
        boxShadow:
          mode === 'light'
            ? '0 1px 2px rgba(16,24,40,0.06), 0 1px 3px rgba(16,24,40,0.04)'
            : '0 1px 2px rgba(0,0,0,0.4)',
        backgroundImage: 'none',
      },
    },
  },
  MuiPaper: { styleOverrides: { root: { backgroundImage: 'none' } } },
  MuiAppBar: {
    styleOverrides: {
      root: { boxShadow: '0 1px 3px rgba(16,24,40,0.12)' },
    },
  },
  MuiButton: { defaultProps: { disableElevation: true } },
});

export const lightTheme: RaThemeOptions = {
  ...defaultTheme,
  palette: {
    mode: 'light',
    primary: brand,
    secondary: { main: '#7c3aed' },
    success: { main: '#16a34a' },
    error: { main: '#dc2626' },
    warning: { main: '#d97706' },
    info: { main: '#0891b2' },
    background: { default: '#f4f6f9', paper: '#ffffff' },
    text: { primary: '#111827', secondary: '#6b7280' },
  },
  shape: { borderRadius: 8 },
  typography: {
    h6: { fontWeight: 600 },
    subtitle1: { fontWeight: 600 },
  },
  components: sharedComponents('light'),
};

export const darkTheme: RaThemeOptions = {
  ...defaultTheme,
  palette: {
    mode: 'dark',
    primary: brand,
    secondary: { main: '#a78bfa' },
    success: { main: '#22c55e' },
    error: { main: '#ef4444' },
    warning: { main: '#f59e0b' },
    info: { main: '#06b6d4' },
    background: { default: '#0f172a', paper: '#1e293b' },
  },
  shape: { borderRadius: 8 },
  typography: { h6: { fontWeight: 600 }, subtitle1: { fontWeight: 600 } },
  components: sharedComponents('dark'),
};
