import {
  List,
  Datagrid,
  TextField,
  DateField,
  BooleanField,
  NumberField,
  FunctionField,
  TextInput,
  SelectInput,
  Show,
  SimpleShowLayout,
  ArrayField,
  TopToolbar,
  Button,
  useRecordContext,
  useNotify,
  useRefresh,
} from 'react-admin';
import { adminFetch } from '../api';

const userFilters = [
  <TextInput key="search" source="search" label="Buscar (nombre/email)" alwaysOn />,
  <SelectInput
    key="role"
    source="role"
    label="Rol"
    choices={[
      { id: 'Customer', name: 'Cliente' },
      { id: 'Provider', name: 'Proveedor' },
      { id: 'Admin', name: 'Admin' },
    ]}
  />,
  <SelectInput
    key="status"
    source="status"
    label="Estado"
    choices={[
      { id: 'active', name: 'Activo' },
      { id: 'suspended', name: 'Suspendido' },
    ]}
  />,
];

export const UserList = () => (
  <List filters={userFilters} sort={{ field: 'createdAt', order: 'DESC' }}>
    <Datagrid rowClick="show" bulkActionButtons={false}>
      <TextField source="fullName" label="Nombre" />
      <TextField source="email" label="Email" />
      <FunctionField label="Roles" render={(r: any) => (r.roles || []).join(', ')} />
      <BooleanField source="isSuspended" label="Suspendido" />
      <DateField source="createdAt" label="Alta" showTime />
    </Datagrid>
  </List>
);

// Suspend/restore/force-reset/role via the custom admin endpoints.
const UserActions = () => {
  const record = useRecordContext();
  const notify = useNotify();
  const refresh = useRefresh();
  if (!record) return null;

  const run = async (path: string, ok: string, body?: unknown) => {
    try {
      await adminFetch(`/users/${record.id}/${path}`, {
        method: 'POST',
        body: body ? JSON.stringify(body) : undefined,
      });
      notify(ok, { type: 'success' });
      refresh();
    } catch (e: any) {
      notify(e.message || 'Error', { type: 'error' });
    }
  };

  const changeRole = () => {
    const role = window.prompt('Nuevo rol (Customer, Provider, Admin):', 'Provider');
    if (role) run('role', `Rol cambiado a ${role}`, { role });
  };

  return (
    <TopToolbar>
      {record.isSuspended ? (
        <Button label="Restaurar" onClick={() => run('restore', 'Usuario restaurado')} />
      ) : (
        <Button label="Suspender" onClick={() => run('suspend', 'Usuario suspendido')} />
      )}
      <Button label="Cambiar rol" onClick={changeRole} />
      <Button label="Forzar reset" onClick={() => run('force-reset', 'Código de reset enviado')} />
    </TopToolbar>
  );
};

export const UserShow = () => (
  <Show actions={<UserActions />}>
    <SimpleShowLayout>
      <TextField source="fullName" label="Nombre" />
      <TextField source="email" label="Email" />
      <FunctionField label="Roles" render={(r: any) => (r.roles || []).join(', ')} />
      <BooleanField source="isSuspended" label="Suspendido" />
      <DateField source="createdAt" label="Alta" showTime />
      <NumberField source="totalRequests" label="Solicitudes totales" />
      <NumberField source="completedRequests" label="Completadas" />
      <NumberField source="cancelledRequests" label="Canceladas" />

      <ArrayField source="recentRequests" label="Solicitudes recientes">
        <Datagrid bulkActionButtons={false}>
          <TextField source="categoryName" label="Categoría" />
          <TextField source="status" label="Estado" />
          <NumberField source="responseCount" label="Cotizaciones" />
          <DateField source="createdAt" label="Fecha" showTime />
        </Datagrid>
      </ArrayField>

      <ArrayField source="reviewsGiven" label="Reseñas dadas">
        <Datagrid bulkActionButtons={false}>
          <TextField source="counterpartName" label="Para" />
          <NumberField source="rating" label="Rating" />
          <TextField source="comment" label="Comentario" />
          <DateField source="createdAt" label="Fecha" />
        </Datagrid>
      </ArrayField>

      <ArrayField source="reviewsReceived" label="Reseñas recibidas">
        <Datagrid bulkActionButtons={false}>
          <TextField source="counterpartName" label="De" />
          <NumberField source="rating" label="Rating" />
          <TextField source="comment" label="Comentario" />
          <DateField source="createdAt" label="Fecha" />
        </Datagrid>
      </ArrayField>
    </SimpleShowLayout>
  </Show>
);
