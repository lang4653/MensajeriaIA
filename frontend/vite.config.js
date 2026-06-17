import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api/service1': {
        target: 'http://localhost:5001',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/service1/, '')
      },
      '/api/service2': {
        target: 'http://localhost:5002',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/service2/, '')
      },
      '/api/service3': {
        target: 'http://localhost:5003',
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path.replace(/^\/api\/service3/, '')
      }
    }
  }
})