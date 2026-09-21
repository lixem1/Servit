import { Admin, Resource } from 'react-admin';
import PeopleIcon from '@mui/icons-material/People';
import CategoryIcon from '@mui/icons-material/Category';
import HandymanIcon from '@mui/icons-material/Handyman';
import AssignmentIcon from '@mui/icons-material/Assignment';
import StarIcon from '@mui/icons-material/Star';
import HistoryIcon from '@mui/icons-material/History';
import InsightsIcon from '@mui/icons-material/Insights';
import MapIcon from '@mui/icons-material/Map';
import { dataProvider } from './dataProvider';
import { authProvider } from './authProvider';
import { i18nProvider } from './i18nProvider';
import { Layout } from './Layout';
import { LoginPage } from './Login';
import { lightTheme, darkTheme } from './theme';
import { Dashboard } from './Dashboard';
import { UserList, UserShow, UserEdit } from './resources/users';
import { CategoryList, CategoryCreate, CategoryEdit } from './resources/categories';
import { ProviderList, ProviderShow } from './resources/providers';
import { ServiceRequestList, ServiceRequestShow } from './resources/serviceRequests';
import { ReviewList } from './resources/reviews';
import { AuditLogList, AuditLogShow } from './resources/auditLogs';
import { AnalyticsPage } from './resources/analytics';
import { GeoPage } from './resources/geo';

export const App = () => (
  <Admin
    dataProvider={dataProvider}
    authProvider={authProvider}
    i18nProvider={i18nProvider}
    dashboard={Dashboard}
    layout={Layout}
    loginPage={LoginPage}
    theme={lightTheme}
    darkTheme={darkTheme}
    title="Servit · Admin"
  >
    <Resource name="users" list={UserList} show={UserShow} edit={UserEdit} icon={PeopleIcon} options={{ label: 'Usuarios' }} />
    <Resource name="providers" list={ProviderList} show={ProviderShow} icon={HandymanIcon} options={{ label: 'Proveedores' }} />
    <Resource name="service-requests" list={ServiceRequestList} show={ServiceRequestShow} icon={AssignmentIcon} options={{ label: 'Solicitudes' }} />
    <Resource name="reviews" list={ReviewList} icon={StarIcon} options={{ label: 'Reseñas' }} />
    <Resource name="categories" list={CategoryList} create={CategoryCreate} edit={CategoryEdit} icon={CategoryIcon} options={{ label: 'Categorías' }} />
    {/* Page-only entries (no REST resource behind them). */}
    <Resource name="analytics" list={AnalyticsPage} icon={InsightsIcon} options={{ label: 'Analítica' }} />
    <Resource name="geo" list={GeoPage} icon={MapIcon} options={{ label: 'Mapa' }} />
    <Resource name="audit-logs" list={AuditLogList} show={AuditLogShow} icon={HistoryIcon} options={{ label: 'Auditoría' }} />
  </Admin>
);
