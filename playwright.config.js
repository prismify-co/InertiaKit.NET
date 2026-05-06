import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/frontend-e2e',
  fullyParallel: true,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['html', { open: 'never' }], ['list']] : 'list',
  timeout: 30_000,
  expect: {
    timeout: 5_000,
  },
  use: {
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
      },
    },
    {
      name: 'webkit',
      use: {
        ...devices['Desktop Safari'],
      },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project examples/MinimalApi/MinimalApi.csproj --urls http://127.0.0.1:6108',
      url: 'http://127.0.0.1:6108',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
    {
      command: 'dotnet run --project examples/Mvc/Mvc.csproj --urls http://127.0.0.1:6109',
      url: 'http://127.0.0.1:6109',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
    {
      command: 'dotnet run --project examples/FastEndpointsExample/FastEndpointsExample.csproj --urls http://127.0.0.1:6110',
      url: 'http://127.0.0.1:6110',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
  ],
});