import { expect, test } from '@playwright/test';

test('vue minimal api example supports navigation, validation, and deferred data', async ({ page }) => {
  await page.goto('http://127.0.0.1:6109/');

  await expect(page.getByTestId('vue-home-title')).toContainText('Launch operations workspace');
  await expect(page.getByTestId('vue-home-highlights')).toContainText('Queue an invite, follow a redirect, and read the flash message on the team board.');

  const docsRequestPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6109/docs/guides/getting-started' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.getByTestId('vue-docs-link').click();
  const docsRequest = await docsRequestPromise;

  expect(docsRequest.headers()['x-inertia-except-once-props']).toContain('appConfig');
  await expect(page).toHaveURL(/\/docs\/guides\/getting-started$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Launch readiness runbook');
  await expect(page.getByTestId('vue-docs-pattern')).toHaveText('Docs/[[...page]]');
  await expect(page.getByTestId('vue-docs-breadcrumbs').getByRole('link', { name: 'docs' })).toBeVisible();
  await expect(page.getByTestId('vue-docs-breadcrumbs').getByRole('link', { name: 'guides' })).toBeVisible();
  await expect(page.getByTestId('vue-docs-breadcrumbs').getByRole('link', { name: 'getting-started' })).toHaveAttribute('aria-current', 'page');
  await expect(page.getByTestId('vue-docs-article')).toContainText('optional catch-all');

  const usersRequestPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6109/users' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.getByTestId('vue-nav').getByRole('link', { name: 'Team' }).click();
  const usersRequest = await usersRequestPromise;

  expect(usersRequest.headers()['x-inertia-except-once-props']).toContain('appConfig');
  await expect(page).toHaveURL(/\/users$/);
  await expect(page.getByTestId('vue-users-total')).toHaveText('4');
  await expect(page.getByTestId('vue-users-list')).toContainText('Maya Chen');

  await page.getByTestId('vue-create-link').click();
  await expect(page.getByTestId('vue-create-form')).toBeVisible();

  await page.getByTestId('vue-submit-button').click();
  await expect(page.getByTestId('vue-create-name-error')).toContainText('Name is required.');

  await page.getByTestId('vue-name-input').fill('Jordan Ellis');
  await page.getByTestId('vue-email-input').fill('jordan.ellis@example.com');
  await page.getByTestId('vue-submit-button').click();

  await expect(page).toHaveURL(/\/users\?success=/);
  await expect(page.getByTestId('vue-flash-success')).toContainText('Jordan Ellis');
  await expect(page.getByTestId('vue-users-list')).toContainText('Launch Director');

  await page.getByTestId('vue-nav').getByRole('link', { name: 'Insights' }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('vue-dashboard-chart')).toContainText('Launch velocity');
  await expect(page.getByTestId('vue-dashboard-chart')).toContainText('Apr');
  await expect(page.getByTestId('vue-dashboard-chart')).toContainText('9');
});

test('vue minimal api demo can sign in and render an authenticated account page', async ({ page }) => {
  await page.goto('http://127.0.0.1:6109/');

  await expect(page.getByTestId('vue-auth-label')).toContainText('guest');
  await expect(page.getByTestId('vue-auth-demo-status')).toContainText('Currently browsing as guest');
  await expect(page.getByTestId('vue-auth-demo-route')).toContainText('/me');

  await page.getByTestId('vue-auth-demo-sign-in').click();

  await expect(page).toHaveURL(/\/me$/);
  await expect(page.getByTestId('vue-auth-label')).toContainText('Maya Chen');
  await expect(page.getByTestId('vue-account-name')).toHaveText('Maya Chen');
  await expect(page.getByTestId('vue-account-email')).toContainText('maya.chen@northstar.example');
  await expect(page.getByTestId('vue-account-role')).toContainText('Launch Director');

  await page.goto('http://127.0.0.1:6109/');
  await expect(page.getByTestId('vue-open-auth-demo-button')).toBeVisible();

  await page.getByTestId('vue-sign-out-button').click();

  await expect(page).toHaveURL(/\/signed-out$/);
  await expect(page.getByTestId('vue-auth-label')).toContainText('guest');
  await expect(page.getByTestId('vue-home-title')).toContainText('You have signed out of the workspace');
});

test('vue minimal api clears encrypted history after sign-out so back navigation refetches data', async ({ page }) => {
  await page.goto('http://127.0.0.1:6109/dashboard');

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('vue-dashboard-chart')).toContainText('Launch velocity');
  await expect(page.getByTestId('vue-dashboard-chart')).toContainText('9');

  await page.goto('http://127.0.0.1:6109/signed-out');

  await expect(page).toHaveURL(/\/signed-out$/);
  await expect(page.getByTestId('vue-home-title')).toContainText('You have signed out of the workspace');

  const dashboardReloadPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6109/dashboard' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.goBack();
  const dashboardReload = await dashboardReloadPromise;

  expect(dashboardReload.headers()['x-inertia']).toBe('true');
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('vue-dashboard-chart')).toContainText('9');
});