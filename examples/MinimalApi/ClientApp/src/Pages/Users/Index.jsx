import { Head, Link } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

export default function UsersIndex({ users = [], total = 0, overview = [] }) {
  return (
    <>
      <Head title="Users" />
      <AppShell
        title="Launch team board"
        subtitle="This is an eager Inertia index: the roster, coverage summary, and any redirect flash message arrive together from the server."
        actions={
          <>
            <Link href="/users/create" role="button" data-testid="react-create-link">
              Invite teammate
            </Link>
            <Link href="/dashboard" role="button" className="secondary">
              View insights
            </Link>
          </>
        }
      >
        <div className="metric-grid">
          <article className="metric-card metric-card--featured">
            <span>Total specialists</span>
            <strong data-testid="react-users-total">{total}</strong>
            <small>Regional launch coverage</small>
          </article>

          {overview.map((item) => (
            <article key={item.label} className="metric-card">
              <span>{item.label}</span>
              <strong>{item.value}</strong>
              <small>{item.detail}</small>
            </article>
          ))}
        </div>

        <article>
          <header><strong>Delivery roster</strong></header>
          <h2>Current launch specialists</h2>
          <div data-testid="react-users-list">
            {users.map((user) => (
              <div key={user.id} className="user-row">
                <div className="user-row__primary">
                  <strong>{user.name}</strong>
                  <p>{user.role}</p>
                  <small>{user.email}</small>
                </div>

                <div className="user-row__details">
                  <span className="tag">{user.region}</span>
                  <span className="tag">{user.focus}</span>
                  <span className="tag">{user.status}</span>
                </div>

                <small>{user.activeLaunches} active launches</small>
              </div>
            ))}
          </div>
        </article>
      </AppShell>
    </>
  );
}
