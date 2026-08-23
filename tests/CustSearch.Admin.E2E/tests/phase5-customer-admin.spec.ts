import { expect, test } from '@playwright/test';

function jwtWithFutureExpiry(): string {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600 })).toString('base64url');
  return `${header}.${payload}.signature`;
}

const tenantUser = {
  userId: 501,
  tenantId: 25,
  tenantCode: 'DEMO-STORE',
  userName: 'owner',
  displayName: 'Demo Shop Owner',
  email: 'owner@example.test',
  isPlatformAdmin: false,
  roles: ['TenantAdmin'],
  permissions: [
    'TenantDashboard.View',
    'TenantStores.View',
    'TenantUsers.View',
    'Staff.View',
    'StoreCategories.View',
    'VoiceCommands.View',
  ],
  storeIds: [101],
};

async function mockPhaseFiveApi(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken: jwtWithFutureExpiry(),
        accessTokenExpiresUtc: new Date(Date.now() + 3_600_000).toISOString(),
        user: tenantUser,
      }),
    });
  });

  await page.route('**/api/tenant/dashboard/summary', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        activeUsers: 4,
        activeStores: 1,
        activeStaff: 3,
        activeCategories: 7,
        openShifts: 2,
        activePresenceSessions: 1,
      }),
    });
  });

  await page.route('**/api/tenant/stores', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 101,
          storeCode: 'SURAT-01',
          storeName: 'Surat Flagship',
          addressLine1: 'Ring Road',
          addressLine2: null,
          landmark: null,
          city: 'Surat',
          district: 'Surat',
          stateOrProvince: 'Gujarat',
          postalCode: '395002',
          countryCode: 'IN',
          latitude: 21.1702,
          longitude: 72.8311,
          geoFenceRadiusMeters: 50,
          externalPlaceId: null,
          locationSource: 1,
          isLocationVerified: false,
          timeZone: 'India Standard Time',
          contactEmail: 'store@example.test',
          contactMobile: null,
          isActive: true,
        },
      ]),
    });
  });

  await page.route('**/api/tenant/stores/101/verify-location', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
  });
}

test('tenant admin signs in and receives API-backed Phase 5 dashboard', async ({ page }) => {
  await mockPhaseFiveApi(page);
  await page.goto('/login');

  await page.getByLabel('Tenant code').fill('DEMO-STORE');
  await page.getByLabel('Email or username').fill('owner');
  await page.getByLabel('Password').fill('safe-e2e-password');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page).toHaveURL(/\/customer-admin\/dashboard$/);
  await expect(page.getByRole('heading', { name: 'Customer Admin' })).toBeVisible();
  await expect(page.getByText('Active stores')).toBeVisible();
  await expect(page.getByText('7', { exact: true })).toBeVisible();
});

test('planned store route exposes location verification and lifecycle controls', async ({ page }) => {
  await mockPhaseFiveApi(page);
  await page.goto('/login');
  await page.getByLabel('Tenant code').fill('DEMO-STORE');
  await page.getByLabel('Email or username').fill('owner');
  await page.getByLabel('Password').fill('safe-e2e-password');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/customer-admin\/dashboard$/);

  await page.getByRole('link', { name: 'Stores' }).first().click();
  await expect(page).toHaveURL(/\/customer-admin\/stores$/);
  await expect(page.getByText('Surat Flagship')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Verify location' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Deactivate' })).toBeVisible();
});
