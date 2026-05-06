import { Link, usePage } from '@inertiajs/react';

const NAV_ITEMS = [
  { href: '/',          label: 'Home' },
  { href: '/docs',      label: 'Docs' },
  { href: '/users',     label: 'Users' },
  { href: '/dashboard', label: 'Dashboard' },
];

function isActive(currentUrl, href) {
  return href === '/' ? currentUrl === href : currentUrl.startsWith(href);
}

export function AppShell({ title, subtitle, actions, children }) {
  const { props, url } = usePage();
  const appName  = props.appConfig?.name ?? 'InertiaKit';
  const authName = props.auth?.user?.name ?? 'guest';

  return (
    <>
      <header className="container">
        <nav>
          <ul>
            <li>
              <Link href="/" className="brand">
                <strong>{appName}</strong>
                <small>Minimal API + React</small>
              </Link>
            </li>
          </ul>
          <ul data-testid="react-nav">
            {NAV_ITEMS.map((item) => (
              <li key={item.href}>
                <Link
                  href={item.href}
                  aria-current={isActive(url, item.href) ? 'page' : undefined}
                >
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
          <ul>
            <li>
              <small className="auth-label">
                Signed in as <strong>{authName}</strong>
              </small>
            </li>
          </ul>
        </nav>
      </header>

      <main className="container" data-framework="react">
        <hgroup>
          <h1>{title}</h1>
          {subtitle && <p>{subtitle}</p>}
        </hgroup>
        {actions && <div className="action-row">{actions}</div>}
        {children}
      </main>
    </>
  );
}
