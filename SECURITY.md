# 🔒 Seguridad de Servit API

## Medidas Implementadas (2026-09-05)

### 1. Rate Limiting ⏱️
- **Límite general**: 100 requests/minuto por IP
- **Login**: 10 intentos/hora por IP
- **Register**: 5 intentos/hora por IP
- **Forgot Password**: 3 intentos/hora por IP
- **Response**: HTTP 429 (Too Many Requests) + Retry-After header

### 2. Security Headers 🛡️
- `X-Content-Type-Options: nosniff` — Previene MIME type sniffing
- `X-Frame-Options: DENY` — Previene clickjacking
- `X-XSS-Protection: 1; mode=block` — Protección contra XSS
- `Strict-Transport-Security: max-age=31536000` — HTTPS obligatorio (1 año)
- `Referrer-Policy: strict-origin-when-cross-origin` — Privacidad de referrer
- `Permissions-Policy` — Desactiva acceso a geolocation, micrófono, cámara
- `Content-Security-Policy` — Restricción de scripts/estilos

### 3. CORS (Cross-Origin Resource Sharing) 🌐
- Configurado para aceptar requests de la app móvil
- Headers expuestos: `content-disposition` (para descargar archivos)

### 4. Autenticación & Autorización 🔐
- JWT Bearer tokens (7 días de validez)
- Validación de issuer, audience, y firma
- Invalidación de tokens para cuentas eliminadas
- Roles: Customer / Provider

### 5. Validación de Entrada ✔️
- Email validation (Identity Core)
- Contraseña mínima 8 caracteres
- Validación de JWT en Google Sign-In
- Sanitización de archivos (solo tipos permitidos)

### 6. Protección de Datos 🔐
- Contraseñas hasheadas (ASP.NET Identity + Bcrypt)
- Soft delete de cuentas (DeletedAt timestamp)
- Tokens de recuperación de contraseña con expiración
- SMTP sin almacenar credenciales en código

### 7. DDoS Protection 📊
**Protegido por Always Free Oracle:**
- No hay método de pago registrado → sin cargos sorpresas
- VM limitada (2 OCPU, 12GB RAM) → escalado limitado
- Rate limiting en aplicación + límites de la VM

**No vulnerable a:**
- Cambio de plan automático por consumo
- Facturación sorpresa por tráfico

**Riesgos residuales:**
- ⚠️ Ataque DDoS volumétrico = API se cae (pero sin cargos)
- ⚠️ Sin WAF (Web Application Firewall) nativo

### 8. Endpoints Sensibles

| Endpoint | Protección |
|----------|-----------|
| `/api/auth/register` | Rate limit 5/h, validación email |
| `/api/auth/login` | Rate limit 10/h, contra brute force |
| `/api/auth/forgot-password` | Rate limit 3/h, código con expiración |
| `/api/account/me` | [Authorize], validación de token |
| `/api/service-requests/*` | [Authorize], SQL injection safe (EF Core) |
| `/api/attachments/upload` | [Authorize], validación de tipo MIME |

## Próximas Mejoras (Tras obtener dominio)

1. **HTTPS/TLS** — Encriptación en tránsito
   - Certificado SSL (Let's Encrypt gratis)
   - Redirect HTTP → HTTPS
   - HSTS preload

2. **WAF (Web Application Firewall)**
   - Oracle Cloud WAF gratis en Always Free
   - Protección contra OWASP Top 10
   - Detección de patrones de ataque

3. **Enhanced Rate Limiting**
   - Cambiar a `AspNetCoreRateLimit` NuGet (actualmente simplificado)
   - Rate limiting por usuario + IP
   - Whitelist de IPs de confianza

4. **Logging & Monitoring**
   - Registrar intentos fallidos de login
   - Alertas para actividad sospechosa
   - Dashboard de métricas de seguridad

5. **CORS Restringido**
   - Cambiar de `AllowAnyOrigin()` a dominio específico
   - Actualmente permite cualquier origen (necesario para desarrollo)

## Cómo Testear Rate Limiting

```bash
# Esto fallará después de 10 intentos en 1 hora
for i in {1..15}; do
  curl -X POST http://159.54.142.12:5220/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"test1234"}'
  sleep 1
done
```

Respuesta esperada en el intento 11+:
```
HTTP/1.1 429 Too Many Requests
Retry-After: 60
```

## Notas de Seguridad

- ✅ **Always Free está protegido**: sin riesgos de cargos por DDoS
- ⚠️ **Dominio falta**: CORS abierto y sin HTTPS por ahora
- ⚠️ **Monitoreo manual**: Sin alertas automáticas de ataques
- ✅ **Datos sensibles seguros**: contraseñas hasheadas, tokens con expiración

## Contacto de Seguridad

Para reportar vulnerabilidades: [pendiente de definir política]

---
*Última actualización: 2026-09-05*
