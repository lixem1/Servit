# 📱 Servit Android - Setup & Deployment

## APK Compilado

**Location:** `mobile/build/app/outputs/flutter-apk/app-release.apk`
**Versión:** Release
**Arquitectura:** ARM64
**Tamaño:** ~60-80 MB (estimado)
**Backend:** Oracle Cloud (159.54.142.12:5220)

## Instalación en Dispositivo Físico

### Opción 1: Transferencia directa (más fácil)

**macOS → Android:**
```bash
# 1. Conectar dispositivo Android por USB
# 2. Habilitar "Depuración USB" en Configuración > Opciones de desarrollo

# 3. Instalar APK
adb install -r mobile/build/app/outputs/flutter-apk/app-release.apk

# 4. Verificar instalación
adb shell pm list packages | grep servit
```

### Opción 2: Google Drive o correo
1. Subir APK a Google Drive
2. Compartir enlace al dispositivo
3. Descargar en el dispositivo
4. Abrir con gestor de archivos
5. Instalar APK

### Opción 3: Servidor HTTP local
```bash
# En macOS (en otra terminal)
python3 -m http.server 8000 --directory mobile/build/app/outputs/flutter-apk

# En dispositivo Android:
# 1. Conectarse a WiFi con la Mac
# 2. Abrir navegador
# 3. Ir a http://<IP-MAC>:8000/app-release.apk
# 4. Descargar e instalar
```

## Requisitos del Dispositivo

- **Android:** 5.0+ (API 21+)
- **RAM:** 2GB mínimo (3GB recomendado)
- **Almacenamiento:** 150MB libre
- **Permisos necesarios:**
  - Ubicación (GPS)
  - Cámara
  - Almacenamiento (fotos)
  - Internet

## Primer Inicio

1. **Permitir permisos** cuando la app los solicite
2. **Crear cuenta** o **iniciar sesión**
3. **Habilitar ubicación** para usar el servicio
4. **Listo!** — La app conectará a Oracle Cloud backend

## Características Funcionales

✅ Autenticación JWT + Google Sign-In  
✅ Ubicación en tiempo real  
✅ Búsqueda de proveedores cercanos  
✅ Creación de solicitudes de servicio  
✅ Chat en tiempo real (SignalR)  
✅ Calificaciones y reseñas  
✅ Subida de fotos con compresión automática  
✅ Gestión de cuenta (perfil, contraseña)  
✅ Auto-logout en sesión expirada  

## Build Alternativo: App Bundle (Google Play)

Para distribuir en Google Play Store:

```bash
flutter build appbundle \
  --release \
  --dart-define=API_HOST=159.54.142.12 \
  --dart-define=API_PORT=5220
```

Output: `mobile/build/app/outputs/bundle/release/app-release.aab`

Este formato se sube a Google Play Console y se distribuye automáticamente.

## Notas de Desarrollo

- **Backend:** Oracle Cloud Always Free (159.54.142.12:5220)
- **Base de Datos:** PostgreSQL con PostGIS
- **Seguridad:** 
  - JWT tokens (7 días validez)
  - Rate limiting en API
  - Contraseñas hasheadas
  - Compresión de imágenes
- **Estado:** Producción (con limitaciones de Always Free)

## Troubleshooting

### "App se cierra al abrir"
- Verificar que el dispositivo tiene acceso a internet
- Permitir todos los permisos cuando se soliciten
- Limpiar cache: Configuración > Aplicaciones > Servit > Almacenamiento > Borrar caché

### "No encuentra el servidor"
- Verificar que Oracle Cloud VM está corriendo: `docker-compose ps`
- Probar conectividad: `ping 159.54.142.12`
- Verificar que Android tiene conexión a internet

### "Errores de ubicación"
- Habilitar GPS en dispositivo
- Permitir permisos de ubicación "Siempre"
- Esperar 10-15 segundos para primera lectura de GPS

### "No puedo subir fotos"
- Permitir acceso a cámara/galería
- Verificar espacio en almacenamiento
- Intentar con una foto más pequeña primero

---

*Última actualización: 2026-09-05*
*Compilado desde: main branch*
