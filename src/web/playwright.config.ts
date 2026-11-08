import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: '../../tests/web/e2e',
  globalSetup: '../../tests/web/setup/playwright.ts',
  outputDir: '../../tests/web/artifacts/results',
  reporter: [['html', { outputFolder: '../../tests/web/artifacts/report', open: 'never' }]],
  fullyParallel: false,
  workers: 1,
  use: { baseURL: process.env.SERYN_E2E_URL ?? 'http://127.0.0.1:8080', trace: 'off', screenshot: 'only-on-failure' },
  projects: [
    { name: 'desktop-chromium', use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 1124 } } },
    { name: 'mobile-webkit', use: { ...devices['iPhone 13'], browserName: 'webkit' } },
  ],
})
