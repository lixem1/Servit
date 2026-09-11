import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Dev proxy sends /api to the local backend; in prod nginx proxies /api.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5202', changeOrigin: true },
    },
  },
});
