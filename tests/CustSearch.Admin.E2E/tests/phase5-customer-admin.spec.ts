import { expect, Page, Route, test } from '@playwright/test';

function jwtWithFutureExpiry(): string {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600 })).toString('base64url');
  return `${header}.${payload}.signature`;
}

const fullPermissions = [
  'TenantDashboard.View',
  'TenantStores.View',
  'TenantUsers.View',
  'TenantUsers.Create',
  'TenantUsers.Edit',
  'TenantUsers.AssignRoles',
  'Staff.View',
  'Staff.Manage',
  'StoreCategories.View',
  'StoreCategories.Manage',
  'VoiceCommands.View',
  'VoiceCommands.Configure',
];

function tenantUser(overrides: Record<string, unknown> = {}) {
  return {
    userId: 501,
    tenantId: 25,
    tenantCode: 'DEMO-STORE',
    userName: 'owner',
    displayName: 'Demo Shop Owner',
    email: 'owner@example.test',
    isPlatformAdmin: false,
    roles: ['TenantAdmin'],
    permissions: fullPermissions,
    storeIds: [101],
    ...overrides,
  };
}

interface StoreState {
  id: number;
  storeCode: string;
  storeName: string;
  addressLine1: string;
  addressLine2: string | null;
  landmark: string | null;
  city: string;
  district: string | null;
  stateOrProvince: string;
  postalCode: string;
  countryCode: string;
  latitude: number | null;
  longitude: number | null;
  geoFenceRadiusMeters: number | null;
  externalPlaceId: string | null;
  locationSource: number;
  isLocationVerified: boolean;
  timeZone: string;
  contactEmail: string | null;
  contactMobile: string | null;
  isActive: boolean;
}

interface UserState {
  id: number;
  userName: string;
  email: string;
  displayName: string;
  isActive: boolean;
  roles: string[];
  storeIds: number[];
  createdUtc: string;
}

interface StaffState {
  id: number;
  userId: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  mobile: string | null;
  isActive: boolean;
  storeIds: number[];
}

interface CategoryState {
  id: number;
  storeId: number | null;
  categoryCode: string;
  name: string;
  parentCategoryId: number | null;
  isActive: boolean;
}

interface VoiceState {
  storeId: number;
  triggerKeyword: string;
  responseMode: string;
  isEnabled: boolean;
  requireConfirmationForAmbiguousCategory: boolean;
  aliases: string[];
  languageCode: string;
  requireConfirmation: boolean;
  listeningTimeoutSeconds: number;
  minimumRecognitionConfidence: number;
}

interface MockOptions {
  identity?: ReturnType<typeof tenantUser>;
  quotaRejectUserId?: number;
  rejectTenantWideRole?: boolean;
}

interface MockState {
  stores: StoreState[];
  users: UserState[];
  staff: StaffState[];
  categories: CategoryState[];
  voice: VoiceState;
  calls: string[];
}

const json = (route: Route, body: unknown, status = 200) => route.fulfill({
  status,
  contentType: 'application/json',
  body: JSON.stringify(body),
});

async function mockPhaseFiveApi(page: Page, options: MockOptions = {}): Promise<MockState> {
  const identity = options.identity ?? tenantUser();
  const state: MockState = {
    stores: [{
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
    }],
    users: [{
      id: 601,
      userName: 'sales1',
      email: 'sales1@example.test',
      displayName: 'Sales One',
      isActive: false,
      roles: ['SalesStaff'],
      storeIds: [101],
      createdUtc: '2026-08-23T08:00:00Z',
    }],
    staff: [{
      id: 701,
      userId: 601,
      employeeCode: 'EMP-001',
      firstName: 'Sales',
      lastName: 'One',
      mobile: '9000000001',
      isActive: true,
      storeIds: [101],
    }],
    categories: [{
      id: 801,
      storeId: 101,
      categoryCode: 'SAREE',
      name: 'Sarees',
      parentCategoryId: null,
      isActive: true,
    }],
    voice: {
      storeId: 101,
      triggerKeyword: 'Aasha Add',
      responseMode: 'InAppAndVoice',
      isEnabled: true,
      requireConfirmationForAmbiguousCategory: true,
      aliases: ['Asha Add'],
      languageCode: 'en-IN',
      requireConfirmation: true,
      listeningTimeoutSeconds: 30,
      minimumRecognitionConfidence: 70,
    },
    calls: [],
  };

  await page.route('**/api/auth/login', route => json(route, {
    accessToken: jwtWithFutureExpiry(),
    accessTokenExpiresUtc: new Date(Date.now() + 3_600_000).toISOString(),
    user: identity,
  }));

  await page.route('**/api/tenant/**', async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname.replace('/api/tenant/', '');
    const method = request.method();
    state.calls.push(`${method} ${path}`);

    if (path === 'dashboard/summary' && method === 'GET') {
      return json(route, {
        activeUsers: state.users.filter(x => x.isActive).length,
        activeStores: state.stores.filter(x => x.isActive).length,
        activeStaff: state.staff.filter(x => x.isActive).length,
        activeCategories: state.categories.filter(x => x.isActive).length,
        openShifts: 2,
        activePresenceSessions: 1,
      });
    }

    if (path === 'stores' && method === 'GET') return json(route, state.stores);
    if (path === 'stores' && method === 'POST') {
      const body = request.postDataJSON() as Record<string, unknown>;
      const created: StoreState = {
        id: 102,
        storeCode: String(body['storeCode'] || 'SURAT-02'),
        storeName: String(body['storeName']),
        addressLine1: String(body['addressLine1']),
        addressLine2: body['addressLine2'] ? String(body['addressLine2']) : null,
        landmark: body['landmark'] ? String(body['landmark']) : null,
        city: String(body['city']),
        district: body['district'] ? String(body['district']) : null,
        stateOrProvince: String(body['stateOrProvince']),
        postalCode: String(body['postalCode']),
        countryCode: String(body['countryCode']),
        latitude: body['latitude'] == null ? null : Number(body['latitude']),
        longitude: body['longitude'] == null ? null : Number(body['longitude']),
        geoFenceRadiusMeters: body['geoFenceRadiusMeters'] == null ? null : Number(body['geoFenceRadiusMeters']),
        externalPlaceId: body['externalPlaceId'] ? String(body['externalPlaceId']) : null,
        locationSource: Number(body['locationSource'] ?? 1),
        isLocationVerified: false,
        timeZone: String(body['timeZone']),
        contactEmail: body['contactEmail'] ? String(body['contactEmail']) : null,
        contactMobile: body['contactMobile'] ? String(body['contactMobile']) : null,
        isActive: true,
      };
      state.stores.push(created);
      return json(route, created, 201);
    }
    const storeUpdate = path.match(/^stores\/(\d+)$/);
    if (storeUpdate && method === 'PUT') {
      const target = state.stores.find(x => x.id === Number(storeUpdate[1]))!;
      Object.assign(target, request.postDataJSON());
      return json(route, target);
    }
    const verify = path.match(/^stores\/(\d+)\/verify-location$/);
    if (verify && method === 'POST') {
      const target = state.stores.find(x => x.id === Number(verify[1]))!;
      target.isLocationVerified = true;
      return json(route, target);
    }
    const lifecycle = path.match(/^stores\/(\d+)\/(activate|deactivate)$/);
    if (lifecycle && method === 'POST') {
      const target = state.stores.find(x => x.id === Number(lifecycle[1]))!;
      target.isActive = lifecycle[2] === 'activate';
      return json(route, target);
    }

    if (path === 'users' && method === 'GET') return json(route, state.users);
    if (path === 'users' && method === 'POST') {
      const body = request.postDataJSON() as { userName: string; email: string; displayName: string; roles: string[]; storeIds: number[] };
      if (options.rejectTenantWideRole && body.roles.some(role => ['TenantAdmin', 'TenantOwner', 'ShopOwner'].includes(role))) {
        return json(route, { message: 'Store-scoped administrators cannot assign TenantAdmin, TenantOwner or ShopOwner roles.' }, 400);
      }
      const created: UserState = {
        id: 602,
        userName: body.userName,
        email: body.email,
        displayName: body.displayName,
        isActive: true,
        roles: body.roles,
        storeIds: body.storeIds,
        createdUtc: new Date().toISOString(),
      };
      state.users.push(created);
      return json(route, created, 201);
    }
    const userUpdate = path.match(/^users\/(\d+)$/);
    if (userUpdate && method === 'PUT') {
      const id = Number(userUpdate[1]);
      const body = request.postDataJSON() as { email: string; displayName: string; isActive: boolean };
      if (options.quotaRejectUserId === id && body.isActive) {
        return json(route, { message: 'Tenant user quota (5) has been reached; the account cannot be reactivated.' }, 409);
      }
      const target = state.users.find(x => x.id === id)!;
      Object.assign(target, body);
      return json(route, target);
    }
    const userRoles = path.match(/^users\/(\d+)\/roles$/);
    if (userRoles && method === 'PUT') {
      const target = state.users.find(x => x.id === Number(userRoles[1]))!;
      target.roles = (request.postDataJSON() as { roles: string[] }).roles;
      return json(route, target);
    }
    const userStores = path.match(/^users\/(\d+)\/stores$/);
    if (userStores && method === 'PUT') {
      const target = state.users.find(x => x.id === Number(userStores[1]))!;
      target.storeIds = (request.postDataJSON() as { storeIds: number[] }).storeIds;
      return json(route, target);
    }

    if (path === 'staff' && method === 'GET') return json(route, state.staff);
    if (path === 'staff' && method === 'POST') {
      const body = request.postDataJSON() as { employeeCode: string; firstName: string; lastName: string; mobile: string | null; storeIds: number[] };
      const created: StaffState = {
        id: 702,
        userId: 602,
        employeeCode: body.employeeCode,
        firstName: body.firstName,
        lastName: body.lastName,
        mobile: body.mobile,
        isActive: true,
        storeIds: body.storeIds,
      };
      state.staff.push(created);
      return json(route, created, 201);
    }
    const staffUpdate = path.match(/^staff\/(\d+)$/);
    if (staffUpdate && method === 'PUT') {
      const target = state.staff.find(x => x.id === Number(staffUpdate[1]))!;
      Object.assign(target, request.postDataJSON());
      return json(route, target);
    }

    if (path.startsWith('store-categories') && method === 'GET') return json(route, state.categories);
    if (path === 'store-categories' && method === 'POST') {
      const body = request.postDataJSON() as { storeId: number | null; categoryCode: string; name: string; parentCategoryId: number | null; isActive: boolean };
      const created: CategoryState = { id: 802, ...body };
      state.categories.push(created);
      return json(route, created, 201);
    }
    const categoryUpdate = path.match(/^store-categories\/(\d+)$/);
    if (categoryUpdate && method === 'PUT') {
      const target = state.categories.find(x => x.id === Number(categoryUpdate[1]))!;
      Object.assign(target, request.postDataJSON());
      return json(route, target);
    }

    const voice = path.match(/^stores\/(\d+)\/voice-command-runtime$/);
    if (voice && method === 'GET') return json(route, state.voice);
    if (voice && method === 'PUT') {
      state.voice = {
        ...state.voice,
        ...(request.postDataJSON() as Omit<VoiceState, 'storeId'>),
        storeId: Number(voice[1]),
      };
      return json(route, state.voice);
    }

    return json(route, { message: `Unhandled E2E route ${method} ${path}` }, 501);
  });

  return state;
}

async function signIn(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Tenant code').fill('DEMO-STORE');
  await page.getByLabel('Email or username').fill('owner');
  await page.getByLabel('Password').fill('safe-e2e-password');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/customer-admin\/dashboard$/);
  await expect(page.locator('app-phase-five-dashboard')).toBeVisible();
}

const navigation: Record<string, string> = {
  '/customer-admin/stores': 'Stores',
  '/customer-admin/users': 'Users',
  '/customer-admin/staff': 'Staff',
  '/customer-admin/store-categories': 'Categories',
  '/customer-admin/voice-commands': 'Voice settings',
};

async function openViaSpa(page: Page, path: string): Promise<void> {
  const linkName = navigation[path];
  if (!linkName) throw new Error(`No Phase 5 SPA navigation mapping for ${path}`);
  const dashboardNav = page.locator('app-phase-five-dashboard main.wrap nav.nav');
  await dashboardNav.getByRole('link', { name: linkName, exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`${path.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}$`));
}

test('login and Customer Admin dashboard use API-backed data', async ({ page }) => {
  await mockPhaseFiveApi(page);
  await signIn(page);

  const dashboard = page.locator('app-phase-five-dashboard main.wrap');
  await expect(dashboard.getByRole('heading', { name: 'Customer Admin', exact: true })).toBeVisible();
  await expect(dashboard.getByText('Active stores', { exact: true })).toBeVisible();
  await expect(dashboard.getByText('1', { exact: true }).first()).toBeVisible();
});

test('store CRUD, location verification and activation/deactivation lifecycle work', async ({ page }) => {
  const state = await mockPhaseFiveApi(page);
  await signIn(page);
  await openViaSpa(page, '/customer-admin/stores');

  await expect(page.getByText('Surat Flagship', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Edit' }).first().click();
  await page.getByPlaceholder('Store name').fill('Surat Flagship Updated');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('Store saved.', { exact: true })).toBeVisible();
  await expect.poll(() => state.stores[0].storeName).toBe('Surat Flagship Updated');

  await page.getByRole('button', { name: 'Verify location' }).click();
  await expect(page.getByText('Store location verified.', { exact: true })).toBeVisible();
  await expect.poll(() => state.stores[0].isLocationVerified).toBe(true);

  await page.getByRole('button', { name: 'Deactivate' }).click();
  await expect.poll(() => state.stores[0].isActive).toBe(false);
  await expect(page.getByRole('button', { name: 'Activate' })).toBeVisible();
  await page.getByRole('button', { name: 'Activate' }).click();
  await expect.poll(() => state.stores[0].isActive).toBe(true);

  await page.getByRole('button', { name: 'Clear' }).click();
  await page.getByPlaceholder('Store code (optional)').fill('SURAT-02');
  await page.getByPlaceholder('Store name').fill('Surat Second');
  await page.getByPlaceholder('Address').fill('Vesu Main Road');
  await page.getByPlaceholder('City').fill('Surat');
  await page.getByPlaceholder('State').fill('Gujarat');
  await page.getByPlaceholder('Postal code').fill('395007');
  await page.locator('input[formcontrolname="countryCode"]').fill('IN');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect.poll(() => state.stores.length).toBe(2);
  await expect(page.getByText('Surat Second', { exact: true })).toBeVisible();
});

test('tenant user create/update flow calls roles and store assignment APIs', async ({ page }) => {
  const state = await mockPhaseFiveApi(page);
  await signIn(page);
  await openViaSpa(page, '/customer-admin/users');

  await page.getByPlaceholder('Username').fill('manager2');
  await page.getByPlaceholder('Display name').fill('Manager Two');
  await page.getByPlaceholder('Email').fill('manager2@example.test');
  await page.getByPlaceholder('Password (new user)').fill('SafePassword#2026');
  await page.getByPlaceholder('Roles comma separated').fill('StoreManager');
  await page.getByPlaceholder('Store IDs comma separated').fill('101');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('User created.', { exact: true })).toBeVisible();
  await expect.poll(() => state.users.length).toBe(2);

  const salesRow = page.getByRole('row').filter({ hasText: 'Sales One' });
  await salesRow.getByRole('button', { name: 'Edit' }).click();
  await page.getByPlaceholder('Display name').fill('Sales One Updated');
  await page.getByPlaceholder('Roles comma separated').fill('SalesStaff');
  await page.getByPlaceholder('Store IDs comma separated').fill('101');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('User saved.', { exact: true })).toBeVisible();
  await expect.poll(() => state.calls.includes('PUT users/601/roles')).toBe(true);
  await expect.poll(() => state.calls.includes('PUT users/601/stores')).toBe(true);
});

test('quota reactivation rejection is surfaced and does not continue role/store writes', async ({ page }) => {
  const state = await mockPhaseFiveApi(page, { quotaRejectUserId: 601 });
  await signIn(page);
  await openViaSpa(page, '/customer-admin/users');

  const row = page.getByRole('row').filter({ hasText: 'Sales One' });
  await row.getByRole('button', { name: 'Edit' }).click();
  await page.getByLabel('Active').check();
  await page.getByRole('button', { name: 'Save' }).click();

  await expect(page.getByText(/Tenant user quota \(5\).*cannot be reactivated/)).toBeVisible();
  await expect.poll(() => state.calls.filter(x => x === 'PUT users/601/roles').length).toBe(0);
  await expect.poll(() => state.calls.filter(x => x === 'PUT users/601/stores').length).toBe(0);
});

test('staff create and update flow is available to authorized tenant administrators', async ({ page }) => {
  const state = await mockPhaseFiveApi(page);
  await signIn(page);
  await openViaSpa(page, '/customer-admin/staff');

  await page.getByPlaceholder('Employee code').fill('EMP-002');
  await page.getByPlaceholder('First name').fill('Ravi');
  await page.getByPlaceholder('Last name').fill('Patel');
  await page.getByPlaceholder('Mobile').fill('9000000002');
  await page.getByPlaceholder('Login username').fill('ravi');
  await page.getByPlaceholder('Email').fill('ravi@example.test');
  await page.getByPlaceholder('Password (new staff)').fill('SafePassword#2026');
  await page.getByPlaceholder('Roles e.g. SalesStaff').fill('SalesStaff');
  await page.getByPlaceholder('Store IDs comma separated').fill('101');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('Staff created.', { exact: true })).toBeVisible();
  await expect.poll(() => state.staff.length).toBe(2);

  const existing = page.getByRole('row').filter({ hasText: 'EMP-001' });
  await existing.getByRole('button', { name: 'Edit' }).click();
  await page.getByPlaceholder('First name').fill('Sales Updated');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('Staff saved.', { exact: true })).toBeVisible();
  await expect.poll(() => state.staff[0].firstName).toBe('Sales Updated');
});

test('store-scoped permission guard blocks pages without required permission', async ({ page }) => {
  await mockPhaseFiveApi(page, {
    identity: tenantUser({
      roles: ['StoreAdmin'],
      permissions: ['TenantDashboard.View', 'TenantStores.View'],
      storeIds: [101],
    }),
  });
  await signIn(page);
  const dashboard = page.locator('app-phase-five-dashboard');
  await dashboard.getByRole('link', { name: 'Users', exact: true }).click();
  await expect(page).toHaveURL(/\/access-denied$/);
  await expect(page.getByText(/access denied/i).first()).toBeVisible();
});

test('store-scoped role escalation is rejected and tenant-wide role is not created', async ({ page }) => {
  const state = await mockPhaseFiveApi(page, {
    identity: tenantUser({
      roles: ['StoreAdmin'],
      permissions: ['TenantDashboard.View', 'TenantUsers.View', 'TenantUsers.Create', 'TenantUsers.Edit', 'TenantUsers.AssignRoles'],
      storeIds: [101],
    }),
    rejectTenantWideRole: true,
  });
  await signIn(page);
  await openViaSpa(page, '/customer-admin/users');

  await page.getByPlaceholder('Username').fill('illegal-admin');
  await page.getByPlaceholder('Display name').fill('Illegal Admin');
  await page.getByPlaceholder('Email').fill('illegal@example.test');
  await page.getByPlaceholder('Password (new user)').fill('SafePassword#2026');
  await page.getByPlaceholder('Roles comma separated').fill('TenantAdmin');
  await page.getByPlaceholder('Store IDs comma separated').fill('101');
  await page.getByRole('button', { name: 'Save' }).click();

  await expect(page.getByText(/Store-scoped administrators cannot assign TenantAdmin/)).toBeVisible();
  await expect.poll(() => state.users.some(x => x.userName === 'illegal-admin')).toBe(false);
});

test('store isolation shows only API-authorized stores and never sends TenantId from browser', async ({ page }) => {
  const state = await mockPhaseFiveApi(page, {
    identity: tenantUser({ roles: ['StoreAdmin'], storeIds: [101] }),
  });
  await signIn(page);
  await openViaSpa(page, '/customer-admin/stores');

  await expect(page.getByText('Surat Flagship', { exact: true })).toBeVisible();
  await expect(page.getByText('Ahmedabad Forbidden', { exact: true })).toHaveCount(0);
  await expect.poll(() => state.calls.some(call => /tenantId/i.test(call))).toBe(false);
});

test('category create and update work on planned store-category route', async ({ page }) => {
  const state = await mockPhaseFiveApi(page);
  await signIn(page);
  await openViaSpa(page, '/customer-admin/store-categories');

  await page.getByPlaceholder('Category code').fill('KURTA');
  await page.getByPlaceholder('Category name').fill('Kurtas');
  await page.getByPlaceholder('Store ID (blank = tenant-wide)').fill('101');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('Category created.', { exact: true })).toBeVisible();
  await expect.poll(() => state.categories.length).toBe(2);

  const existing = page.getByRole('row').filter({ hasText: 'SAREE' });
  await existing.getByRole('button', { name: 'Edit' }).click();
  await page.getByPlaceholder('Category name').fill('Premium Sarees');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('Category saved.', { exact: true })).toBeVisible();
  await expect.poll(() => state.categories[0].name).toBe('Premium Sarees');
});

test('dynamic voice settings load and save a store-specific trigger', async ({ page }) => {
  const state = await mockPhaseFiveApi(page);
  await signIn(page);
  await openViaSpa(page, '/customer-admin/voice-commands');

  await page.getByPlaceholder('Store ID').fill('101');
  await page.getByRole('button', { name: 'Load voice settings' }).click();
  await expect(page.getByPlaceholder('Trigger phrase')).toHaveValue('Aasha Add');
  await page.getByPlaceholder('Trigger phrase').fill('Mira Add');
  await page.getByPlaceholder('Trigger aliases comma separated').fill('Mira Add, Mira Please Add');
  await page.getByRole('button', { name: 'Save voice settings' }).click();

  await expect(page.getByText('Voice settings saved.', { exact: true })).toBeVisible();
  await expect.poll(() => state.voice.triggerKeyword).toBe('Mira Add');
  await expect.poll(() => state.voice.aliases).toEqual(['Mira Add', 'Mira Please Add']);
});
