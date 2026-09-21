import { useState, type ReactNode } from 'react';
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
  Edit,
  SimpleForm,
  TopToolbar,
  Button,
  EditButton,
  useRecordContext,
  useNotify,
  useRefresh,
  required,
  email,
} from 'react-admin';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  TextField as MuiTextField,
  Button as MuiButton,
} from '@mui/material';
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

// Edit form for name and email.
export const UserEdit = () => (
  <Edit mutationMode="pessimistic">
    <SimpleForm>
      <TextInput source="fullName" label="Nombre completo" validate={required()} fullWidth />
      <TextInput source="email" label="Email" validate={[required(), email()]} fullWidth />
    </SimpleForm>
  </Edit>
);

// Confirmation dialog for "Dar de baja" — requires typing "ELIMINAR".
const DeactivateDialog = ({
  open,
  onClose,
  onConfirm,
}: {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
}) => {
  const [text, setText] = useState('');
  const match = text.trim().toUpperCase() === 'ELIMINAR';

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Dar de baja al usuario</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          Esta acción dará de baja permanente al usuario. No podrá iniciar sesión.
          Escribe <strong>ELIMINAR</strong> para confirmar.
        </DialogContentText>
        <MuiTextField
          autoFocus
          fullWidth
          size="small"
          placeholder="Escribe ELIMINAR"
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
      </DialogContent>
      <DialogActions>
        <MuiButton onClick={onClose}>Cancelar</MuiButton>
        <MuiButton
          onClick={() => {
            onConfirm();
            setText('');
          }}
          disabled={!match}
          color="error"
          variant="contained"
        >
          Confirmar baja
        </MuiButton>
      </DialogActions>
    </Dialog>
  );
};

// Suspend/restore/force-reset/role/deactivate via the custom admin endpoints.
const UserActions = () => {
  const record = useRecordContext();
  const notify = useNotify();
  const refresh = useRefresh();
  const [deactivateOpen, setDeactivateOpen] = useState(false);
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

  const handleDeactivate = () => {
    setDeactivateOpen(false);
    run('suspend', 'Usuario dado de baja');
  };

  return (
    <>
      <TopToolbar>
        <EditButton />
        {record.isSuspended ? (
          <Button label="Restaurar" onClick={() => run('restore', 'Usuario restaurado')} />
        ) : (
          <Button label="Suspender" onClick={() => run('suspend', 'Usuario suspendido')} />
        )}
        <Button label="Cambiar rol" onClick={changeRole} />
        <Button label="Forzar reset" onClick={() => run('force-reset', 'Código de reset enviado')} />
        {!record.isSuspended && (
          <Button
            label="Dar de baja"
            onClick={() => setDeactivateOpen(true)}
            sx={{ color: 'error.main' }}
          />
        )}
      </TopToolbar>
      <DeactivateDialog
        open={deactivateOpen}
        onClose={() => setDeactivateOpen(false)}
        onConfirm={handleDeactivate}
      />
    </>
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
