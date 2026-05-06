import { Head, Link, useForm } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

export default function UsersCreate({ errors = {} }) {
  const form = useForm({ name: '', email: '' });
  const roleSuggestions = [
    'Launch Director',
    'Implementation Lead',
    'Technical Architect',
    'Customer Enablement',
  ];
  const workflowNotes = [
    'Leave the form empty to see the inline 422 validation response.',
    'Submit valid data to trigger a redirect back to the team board.',
    'The success message is shared through the next Inertia response.',
  ];

  const submit = (e) => {
    e.preventDefault();
    form.post('/users');
  };

  const nameError = form.errors.name ?? errors.name;

  return (
    <>
      <Head title="Create User" />
      <AppShell
        title="Invite a launch specialist"
        subtitle="The form stays on the same page for validation errors, then redirects back to the team board with a flash message after success."
        actions={
          <Link href="/users" role="button" className="secondary">
            Back to team board
          </Link>
        }
      >
        <section className="two-column-grid">
          <article>
            <header><strong>Invitation form</strong></header>
            <form onSubmit={submit} data-testid="react-create-form">
              <label htmlFor="name-input">
                Name
                <input
                  id="name-input"
                  name="name"
                  data-testid="react-name-input"
                  value={form.data.name}
                  onChange={(e) => form.setData('name', e.target.value)}
                  placeholder="Jordan Ellis"
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
                  placeholder="jordan.ellis@example.com"
                />
              </label>

              <button
                type="submit"
                disabled={form.processing}
                aria-busy={form.processing}
                data-testid="react-submit-button"
              >
                Queue invitation
              </button>
            </form>
          </article>

          <article>
            <header><strong>What this page demonstrates</strong></header>
            <ul className="feature-list">
              {workflowNotes.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>

            <div className="chip-grid">
              {roleSuggestions.map((item) => (
                <span key={item} className="tag">{item}</span>
              ))}
            </div>
          </article>
        </section>
      </AppShell>
    </>
  );
}
