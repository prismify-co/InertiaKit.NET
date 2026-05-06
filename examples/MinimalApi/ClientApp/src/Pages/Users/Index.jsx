import { Head, Link } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

export default function UsersIndex({ users = [], total = 0 }) {
  return (
    <>
      <Head title="Users" />
      <AppShell
        title="Users"
        subtitle="Reached via an Inertia client-side visit; props are returned eagerly by the Minimal API backend."
        actions={
          <>
            <Link href="/users/create" role="button" data-testid="react-create-link">
              Create user
            </Link>
            <Link href="/dashboard" role="button" className="secondary">
              View dashboard
            </Link>
          </>
        }
      >
        <div className="grid">
          <article>
            <header><strong>Total users</strong></header>
            <p style={{ fontSize: '2.5rem', fontWeight: 700, margin: 0 }} data-testid="react-users-total">
              {total}
            </p>
          </article>
          <article>
            <header><strong>Page type</strong></header>
            <p style={{ fontWeight: 600 }}>Eager index</p>
          </article>
        </div>

        <article>
          <header><strong>Directory</strong></header>
          <h2>Current users</h2>
          <div data-testid="react-users-list">
            {users.map((user) => (
              <div key={user.id} className="user-row">
                <div>
                  <strong>{user.name}</strong>
                  <br />
                  <small>{user.email}</small>
                </div>
                <small>#{user.id}</small>
              </div>
            ))}
          </div>
        </article>
      </AppShell>
    </>
  );
}
