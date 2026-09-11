import {
  List,
  Datagrid,
  TextField,
  NumberField,
  TextInput,
  Create,
  Edit,
  SimpleForm,
  required,
} from 'react-admin';

const categoryFilters = [
  <TextInput key="search" source="search" label="Buscar" alwaysOn />,
];

export const CategoryList = () => (
  <List filters={categoryFilters} sort={{ field: 'name', order: 'ASC' }}>
    <Datagrid rowClick="edit">
      <TextField source="id" label="ID" />
      <TextField source="name" label="Nombre" />
      <TextField source="description" label="Descripción" />
      <NumberField source="providerCount" label="Proveedores" />
      <NumberField source="requestCount" label="Solicitudes" />
    </Datagrid>
  </List>
);

export const CategoryCreate = () => (
  <Create>
    <SimpleForm>
      <TextInput source="name" label="Nombre" validate={required()} />
      <TextInput source="description" label="Descripción" multiline fullWidth />
    </SimpleForm>
  </Create>
);

export const CategoryEdit = () => (
  <Edit>
    <SimpleForm>
      <TextInput source="name" label="Nombre" validate={required()} />
      <TextInput source="description" label="Descripción" multiline fullWidth />
    </SimpleForm>
  </Edit>
);
