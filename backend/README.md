# Servit — Backend

ASP.NET Core 10 Web API. Monolito modular: `Servit.Api` (controllers), `Servit.Domain` (entidades), `Servit.Infrastructure` (EF Core / PostgreSQL + PostGIS).

## Requisitos

- .NET SDK 10 (`dotnet --version` → 10.0.400)
- PostgreSQL 16 con PostGIS. En este equipo se instaló **Postgres.app** (sin Homebrew) en `/Applications/Postgres.app`, con datos en `~/.servit-postgres-data`.

## Base de datos: iniciar / detener

```bash
PGBIN="/Applications/Postgres.app/Contents/Versions/16/bin"
PGDATA="$HOME/.servit-postgres-data"

# iniciar
"$PGBIN/pg_ctl" -D "$PGDATA" -l "$PGDATA/server.log" -o "-p 5432 -k $PGDATA" start

# detener
"$PGBIN/pg_ctl" -D "$PGDATA" stop
```

No se abrió vía la app gráfica de Postgres.app — si la abres desde Launchpad, puede intentar levantar su propio servidor por defecto en el mismo puerto 5432 y chocar con este. Usa los comandos de arriba en vez del ícono de la barra de menú.

## Configuración local (secrets)

La cadena de conexión y la clave JWT están en `dotnet user-secrets` (no en `appsettings.json`, para no commitear secretos):

```bash
cd src/Servit.Api
dotnet user-secrets list
```

## Migraciones

```bash
dotnet ef database update \
  --project src/Servit.Infrastructure \
  --startup-project src/Servit.Api
```

## Correr la API

```bash
cd src/Servit.Api
dotnet run
```

Endpoints disponibles:
- `POST /api/auth/register` — body: `{ fullName, email, password, role }` (`role`: `Customer` | `Provider`)
- `POST /api/auth/login` — body: `{ email, password }`
