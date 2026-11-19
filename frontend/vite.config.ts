import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // El backend no expone CORS: redirigimos /api al WebApi (perfil http)
    proxy: {
      '/api': 'http://localhost:5062',
    },
  },
})
