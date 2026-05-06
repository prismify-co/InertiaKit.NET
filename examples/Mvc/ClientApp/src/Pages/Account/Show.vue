<script setup>
import { Head, Link } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';
defineProps({
  profile: Object,
  workspaceAccess: Array,
  secureFeatures: Array,
  recentEvents: Array,
});
</script>
<template>
  <Head title="Authenticated Demo" />
  <AppShell>
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Authenticated</p>
        <h1>Launch director access profile</h1>
        <p class="page-subtitle">This route is protected by ASP.NET Core authorization and still renders through the same Inertia adapter pipeline.</p>
      </div>
    </section>

    <section class="two-column-grid">
      <article>
        <header><strong>Authenticated identity</strong></header>
        <h2 data-testid="vue-account-name">{{ profile?.name ?? 'Unknown user' }}</h2>
        <p data-testid="vue-account-email">{{ profile?.email ?? 'No email available' }}</p>
        <p>
          <small data-testid="vue-account-role">{{ profile?.role ?? 'No role assigned' }}</small>
        </p>
      </article>

      <article>
        <header><strong>Workspace access</strong></header>
        <div class="chip-grid">
          <span v-for="item in workspaceAccess" :key="item" class="tag">{{ item }}</span>
        </div>
      </article>
    </section>

    <section class="two-column-grid">
      <article>
        <header><strong>What this proves</strong></header>
        <ul data-testid="vue-account-features" class="feature-list">
          <li v-for="feature in secureFeatures" :key="feature">{{ feature }}</li>
        </ul>
      </article>

      <article>
        <header><strong>Recent authenticated events</strong></header>
        <div data-testid="vue-account-events">
          <div v-for="event in recentEvents" :key="event.id" class="activity-row">
            <strong>{{ event.action }}</strong>
            <small>{{ event.at }}</small>
          </div>
        </div>
      </article>
    </section>
  </AppShell>
</template>
