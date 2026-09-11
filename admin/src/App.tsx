import { Admin, Resource } from 'react-admin';
import PeopleIcon from '@mui/icons-material/People';
import CategoryIcon from '@mui/icons-material/Category';
import { dataProvider } from './dataProvider';
import { authProvider } from './authProvider';
import { i18nProvider } from './i18nProvider';
import { Dashboard } from './Dashboard';
import { UserList, UserShow } from './resources/users';
import { CategoryList, CategoryCreate, CategoryEdit } from './resources/categories';

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
      name="categories"
      list={CategoryList}
      create={CategoryCreate}
      edit={CategoryEdit}
      icon={CategoryIcon}
      options={{ label: 'Categorías' }}
    />
  </Admin>
);
