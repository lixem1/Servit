import { Admin, Resource } from 'react-admin';
import PeopleIcon from '@mui/icons-material/People';
import CategoryIcon from '@mui/icons-material/Category';
import HandymanIcon from '@mui/icons-material/Handyman';
import AssignmentIcon from '@mui/icons-material/Assignment';
import StarIcon from '@mui/icons-material/Star';
import { dataProvider } from './dataProvider';
import { authProvider } from './authProvider';
import { i18nProvider } from './i18nProvider';
import { Dashboard } from './Dashboard';
import { UserList, UserShow } from './resources/users';
import { CategoryList, CategoryCreate, CategoryEdit } from './resources/categories';
import { ProviderList, ProviderShow } from './resources/providers';
import { ServiceRequestList, ServiceRequestShow } from './resources/serviceRequests';
import { ReviewList } from './resources/reviews';

export const App = () => (
  <Admin
    dataProvider={dataProvider}
    authProvider={authProvider}
    i18nProvider={i18nProvider}
    dashboard={Dashboard}
    title="Servit · Admin"
  >
    <Resource
      name="users"
      list={UserList}
      show={UserShow}
      icon={PeopleIcon}
      options={{ label: 'Usuarios' }}
    />
    <Resource
      name="providers"
      list={ProviderList}
      show={ProviderShow}
      icon={HandymanIcon}
      options={{ label: 'Proveedores' }}
    />
    <Resource
      name="service-requests"
      list={ServiceRequestList}
      show={ServiceRequestShow}
      icon={AssignmentIcon}
      options={{ label: 'Solicitudes' }}
    />
    <Resource
      name="reviews"
      list={ReviewList}
      icon={StarIcon}
      options={{ label: 'Reseñas' }}
    />
    <Resource
      name="categories"
      list={CategoryList}
      create={CategoryCreate}
      edit={CategoryEdit}
      icon={CategoryIcon}
      options={{ label: 'Categorías' }}
    />
  </Admin>
);
