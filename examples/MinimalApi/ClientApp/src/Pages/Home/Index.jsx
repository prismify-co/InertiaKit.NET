import { Head, Link, router, usePage } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

export default function HomeIndex({ greeting, highlights = [], overview = [], workflow = [], featuredAccounts = [], statusNote = null }) {
  const { props } = usePage();
  const authUser = props.auth?.user ?? null;

  return (
    <>
                Version {props.appConfig?.version ?? '1.2.0'} is cached once by the client.
      <AppShell
        title={<span data-testid="react-home-title">{greeting}</span>}
        subtitle="A realistic launch-operations workspace that ties shared props, redirects, auth, deferred data, and nested docs into one product flow."
        actions={
          <>
            <Link href="/users" role="button" data-testid="react-users-link">
              Open team board
            </Link>
            <Link href="/docs/guides/getting-started" role="button" className="secondary" data-testid="react-docs-link">
              Read runbook
            </Link>
            <Link href="/dashboard" role="button" className="secondary" data-testid="react-dashboard-link">
              View insights
            </Link>
            {authUser && (
              <Link href="/me" role="button" className="secondary" data-testid="react-authenticated-demo-link">
                Open access profile
              </Link>
            )}
          </>
        }
      >
        {statusNote && (
          <article className="page-note">
            <header>
              <strong>Session update</strong>
            </header>
            <p>{statusNote}</p>
          </article>
        )}

        <section className="story-grid">
          <article>
            <header>
              <strong>Workspace pulse</strong>
            </header>
            <div className="metric-grid compact">
              {overview.map((item) => (
                <div key={item.label} className="metric-card">
                  <span>{item.label}</span>
                  <strong>{item.value}</strong>
                  <small>{item.detail}</small>
                </div>
              ))}
            </div>
          </article>

          <article>
            <header>
              <strong>Why this demo feels real</strong>
            </header>
            <ul data-testid="react-home-highlights" className="feature-list">
              {highlights.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </article>

          <article data-testid="react-auth-demo-card">
            <header>
              <strong>Protected access path</strong>
            </header>
            <p className="demo-route-pill" data-testid="react-auth-demo-route">
              Protected route: <strong>/me</strong>
            </p>
            {authUser ? (
              <>
                <p data-testid="react-auth-demo-status">
                  Signed in as <strong>{authUser.name}</strong>
                </p>
                <p>{authUser.email}</p>
                <div className="action-row compact-action-row">
                  <Link href="/me" role="button" className="secondary" data-testid="react-open-auth-demo-button">
                    Open access profile
                  </Link>
                </div>
                <footer><small>{authUser.role}</small></footer>
              </>
            ) : (
              <>
                <p data-testid="react-auth-demo-status">Currently browsing as guest.</p>
                <p>Sign in as the launch director to review the protected workspace profile.</p>
                <div className="action-row compact-action-row">
                  <button
                    type="button"
                    className="secondary"
                    onClick={() => router.post('/auth/demo-sign-in')}
                    data-testid="react-auth-demo-sign-in"
                  >
                    Sign in and open access profile
                  </button>
                </div>
                <footer><small>This signs in the demo user and redirects to the protected route.</small></footer>
              </>
            )}
          </article>
        </section>

        <section className="two-column-grid">
          <article>
            <header>
              <strong>Suggested walkthrough</strong>
            </header>
            <ol className="workflow-list">
              {workflow.map((step) => (
                <li key={step.title}>
                  <div>
                    <strong>{step.title}</strong>
                    <p>{step.description}</p>
                  </div>
                  <Link href={step.href}>Open</Link>
                </li>
              ))}
            </ol>
          </article>

          <article>
            <header>
              <strong>Customer launches in motion</strong>
            </header>
            <div className="account-list">
              {featuredAccounts.map((account) => (
                <div key={account.name} className="account-list__row">
                  <div>
                    <strong>{account.name}</strong>
                    <p>{account.stage}</p>
                  </div>
                  <small>{account.nextStep}</small>
                </div>
              ))}
            </div>
            <footer>
              <small>
                Shared props keep the workspace identity available on every visit.
                Version {props.appConfig?.version ?? '1.1.0'} is cached once by the client.
              </small>
            </footer>
          </article>
        </section>
      </AppShell>
    </>
  );
}
