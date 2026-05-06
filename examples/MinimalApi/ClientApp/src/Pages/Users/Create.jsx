import { Head, Link, useForm } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

export default function UsersCreate({ errors = {} }) {
  const form = useForm({ name: '', email: '' });

  const submit = (e) => {
    e.preventDefault();
    form.post('/users');
  };

  const nameError = form.errors.name ?? errors.name;

  return (
    <>
      <Head title="Create User" />
      <AppShell
        title="Create a user"
        subtitle="Submit the form empty to trigger an inline 422 Inertia response, then submit valid data to exercise the redirect flow."
        actions={
          <Link href="/users" role="button" className="secondary">
            Back to users
          </Link>
        }
      >
        <article style={{ maxWidth: '36rem' }}>
          <header><strong>New user</strong></header>
          <form onSubmit={submit} data-testid="react-create-form">
            <label htmlFor="name-input">
              Name
              <input
                id="name-input"
                name="name"
                data-testid="react-name-input"
                value={form.data.name}
                onChange={(e) => form.setData('name', e.target.value)}
                placeholder="Charlie"
                aria-invalid={nameError ? 'true' : undefined}
              />
            </label>
            {nameError && (
              <small className="field-error" data-testid="react-create-name-error">
                {nameError}
              </small>
            )}

            <label htmlFor="email-input">
              Email
              <input
                id="email-input"
                name="email"
                type="email"
                data-testid="react-email-input"
                value={form.data.email}
                onChange={(e) => form.setData('email', e.target.value)}
                placeholder="charlie@example.com"
              />
            </label>

            <button
              type="submit"
              disabled={form.processing}
              aria-busy={form.processing}
              data-testid="react-submit-button"
            >
              Create user
            </button>
          </form>
        </article>
      </AppShell>
    </>
  );
}
