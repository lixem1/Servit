# Deploy Fase 3 — Panel administrativo (VM + Cloudflare Tunnel)

> **Estado:** artefactos en repo, listos. El encendido en la VM es **humano-gated**
> (requiere secretos que solo tú tienes: dominio/hostname + token de Cloudflare, y
> acceso SSH a la VM). Este runbook es el procedimiento exacto — AP-24..AP-27.

## Arquitectura

```
  navegador (HTTPS)
        │
        ▼
  Cloudflare Edge  ──túnel cifrado──►  cloudflared (contenedor en la VM)
                                              │
                                              ▼
                                        admin (nginx :80)
                                        ├─ /            → SPA React (dist)
                                        └─ /api/…        → proxy_pass http://api:8080
                                                                 │
                                                                 ▼
                                                           api (:8080, expuesto 5220 para mobile)
                                                                 │
                                                                 ▼
                                                           db (Postgres+PostGIS)
```

- El panel y el API quedan en el **mismo origen** (nginx sirve la SPA y proxya `/api`),
  así que el panel **no necesita CORS**.
- La app **mobile sigue igual**: apunta a `159.54.142.12:5220` (puerto del API sin cambios).
- La IP de la VM **no se expone** para el panel: todo el tráfico web entra por Cloudflare.

## Artefactos (ya en repo)

| Archivo | Rol |
|---|---|
| `admin/Dockerfile` | build node:22 → runtime nginx:alpine (sirve `dist/`) |
| `admin/nginx.conf` | SPA (`try_files … /index.html`) + `proxy_pass /api → api:8080` + headers |
| `admin/.dockerignore` | excluye node_modules/dist/e2e-auth del contexto |
| `backend/docker-compose.yml` | servicios `admin` y `cloudflared`; env `Admin__*`, `Cors__AllowedOrigins__0` en `api` |
| `backend/.env.example` | vars nuevas: `ADMIN_EMAIL/PASSWORD`, `PANEL_ORIGIN`, `CLOUDFLARE_TUNNEL_TOKEN`, `SMTP_*` |
| `Program.cs` (CORS) | `Cors:AllowedOrigins` → `WithOrigins(...)` si está seteado; si no, `AllowAnyOrigin` (mobile) |

---

## Paso 0 — Prerrequisitos (una sola vez)

1. **Dominio en Cloudflare.** Ten un dominio con los nameservers apuntando a Cloudflare
   (plan gratuito sirve). Elige el hostname del panel, p. ej. `panel.tu-dominio.com`.
2. **Crear el túnel (Zero Trust).**
   - Cloudflare Dashboard → **Zero Trust → Networks → Tunnels → Create a tunnel**.
   - Tipo **Cloudflared**. Nómbralo p. ej. `servit-panel`.
   - Copia el **token** que muestra (empieza con `eyJ…`). Ese es `CLOUDFLARE_TUNNEL_TOKEN`.
   - En **Public Hostnames** del túnel, agrega:
     - Subdomain: `panel` · Domain: `tu-dominio.com`
     - Service: **HTTP** → `admin:80`  (nombre del servicio docker, no la IP).

## Paso 1 — Configurar `.env` en la VM

```bash
ssh -i ~/.ssh/servit-oracle.key ubuntu@159.54.142.12
cd ~/Servit/backend        # ruta del repo en la VM
```

Edita `.env` (NO se commitea) y agrega/rellena:

```dotenv
ADMIN_EMAIL=admin@tu-dominio.com
ADMIN_PASSWORD=<contraseña-fuerte>
PANEL_ORIGIN=https://panel.tu-dominio.com
CLOUDFLARE_TUNNEL_TOKEN=eyJ...el-token-del-paso-0...
```

> `PANEL_ORIGIN` sin barra final. Si lo dejas vacío, el API cae a `AllowAnyOrigin`
> (mobile sigue funcionando, pero el panel queda sin restricción cross-origin).

## Paso 2 — Traer el código y construir

```bash
cd ~/Servit
git pull origin main

cd backend
docker-compose build admin api      # construye la SPA y el API
docker-compose up -d                 # levanta db, api, admin, cloudflared
```

## Paso 3 — Verificación

```bash
# Contenedores arriba (incluye admin y cloudflared):
docker-compose ps

# El API sigue sirviendo al mobile:
curl -s http://localhost:5220/api/categories | head -c 200

# nginx sirve la SPA y proxya el API dentro de la red docker:
docker-compose exec admin wget -qO- http://localhost/api/categories | head -c 200

# El túnel conectó (busca "Registered tunnel connection"):
docker-compose logs --tail=30 cloudflared
```

Luego, en el navegador: **https://panel.tu-dominio.com**
- Debe cargar el login del panel.
- Entra con `ADMIN_EMAIL` / `ADMIN_PASSWORD` (el seeder lo creó al arrancar el API).
- Un usuario no-admin debe ser rechazado ("no tiene acceso de administrador").

## Paso 4 — Smoke manual (checklist)

- [ ] Login admin OK; login no-admin rechazado.
- [ ] Menú: Usuarios, Proveedores, Solicitudes, Reseñas, Categorías, Analítica, Mapa, Auditoría cargan sin error.
- [ ] Dashboard: KPIs + "Exportar CSV" descarga `servit-solicitudes.csv`.
- [ ] Categorías: crear → editar → eliminar.
- [ ] Solicitud: detalle con timeline + cambio de estado.
- [ ] Mobile (iPhone/Android) sigue conectando a `159.54.142.12:5220` sin cambios.

---

## Rollback

```bash
cd ~/Servit/backend
# Quitar solo el panel, dejando API+DB intactos:
docker-compose stop admin cloudflared
docker-compose rm -f admin cloudflared
```

El API y la DB no se tocan; el mobile no se ve afectado en ningún momento
(el panel vive en contenedores separados y en otro camino de red).

## Notas de seguridad

- Secretos (`ADMIN_PASSWORD`, `CLOUDFLARE_TUNNEL_TOKEN`) viven **solo** en `.env` de la VM
  (o user-secrets), **nunca** en git.
- Opcional (recomendado): en Cloudflare **Zero Trust → Access** añade una policy
  (email OTP / Google) delante de `panel.tu-dominio.com` para una segunda capa antes
  del propio login del panel.
- El puerto `5220` del API sigue abierto para el mobile; si en el futuro el mobile
  también pasa por Cloudflare, se puede cerrar en el firewall de Oracle.
