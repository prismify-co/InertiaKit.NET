<script setup>
import { onMounted } from 'vue';
import { Head, Link, router } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';

const props = defineProps({
  summary: {
    type: Object,
    default: () => ({}),
  },
  recentUsers: {
    type: Array,
    default: null,
  },
  feed: {
    type: Array,
    default: () => [],
  },
});

onMounted(() => {
  if (!props.recentUsers) {
    router.reload({
      only: ['recentUsers'],
      preserveState: true,
      preserveScroll: true,
    });
  }
});
</script>

<template>
  <Head title="Dashboard" />

  <AppShell>
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Deferred sidebar</p>
        <h1>Users dashboard</h1>
        <p class="page-subtitle">The server ships the dashboard frame first, then Vue asks Inertia for the deferred recent-users sidebar without reloading the page.</p>
      </div>
    </section>

    <section class="stats-grid">
      <article class="stat-card">
        <p class="section-label">Known users</p>
        <strong>{{ summary.total ?? 0 }}</strong>
      </article>

      <article class="stat-card">
        <p class="section-label">Feed strategy</p>
        <strong>Merged activity feed</strong>
      </article>
    </section>

    <section class="card-grid">
      <article data-testid="vue-dashboard-recent-users">
        <header><strong>Recent users</strong></header>

        <ul v-if="recentUsers" class="stack-list">
          <li v-for="user in recentUsers" :key="user.id" class="list-row">
            <strong>{{ user.name }}</strong>
          </li>
        </ul>
        <p v-else class="loading-pill" data-testid="vue-dashboard-loading">Fetching deferred sidebar data…</p>
      </article>

      <article>
        <header><strong>Activity feed</strong></header>
        <ul class="stack-list">
          <li v-for="entry in feed" :key="entry.id" class="list-row">
            <strong>{{ entry.action }}</strong>
            <span class="muted-text">event #{{ entry.id }}</span>
          </li>
        </ul>
      </article>
    </section>

    <div class="button-row">
      <Link href="/users" class="button button--secondary">Back to users</Link>
    </div>
  </AppShell>
</template>