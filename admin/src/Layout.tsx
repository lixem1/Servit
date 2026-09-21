import { Layout as RALayout, AppBar, TitlePortal, Menu, Sidebar } from 'react-admin';
import type { LayoutProps, SidebarProps } from 'react-admin';
import { Box, Typography } from '@mui/material';
import { sidebar } from './theme';

// Topbar de color (marca).
const AppTopBar = () => (
  <AppBar color="primary">
    <TitlePortal />
  </AppBar>
);

// Sidebar oscuro estilo AdminLTE. Estilamos los items del menu desde el
// contenedor usando las clases estables de react-admin.
const DarkSidebar = (props: SidebarProps) => (
  <Sidebar
    {...props}
    sx={{
      backgroundColor: sidebar.bg,
      '& .RaSidebar-fixed': { backgroundColor: sidebar.bg },
      '& .MuiPaper-root': { backgroundColor: sidebar.bg },
      '& .RaMenuItemLink-root': {
        color: sidebar.text,
        borderRadius: 1,
        marginInline: 0.75,
        marginBlock: 0.25,
        paddingBlock: 0.75,
      },
      '& .RaMenuItemLink-root:hover': { backgroundColor: sidebar.hoverBg, color: '#fff' },
      '& .RaMenuItemLink-active': {
        backgroundColor: sidebar.activeBg,
        color: sidebar.activeText,
        fontWeight: 600,
      },
      '& .RaMenuItemLink-active:hover': { backgroundColor: sidebar.activeBg },
      '& .RaMenuItemLink-active .MuiSvgIcon-root': { color: '#fff' },
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
        borderBottom: '1px solid rgba(255,255,255,0.08)',
      }}
    >
      <Box
        sx={{
          width: 30,
          height: 30,
          borderRadius: 1,
          bgcolor: 'primary.main',
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
      <Typography sx={{ color: '#fff', fontWeight: 700, letterSpacing: 0.3 }}>Servit</Typography>
    </Box>
    <Menu sx={{ mt: 1, flex: 1 }} />
  </Box>
);

export const Layout = (props: LayoutProps) => (
  <RALayout {...props} appBar={AppTopBar} sidebar={DarkSidebar} menu={BrandMenu} />
);
