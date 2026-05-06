import { expect, test } from '@playwright/test';

test('react minimal api example supports navigation, validation, and deferred data', async ({ page }) => {
  await page.goto('http://127.0.0.1:6108/');

  await expect(page.getByTestId('react-home-title')).toContainText('Launch operations workspace');
  await expect(page.getByTestId('react-home-highlights')).toContainText('Queue an invite, follow a redirect, and read the flash message on the team board.');

  const docsRequestPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6108/docs/guides/getting-started' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.getByTestId('react-docs-link').click();
  const docsRequest = await docsRequestPromise;

  expect(docsRequest.headers()['x-inertia-except-once-props']).toContain('appConfig');
  await expect(page).toHaveURL(/\/docs\/guides\/getting-started$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Launch readiness runbook');
  await expect(page.getByTestId('react-docs-pattern')).toHaveText('Docs/[[...page]]');
  await expect(page.getByTestId('react-docs-breadcrumbs').getByRole('link', { name: 'docs' })).toBeVisible();
  await expect(page.getByTestId('react-docs-breadcrumbs').getByRole('link', { name: 'guides' })).toBeVisible();
  await expect(page.getByTestId('react-docs-breadcrumbs').getByRole('link', { name: 'getting-started' })).toHaveAttribute('aria-current', 'page');
  await expect(page.getByTestId('react-docs-article')).toContainText('optional catch-all');

  const usersRequestPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6108/users' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.getByTestId('react-nav').getByRole('link', { name: 'Team' }).click();
  const usersRequest = await usersRequestPromise;

  expect(usersRequest.headers()['x-inertia-except-once-props']).toContain('appConfig');
  await expect(page).toHaveURL(/\/users$/);
  await expect(page.getByTestId('react-users-total')).toHaveText('4');
  await expect(page.getByTestId('react-users-list')).toContainText('Maya Chen');

  await page.getByTestId('react-create-link').click();
  await expect(page.getByTestId('react-create-form')).toBeVisible();

  await page.getByTestId('react-submit-button').click();
  await expect(page.getByTestId('react-create-name-error')).toContainText('Name is required.');

  await page.getByTestId('react-name-input').fill('Jordan Ellis');
  await page.getByTestId('react-email-input').fill('jordan.ellis@example.com');
  await page.getByTestId('react-submit-button').click();

  await expect(page).toHaveURL(/\/users\?success=/);
  await expect(page.getByTestId('react-flash-success')).toContainText('Jordan Ellis');
  await expect(page.getByTestId('react-users-list')).toContainText('Launch Director');

  await page.getByTestId('react-nav').getByRole('link', { name: 'Insights' }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('Launch velocity');
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('Apr');
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('9');
});

test('react minimal api demo can sign in and render an authenticated account page', async ({ page }) => {
  await page.goto('http://127.0.0.1:6108/');

  await expect(page.getByTestId('react-auth-label')).toContainText('guest');
  await expect(page.getByTestId('react-auth-demo-status')).toContainText('Currently browsing as guest');
  await expect(page.getByTestId('react-auth-demo-route')).toContainText('/me');

  await page.getByTestId('react-auth-demo-sign-in').click();

  await expect(page).toHaveURL(/\/me$/);
  await expect(page.getByTestId('react-auth-label')).toContainText('Maya Chen');
  await expect(page.getByTestId('react-account-name')).toHaveText('Maya Chen');
  await expect(page.getByTestId('react-account-email')).toContainText('maya.chen@northstar.example');
  await expect(page.getByTestId('react-account-role')).toContainText('Launch Director');

  await page.goto('http://127.0.0.1:6108/');
  await expect(page.getByTestId('react-open-auth-demo-button')).toBeVisible();

  await page.getByTestId('react-sign-out-button').click();

  await expect(page).toHaveURL(/\/signed-out$/);
  await expect(page.getByTestId('react-auth-label')).toContainText('guest');
  await expect(page.getByTestId('react-home-title')).toContainText('You have signed out of the workspace');
});

test('react minimal api clears encrypted history after sign-out so back navigation refetches data', async ({ page }) => {
  await page.goto('http://127.0.0.1:6108/dashboard');

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('Launch velocity');
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('9');

  await page.goto('http://127.0.0.1:6108/signed-out');

  await expect(page).toHaveURL(/\/signed-out$/);
  await expect(page.getByTestId('react-home-title')).toContainText('You have signed out of the workspace');

  const dashboardReloadPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6108/dashboard' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.goBack();
  const dashboardReload = await dashboardReloadPromise;

  expect(dashboardReload.headers()['x-inertia']).toBe('true');
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('9');
});