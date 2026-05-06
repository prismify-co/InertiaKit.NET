<script setup>
import { Head, Link, router, usePage } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';

const page = usePage();
const authUser = page.props.auth?.user;

defineProps({
  greeting: String,
  statusNote: String,
  systemSpecs: Array,
});
</script>
<template>
  <Head title="Home" />
  <AppShell>
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Practical Inertia example</p>
        <h1 data-testid="vue-home-title">{{ greeting }}</h1>
        <p class="page-subtitle">A realistic launch-operations workspace that ties shared props, redirects, auth, deferred data, and nested docs into one product flow.</p>
        <div class="action-row">
          <Link href="/users" class="button button--primary">Open team board</Link>
          <Link href="/docs/guides/getting-started" class="button button--secondary" data-testid="vue-docs-link">Read runbook</Link>
          <Link href="/dashboard" class="button button--secondary">View insights</Link>
        </div>
      </div>

      <aside class="page-hero__meta" aria-label="Page context">
        <div class="meta-grid">
          <div v-for="spec in systemSpecs" :key="spec.name">
            <span>{{ spec.name }}</span>
            <strong>{{ spec.value }}</strong>
          </div>
        </div>
      </aside>
    </section>

    <section class="story-grid">
      <article>
        <header><strong>Why this demo feels real</strong></header>
        <ul data-testid="vue-home-highlights" class="feature-list">
          <li>Queue an invite, follow a redirect, and read the flash message on the team board.</li>
          <li>Shared props keep the workspace identity available on every visit.</li>
          <li>Deferred data hydrates after first paint via partial reloads.</li>
          <li>Protected routes with ASP.NET Core authorization.</li>
        </ul>
      </article>

      <article>
        <header><strong>Protected access path</strong></header>
        <p class="demo-route-pill" data-testid="vue-auth-demo-route">Protected route: <strong>/me</strong></p>
        <template v-if="authUser">
          <p data-testid="vue-auth-demo-status">Signed in as <strong>{{ authUser.name }}</strong></p>
          <p>{{ authUser.email }}</p>
          <div class="compact-action-row">
            <Link href="/me" class="button button--secondary" data-testid="vue-open-auth-demo-button">Open access profile</Link>
          </div>
        </template>
        <template v-else>
          <p data-testid="vue-auth-demo-status">Currently browsing as guest.</p>
          <p>Sign in as the launch director to review the protected workspace profile.</p>
          <div class="compact-action-row">
            <button
              type="button"
              class="button button--secondary"
              data-testid="vue-auth-demo-sign-in"
              @click="router.post('/auth/demo-sign-in')"
            >
              Sign in and open access profile
            </button>
          </div>
        </template>
      </article>
    </section>
  </AppShell>
</template>
