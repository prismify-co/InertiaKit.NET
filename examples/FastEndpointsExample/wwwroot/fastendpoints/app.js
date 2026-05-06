const page = JSON.parse(document.getElementById('app-data')?.textContent ?? '{}');
const app = document.getElementById('app');

if (!app) {
  throw new Error('Missing #app mount element');
}

const routes = [
  { href: '/', label: 'Home' },
  { href: '/users', label: 'Users' },
  { href: '/users/dashboard', label: 'Dashboard' },
];

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function renderList(items, renderItem) {
  if (!Array.isArray(items) || items.length === 0) {
    return '<p class="muted">No items for this page.</p>';
  }

  return `<ul class="stack-list">${items.map(renderItem).join('')}</ul>`;
}

function renderContent() {
  switch (page.component) {
    case 'Users/Index':
      return `
        <section class="panel" data-testid="fastendpoints-users-panel">
          <h2>Users</h2>
          ${renderList(page.props?.users, (user) => `
            <li class="list-row">
              <strong>${escapeHtml(user.name)}</strong>
              <span class="muted">${escapeHtml(user.email)}</span>
            </li>
          `)}
        </section>
        <section class="panel" data-testid="fastendpoints-protocol-panel">
          <h2>Protocol features</h2>
          <ul class="stack-list">
            <li class="list-row">Optional permissions are resolved on full loads.</li>
            <li class="list-row">Deferred stats are advertised in the page object.</li>
            <li class="list-row">Once props cache countries client-side.</li>
          </ul>
        </section>
      `;

    case 'Users/Dashboard':
      return `
        <section class="panel" data-testid="fastendpoints-dashboard-summary">
          <h2>Dashboard summary</h2>
          <p>Total users: <strong>${escapeHtml(page.props?.summary?.total ?? 0)}</strong></p>
        </section>
        <section class="panel" data-testid="fastendpoints-dashboard-activities">
          <h2>Merged activities</h2>
          ${renderList(page.props?.activities, (entry) => `
            <li class="list-row">
              <strong>${escapeHtml(entry.action)}</strong>
              <span class="muted">${escapeHtml(entry.at)}</span>
            </li>
          `)}
        </section>
      `;

    default:
      return `
        <section class="panel">
          <h2>FastEndpoints + Inertia</h2>
          <p>This example now uses the built-in asset shell for first visits without a Razor view or framework-specific client bundle.</p>
        </section>
      `;
  }
}

app.innerHTML = `
  <div class="page-shell" data-testid="fastendpoints-shell">
    <header class="panel hero">
      <p class="eyebrow">Asset shell example</p>
      <h1>${escapeHtml(page.component ?? 'Unknown component')}</h1>
      <p class="muted">This is a lightweight browser shell for the FastEndpoints sample. The protocol-focused tests still exercise the server adapter directly.</p>
      <nav class="nav-row" data-testid="fastendpoints-nav">
        ${routes.map((route) => `<a href="${route.href}" class="nav-link">${route.label}</a>`).join('')}
      </nav>
    </header>

    ${renderContent()}

    <section class="panel code-panel" data-testid="fastendpoints-page-object">
      <h2>Embedded page object</h2>
      <pre>${escapeHtml(JSON.stringify(page, null, 2))}</pre>
    </section>
  </div>
`;