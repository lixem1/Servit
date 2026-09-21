import { Login } from 'react-admin';

// Patron de puntos sutil (autocontenido, sin imagenes externas).
const dots =
  "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='40' height='40'%3E%3Ccircle cx='2' cy='2' r='1' fill='%23ffffff' fill-opacity='0.05'/%3E%3C/svg%3E\")";

// Login con estilo FIJO (no depende de claro/oscuro del SO): tarjeta oscura
// tipo "glass" sobre degradado azul-slate, con texto claro siempre legible.
export const LoginPage = () => (
  <Login
    sx={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: `
        ${dots},
        radial-gradient(1100px 550px at 18% -10%, rgba(59,130,246,0.35) 0%, transparent 60%),
        radial-gradient(900px 500px at 100% 110%, rgba(37,99,235,0.35) 0%, transparent 55%),
        linear-gradient(135deg, #0f172a 0%, #1e293b 55%, #172033 100%)
      `,

      // Tarjeta centrada, oscura tipo glass.
      '& .RaLogin-card': {
        width: 360,
        margin: '0 auto',
        borderRadius: 3,
        overflow: 'hidden',
        backgroundColor: 'rgba(30, 41, 59, 0.72)',
        backdropFilter: 'blur(10px)',
        border: '1px solid rgba(255,255,255,0.10)',
        boxShadow: '0 24px 50px -12px rgba(0,0,0,0.6)',
      },
      // Franja de marca.
      '& .RaLogin-card::before': {
        content: '"Servit · Admin"',
        display: 'block',
        textAlign: 'center',
        padding: '14px 12px',
        fontWeight: 700,
        letterSpacing: '0.4px',
        color: '#fff',
        background: 'linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%)',
      },
      '& .RaLogin-avatar': { marginTop: 2 },
      '& .RaLogin-icon': { backgroundColor: '#2563eb' },

      // Formulario a ancho completo con padding parejo.
      '& form': { padding: '4px 28px 12px' },
      '& .MuiFormControl-root, & .MuiTextField-root': { width: '100%' },
      '& button[type="submit"]': { width: '100%' },

      // --- Texto SIEMPRE claro (no se pierde en ningun modo) ---
      '& .MuiInputBase-input': { color: '#f8fafc' },
      '& .MuiInputBase-input::placeholder': { color: 'rgba(255,255,255,0.5)' },
      '& .MuiInputLabel-root': { color: 'rgba(255,255,255,0.72)' },
      '& .MuiInputLabel-root.Mui-focused': { color: '#93c5fd' },
      '& .MuiIconButton-root': { color: 'rgba(255,255,255,0.7)' },

      // Campos "filled" translucidos con subrayado claro (azul al enfocar).
      '& .MuiFilledInput-root': { backgroundColor: 'rgba(255,255,255,0.06)' },
      '& .MuiFilledInput-root:hover': { backgroundColor: 'rgba(255,255,255,0.10)' },
      '& .MuiFilledInput-root.Mui-focused': { backgroundColor: 'rgba(255,255,255,0.10)' },
      '& .MuiFilledInput-underline:before': { borderBottomColor: 'rgba(255,255,255,0.28)' },
      '& .MuiFilledInput-underline:hover:before': { borderBottomColor: 'rgba(255,255,255,0.45)' },
      '& .MuiFilledInput-underline:after': { borderBottomColor: '#3b82f6' },

      // Por si el input fuese "outlined".
      '& .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255,255,255,0.25)' },
      '& .MuiOutlinedInput-root.Mui-focused .MuiOutlinedInput-notchedOutline': {
        borderColor: '#3b82f6',
      },
    }}
  />
);
