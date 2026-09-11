import polyglotI18nProvider from 'ra-i18n-polyglot';
import englishMessages from 'ra-language-english';

// The community `ra-language-spanish` is stale (RA v3). We start from the
// bundled English messages and override the common UI chrome to Spanish;
// all resource/field labels are already Spanish in the views themselves.
const messages: any = {
  ...englishMessages,
  ra: {
    ...englishMessages.ra,
    action: {
      ...englishMessages.ra.action,
      add_filter: 'Añadir filtro',
      add: 'Añadir',
      back: 'Volver',
      bulk_actions: '%{smart_count} seleccionado(s)',
      cancel: 'Cancelar',
      clear_input_value: 'Limpiar',
      clone: 'Clonar',
      confirm: 'Confirmar',
      create: 'Crear',
      create_item: 'Crear %{item}',
      delete: 'Eliminar',
      edit: 'Editar',
      export: 'Exportar',
      list: 'Lista',
      refresh: 'Refrescar',
      remove_filter: 'Quitar filtro',
      remove: 'Quitar',
      save: 'Guardar',
      search: 'Buscar',
      show: 'Ver',
      sort: 'Ordenar',
      undo: 'Deshacer',
      unselect: 'Deseleccionar',
      expand: 'Expandir',
      close: 'Cerrar',
      open_menu: 'Abrir menú',
      close_menu: 'Cerrar menú',
    },
    page: {
      ...englishMessages.ra.page,
      dashboard: 'Panel',
      empty: 'Sin %{name} todavía.',
      invite: '¿Quieres añadir uno?',
      list: 'Lista de %{name}',
      loading: 'Cargando',
      not_found: 'No encontrado',
    },
    navigation: {
      ...englishMessages.ra.navigation,
      no_results: 'No hay resultados',
      page_rows_per_page: 'Filas por página:',
      next: 'Siguiente',
      previous: 'Anterior',
      page_range_info: '%{offsetBegin}-%{offsetEnd} de %{total}',
    },
    auth: {
      ...englishMessages.ra.auth,
      username: 'Email',
      password: 'Contraseña',
      sign_in: 'Entrar',
      sign_in_error: 'Error de autenticación, reintenta',
      logout: 'Cerrar sesión',
    },
    notification: {
      ...englishMessages.ra.notification,
      created: 'Elemento creado',
      updated: 'Elemento actualizado',
      deleted: 'Elemento eliminado',
      item_doesnt_exist: 'El elemento no existe',
      http_error: 'Error de comunicación con el servidor',
      logged_out: 'Tu sesión terminó, vuelve a conectarte',
    },
  },
};

export const i18nProvider = polyglotI18nProvider(() => messages, 'es', [
  { locale: 'es', name: 'Español' },
]);
