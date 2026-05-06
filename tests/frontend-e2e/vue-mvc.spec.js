import { expect, test } from '@playwright/test';

test('vue mvc example supports navigation, prg validation, flash, and deferred data', async ({ page }) => {
  await page.goto('http://127.0.0.1:6109/');

  await expect(page.getByTestId('vue-home-title')).toContainText('Vue is handling this page');

  const privacyRequestPromise = page.waitForRequest(
    (request) =>
      request.url() === 'http://127.0.0.1:6109/Home/Privacy' &&
      request.headers()['x-inertia'] === 'true'
  );

  await page.getByTestId('vue-privacy-link').click();
  const privacyRequest = await privacyRequestPromise;

  expect(privacyRequest.headers()['x-inertia-except-once-props']).toContain('appConfig');
  await expect(page).toHaveURL(/\/Home\/Privacy$/);
  await expect(page.getByTestId('vue-privacy-title')).toContainText('second controller action');

  await page.getByTestId('vue-nav').getByRole('link', { name: 'Users' }).click();
  await expect(page).toHaveURL(/\/users$/i);
  await expect(page.getByTestId('vue-users-total')).toHaveText('2');
  await expect(page.getByTestId('vue-users-list')).toContainText('Alice');

  await page.getByTestId('vue-create-link').click();
  await expect(page.getByTestId('vue-create-form')).toBeVisible();

  await page.getByTestId('vue-submit-button').click();
  await expect(page.getByTestId('vue-create-name-error')).toContainText('Name is required.');

  await page.getByTestId('vue-name-input').fill('Charlie');
  await page.getByTestId('vue-email-input').fill('charlie@example.com');
  await page.getByTestId('vue-submit-button').click();

  await expect(page).toHaveURL(/\/users(?:\/index)?(\?.*)?$/i);
  await expect(page.getByTestId('vue-flash')).toContainText('User created!');

  await page.getByTestId('vue-nav').getByRole('link', { name: 'Dashboard' }).click();
  await expect(page).toHaveURL(/\/users\/dashboard$/i);
  await expect(page.getByTestId('vue-dashboard-recent-users')).toContainText('Recent users');
  await expect(page.getByTestId('vue-dashboard-recent-users')).toContainText('Alice');
});