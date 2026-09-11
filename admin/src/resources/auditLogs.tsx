import {
  List,
  Datagrid,
  TextField,
  DateField,
  Show,
  SimpleShowLayout,
  TextInput,
  DateInput,
} from 'react-admin';

const auditFilters = [
  <TextInput key="search" source="search" label="Buscar (acción/entidad)" alwaysOn />,
  <TextInput key="action" source="action" label="Acción" />,
  <TextInput key="entityType" source="entityType" label="Tipo de entidad" />,
  <DateInput key="from" source="dateFrom" label="Desde" />,
  <DateInput key="to" source="dateTo" label="Hasta" />,
];

export const AuditLogList = () => (
  <List filters={auditFilters} sort={{ field: 'createdAt', order: 'DESC' }}>
    <Datagrid rowClick="show" bulkActionButtons={false}>
      <DateField source="createdAt" label="Cuándo" showTime />
      <TextField source="adminUserName" label="Admin" />
      <TextField source="action" label="Acción" />
      <TextField source="entityType" label="Entidad" />
      <TextField source="entityId" label="ID entidad" />
    </Datagrid>
  </List>
);

export const AuditLogShow = () => (
  <Show>
    <SimpleShowLayout>
      <DateField source="createdAt" label="Cuándo" showTime />
      <TextField source="adminUserName" label="Admin" />
      <TextField source="adminUserId" label="Admin ID" />
      <TextField source="action" label="Acción" />
      <TextField source="entityType" label="Tipo de entidad" />
      <TextField source="entityId" label="ID entidad" />
      <TextField source="metadataJson" label="Metadata" />
      <TextField source="oldValueJson" label="Valor anterior" />
      <TextField source="newValueJson" label="Valor nuevo" />
    </SimpleShowLayout>
  </Show>
);
