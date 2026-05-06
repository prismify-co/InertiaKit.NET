import { Head, Link } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

export default function AccountShow({ profile, workspaceAccess = [], secureFeatures = [], recentEvents = [] }) {
  return (
    <>
      <Head title="Authenticated Demo" />
      <AppShell
        title="Launch director access profile"
        subtitle="This route is protected by ASP.NET Core authorization and still renders through the same Inertia adapter pipeline as the rest of the workspace."
        actions={
          <>
            <Link href="/dashboard" role="button" className="secondary">
              Open secure insights
            </Link>
            <Link href="/users" role="button" className="secondary">
              Back to team board
            </Link>
          </>
        }
      >
        <section className="two-column-grid">
          <article>
            <header><strong>Authenticated identity</strong></header>
            <h2 data-testid="react-account-name">{profile?.name ?? 'Unknown user'}</h2>
            <p data-testid="react-account-email">{profile?.email ?? 'No email available'}</p>
            <p>
              <small data-testid="react-account-role">{profile?.role ?? 'No role assigned'}</small>
            </p>
          </article>

          <article>
            <header><strong>Workspace access</strong></header>
            <div className="chip-grid">
              {workspaceAccess.map((item) => (
                <span key={item} className="tag">{item}</span>
              ))}
            </div>
          </article>
        </section>

        <section className="two-column-grid">
          <article>
            <header><strong>What this proves</strong></header>
            <ul data-testid="react-account-features">
              {secureFeatures.map((feature) => (
                <li key={feature}>{feature}</li>
              ))}
            </ul>
          </article>

          <article>
            <header><strong>Recent authenticated events</strong></header>
            <div data-testid="react-account-events">
              {recentEvents.map((event) => (
                <div key={event.id} className="activity-row">
                  <strong>{event.action}</strong>
                  <small>{event.at}</small>
                </div>
              ))}
            </div>
          </article>
        </section>
      </AppShell>
    </>
  );
}