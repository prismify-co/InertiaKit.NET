<script setup>
import { Head, Link, usePage } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';

defineProps({
  greeting: {
    type: String,
    required: true,
  },
});

const page = usePage();
</script>

<template>
  <Head title="Home" />

  <AppShell
    eyebrow="Vue client"
    :title="greeting"
    description="This MVC example now mounts a real Vue Inertia app and covers navigation, PRG validation, redirects, and deferred data loading."
  >
    <section class="card-grid">
      <article class="panel accent-card">
        <p class="section-label">Shared app config</p>
        <strong>{{ page.props.appConfig?.name }}</strong>
      </article>

      <article class="panel">
        <p class="section-label">Browser cases</p>
        <ul class="stack-list">
          <li>Visit another page without a full reload.</li>
          <li>Submit an invalid form and read flashed errors.</li>
          <li>Trigger a deferred partial reload for sidebar data.</li>
        </ul>
      </article>
    </section>

    <section class="panel narrative-panel">
      <p class="section-label">Entry point</p>
      <h2 data-testid="vue-home-title">Vue is handling this page through Inertia, not Razor page-by-page rendering.</h2>
      <div class="button-row">
        <Link href="/users" class="button button--primary" data-testid="vue-users-link">Explore users</Link>
        <Link href="/Home/Privacy" class="button button--secondary" data-testid="vue-privacy-link">Read privacy page</Link>
      </div>
    </section>
  </AppShell>
</template>