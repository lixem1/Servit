import {
  List,
  Datagrid,
  TextField,
  NumberField,
  DateField,
  TextInput,
  SelectInput,
  ReferenceInput,
  DateInput,
} from 'react-admin';

const reviewFilters = [
  <TextInput key="search" source="search" label="Buscar en comentario" alwaysOn />,
  <ReferenceInput key="prov" source="providerId" reference="providers">
    <SelectInput label="Proveedor" optionText="fullName" />
  </ReferenceInput>,
  <SelectInput
    key="rating"
    source="rating"
    label="Rating"
    choices={[1, 2, 3, 4, 5].map((n) => ({ id: n, name: '★'.repeat(n) }))}
  />,
  <DateInput key="from" source="dateFrom" label="Desde" />,
  <DateInput key="to" source="dateTo" label="Hasta" />,
];

// Deleting a review recalculates the provider's AverageRating/RatingCount server-side.
export const ReviewList = () => (
  <List filters={reviewFilters} sort={{ field: 'createdAt', order: 'DESC' }}>
    <Datagrid>
      <TextField source="providerName" label="Proveedor" />
      <TextField source="customerName" label="Cliente" />
      <NumberField source="rating" label="Rating" />
      <TextField source="comment" label="Comentario" />
      <DateField source="createdAt" label="Fecha" showTime />
    </Datagrid>
  </List>
);
