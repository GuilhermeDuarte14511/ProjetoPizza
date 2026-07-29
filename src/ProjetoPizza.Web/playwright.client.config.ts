import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e-client',
  fullyParallel: false,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: 'http://127.0.0.1:4175',
    trace: 'retain-on-failure',
    ...devices['iPad Pro 11'],
    browserName: 'chromium',
  },
  webServer: {
    command: 'npm run dev -- --host 127.0.0.1 --port 4175',
    url: 'http://127.0.0.1:4175',
    reuseExistingServer: true,
    env: { VITE_API_URL: 'http://127.0.0.1:5080' },
  },
})
