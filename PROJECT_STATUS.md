# Servit — Estado del proyecto

> Este documento existe para que cualquier persona o IA pueda retomar el trabajo desde cero si se pierde el contexto de la conversación. Refleja el estado real al **2026-08-24**: Milestone 1 (auth + scaffolding), Milestone 2 (flujo completo de solicitudes de servicio estilo InDrive), Google Sign-In, gestión de cuenta (sección 10), notificaciones en tiempo real comprehensivas (sección 11) y calificaciones visibles para el proveedor (sección 12) completados y verificados end-to-end. En curso: deploy del backend a una VM Ampere de Oracle Cloud Always Free (ver sección 9, bloqueado por capacidad de Oracle) y verificación end-to-end de Google Sign-In con cuenta real (sección 8).

## 1. Qué es Servit

App tipo Uber/InDrive para contratar servicios del hogar (gasfitería, carpintería, melaminería, electricidad, pintura, etc.).

Reglas de negocio clave (decididas explícitamente por el usuario):
- El usuario busca proveedores cercanos por categoría, ve su rating y solicita el servicio.
- **Modelo de match tipo InDrive, NO tipo Uber**: cuando un usuario crea una solicitud, varios proveedores cercanos pueden "postularse" (mostrar interés). El usuario ve la lista completa de todos los que respondieron (con rating/distancia) y **elige manualmente** a uno. No es "el primero que acepta gana".
- Los proveedores ven las solicitudes entrantes y pueden postularse.
- El usuario califica al proveedor al finalizar el servicio y puede subir evidencia (fotos).
- Debe funcionar en Android e iOS.

## 2. Stack tecnológico (decidido con el usuario)

| Capa | Elección | Alternativas descartadas |
|---|---|---|
| Backend | ASP.NET Core 10 Web API | — (preferencia explícita del usuario, confirmada como buen fit) |
| Base de datos | PostgreSQL + PostGIS (vía NetTopologySuite) | — |
| Auth | ASP.NET Core Identity + JWT Bearer | — |
| Mobile | Flutter | React Native, .NET MAUI, nativo |
| State management (Flutter) | Riverpod (`flutter_riverpod`, patrón `AsyncNotifier`) | Provider, Bloc |
| Routing (Flutter) | `go_router` | — |
| HTTP client (Flutter) | `dio` | — |
| Storage de sesión (Flutter) | `flutter_secure_storage` | — |
| Tipografía (Flutter) | `google_fonts` (`Inter`, sustituto libre de SF Pro) | — |

Arquitectura backend: **monolito modular** con 3 proyectos:
- `Servit.Api` — controllers, DTOs, servicios de aplicación (JWT).
- `Servit.Domain` — entidades y constantes de dominio, sin dependencias de infraestructura.
- `Servit.Infrastructure` — EF Core, `DbContext`, migraciones, configuración de persistencia.

El desarrollo local corre directo con Postgres.app + `dotnet run` + `flutter run` (sin Docker). Docker **sí se usa para el deploy en producción** (ver sección 8) — `backend/Dockerfile` + `backend/docker-compose.yml`, no para el flujo de desarrollo día a día en la Mac.

## 3. Metodología de trabajo con el usuario

- El usuario pidió trabajar **"poco a poco, milestone by milestone"** ("en modo plan, poco a poco iremos puliendo todo el proceso"). No adelantar features de milestones futuros sin acordarlo antes.
- Cuando hay ambigüedad de arquitectura/librería, preguntar con `AskUserQuestion` en vez de asumir (así se decidieron Flutter, Riverpod, y el modelo de match InDrive-style).
- El usuario prefiere que las tareas técnicas (instalaciones, fixes) se **ejecuten directamente** en vez de solo describir los pasos, cuando sea posible hacerlo por terminal.
- Convención de verificación: no basta con que compile / pase el analizador. Los cambios se prueban corriendo la app de verdad (simulador/emulador + backend real, sin mocks) antes de darlos por completos.
- Nunca commitear secretos: connection strings y JWT key viven en `dotnet user-secrets`, no en `appsettings.json`. `.gitignore` cubre artefactos de build de .NET y Flutter, y `.DS_Store`.

## 4. Milestone 1 — COMPLETADO Y VERIFICADO

Alcance: scaffolding completo — solución ASP.NET Core + esquema DB + auth (JWT, roles Customer/Provider) + proyecto Flutter inicial con login/register conectados al backend.

### 4.1 Backend (`backend/`)

Estructura de archivos real (sin `bin/`/`obj/`):
```
backend/
├── README.md
├── Servit.slnx
└── src/
    ├── Servit.Api/
    │   ├── Contracts/Auth/{RegisterRequest,LoginRequest,AuthResponse}.cs
    │   ├── Controllers/AuthController.cs
    │   ├── Controllers/WeatherForecastController.cs   # ← template default, no eliminado (limpieza pendiente)
    │   ├── Services/JwtTokenService.cs
    │   ├── Program.cs
    │   ├── appsettings.json / appsettings.Development.json
    │   └── WeatherForecast.cs                          # ← ídem, pendiente eliminar
    ├── Servit.Domain/
    │   ├── Constants/Roles.cs                          # "Customer" / "Provider"
    │   └── Entities/{ApplicationUser,Category,Provider,ProviderCategory}.cs
    └── Servit.Infrastructure/
        └── Persistence/
            ├── ServitDbContext.cs
            └── Migrations/20260818002014_InitialCreate.cs (+ Designer/Snapshot)
```

**Entidades de dominio actuales:**
- `ApplicationUser : IdentityUser<Guid>` — agrega `FullName`, `CreatedAt`, navegación opcional a `Provider`.
- `Provider` — `Id`, `UserId` (FK 1-1 a user), `Bio?`, `Location` (`Point?` de NetTopologySuite, columna `geography (point)`, SRID 4326/WGS84), `AverageRating`, `RatingCount`, `CreatedAt`, colección `ProviderCategories`.
- `Category` — `Id`, `Name`, `Description?`. Seed inicial de 5: Gasfitería, Carpintería, Melaminería, Electricidad, Pintura.
- `ProviderCategory` — tabla puente `Provider`↔`Category` (clave compuesta).

**`ServitDbContext`**: hereda de `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`. Configura relación 1-1 User↔Provider, clave compuesta de `ProviderCategory`, tipo de columna `geography (point)` para `Location`, y el seed de categorías.

**Auth (`AuthController`)** — endpoints ya implementados y **probados end-to-end contra Postgres real**:
- `POST /api/auth/register` — body `{ fullName, email, password, role }` (`role`: `Customer` | `Provider`). Valida rol permitido, crea `ApplicationUser` vía `UserManager`, crea el rol si no existe, asigna rol, devuelve `AuthResponse` con JWT.
- `POST /api/auth/login` — body `{ email, password }`. Verifica credenciales, devuelve `AuthResponse` con JWT y roles del usuario. 401 si credenciales inválidas.
- `AuthResponse` = `{ token, expiresAt, userId, fullName, role }`.

**`Program.cs`**: registra `ServitDbContext` (Npgsql + NetTopologySuite), `AddIdentityCore<ApplicationUser>` + roles + EF stores, JWT Bearer auth (lee `Jwt:Key`/`Issuer`/`Audience` de config, **lanza excepción si falta la key** — así se fuerza a no correr sin secrets configurados), pipeline estándar (`UseAuthentication`, `UseAuthorization`, `MapControllers`).

**Infraestructura local instalada en esta máquina** (macOS, sin Homebrew/sudo desde el shell del agente):
- .NET SDK 10 (10.0.400) — ya estaba instalado.
- `dotnet-ef` global tool, pineado a `10.0.11` (la versión RC por defecto causaba mismatch).
- **Postgres.app** (v2.9.6, sin Homebrew) en `/Applications/Postgres.app`, datos en `~/.servit-postgres-data`. Trae PostGIS/pgRouting/pgvector por defecto.
- Homebrew fue instalado por el usuario mismo en su propia terminal (el Bash sandboxeado del agente no puede pasar el prompt de sudo interactivo).
- CocoaPods vía `brew install cocoapods` (el Ruby de sistema de macOS, 2.6.10, es muy viejo para `ffi`; Homebrew trae su propio Ruby moderno como dependencia).

**Cómo levantar el backend** (ver también `backend/README.md`):
```bash
# 1. Iniciar Postgres.app
PGBIN="/Applications/Postgres.app/Contents/Versions/16/bin"
PGDATA="$HOME/.servit-postgres-data"
"$PGBIN/pg_ctl" -D "$PGDATA" -l "$PGDATA/server.log" -o "-p 5432 -k $PGDATA" start
# ⚠️ NO abrir Postgres.app desde el ícono/Launchpad — puede levantar otro server en el mismo puerto y chocar.

# 2. Aplicar migraciones (solo si es necesario)
cd backend
dotnet ef database update --project src/Servit.Infrastructure --startup-project src/Servit.Api

# 3. Correr la API (necesita ASPNETCORE_ENVIRONMENT=Development para que carguen los user-secrets)
cd src/Servit.Api
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --no-launch-profile --urls=http://0.0.0.0:5220
```

Secrets configurados vía `dotnet user-secrets` (NO están en el repo, ver claves con `dotnet user-secrets list` dentro de `src/Servit.Api`):
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- `ConnectionStrings:DefaultConnection`

### 4.2 Mobile (`mobile/`)

Creado con `flutter create --org com.servit --project-name servit_app mobile`.

Estructura real:
```
mobile/
├── lib/
│   ├── main.dart                                    # ProviderScope + MaterialApp.router, aplica AppTheme
│   ├── core/
│   │   ├── network/{api_client.dart, api_error.dart} # api_client soporta --dart-define=API_HOST (ver 4.6)
│   │   ├── router/app_router.dart                    # go_router + redirect basado en sesión
│   │   └── theme/app_theme.dart                      # Material 3 con estética iOS (ver 4.5)
│   └── features/
│       ├── auth/
│       │   ├── domain/{auth_session.dart, roles.dart}
│       │   ├── data/auth_repository.dart
│       │   └── presentation/{auth_controller.dart, login_screen.dart, register_screen.dart}
│       └── home/presentation/home_screen.dart        # placeholder post-login, restyled (avatar + badge + card)
├── ios/Runner/Info.plist                             # NSAppTransportSecurity/NSAllowsArbitraryLoads (ver 4.6)
├── test/widget_test.dart                             # smoke test (no usa pumpAndSettle, ver nota abajo)
└── integration_test/auth_flow_test.dart              # e2e real: register → home → logout → login
```

**Piezas clave:**
- `ApiClient` (`core/network/api_client.dart`) — envuelve `Dio`, inyecta `Authorization: Bearer <token>` vía interceptor. Base URL: `http://10.0.2.2:5220/api` en Android emulator, `http://localhost:5220/api` en iOS simulator/web/desktop.
- `describeApiError()` (`core/network/api_error.dart`) — traduce errores de Dio: `ValidationProblemDetails` de ASP.NET (`errors` como `Map<string, List>`), listas simples de strings, strings planos, y fallback 401 "Credenciales inválidas.".
- `AuthSession` (`features/auth/domain/auth_session.dart`) — `{ token, expiresAt, userId, fullName, role }`, `fromJson`/`toJson` espejando el `AuthResponse` del backend.
- `AuthRepository` — `register(...)` / `login(...)`, hace POST a `/auth/register` / `/auth/login`.
- `AuthController` (Riverpod `AsyncNotifier<AuthSession?>`, provider `authControllerProvider`) — `build()` restaura sesión desde `flutter_secure_storage`; `register`/`login` usan `AsyncValue.guard` y persisten sesión al éxito; `logout()` borra el storage.
- `LoginScreen` / `RegisterScreen` — forms con validación, muestran loading state y errores vía `SnackBar` (usando `describeApiError`). `RegisterScreen` tiene un `SegmentedButton` para elegir rol (Customer/Provider).
- `HomeScreen` — placeholder: saluda con `fullName`, muestra el rol, botón de logout.
- `app_router.dart` — `GoRouter` con rutas `/login`, `/register`, `/home`. Redirect: no autenticado → `/login`; autenticado en ruta de auth → `/home`. Usa una clase `_AuthRefreshNotifier` (un `ChangeNotifier` que hace `ref.listen(authControllerProvider, ...)`) como `refreshListenable`, porque `AsyncNotifier` de Riverpod no expone un `.stream` usable directamente ahí.

**Dependencias añadidas sobre el template default:** `flutter_riverpod`, `dio`, `go_router` (17.5.0), `flutter_secure_storage`, y como dev dependency `integration_test` (viene con el SDK de Flutter, se usó para el test e2e).

### 4.3 Entorno de desarrollo instalado en esta máquina (macOS)

- Flutter SDK 3.47.0 stable, instalado manualmente (zip + checksum) en `~/development/flutter`. **No está en el PATH global del shell** — hay que exportarlo: `export PATH="$PATH:/Users/lixem/development/flutter/bin"`.
- Android SDK cmdline-tools instalados manualmente en `$ANDROID_HOME/cmdline-tools/latest/` (Android Studio no los trae por defecto).
- `JAVA_HOME` apunta al JBR embebido de Android Studio: `/Applications/Android Studio.app/Contents/jbr/Contents/Home`.
- Xcode 26.6 + iOS Simulator runtime (26.5) descargado vía `xcodebuild -downloadPlatform iOS`.
- `flutter doctor` reporta "No issues found!" en todas las categorías.
- Homebrew instalado en `/opt/homebrew` (por el usuario, en su propia terminal) — si el agente corre comandos que dependen de `brew`/`pod`, agregar `/opt/homebrew/bin` al PATH explícitamente, porque el shell del agente no siempre lo hereda.

### 4.4 Verificación end-to-end realizada (no solo build/analyze)

1. `flutter analyze` → sin issues.
2. `flutter test` (`test/widget_test.dart`) → pasa. *Nota:* el smoke test usa `tester.pump(Duration(milliseconds: 500))` en vez de `pumpAndSettle()`, porque mientras `AuthController.build()` espera la lectura de `flutter_secure_storage` (que no está mockeado en el entorno de widget test), el botón de login muestra un `CircularProgressIndicator` indeterminado que hace que `pumpAndSettle()` nunca converja (timeout). Si se agregan más widget tests con esta forma, aplicar el mismo patrón o mockear el storage.
3. Backend levantado real contra Postgres real (no mocks), confirmado con `curl` (login con credenciales inexistentes → 401 correcto).
4. App Flutter compilada y corrida en un simulador de iOS real (`iPhone 17 Pro`) y en un iPhone físico (ver 4.6), confirmado visualmente con screenshots que login/register/home se renderizan bien, en modo claro y oscuro.
5. **Test de integración real** (`integration_test/auth_flow_test.dart`), corrido con `flutter test integration_test/auth_flow_test.dart -d <simulator-id>` contra el backend real: registra un usuario nuevo (email único con timestamp), verifica navegación a Home con el nombre correcto, hace logout, vuelve a loguearse con las mismas credenciales, verifica que vuelve a Home. **Pasa** (re-verificado después del restyle visual de 4.5). Se confirmó además en los logs del backend que el `INSERT` a `AspNetUsers` ocurrió de verdad en Postgres.
   - Nota de debugging: la contraseña de prueba inicial (`Password123`) fue rechazada por la política default de ASP.NET Identity (requiere al menos un carácter no alfanumérico) — el test lo detectó correctamente vía el `SnackBar` de error, confirmando que el manejo de errores de validación funciona end-to-end. Se corrigió a `Password123!`.

### 4.5 Restyle visual — Material 3 con estética iOS

El usuario pidió modernizar los estilos visuales, con un look similar a iOS. Se preguntó explícitamente con `AskUserQuestion` y se optó por **Material 3 con estética iOS** (no Cupertino nativo, no adaptive-por-plataforma), para mantener comportamiento y mantenibilidad consistentes en Android e iOS.

- **`lib/core/theme/app_theme.dart`** (nuevo) — `AppTheme.light()` / `AppTheme.dark()`, ambos `ThemeData` Material 3. Aplicado en `main.dart` vía `theme`/`darkTheme`/`themeMode: ThemeMode.system` (sigue el modo del sistema operativo).
  - Paleta inspirada en el Human Interface Guidelines de Apple: azul sistema `#007AFF`, fondo agrupado `#F2F2F7` (claro) / negro puro `#000000` (oscuro), tarjetas `#FFFFFF`/`#1C1C1E`, separadores `#E5E5EA`/`#38383A`, label secundario `#8E8E93`.
  - Tipografía: `google_fonts` con `GoogleFonts.interTextTheme()` (sustituto libre de SF Pro, que no es licenciable para redistribución).
  - Theming de `InputDecorationTheme`, `FilledButtonThemeData`, `AppBarTheme`, `SegmentedButtonThemeData`, `SnackBarThemeData`, `CardThemeData`, `DividerThemeData` — bordes redondeados (14-16px), sin elevación/sombras, colores planos.
- **`login_screen.dart` / `register_screen.dart`** — se quitó el `AppBar` centrado; ahora usan un "large title" alineado a la izquierda (patrón de navegación iOS), campos con ícono prefijo (`Icons.mail_outline`, `Icons.lock_outline`, etc.) y toggle de visibilidad de contraseña. `RegisterScreen` agrega un botón de volver manual (`Icons.arrow_back_ios_new`).
- **`home_screen.dart`** — `CircleAvatar` con la inicial del nombre, badge tipo "pill" para el rol, y una `Card` con ícono + texto de ayuda según el rol.
- Verificado visualmente en el simulador de iPhone 17 Pro, en **modo claro y oscuro** (alternando con `xcrun simctl ui booted appearance light|dark`), capturando screenshots de login, register y home. Todo se ve correcto en ambos modos.
- `flutter analyze` y `flutter test` quedaron limpios después del restyle (se corrigió el texto esperado en `test/widget_test.dart` e `integration_test/auth_flow_test.dart`, de `'Iniciar sesión'` a `'Bienvenido'`, ya que el título cambió de `AppBar` a texto de body).

### 4.6 Prueba en iPhone físico

El usuario pidió probar la app en su iPhone físico (no solo simulador). Pasos que se hicieron (una sola vez, no hace falta repetirlos salvo que cambie el dispositivo o se revoque la confianza):
- Se agregó el Apple ID personal del usuario en Xcode (`Xcode > Settings > Accounts`), habilitando firma automática (`Signing & Capabilities > Team`).
- El primer emparejamiento del dispositivo tuvo que ser por cable (requisito de Apple), aunque el objetivo final era depuración inalámbrica — luego se habilitó "Connect via network" en la ventana Devices de Xcode.
- Se activó "Developer Mode" en el iPhone (`Ajustes > Privacidad y seguridad > Modo desarrollador`, requiere reinicio).
- Al primer lanzamiento, iOS bloqueó la app como "desarrollador no confiable" — se resolvió en `Ajustes > General > VPN y administración de dispositivos > [Apple ID] > Confiar`.
- `flutter run -d <device-id>` funciona igual que con el simulador una vez confiado el certificado.

**Nota importante (2026-08-24): usar modo release, no debug, para instalar en teléfonos de prueba (no del desarrollador).** `flutter run` en modo debug (default) mantiene el proceso atado a una conexión viva con el Dart VM Service del Mac — si el teléfono se bloquea, pierde WiFi, o el Mac se duerme, la conexión se corta (`Lost connection to device`) y la app queda inestable/no utilizable hasta volver a correr `flutter run`. Para instalar una versión que el usuario (o alguien probando, ej. Kevin/Leidy) pueda usar en cualquier momento sin pedir un redeploy, usar **modo release**: `flutter run --release -d <device-id> --dart-define=API_HOST=<ip-lan>`. Una vez instalada, el proceso `flutter run` puede matarse inmediatamente (`pkill -f "flutter run --release -d <device-id>"`) sin afectar la app ya instalada — corre standalone. **Caveat**: con la firma de Apple Developer personal/gratis (team `FYRLGKRFVB`, ver 4.6), el build instalado expira a los ~7 días y hay que reinstalar (mismo comando, no requiere cambios de código) — esto seguirá así mientras no se compre la cuenta paga de Apple Developer (ver sección 7).

**Cambios de código necesarios para que la app llegue al backend desde un dispositivo físico** (un iPhone real, a diferencia del simulador o el emulador de Android, no tiene forma de mapear `localhost`/`10.0.2.2` a la máquina de desarrollo):
- `lib/core/network/api_client.dart` — `_resolveBaseUrl()` ahora lee `const overrideHost = String.fromEnvironment('API_HOST')`; si no está vacío, usa `http://$overrideHost:5220/api`. Se corre así: `flutter run -d <device-id> --dart-define=API_HOST=<ip-lan-del-mac>` (ej. `192.168.18.48`, obtenida con `ipconfig getifaddr en0`). El comportamiento default (sin el flag) para emulador Android / simulador iOS / web no cambió.
- `ios/Runner/Info.plist` — se agregó una excepción de App Transport Security:
  ```xml
  <key>NSAppTransportSecurity</key>
  <dict>
      <key>NSAllowsArbitraryLoads</key>
      <true/>
  </dict>
  ```
  Necesaria porque un iPhone físico (a diferencia del Simulator) exige HTTPS o un dominio exento por defecto; `localhost` está exento pero una IP de LAN arbitraria no. **Esto es solo para desarrollo local** — HTTP en claro a una IP de LAN. Debe reconsiderarse/quitarse antes de cualquier build de release.
- Requisito de red: el iPhone y el Mac deben estar en la misma WiFi. El backend debe escucharse en `0.0.0.0` (no solo `localhost`), por eso el comando de arranque usa `--urls=http://0.0.0.0:5220` (ver sección 4.1/5).

### 4.7 Limpieza pendiente (no bloqueante)

- `Servit.Api/Controllers/WeatherForecastController.cs` y `Servit.Api/WeatherForecast.cs` son el template default de ASP.NET Core y no se han eliminado todavía.
- El test de integración usa un email con timestamp (`e2e_<microseconds>@servit.test`) para no chocar con usuarios previos, pero **no limpia los usuarios de prueba que va creando en la base de datos** — con el tiempo se acumulan filas de test en `AspNetUsers`. Considerar limpiar o usar una DB de test separada antes de escalar el uso de este test.
- La excepción de ATS (`NSAllowsArbitraryLoads`) en `Info.plist` es solo para desarrollo — quitarla o acotarla (`NSExceptionDomains`) antes de un build de producción/release.

## 5. Cómo levantar todo desde cero (para retomar)

```bash
# Terminal 1 — Postgres
PGBIN="/Applications/Postgres.app/Contents/Versions/16/bin"
PGDATA="$HOME/.servit-postgres-data"
"$PGBIN/pg_ctl" -D "$PGDATA" -l "$PGDATA/server.log" -o "-p 5432 -k $PGDATA" start

# Terminal 2 — Backend
cd backend/src/Servit.Api
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --no-launch-profile --urls=http://0.0.0.0:5220

# Terminal 3 — Mobile (simulador iOS ya booteado, o -d <device-id>)
export PATH="$PATH:/Users/lixem/development/flutter/bin"
cd mobile
flutter run
# Para probar en un iPhone físico en la misma WiFi (ver 4.6):
# flutter run -d <device-id> --dart-define=API_HOST=<ip-lan-del-mac>
```

## 6. Milestone 2 — COMPLETADO Y VERIFICADO: flujo de solicitudes estilo InDrive

Alcance: todo el ciclo de vida de una solicitud de servicio — creación con adjuntos, matching por cercanía/categoría, postulación de proveedores con **una sola oferta editable por proveedor** (no InDrive-style duplicado), aceptación manual por el cliente, remoción en tiempo real de la solicitud de la lista de todos los proveedores al ser aceptada/cancelada, finalización, reseña, cancelación por el cliente, y una pantalla "Mis servicios" para que el proveedor vea sus trabajos pendientes/realizados con aviso en vivo de aceptación.

### 6.1 Modelo de dominio y matching

**Entidades nuevas** (`Servit.Domain/Entities`): `ServiceRequest` (categoría, descripción, `Location` como `Point` PostGIS, `Status`: `Pending` / `Assigned` / `Completed` / `Cancelled`, `CreatedAt`, colecciones `Attachments` y `Responses`), `ProviderResponse` (mensaje, `ProposedPrice`, `Status`: `Pending` / `Accepted` / `Declined`), `Attachment` (foto/video/audio, `Type`, `FileName`, ligado a un `ServiceRequest`), `Review` (rating 1-5, comentario opcional, ligado a un `ServiceRequest` ya completado).

**Matching**: al crear una solicitud, se buscan proveedores de la misma categoría dentro de `MatchRadiusMeters` usando PostGIS (`Location.IsWithinDistance` / distancia geográfica real, no un bounding box). La lógica de "qué proveedores deben ser notificados" está centralizada en el helper privado `GetMatchedProviderUserIdsAsync(categoryId, location)` en `ServiceRequestsController.cs`, reusado por `Create()`, `AcceptResponse()` y `Cancel()` para no triplicar la query.

**Regla de negocio clave (pedida explícitamente por el usuario, ver 6.3)**: cada proveedor solo puede tener **una `ProviderResponse` activa por solicitud** — responder de nuevo actualiza el precio/mensaje de la oferta existente en vez de crear una duplicada. Enforced con un índice único en `(ServiceRequestId, ProviderId)` a nivel de base de datos, no solo en el controller.

### 6.2 Backend — endpoints (`ServiceRequestsController`, `ProvidersController`, `CategoriesController`)

- `POST /service-requests` — multipart, crea la solicitud + adjuntos (fotos/video/audios), notifica a proveedores matcheados vía SignalR (`ServiceRequestCreated`).
- `GET /service-requests/nearby` — solicitudes `Pending` visibles para el proveedor actual, con `DistanceMeters`, `ResponseCount` y `MyResponse` (la oferta propia del proveedor para esa solicitud, si existe — así el mobile sabe si mostrar "Responder" o "Actualizar oferta").
- `GET /service-requests/mine` — solicitudes del cliente actual (para "Mis solicitudes"), con `HasReview`/`ResponseCount`.
- `GET /service-requests/assigned` — **(nuevo)** trabajos donde el proveedor actual tiene una oferta `Accepted`, con `CustomerName` y `MyResponse` (precio acordado). Alimenta la pantalla "Mis servicios".
- `POST /service-requests/{id}/responses` — el proveedor postula/actualiza su oferta. **Upsert**: busca una `ProviderResponse` existente por `(ServiceRequestId, ProviderId)`; si existe y no está `Accepted`, actualiza `Message`/`ProposedPrice` y resetea `Status` a `Pending`; si no existe, crea una nueva. Si ya fue `Accepted`, rechaza con 400.
- `GET /service-requests/{id}/responses` — lista de ofertas para que el cliente elija (rating/distancia/precio del proveedor).
- `POST /service-requests/{id}/responses/{responseId}/accept` — el cliente acepta una oferta → `ServiceRequest.Status = Assigned`, la oferta aceptada pasa a `Accepted`, las demás a `Declined`; notifica al proveedor aceptado (`ResponseAccepted`) y **a todos los proveedores matcheados** que la solicitud ya no está disponible (`ServiceRequestUnavailable`, ver 6.4).
- `POST /service-requests/{id}/responses/{responseId}/reject` — el cliente rechaza una oferta puntual (no afecta las demás).
- `POST /service-requests/{id}/complete` — el cliente marca el trabajo como terminado → `Status = Completed`.
- `POST /service-requests/{id}/cancel` — **(feature aparte, ver 6.5)** el cliente cancela una solicitud `Pending`/`Assigned` → `Status = Cancelled`, rechaza ofertas pendientes, notifica remoción en tiempo real a los proveedores matcheados.
- `POST /service-requests/{id}/review` — reseña (rating + comentario) sobre una solicitud `Completed`, marca `HasReview`.
- `GET /service-requests/{id}/attachments/{attachmentId}` — descarga de un adjunto.
- `ProvidersController.GetPublicProfile` — perfil público de un proveedor (bio, categorías, rating, reseñas) para la pantalla de detalle.
- `CategoriesController` — listado de categorías (seed inicial de 5).

**`ServiceRequestDto`** (contrato compartido de todos estos endpoints) trae campos opcionales que cada endpoint puebla solo si aplica — patrón establecido a seguir para cualquier campo derivado nuevo: `DistanceMeters`, `HasReview`, `ResponseCount`, `MyResponse` (`MyProviderResponseDto?`), `CustomerName`.

### 6.3 Feature: una sola oferta editable por proveedor + remoción en tiempo real (estilo InDrive)

Pedido explícito del usuario: *"por proveedor solo se debe permitir 1 respuesta a la vez... una vez que el servicio es aceptado ya se debe eliminar de la lista de todos los proveedores, debe ser parecido a indrive"* — antes de este cambio, un proveedor podía responder repetidas veces y acumular varias cotizaciones propias duplicadas en la misma solicitud.

- **Backend**: `Respond()` reescrito como upsert (ver 6.2). Migración `EnforceOneResponsePerProvider` agrega índice único `(ServiceRequestId, ProviderId)` en `ProviderResponse` — antes de aplicarla se detectaron y limpiaron 8 filas duplicadas existentes en la base (dedupe priorizando la fila `Accepted` si existía, si no la más reciente).
- **Tiempo real**: nuevo evento SignalR `HubEvents.ServiceRequestUnavailable` (payload: solo el `Guid` de la solicitud), emitido a todos los proveedores matcheados cuando la solicitud sale de `Pending` (por accept o por cancel). El mobile la remueve de la lista local sin esperar un refresh manual.
- **Mobile** (`nearby_requests_screen.dart`): la tarjeta de una solicitud donde el proveedor ya ofertó muestra un badge "Tu oferta: $X" y el botón cambia a "Actualizar oferta" (precargando el formulario con el mensaje/precio existentes). `NearbyRequestsController` escucha tanto `onRequestCreated` como el nuevo `onRequestUnavailable` para mantener la lista sincronizada en vivo.

### 6.4 Feature: pantalla "Mis servicios" + aviso de oferta aceptada

Pedido explícito del usuario: *"el proveedor de servicios debe tener una forma de ver los servicios que tiene pendientes y los que ya realizo, tambien le debe salir un aviso como que su servicio fue aceptado"*.

- **Backend**: endpoint `GET /service-requests/assigned` (ver 6.2) + campo `CustomerName` en el DTO.
- **Mobile**: nueva pantalla `my_services_screen.dart` (ruta `/provider/services`, tarjeta de navegación "Mis servicios" en el home del proveedor) con dos pestañas — **Pendientes** (`status == Assigned`) y **Realizados** (`status == Completed`) — mostrando categoría, cliente, descripción, adjuntos y precio acordado. `myServicesControllerProvider` (`AsyncNotifier`) hace el fetch inicial y expone `refresh()` para pull-to-refresh (no hay push en tiempo real para `Complete()`, así que esa transición se ve con refresh manual).
- **Aviso de aceptación**: el backend ya emitía `HubEvents.ResponseAccepted` al proveedor aceptado, pero nada en el mobile lo escuchaba. Se agregó `responseAcceptedEventsProvider` (`StreamProvider` sobre `realtimeServiceProvider.onResponseAccepted`) y un `ref.listen(...)` en `home_screen.dart` que muestra un `SnackBar` ("¡Tu oferta fue aceptada! Revisa Mis servicios.") e invalida `myServicesControllerProvider`, apenas ocurre — sin que el proveedor tenga que refrescar nada. `home_screen.dart` también dispara `realtimeServiceProvider.connect()` para el rol proveedor al construirse, para asegurar que la conexión SignalR exista aunque el proveedor no haya abierto "Solicitudes cercanas" en esa sesión.

### 6.5 Otras features de Milestone 2

- **Adjuntos multimedia**: el cliente puede adjuntar fotos, un video y audios a una solicitud (`Attachment` entity, `IFileStorageService`, subida multipart, descarga vía endpoint dedicado). Mobile: `attachment_picker`/`attachments_viewer` widgets.
- **Rechazo por respuesta**: el cliente puede rechazar una oferta puntual sin afectar las demás (distinto de aceptar, que rechaza automáticamente el resto).
- **Rating y reseñas**: al completar una solicitud, el cliente puede calificar (1-5 estrellas) y comentar; se refleja en `AverageRating`/`RatingCount` del proveedor y en su perfil público.
- **Bugfix de refresco de ubicación**: corregido un caso donde la ubicación del proveedor no se refrescaba correctamente antes de calcular matches.
- **Cancelación de solicitud por el cliente**: `POST /service-requests/{id}/cancel`, con confirmación en el mobile (`request_detail_screen.dart`) y notificación de remoción en tiempo real a los proveedores matcheados (mismo mecanismo de 6.3).
- **Label de estado "Cotizada"**: en `status_labels.dart`, una solicitud `Pending` con `ResponseCount > 0` se muestra como "Cotizada" (en vez de "Buscando proveedor") tanto en color como en texto, para que el cliente distinga de un vistazo si ya tiene ofertas esperando su decisión.

### 6.6 Patrones establecidos a reutilizar en features futuras

- **Upsert por `(ServiceRequestId, ProviderId)`** para "una entidad activa por combinación" — reusar si aparece otra regla de unicidad similar.
- **Evento SignalR de "quítalo de tu lista"** (payload mínimo, solo el id) para remociones en tiempo real, en vez de reenviar el objeto completo — separado de los eventos que sí llevan el DTO completo (`ResponseReceived`, `ResponseAccepted`, etc.).
- **Campos opcionales en `ServiceRequestDto`** poblados solo por el endpoint que los necesita — no agregar lógica condicional dentro de `ToDto`, sino pasar el valor ya resuelto como parámetro opcional.
- **`AsyncNotifierProvider` con `build()` + `refresh()`** para toda lista fetcheada del backend; **`StreamProvider` standalone** para eventos en tiempo real que solo importan como side-effect (SnackBar, invalidation), sin que ese provider posea el estado de una lista.
- **Deploy secuencial a dos dispositivos** (fisico + simulador): matar todo `flutter run`, borrar `mobile/build/ios` y `DerivedData/Runner-*`, lanzar uno con `nohup ... &`, esperar el marcador de éxito en el log ("Flutter run key commands") antes de lanzar el segundo — lanzarlos en paralelo corrompió el build de Xcode en un incidente previo.

## 7. Próximos milestones (no iniciados)

No se ha discutido aún el detalle técnico de lo que sigue — habrá que retomarlo con el usuario cuando se llegue a esa etapa, siguiendo la misma metodología: preguntar antes de asumir decisiones de arquitectura no triviales. Algunas áreas conocidas de limpieza pendiente:

- `Servit.Api/Controllers/WeatherForecastController.cs` y `Servit.Api/WeatherForecast.cs` (template default de ASP.NET Core) siguen sin eliminarse.
- La excepción de ATS (`NSAllowsArbitraryLoads`) en `Info.plist` es solo para desarrollo local — quitarla o acotarla antes de un build de producción/release.
- **Decisión explícita del usuario (2026-08-24): no se comprará la cuenta paga de Apple Developer Program (US$99/año) por ahora.** Esto bloquea las push notifications reales de iOS (requieren el entitlement de Push Notifications, que Apple solo habilita con cuenta paga). Mientras esto no cambie, "notificaciones" en este proyecto significa el sistema en tiempo real vía SignalR (sección 11) — solo funciona con la app abierta, no hay banners del sistema con la app cerrada. Si el usuario reconsidera esto, el siguiente paso sería Firebase Cloud Messaging (gratis) + el entitlement de iOS + registro de device tokens contra el backend.

## 8. Google Sign-In — COMPLETADO (backend + mobile), verificación end-to-end pendiente

Se agregó login con cuenta de Google como alternativa a email/password.

- **Backend**: `POST /api/auth/google` — recibe el `idToken` de Google, lo valida contra el Client ID de iOS configurado en `GoogleAuth:IosClientId` (secret, ver `dotnet user-secrets`), crea o encuentra el `ApplicationUser` correspondiente (por email), devuelve el mismo `AuthResponse` que login/register normales.
- **Mobile**: paquete `google_sign_in`, botón de login con Google en `login_screen.dart`, flujo integrado con `AuthController`/`AuthRepository` existentes (mismo `AuthSession` resultante).
- **Pendiente**: verificar el flujo completo en un dispositivo físico con una cuenta de Google real (el Client ID ya está configurado en backend + `Info.plist`, falta la prueba end-to-end real).

## 9. Deploy a producción — EN CURSO: Oracle Cloud Always Free

### Contexto y decisión

El backend solo corría en la Mac del usuario, con el mobile apuntando a la IP de LAN vía `--dart-define=API_HOST` (ver 4.6) — obliga a tener la laptop encendida y en la misma red que el teléfono. Se evaluó Google Cloud Free Tier (`e2-micro`) primero, pero el usuario terminó de crear su cuenta de Oracle Cloud y se decidió migrar a **Oracle Cloud "Always Free"**: da una VM Ampere A1 real gratis para siempre (no es una función serverless que se duerme).

Se usa **HTTP simple, sin dominio ni TLS** — suficiente para pruebas, ya que la app móvil tiene la excepción ATS (`NSAllowsArbitraryLoads`, ver 4.6) que de todas formas hay que quitar antes de un release real.

El plan detallado, paso a paso, vive en `/Users/lixem/.claude/plans/smooth-crafting-badger.md` (incluye todos los OCIDs de la VCN/subnet/security list/imagen ya creados, la config de la OCI CLI, y los comandos exactos de cada paso restante). Esta sección es el resumen de alto nivel; ese plan es la fuente de verdad con los detalles técnicos.

### Lo ya implementado en el repo (no depende de qué nube se use)

- `backend/Dockerfile` — build multi-stage con `.NET 10` SDK/runtime, expone puerto 8080.
- `backend/docker-compose.yml` — servicio `db` (`postgis/postgis:16-3.4`) + servicio `api` (build del Dockerfile), variables de entorno mapeadas a la convención de config de .NET, puerto publicado `5220:8080`.
- `Program.cs` ya llama a `db.Database.Migrate()` en el arranque — las migraciones de EF se aplican solas al levantar el contenedor, no hace falta correrlas a mano en el servidor.
- El repo **no tiene git remoto ni commits** — el código se transfiere a la VM por `rsync`, no por `git clone`.

### Estado del despliegue (a la fecha de este documento)

1. Se creó la cuenta de Oracle Cloud, se configuró la VCN (`servit-vcn`) + subred pública (`servit-subnet`) + Security List con el puerto `5220` ya abierto, todo vía la consola web primero y luego verificado/gestionado con la **OCI CLI** (instalada y autenticada en esta Mac, ver plan para los OCIDs exactos).
2. Se generó un par de llaves SSH (`~/.ssh/servit-oracle.key` + `.pub`), ya subida a Oracle.
3. **Bloqueante actual**: Oracle está devolviendo `"Out of host capacity"` para el shape Ampere `VM.Standard.A1.Flex` (2 OCPU / 12GB) en la región del usuario (`mx-queretaro-1`) — es un problema de disponibilidad de Oracle, no de la configuración. Es normal y puede tardar de minutos a varios días en resolverse solo.
4. Se automatizó el reintento: script `~/servit-deploy/retry-create-vm.sh` corre en loop (cada 60s) llamando a `oci compute instance launch` hasta que Oracle acepte la creación; al lograrlo notifica por macOS (notificación + voz) y guarda el resultado en `/tmp/servit-instance-created.json`. **Este script corre como proceso en background de la sesión de Claude Code activa** — si se cierra la sesión/terminal o la Mac se duerme, hay que relanzarlo manualmente (comando exacto en el plan).
5. Alternativas si la espera se hace muy larga (discutidas con el usuario, no descartadas): cambiar a shape AMD `VM.Standard.E2.1.Micro` (Always Free, casi nunca sin capacidad, pero solo 1 OCPU/1GB — justo de RAM para Postgres+API), o convertir la cuenta a "Pay As You Go" (agrega tarjeta, el uso Always Free se mantiene gratis pero da prioridad de capacidad).

### Pasos que faltan una vez la VM esté "Running" (detalle completo en el plan)

1. SSH a la VM, instalar Docker, aplicar el fix de `iptables`/`netfilter-persistent` para el puerto 5220 (gotcha conocido de las imágenes Ubuntu de Oracle: bloquean puertos entrantes salvo 22 incluso con la Security List abierta).
2. `rsync` del código de `backend/` desde la Mac a la VM (sin `bin`/`obj`/`uploads`).
3. Crear `.env` en la VM con secrets de producción nuevos (password de Postgres, JWT key — no reusar los de desarrollo).
4. `docker compose up -d --build`, verificar logs.
5. Apuntar el mobile a la IP pública de la VM (`--dart-define=API_HOST=<ip>`), verificar end-to-end en un iPhone físico (registro, crear solicitud, confirmar filas en la DB vía `psql`), y confirmar que los contenedores sobreviven un reinicio de la VM.

**Actualización (2026-08-25): el reintento de creación de la VM ya no depende de la Mac del usuario.** Se migró a un workflow de **GitHub Actions** (`.github/workflows/oracle-vm-retry.yml`, repo privado `lixem1/Servit`) que corre cada 15 minutos en la nube. Cada corrida hace un intento de `oci compute instance launch`; si Oracle sigue sin capacidad, no hace nada más (el próximo intento programado se encarga). Si tiene éxito: envía un correo a `pantojakevin@gmail.com` (reusando las credenciales SMTP ya configuradas para recuperación de contraseña) con el instance ID y la IP pública, y se desactiva solo (`gh workflow disable`) para no seguir corriendo innecesariamente. Las credenciales de OCI y SMTP están como GitHub Actions secrets (encriptados, nunca en el código). El script local `~/servit-deploy/retry-create-vm.sh` quedó obsoleto y se detuvo. Para revisar el estado: `gh run list -R lixem1/Servit --workflow=oracle-vm-retry.yml`.

## 10. Gestión de cuenta — COMPLETADO Y VERIFICADO

Antes no existía forma de editar el nombre, cambiar/recuperar contraseña, o eliminar la cuenta — el único control de sesión era logout.

- **Backend**: `ApplicationUser.DeletedAt` (soft delete, no se borra nada — reseñas/historial de otros usuarios con esa cuenta se conservan). Entidad `PasswordResetCode` (código de 6 dígitos, hash SHA256, expira a los 15 min, se borra al usarse o al pedir uno nuevo). `AccountController` (`[Authorize]`): `GET/PUT /account/me`, `POST /account/change-password` (distingue usuarios con password vs usuarios de Google Sign-In sin password vía `userManager.HasPasswordAsync`), `DELETE /account/me`. `AuthController` extendido con `POST /auth/forgot-password` (siempre 200, no revela si el email existe) y `POST /auth/reset-password`. Envío de email vía `SmtpEmailSender` (MailKit, Gmail SMTP con contraseña de aplicación, config en `dotnet user-secrets` bajo `Smtp:*`). Middleware que devuelve 401 si `DeletedAt` está seteado, aunque el JWT viejo siga sin expirar (JWT es stateless, dura 7 días). `GetMatchedProviderUserIdsAsync` excluye proveedores con `DeletedAt != null` del matching de nuevas solicitudes.
- **Mobile**: feature `features/account/` completa (perfil editable, cambiar/establecer contraseña según `hasPassword`, eliminar cuenta con diálogo de confirmación) + `features/auth/forgot_password_screen.dart` (flujo de 2 pasos: pedir código por email, luego código + nueva contraseña). Ícono de cuenta en el `AppBar` del home junto al de logout.
- Verificado end-to-end: cambio de nombre persiste, cambio de contraseña (con/sin password previo), flujo completo de recuperación con email real de Gmail, eliminación de cuenta bloquea login inmediatamente con el token viejo.

## 11. Notificaciones en tiempo real comprehensivas — COMPLETADO Y VERIFICADO

### 11.1 Contexto: bugs reportados por el usuario probando con Kevin y Leidy

Usando la app en los iPhones físicos de prueba, el usuario reportó tres bugs de sincronización, todos con la misma causa raíz: los `AsyncNotifierProvider` de Riverpod (no-`.autoDispose`) no refetchean solos al revisitar una pantalla, solo si se invalida explícitamente o si llega un push de SignalR — y en varios casos no llegaba ninguno de los dos.

1. **Categorías actualizadas no muestran solicitudes ya existentes**: el proveedor marca categorías nuevas, pero las solicitudes `Pending` creadas *antes* de ese cambio no aparecen en "Solicitudes cercanas". El backend (`GetNearby()`) ya filtraba correctamente por las categorías *actuales* del proveedor — el bug era 100% del mobile (estado cacheado sin invalidar). **Fix**: `ProviderProfileController.updateCategories()` ahora invalida `nearbyRequestsControllerProvider`; además, `nearby_requests_screen.dart` se convirtió a `ConsumerStatefulWidget` que fuerza `refresh()` en `initState()` (defensa en profundidad, por si el push se pierde).
2. **Rechazo de cotización no se reflejaba al proveedor**: cuando el cliente le da "Rechazar" a una oferta puntual (`RejectResponse`, distinto de cancelar toda la solicitud), el backend ya emitía `HubEvents.ResponseDeclined` correctamente, pero nada en el mobile lo escuchaba. El proveedor no se enteraba y no podía reenviar una oferta más barata (comparado explícitamente por el usuario con el flujo de reset de cotización de un conductor en InDrive). **Fix**: nuevo `responseDeclinedEventsProvider`, actualización in-place del `myResponse` en `NearbyRequestsController` (agregado `ServiceRequest.copyWith()`), SnackBar en `home_screen.dart`, y en `nearby_requests_screen.dart` la tarjeta muestra "Tu oferta fue rechazada. Envía una nueva propuesta." con botón "Enviar nueva oferta" (el backend ya permitía reenviar tras un `Declined`, solo faltaba reflejarlo en el cliente).
3. **Servicio aceptado no aparecía en "Mis servicios → Pendientes"**: mismo patrón de causa raíz (provider cacheado sin invalidar/refrescar). **Fix**: `my_services_screen.dart` convertido a `ConsumerStatefulWidget` con `refresh()` forzado en `initState()`.

### 11.2 Decisión de alcance: no habrá push real de iOS (por ahora)

El usuario preguntó explícitamente por notificaciones para "todos los cambios de estado, tanto al cliente como al proveedor". Se le explicó la diferencia entre el sistema actual (SignalR en tiempo real, solo funciona con la app abierta) y push real del sistema operativo (requiere Firebase Cloud Messaging +, para iOS, el entitlement de Push Notifications que exige cuenta paga de Apple Developer, US$99/año). **El usuario decidió explícitamente no comprar la cuenta paga por ahora** ("No, por ahora no."). Ver también sección 7. Consecuencia: todo el trabajo de esta sección es sobre el sistema in-app existente (SignalR), no sobre push nativo.

### 11.3 Cobertura ampliada de eventos (ambos roles)

- **Backend** (`Hubs/HubEvents.cs`): nuevo evento `ServiceRequestCancelled`, distinto de `ResponseDeclined`. Antes, `Cancel()` reusaba `ResponseDeclined` para las ofertas pendientes al cancelar toda la solicitud — mismo evento que "tu cotización fue rechazada, envía una más barata", lo cual era engañoso porque en una cancelación no queda nada que recotizar. Ahora `Cancel()` emite `ServiceRequestCancelled` (payload `{ requestId, categoryName }`, requiere `.Include(sr => sr.Category)`) a los proveedores con oferta pendiente, y sigue emitiendo `ServiceRequestUnavailable` a todos los proveedores matcheados (sin cambios ahí).
- **Mobile** (`realtime_service.dart`): nuevo stream `onServiceRequestCancelled` (+ clase `CancelledRequestInfo`). Nuevos `StreamProvider` globales en `service_requests_controller.dart`: `requestCreatedEventsProvider` (nueva solicitud cercana, para proveedores), `responseReceivedEventsProvider` (nueva cotización recibida, para clientes), `serviceRequestCancelledEventsProvider`.
- **`home_screen.dart`**: la conexión SignalR (`realtimeServiceProvider.connect()`) ahora se abre para **ambos roles** al construir el home (antes solo para proveedores). Listeners agregados: solicitud cancelada (proveedor, invalida `nearbyRequestsControllerProvider`), nueva solicitud cercana (proveedor), nueva cotización recibida (cliente, invalida `myRequestsControllerProvider`) — sumados a los ya existentes de oferta aceptada/rechazada.
- Todos los cambios verificados con `flutter analyze` limpio y redeploy release a ambos iPhones (Kevin, Leidy).

## 12. Calificación visible para el proveedor — COMPLETADO Y VERIFICADO

El usuario notó que el proveedor no tenía forma de ver la calificación que le dio un cliente. Primer intento: tarjeta "Mis calificaciones" en el home (navegando al perfil público propio, reusando `ProviderProfileViewScreen` y la ruta ya existente `/providers/:id`) — **el usuario pidió quitarla**: prefiere ver la calificación directamente en el servicio realizado, sin una sección aparte.

- **Backend**: `ServiceRequestDto` ahora incluye `Rating`/`ReviewComment` (opcionales). `GetAssigned()` hace un solo query extra (`dbContext.Reviews` por los `requestIds` del batch, `ToDictionaryAsync`) y los popula en el DTO vía el parámetro `review` ya existente en `ToDto(...)`.
- **Mobile**: `ServiceRequest` (domain) agrega `rating`/`reviewComment`. En `my_services_screen.dart`, cada tarjeta de la pestaña **Realizados** muestra las estrellas + el comentario del cliente si ya calificó, o el texto "El cliente todavía no ha calificado este servicio." si no. Se removió la tarjeta "Mis calificaciones" del home (y el import/variable de `providerProfileControllerProvider` que ya no hacía falta ahí).
- Verificado con `dotnet build` + `flutter analyze` limpios y redeploy release a ambos iPhones.

---

## 13. Deploy a Oracle Cloud (Actualizado 2026-08-27)

**Estado**: ✅ COMPLETADO — Backend corriendo en VM Oracle Always Free con compresión de imágenes.

### 13.1 VM Creada
- **IP pública**: 159.54.142.12
- **Shape**: VM.Standard.A1.Flex (2 OCPU, 12GB RAM, Always Free)
- **Imagen**: Ubuntu 24.04 arm64
- **Región**: mx-queretaro-1

### 13.2 Código Deployado
- ✅ Dockerfile (multi-stage: SDK 10 → runtime 10)
- ✅ docker-compose.yml (API + PostgreSQL 16 + PostGIS)
- ✅ Auto-migración EF Core al startup (`db.Database.Migrate()`)
- ✅ Compresión inteligente de imágenes (SkiaSharp, max 1920px, JPEG 75%, ≤2MB final)

### 13.3 Resolución de Problemas Técnicos

#### Problema 1: "Out of host capacity" (resolvido)
- **Causa**: Ampere A1.Flex sin capacidad en la región (50+ intentos fallidos)
- **Solución**: Automatización con GitHub Actions workflow `oracle-vm-retry.yml`
  - Retry cada 12 minutos (más agresivo que el inicial de 15min → 20min → 12min final)
  - Exponential backoff al detectar rate limit (TooManyRequests)
  - Email notificación al éxito
  - Auto-desactiva el workflow una vez que la VM esté RUNNING
- **Resultado**: VM creada exitosamente en el 2do intento tras varias horas

#### Problema 2: SixLabors.ImageSharp licencia en Docker (resolvido)
- **Causa**: Validación de licencia de SixLabors falla en Docker pero no localmente
- **Solución**: Reemplazar con SkiaSharp (Microsoft, sin requisitos de licencia)
- **Cambios**: 
  - Remove `SixLabors.ImageSharp` 4.1.1
  - Add `SkiaSharp` 4.151.1 + `SkiaSharp.NativeAssets.Linux`
  - Reescribir `FileStorageService.cs` (compresión con SKCanvas + SKImage)

#### Problema 3: PostgreSQL build error ARM64 (resolvido)
- **Causa**: `postgis/postgis:16-3.4` → "exec format error" en ARM64
- **Solución**: Dockerfile personalizado basado en `postgres:16` + instalar postgis plugin
  - `backend/postgres-init/Dockerfile` (2 líneas: base + apt install postgis)
  - docker-compose.yml ahora construye DB desde `./postgres-init/`
- **Resultado**: Contenedores ambos `healthy` y `running` tras 20s

### 13.4 Configuración de la VM

Secretos (`~/.oci/config` ya importados en GitHub Actions):
```
OCI_USER_OCID=ocid1.user.oc1..aaaaaaaanz3rn5rl677reeyrsgmcfmdgt22wype43cmrkhbsaw7remz6q6qa
OCI_TENANCY_OCID=ocid1.tenancy.oc1..aaaaaaaagq2jr7uxp7mkeda4iadkkhrpf2j4i2igabmjte2s4zppntg63kua
OCI_FINGERPRINT=ee:e0:66:82:0a:20:4e:b4:31:3b:7c:fb:23:37:6a:32
OCI_REGION=mx-queretaro-1
OCI_PRIVATE_KEY=(PEM privada)
OCI_SSH_PUBLIC_KEY=(Ed25519 pública para Ampere)
```

Variables de entorno `.env` (en VM):
```
POSTGRES_USER=servit
POSTGRES_PASSWORD=servit_db_secure_2026
JWT_KEY=K6dEI/3ySV6iTdB8adNUlC/5Qrj1Z0wiZZFcRKtcNSo2lDlw/4T9j15FVvCkbUqH
JWT_ISSUER=servit-api
JWT_AUDIENCE=servit-app
GOOGLE_IOS_CLIENT_ID=123326191723-vgl4l913p2u3ftu5fdbjnia34mq8k9cb.apps.googleusercontent.com
Smtp__Host=mail.denetcon.com
Smtp__Port=465
Smtp__User=developer@denetcon.com
Smtp__Password=-% _YVgS+Z+HL
Smtp__FromEmail=developer@denetcon.com
Smtp__FromName=Servit
```

### 13.5 Verificación
- ✅ Contenedores corriendo: `docker-compose ps` → api + db ambos "Up (healthy)"
- ✅ Migraciones aplicadas al startup
- ✅ API escuchando en `http://[::]:8080` dentro del contenedor, expuesto al puerto 5220 en host
- ✅ Flutter app desplegada a iPhones apuntando a `API_HOST=159.54.142.12:5220`

---
