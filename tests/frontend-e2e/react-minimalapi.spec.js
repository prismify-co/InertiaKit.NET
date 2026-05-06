import { expect, test } from '@playwright/test';

test('react minimal api example supports navigation, validation, and deferred data', async ({ page }) => {
  await page.goto('http://127.0.0.1:6108/');

  await expect(page.getByTestId('react-home-title')).toContainText('React + Minimal API');
  await expect(page.getByTestId('react-home-highlights')).toContainText('Inline validation on mutation');

  const docsRequestPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6108/docs/guides/getting-started' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.getByTestId('react-docs-link').click();
  const docsRequest = await docsRequestPromise;

  expect(docsRequest.headers()['x-inertia-except-once-props']).toContain('appConfig');
  await expect(page).toHaveURL(/\/docs\/guides\/getting-started$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Getting started');
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

  await page.getByTestId('react-nav').getByRole('link', { name: 'Users' }).click();
  const usersRequest = await usersRequestPromise;

  expect(usersRequest.headers()['x-inertia-except-once-props']).toContain('appConfig');
  await expect(page).toHaveURL(/\/users$/);
  await expect(page.getByTestId('react-users-total')).toHaveText('2');
  await expect(page.getByTestId('react-users-list')).toContainText('Alice');

  await page.getByTestId('react-create-link').click();
  await expect(page.getByTestId('react-create-form')).toBeVisible();

  await page.getByTestId('react-submit-button').click();
  await expect(page.getByTestId('react-create-name-error')).toContainText('Name is required.');

  await page.getByTestId('react-name-input').fill('Charlie');
  await page.getByTestId('react-email-input').fill('charlie@example.com');
  await page.getByTestId('react-submit-button').click();

  await expect(page).toHaveURL(/\/users$/);
  await expect(page.getByTestId('react-users-list')).toContainText('Bob');

  await page.getByTestId('react-nav').getByRole('link', { name: 'Dashboard' }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('Monthly chart');
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('Jan');
  await expect(page.getByTestId('react-dashboard-chart')).toContainText('140');
});