import { chromium } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const password = process.env.CUSTSEARCH_MANUAL_TEST_PASSWORD;
if (!password) {
  throw new Error('CUSTSEARCH_MANUAL_TEST_PASSWORD is required; do not store the UAT password in this script.');
}

function required(name) {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required; runtime identities and cameras must not be hard-coded.`);
  return value;
}

const baseUrl = process.env.CUSTSEARCH_ADMIN_URL ?? 'http://127.0.0.1:4200';
const chromePath = process.env.CUSTSEARCH_CHROME_PATH ?? 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const platformUser = required('CUSTSEARCH_UAT_PLATFORM_USER');
const officeTenant = required('CUSTSEARCH_UAT_OFFICE_TENANT');
const officeUser = required('CUSTSEARCH_UAT_OFFICE_USER');
const officeCameraName = required('CUSTSEARCH_UAT_OFFICE_CAMERA_NAME');
const noCameraTenant = required('CUSTSEARCH_UAT_NO_CAMERA_TENANT');
const noCameraUser = required('CUSTSEARCH_UAT_NO_CAMERA_USER');
const evidenceDirectory = path.resolve('artifacts', 'manual-server-camera-access');
await mkdir(evidenceDirectory, { recursive: true });

const browser = await chromium.launch({ executablePath: chromePath, headless: false, slowMo: 100 });

async function signIn(context, tenantCode, userName) {
  const page = await context.newPage();
  await page.goto(`${baseUrl}/login`);
  await page.locator('input[name="tenantCode"]').fill(tenantCode);
  await page.locator('input[name="username"]').fill(userName);
  await page.locator('input[name="password"]').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(url => !url.pathname.endsWith('/login'));
  return page;
}

try {
  // Each actor uses a separate browser context so refresh cookies and access tokens cannot mix.
  const platformContext = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const platformPage = await signIn(platformContext, '', platformUser);
  await platformPage.getByText('Total tenants', { exact: true }).waitFor();
  await platformPage.screenshot({ path: path.join(evidenceDirectory, '01-platform-admin-dashboard.png'), fullPage: true });
  await platformContext.close();

  const officeContext = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const officePage = await signIn(officeContext, officeTenant, officeUser);
  await officePage.getByRole('heading', { name: 'Customer Admin' }).first().waitFor();
  await officePage.goto(`${baseUrl}/customer-admin/cameras`);
  await officePage.getByText(officeCameraName, { exact: false }).first().waitFor();
  await officePage.getByText('1', { exact: true }).first().waitFor();
  await officePage.screenshot({ path: path.join(evidenceDirectory, '02-office-camera-operator.png'), fullPage: true });
  await officeContext.close();

  const randomContext = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const randomPage = await signIn(randomContext, noCameraTenant, noCameraUser);
  await randomPage.getByRole('heading', { name: 'Customer Admin' }).first().waitFor();
  await randomPage.goto(`${baseUrl}/customer-admin/cameras`);
  await randomPage.getByText('No cameras configured.', { exact: true }).waitFor();
  await randomPage.getByText('0', { exact: true }).first().waitFor();
  if (await randomPage.getByText(officeCameraName, { exact: false }).count() !== 0) {
    throw new Error('Cross-tenant camera leaked into the no-camera user UI.');
  }
  await randomPage.screenshot({ path: path.join(evidenceDirectory, '03-random-user-no-camera.png'), fullPage: true });
  await randomContext.close();

  process.stdout.write(`PASS: isolated Chrome flow evidence written to ${evidenceDirectory}\n`);
} finally {
  await browser.close();
}
