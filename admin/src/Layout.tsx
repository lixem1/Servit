import { Layout as RALayout, AppBar, TitlePortal, Menu, Sidebar } from 'react-admin';
import type { LayoutProps, SidebarProps } from 'react-admin';
import { Box, Typography } from '@mui/material';
import { sidebar } from './theme';

// Topbar blanco con borde inferior sutil.
const AppTopBar = () => (
  <AppBar
    sx={{
      backgroundColor: '#ffffff',
      color: '#333333',
      '& .RaAppBar-title': { color: '#333333' },
      '& .MuiIconButton-root': { color: '#5a6474' },
    }}
  >
    <TitlePortal />
  </AppBar>
);

// Sidebar claro estilo Dompet: blanco con borde derecho, items con hover teal suave.
const LightSidebar = (props: SidebarProps) => (
  <Sidebar
    {...props}
    sx={{
      backgroundColor: sidebar.bg,
      borderRight: `1px solid ${sidebar.borderRight}`,
      '& .RaSidebar-fixed': { backgroundColor: sidebar.bg },
      '& .MuiPaper-root': { backgroundColor: sidebar.bg, borderRadius: 0 },
      '& .RaMenuItemLink-root': {
        color: sidebar.text,
        borderRadius: 2,
        marginInline: 0.75,
        marginBlock: 0.25,
        paddingBlock: 0.75,
        transition: 'all 0.15s ease',
      },
      '& .RaMenuItemLink-root:hover': {
        backgroundColor: sidebar.hoverBg,
        color: sidebar.activeText,
      },
      '& .RaMenuItemLink-active': {
        backgroundColor: sidebar.activeBg,
        color: sidebar.activeText,
        fontWeight: 600,
      },
      '& .RaMenuItemLink-active:hover': { backgroundColor: sidebar.activeBg },
      '& .RaMenuItemLink-active .MuiSvgIcon-root': { color: sidebar.activeText },
      '& .MuiSvgIcon-root': { color: sidebar.icon },
    }}
  />
);

// Encabezado de marca arriba del menu.
const BrandMenu = () => (
  <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
    <Box
      sx={{
        px: 2,
        py: 1.75,
        display: 'flex',
        alignItems: 'center',
        gap: 1,
        borderBottom: `1px solid ${sidebar.borderRight}`,
      }}
    >
      <Box
        sx={{
          width: 32,
          height: 32,
          borderRadius: 2,
          background: 'linear-gradient(135deg, #4fd1c5 0%, #38b2ac 100%)',
          color: '#fff',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontWeight: 700,
          fontSize: 16,
        }}
      >
        S
      </Box>
      <Typography sx={{ color: '#333333', fontWeight: 700, letterSpacing: 0.3, fontSize: '1.1rem' }}>
        Servit
      </Typography>
    </Box>
    <Menu sx={{ mt: 1, flex: 1 }} />
  </Box>
);

export const Layout = (props: LayoutProps) => (
  <RALayout {...props} appBar={AppTopBar} sidebar={LightSidebar} menu={BrandMenu} />
);
