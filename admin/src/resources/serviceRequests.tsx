import {
  List,
  Datagrid,
  TextField,
  NumberField,
  DateField,
  FunctionField,
  TextInput,
  SelectInput,
  ReferenceInput,
  DateInput,
  Show,
  SimpleShowLayout,
  ArrayField,
  TopToolbar,
  Button,
  DeleteButton,
  useRecordContext,
  useNotify,
  useRefresh,
} from 'react-admin';
import { Stack, Typography } from '@mui/material';
import { adminFetch, openAdminBlob } from '../api';

const STATUS = [
  { id: 'Pending', name: 'En cola' },
  { id: 'Assigned', name: 'En curso' },
  { id: 'Completed', name: 'Completada' },
  { id: 'Cancelled', name: 'Cancelada' },
];

const requestFilters = [
  <TextInput key="search" source="search" label="Buscar (desc./cliente/categoría)" alwaysOn />,
  <SelectInput key="status" source="status" label="Estado" choices={STATUS} />,
  <ReferenceInput key="cat" source="categoryId" reference="categories">
    <SelectInput label="Categoría" optionText="name" />
  </ReferenceInput>,
  <DateInput key="from" source="dateFrom" label="Desde" />,
  <DateInput key="to" source="dateTo" label="Hasta" />,
];

export const ServiceRequestList = () => (
  <List filters={requestFilters} sort={{ field: 'createdAt', order: 'DESC' }}>
    <Datagrid rowClick="show" bulkActionButtons={false}>
      <TextField source="categoryName" label="Categoría" />
      <TextField source="customerName" label="Cliente" />
      <TextField source="status" label="Estado" />
      <NumberField source="responseCount" label="Cotizaciones" />
      <TextField source="acceptedProviderName" label="Proveedor asignado" />
      <DateField source="createdAt" label="Fecha" showTime />
    </Datagrid>
  </List>
);

const RequestActions = () => {
  const record = useRecordContext();
  const notify = useNotify();
  const refresh = useRefresh();
  if (!record) return null;

  const changeStatus = async () => {
    const status = window.prompt('Nuevo estado (Pending, Assigned, Completed, Cancelled):', 'Completed');
    if (!status) return;
    try {
      await adminFetch(`/service-requests/${record.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ status }),
      });
      notify(`Estado cambiado a ${status}`, { type: 'success' });
      refresh();
    } catch (e: any) {
      notify(e.message || 'Error', { type: 'error' });
    }
  };

  const cancel = async () => {
    if (!window.confirm('¿Cancelar esta solicitud?')) return;
    try {
      await adminFetch(`/service-requests/${record.id}/cancel`, { method: 'POST' });
      notify('Solicitud cancelada', { type: 'success' });
      refresh();
    } catch (e: any) {
      notify(e.message || 'Error', { type: 'error' });
    }
  };

  return (
    <TopToolbar>
      <Button label="Cambiar estado" onClick={changeStatus} />
      <Button label="Cancelar" onClick={cancel} />
      <DeleteButton mutationMode="pessimistic" />
    </TopToolbar>
  );
};

// Attachments need the parent request id + attachment id and a Bearer token,
// so they're rendered from the request record with a blob-opening button.
const AttachmentsList = () => {
  const record = useRecordContext();
  const notify = useNotify();
  if (!record) return null;
  const items = (record.attachments as any[]) || [];
  return (
    <Stack spacing={1}>
      {items.length === 0 && <Typography variant="body2">Sin adjuntos</Typography>}
      {items.map((a) => (
        <Button
          key={a.id}
          label={`${a.type}: ${a.fileName}`}
          onClick={() =>
            openAdminBlob(`/service-requests/${record.id}/attachments/${a.id}`).catch((e) =>
              notify(e.message || 'No se pudo abrir', { type: 'error' }),
            )
          }
        />
      ))}
    </Stack>
  );
};

const ReviewBlock = () => {
  const record = useRecordContext();
  const review = record?.review as any;
  if (!review) return <Typography variant="body2">Sin reseña</Typography>;
  return (
    <Typography variant="body2">
      {'★'.repeat(review.rating)} — {review.comment || '(sin comentario)'}
    </Typography>
  );
};

export const ServiceRequestShow = () => (
  <Show actions={<RequestActions />}>
    <SimpleShowLayout>
      <TextField source="categoryName" label="Categoría" />
      <TextField source="customerName" label="Cliente" />
      <TextField source="customerEmail" label="Email cliente" />
      <TextField source="description" label="Descripción" />
      <TextField source="status" label="Estado" />
      <FunctionField label="Ubicación" render={(r: any) => `${r.lat}, ${r.lng}`} />
      <DateField source="createdAt" label="Creada" showTime />

      <FunctionField label="Adjuntos" render={() => <AttachmentsList />} />

      <ArrayField source="timeline" label="Línea de tiempo">
        <Datagrid bulkActionButtons={false}>
          <TextField source="type" label="Evento" />
          <TextField source="description" label="Detalle" />
          <DateField source="at" label="Cuándo" showTime />
        </Datagrid>
      </ArrayField>

      <ArrayField source="quotes" label="Cotizaciones">
        <Datagrid bulkActionButtons={false}>
          <TextField source="providerName" label="Proveedor" />
          <TextField source="message" label="Mensaje" />
          <NumberField source="proposedPrice" label="Precio" />
          <TextField source="status" label="Estado" />
          <DateField source="createdAt" label="Fecha" showTime />
        </Datagrid>
      </ArrayField>

      <FunctionField label="Reseña" render={() => <ReviewBlock />} />
    </SimpleShowLayout>
  </Show>
);
