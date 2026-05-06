import { Link, router, usePage } from '@inertiajs/react';

const NAV_ITEMS = [
  { href: '/',          label: 'Overview' },
  { href: '/docs',      label: 'Runbooks' },
  { href: '/users',     label: 'Team' },
  { href: '/dashboard', label: 'Insights' },
];

function isActive(currentUrl, href) {
  return href === '/' ? currentUrl === href : currentUrl.startsWith(href);
}

export function AppShell({ title, subtitle, actions, children, heroContent = null }) {
  const { props, url } = usePage();
  const appName = props.appConfig?.name ?? 'Northstar Launch Ops';
  const environment = props.appConfig?.environment ?? 'Practical demo';
  const authUser = props.auth?.user ?? null;
  const authName = authUser?.name ?? 'guest';
  const flashSuccess = props.flash?.success ?? null;
  const navItems = authUser
    ? [...NAV_ITEMS, { href: '/me', label: 'Account' }]
    : NAV_ITEMS;

  return (
    <>
      <header className="shell-header">
        <div className="container shell-header__inner">
          <Link href="/" className="brand">
            <strong>{appName}</strong>
            <small>{environment} | Minimal API + React</small>
          </Link>

          <nav className="shell-nav" aria-label="Primary">
            <ul data-testid="react-nav">
              {navItems.map((item) => (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    className="nav-pill"
                    aria-current={isActive(url, item.href) ? 'page' : undefined}
                  >
                    {item.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>

          <div className="shell-session">
            <small className="auth-label" data-testid="react-auth-label">
              Signed in as <strong>{authName}</strong>
            </small>
            {authUser ? (
              <>
                <Link href="/me" className="session-link" data-testid="react-account-link">
                  Account
                </Link>
                <button
                  type="button"
                  className="auth-action"
                  onClick={() => router.post('/auth/demo-sign-out')}
                  data-testid="react-sign-out-button"
                >
                  Sign out
                </button>
              </>
            ) : (
              <button
                type="button"
                className="auth-action"
                onClick={() => router.post('/auth/demo-sign-in')}
                data-testid="react-sign-in-button"
              >
                Sign in demo user
              </button>
            )}
          </div>
        </div>
      </header>

      <main className="container page-shell" data-framework="react">
        {flashSuccess && (
          <p className="flash-banner" data-testid="react-flash-success">
            {flashSuccess}
          </p>
        )}

        {heroContent ?? (
          <section className="page-hero">
            <div className="page-hero__body">
              <p className="eyebrow">Practical Inertia example</p>
              <h1>{title}</h1>
              {subtitle && <p className="page-subtitle">{subtitle}</p>}
            </div>

            <aside className="page-hero__meta" aria-label="Page context">
              <div className="meta-grid">
                <div>
                  <span>Workspace</span>
                  <strong>{appName}</strong>
                </div>
                <div>
                  <span>Session</span>
                  <strong>{authUser ? 'Authenticated' : 'Guest preview'}</strong>
                </div>
                <div>
                  <span>Surface</span>
                  <strong>Minimal API + React</strong>
                </div>
              </div>
            </aside>
          </section>
        )}

        {actions && <div className="action-row">{actions}</div>}
        {children}
      </main>
    </>
  );
}
