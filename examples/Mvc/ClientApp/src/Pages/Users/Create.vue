<script setup>
import { Head, useForm, Link } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';

defineProps({ errors: Object });

const form = useForm({ name: '', email: '' });
</script>
<template>
  <Head title="Create User" />
  <AppShell>
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Team management</p>
        <h1>Invite a launch specialist</h1>
        <p class="page-subtitle">The form stays on the same page for validation errors, then redirects back to the team board with a flash message after success.</p>
      </div>
    </section>

    <section class="two-column-grid">
      <article>
        <header><strong>Invitation form</strong></header>
        <form @submit.prevent="form.post('/users')" data-testid="vue-create-form">
          <div style="margin-bottom: var(--space-4);">
            <label for="name-input">Name</label>
            <input
              id="name-input"
              v-model="form.name"
              type="text"
              data-testid="vue-name-input"
              placeholder="Jordan Ellis"
              :aria-invalid="form.errors.name ? 'true' : undefined"
            />
            <small v-if="form.errors.name" class="field-error" data-testid="vue-create-name-error">{{ form.errors.name }}</small>
          </div>
          <div style="margin-bottom: var(--space-4);">
            <label for="email-input">Email</label>
            <input
              id="email-input"
              v-model="form.email"
              type="email"
              data-testid="vue-email-input"
              placeholder="jordan.ellis@example.com"
            />
          </div>
          <div class="button-row">
            <button type="submit" class="button button--primary" :disabled="form.processing" data-testid="vue-submit-button">Queue invitation</button>
            <Link href="/users" class="button button--secondary">Back to team board</Link>
          </div>
        </form>
      </article>

      <article>
        <header><strong>What this demonstrates</strong></header>
        <ul class="feature-list">
          <li>Leave the form empty to see the inline 422 validation response</li>
          <li>Submit valid data to trigger a redirect back to the team board</li>
          <li>The success message is shared through the next Inertia response</li>
        </ul>
      </article>
    </section>
  </AppShell>
</template>
