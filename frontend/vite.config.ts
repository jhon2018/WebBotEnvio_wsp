import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Todo lo que empiece por /api se redirige al backend .NET en dev.
      // Así React nunca llama directamente a WAHA y se evita CORS en local.
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        // Necesario para multipart/form-data (upload de archivos):
        // sin esto el proxy de Vite puede cortar el stream antes de enviarlo.
        configure: (proxy) => {
          proxy.on('error', (err) => {
            console.error('[vite-proxy] error:', err.message);
          });
        },
        // Timeout generoso para archivos grandes (xlsx con 1000+ registros)
        timeout: 60000,
      },
    },
  },
})
