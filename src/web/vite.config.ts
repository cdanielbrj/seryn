import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

const webModule = (name: string) => fileURLToPath(new URL(`./node_modules/${name}`, import.meta.url))

export default defineConfig({
  publicDir: 'static',
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./app', import.meta.url)),
      '@tests': fileURLToPath(new URL('../../tests/web', import.meta.url)),
      react: webModule('react'),
      'react-router-dom': webModule('react-router-dom'),
      '@testing-library/react': webModule('@testing-library/react'),
      '@testing-library/user-event': webModule('@testing-library/user-event'),
      '@testing-library/jest-dom': webModule('@testing-library/jest-dom'),
      vitest: webModule('vitest'),
    },
  },
  server: { proxy: { '/api': 'http://127.0.0.1:8080', '/health': 'http://127.0.0.1:8080' } },
  test: {
    environment: 'jsdom',
    include: ['tests/web/unit/**/*.test.{ts,tsx}'],
  },
})
