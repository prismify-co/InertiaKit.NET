import { expect, test } from '@playwright/test';

test('fastendpoints example serves the built-in asset shell in a browser', async ({ page }) => {
  await page.goto('http://127.0.0.1:6110/');

  await expect(page.getByTestId('fastendpoints-shell')).toBeVisible();
  await expect(page.getByRole('heading', { level: 1 })).toContainText('FastEndpoints + Inertia');

  await page.getByRole('link', { name: 'Team' }).click();
  await expect(page).toHaveURL(/\/users$/);
  await expect(page.getByTestId('fastendpoints-protocol-panel')).toContainText('Protocol features');
  await expect(page.getByTestId('fastendpoints-users-panel')).toContainText('Launch specialists');
  await expect(page.locator('.user-rows')).toContainText('Alice');

  await page.getByRole('link', { name: 'Insights' }).click();
  await expect(page).toHaveURL(/\/users\/dashboard$/);
  await expect(page.getByTestId('fastendpoints-dashboard-summary')).toContainText('Operations dashboard');
  await expect(page.getByTestId('fastendpoints-dashboard-activities')).toContainText('Signed up');
});