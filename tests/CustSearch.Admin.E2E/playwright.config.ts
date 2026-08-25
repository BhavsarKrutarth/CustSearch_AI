import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  fullyParallel: false,
  retries: 0,
  reporter: 'line',
  use: {
    baseURL: 'http://127.0.0.1:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {command:'node signalr-playwright-server.mjs',url:'http://127.0.0.1:4317/health',reuseExistingServer:false,timeout:30_000},
    {command:'npm --prefix ../../src/CustSearch.Admin start -- --host 127.0.0.1 --port 4200 --proxy-config ../../tests/CustSearch.Admin.E2E/proxy.playwright.json',url:'http://127.0.0.1:4200/login',reuseExistingServer:false,timeout:120_000},
  ],
});
