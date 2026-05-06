import { Head, Link, usePage } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

export default function HomeIndex({ greeting, highlights = [] }) {
  const { props } = usePage();

  return (
    <>
      <Head title="Home" />
      <AppShell
        title={greeting}
        subtitle="A real React Inertia app running inside the Minimal API sample."
        actions={
          <>
            <Link href="/users" role="button" data-testid="react-users-link">
              Explore users
            </Link>
            <Link href="/docs/guides/getting-started" role="button" className="secondary" data-testid="react-docs-link">
              Open docs route
            </Link>
            <Link href="/dashboard" role="button" className="secondary" data-testid="react-dashboard-link">
              Open dashboard
            </Link>
          </>
        }
      >
        <div className="grid">
          <article>
            <header>
              <strong>Shared app config</strong>
            </header>
            <p><strong>{props.appConfig?.name ?? 'InertiaKit Demo'}</strong></p>
            <footer><small>Version {props.appConfig?.version ?? '1.0'}</small></footer>
          </article>
          <article>
            <header>
              <strong>What this page exercises</strong>
            </header>
            <ul data-testid="react-home-highlights">
              {highlights.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </article>
        </div>

        <article>
          <header>
            <strong>Practical flow</strong>
          </header>
          <h2 data-testid="react-home-title">{greeting}</h2>
          <p>
            Build a user, trigger validation, then watch the dashboard hydrate
            deferred data. The Playwright suite walks that path end-to-end.
          </p>
        </article>
      </AppShell>
    </>
  );
}
