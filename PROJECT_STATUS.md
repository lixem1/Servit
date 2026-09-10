# Servit — Estado del proyecto

> Refleja el estado real al **2026-09-10**: Proyecto completo y funcional en producción (Oracle Cloud Always Free). Backend deployado, iOS funcionando, Android en emulador. Google Sign-In habilitado en Android. Rate limiter endurecido (IMemoryCache). Listo para distribución.

## 1. Qué es Servit

App tipo Uber/InDrive para contratar servicios del hogar (gasfitería, carpintería, melaminería, electricidad, pintura, etc.).

**Reglas de negocio:**
- Modelo de match tipo **InDrive** (no Uber): múltiples proveedores se postulan, el cliente elige manualmente
- Ubicación GPS en tiempo real
- Calificaciones y reseñas
- Subida de fotos con compresión automática
- Chat en tiempo real (SignalR)
- Disponible en iOS y Android

## 2. Stack tecnológico

| Capa | Elección |
|---|---|
| Backend | ASP.NET Core 10 Web API |
| Base de datos | PostgreSQL 16 + PostGIS |
| Auth | JWT Bearer + Google Sign-In |
| Mobile | Flutter (iOS + Android) |
| Estado | Riverpod (AsyncNotifier) |
| Routing | go_router |
| HTTP | dio |
| Almacenamiento seguro | flutter_secure_storage |

## 3. Estado de Deployments

### 3.1 Backend — ✅ PRODUCCIÓN (Oracle Cloud Always Free)

**VM Información:**
- IP: **159.54.142.12:5220**
- Región: Mexico Central (Querétaro)
- Shape: VM.Standard.A1.Flex (2 OCPU, 12GB RAM)
- Costo: **$0 (Always Free permanente)**
- Estatus: Running y healthy

**Docker Containers:**
- ✅ PostgreSQL 16 + PostGIS (healthy)
- ✅ API ASP.NET Core 10 (running)

**Características:**
- Auto-migración EF Core al startup
- Compresión inteligente de imágenes (SkiaSharp, max 1920px, 75% JPEG, ≤2MB)
- Rate limiting (100/min general, 10/h login, 5/h register, 3/h forgot-password)
- Security headers (HSTS, CSP, X-Frame-Options, XSS-Protection)
- JWT tokens (7 días validez)
- Soft delete de cuentas (DeletedAt)
- SMTP para recuperación de contraseña

### 3.2 iOS — ✅ FUNCIONANDO

- Deployado en Kevin's iPhone y Leidy's iPhone
- Apunta a backend Oracle Cloud
- Todas las features funcionales
- Auto-logout en sesión expirada (fix reciente)
- Firma: Apple Team FYRLGKRFVB (Always Free personal)

### 3.3 Android — ✅ APK COMPILADO

- **Ubicación:** `mobile/build/app/outputs/flutter-apk/app-release.apk`
- **Tamaño:** 56 MB
- **Arquitectura:** ARM64
- **Android:** 5.0+ (API 21+)
- **Listo para instalar** en cualquier dispositivo

## 4. Seguridad Implementada (2026-09-06)

### Medidas Activas:
1. **Rate Limiting** — Por IP y endpoint
2. **Security Headers** — HSTS, CSP, X-Frame-Options, etc.
3. **CORS Configurado** — Para app móvil
4. **Validación de entrada** — Email, contraseña, JWT
5. **Soft delete** — Cuentas eliminadas sin perder historial
6. **Auto-logout** — Detecta 401, limpia sesión automáticamente
7. **DDoS Protection** — No hay riesgo de cargos (Always Free limitado)

### Próximas (cuando obtenga dominio):
- HTTPS/TLS (Let's Encrypt)
- WAF nativo de Oracle Cloud
- Enhanced monitoring

## 5. Bug Fixes Recientes (2026-09-06)

### ✅ Google Sign-In en Android (2026-09-10)
**Problema:** "Ocurrió un error inesperado" al pulsar "Continuar con Google" en Android.
**Causa raíz (dos partes):**
1. `google_sign_in` v7.2.0 requiere la API nueva (`GoogleSignIn.instance` + `initialize()` + `authenticate()`), y faltaba pasar el `serverClientId`.
2. El backend solo aceptaba como audiencia el **iOS Client ID**. En Android el idToken lleva audiencia = **Web/Server Client ID**, así que se rechazaba.
**Solución:**
- Mobile: `auth_controller.dart` usa `initialize(serverClientId: <web client id>)` + `authenticate()` (API v7).
- Backend: `AuthController.Google` ahora acepta una lista de audiencias: `GoogleAuth:IosClientId` **y** `GoogleAuth:AndroidClientId`.
- **Android Web Client ID:** `123326191723-a813r1bf79aphngt6p37j93h6b6isj10.apps.googleusercontent.com`
  - Package: `com.servit.servit_app` · SHA-1 debug: `94:FD:EE:A1:5E:12:F2:36:D9:D5:2A:E7:8C:A1:D0:53:29:1C:D1:72`

⚠️ **Paso de deploy pendiente:** setear el client ID en la VM:
```bash
# En la VM (dentro del contenedor/entorno del API):
dotnet user-secrets set "GoogleAuth:AndroidClientId" "123326191723-a813r1bf79aphngt6p37j93h6b6isj10.apps.googleusercontent.com"
# o agregar GoogleAuth__AndroidClientId al .env / docker-compose y reiniciar
```

### ✅ Rate limiter endurecido (2026-09-10)
**Problema:** usaba un `Dictionary` no thread-safe mutado desde middleware concurrente → race conditions y memory leak (crecía sin límite).
**Solución:** reescrito con `IMemoryCache` (ya registrado) + `Interlocked.Increment`. Expira solo tras la ventana de 60s, sin fugas.

### ✅ Otros arreglos (2026-09-10)
- `api_client.dart` ahora respeta `--dart-define=API_PORT` (antes hardcodeaba 5220 e ignoraba el flag).
- Comentario de política CORS aclarado (hacía `AllowAnyOrigin`, no restringía).
- Eliminado scaffolding de plantilla (`WeatherForecast.cs` / `WeatherForecastController.cs`).


### ✅ Auto-logout en sesión expirada
**Problema:** Cuando token expiraba, app mostraba "credenciales inválidas" pero no redirigía a login
**Solución:** 
- ApiClient interceptor detecta 401
- Dispara callback que llama `logout()`
- Router redirige automáticamente a `/login`
- Usuario no necesita hacer nada manualmente

## 6. Features Completadas

### Backend:
- ✅ Auth JWT + Google Sign-In
- ✅ ServiceRequests (CRUD + nearby + matching)
- ✅ Providers con location + rating
- ✅ Categories (5 predefinidas)
- ✅ Attachments (fotos con compresión)
- ✅ Reviews + ratings
- ✅ SignalR real-time
- ✅ Account management
- ✅ Password reset por email
- ✅ Rate limiting
- ✅ Security headers

### Mobile:
- ✅ Login/Register/Google Sign-In
- ✅ Ubicación GPS
- ✅ Ver proveedores cercanos
- ✅ Crear solicitudes de servicio
- ✅ Chat real-time
- ✅ Ver ofertas de proveedores
- ✅ Aceptar/rechazar ofertas
- ✅ Calificar al proveedor
- ✅ Subir fotos
- ✅ Perfil de usuario
- ✅ Cambiar contraseña
- ✅ Recuperar contraseña olvidada
- ✅ Ver perfil del proveedor
- ✅ Mis servicios (customer view)
- ✅ Auto-logout

## 7. Cómo Instalar

### iOS (si tienes Mac)
```bash
cd mobile
flutter run --release --dart-define=API_HOST=159.54.142.12 --dart-define=API_PORT=5220
```

### Android
```bash
# Opción 1: Por USB
adb install -r mobile/build/app/outputs/flutter-apk/app-release.apk

# Opción 2: Servidor HTTP local
python3 -m http.server 8000 --directory mobile/build/app/outputs/flutter-apk
# Luego en Android: http://<TU_IP>:8000/app-release.apk
```

## 8. Información del Backend

**URL:** `http://159.54.142.12:5220`

**Credenciales (almacenadas en `.env` de la VM):**
- POSTGRES_USER=servit
- POSTGRES_PASSWORD=[segura]
- JWT_KEY=[segura]
- GOOGLE_IOS_CLIENT_ID (GoogleAuth:IosClientId)=123326191723-vgl4l913p2u3ftu5fdbjnia34mq8k9cb.apps.googleusercontent.com
- GOOGLE_ANDROID_WEB_CLIENT_ID (GoogleAuth:AndroidClientId)=123326191723-a813r1bf79aphngt6p37j93h6b6isj10.apps.googleusercontent.com

**SSH a la VM:**
```bash
ssh -i ~/.ssh/servit-oracle.key ubuntu@159.54.142.12

# Ver estado de containers:
docker-compose ps

# Ver logs:
docker-compose logs api

# Reiniciar:
docker-compose restart
```

## 9. Costos y Limitaciones

### Costos:
- **Servidor:** $0/mes (Oracle Cloud Always Free)
- **Dominio:** ~$12/año (cuando lo compres)
- **SSL:** $0 (Let's Encrypt)

### Limitaciones Always Free:
- VM: 2 OCPU, 12GB RAM (limitado pero suficiente para MVP)
- Tráfico: Limitado internamente pero sin cargos extras
- Base de datos: 20GB (suficiente para muchos datos)

## 10. Próximos Pasos Recomendados

1. **Dominio:** Comprar dominio y configurar DNS
2. **HTTPS:** Obtener certificado SSL (Let's Encrypt gratis)
3. **Google Play Store:** Publicar APK en tienda
4. **App Store:** Publicar en tienda (requiere Apple Developer $99/año)
5. **Monitoring:** Agregar Sentry o similar para errores
6. **Analytics:** Agregar Firebase Analytics
7. **Notificaciones:** Push notifications (FCM + APNS)
8. **Pagos:** Integrar payment gateway (Stripe, etc)

## 11. Verificación Rápida

**¿Está todo funcionando?**

```bash
# Test backend:
curl http://159.54.142.12:5220/api/categories

# Esperado: JSON array de categorías
# Si falla: verificar que la VM está corriendo (ssh + docker-compose ps)
```

## 12. Notas Finales

- **Código:** Todo pusheado a GitHub (lixem1/Servit)
- **Secrets:** No en git (user-secrets + .env)
- **CI/CD:** Workflows de Oracle VM removidos (VM manual)
- **Estado del código:** Producción (compilado release)
- **Documentación:** SECURITY.md, ANDROID_SETUP.md disponibles

## 13. Equipo de Agentes (Claude Code) — cuándo usarlo

Existe un equipo de 4 subagentes en `.claude/agents/` + un workflow automático `.claude/workflows/dev-qa-loop.js` (nombre `servit-dev-qa-loop`):

- **supervisor** (Sonnet 5) — planifica y descompone en tareas.
- **developer** (Opus 4.8) — implementa Flutter + .NET en rama `agent/<slug>`.
- **qa** (Sonnet 5) — integration tests en simulador iOS + gates (`flutter analyze`, `dotnet build`).
- **release** (Haiku 4.5) — deploy a VM + iPhones.

**⚠️ Regla de uso (importante):** el workflow automático **solo se invoca cuando haya un backlog que lo justifique** (varias features/tareas). Consume **~2–4× más tokens** que la edición directa, así que para fixes puntuales (1–2 archivos) se trabaja directo, sin agentes.

**🔒 Producción:** el deploy a la VM y a los iPhones se hace **solo con aprobación humana explícita** vía el agente `release`; nunca dentro del loop automático. Ni el developer ni el ciclo dev↔QA tocan producción.


---

**Última actualización:** 2026-09-10  
**Estado:** ✅ FUNCIONAL EN PRODUCCIÓN  
**Próxima revisión:** Cuando obtengas dominio o agregues nuevas features
