import {
  List,
  Datagrid,
  TextField,
  NumberField,
  BooleanField,
  DateField,
  FunctionField,
  TextInput,
  ReferenceInput,
  SelectInput,
  Show,
  SimpleShowLayout,
} from 'react-admin';

const providerFilters = [
  <TextInput key="search" source="search" label="Buscar (nombre/email)" alwaysOn />,
  <ReferenceInput key="cat" source="categoryId" reference="categories">
    <SelectInput label="Categoría" optionText="name" />
  </ReferenceInput>,
];

export const ProviderList = () => (
  <List filters={providerFilters} sort={{ field: 'averageRating', order: 'DESC' }}>
    <Datagrid rowClick="show" bulkActionButtons={false}>
      <TextField source="fullName" label="Nombre" />
      <TextField source="email" label="Email" />
      <NumberField source="averageRating" label="Rating" options={{ maximumFractionDigits: 2 }} />
      <NumberField source="ratingCount" label="# Reseñas" />
      <NumberField source="categoryCount" label="# Categorías" />
      <NumberField source="completedJobs" label="Trabajos" />
      <BooleanField source="isSuspended" label="Suspendido" />
      <DateField source="createdAt" label="Alta" />
    </Datagrid>
  </List>
);

const pct = (v: number) => `${Math.round((v || 0) * 100)}%`;
const secs = (v?: number) =>
  v == null ? '—' : v >= 3600 ? `${(v / 3600).toFixed(1)} h` : `${Math.round(v / 60)} min`;

export const ProviderShow = () => (
  <Show>
    <SimpleShowLayout>
      <TextField source="fullName" label="Nombre" />
      <TextField source="email" label="Email" />
      <TextField source="bio" label="Bio" />
      <BooleanField source="isSuspended" label="Suspendido" />
      <FunctionField label="Categorías" render={(r: any) => (r.categoryNames || []).join(', ')} />
      <NumberField source="averageRating" label="Rating" options={{ maximumFractionDigits: 2 }} />
      <NumberField source="ratingCount" label="# Reseñas" />
      <NumberField source="completedJobs" label="Trabajos completados" />
      <NumberField source="totalResponses" label="Cotizaciones enviadas" />
      <NumberField source="acceptedResponses" label="Cotizaciones aceptadas" />
      <FunctionField label="Tasa de aceptación" render={(r: any) => pct(r.acceptanceRate)} />
      <FunctionField label="Tiempo medio de respuesta" render={(r: any) => secs(r.averageResponseSeconds)} />
      <FunctionField label="GMV generado" render={(r: any) => (r.gmvGenerated ?? 0).toLocaleString('es-PE')} />
      <DateField source="createdAt" label="Alta" showTime />
    </SimpleShowLayout>
  </Show>
);
