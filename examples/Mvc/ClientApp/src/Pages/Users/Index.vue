<script setup>
import { Head, Link } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';

defineProps({
  users: {
    type: Array,
    default: () => [],
  },
  errors: {
    type: Object,
    default: () => ({}),
  },
});
</script>

<template>
  <Head title="Users" />

  <AppShell
    eyebrow="Vue pages"
    title="Users index"
    description="This page is controller-driven on the server and Vue-rendered on the client, with shared props and redirect flash support."
  >
    <section class="stats-grid">
      <article class="panel stat-card">
        <p class="section-label">Visible users</p>
        <strong data-testid="vue-users-total">{{ users.length }}</strong>
      </article>

      <article class="panel stat-card">
        <p class="section-label">Always prop</p>
        <strong>{{ Object.keys(errors).length === 0 ? 'errors ready' : 'errors present' }}</strong>
      </article>
    </section>

    <section class="button-row">
      <Link href="/users/create" class="button button--primary" data-testid="vue-create-link">Create user</Link>
      <Link href="/users/dashboard" class="button button--secondary">Open dashboard</Link>
    </section>

    <section class="panel">
      <h2 class="section-heading">Current users</h2>
      <ul class="user-list" data-testid="vue-users-list">
        <li v-for="user in users" :key="user.id" class="user-card">
          <div>
            <strong>{{ user.name }}</strong>
            <p class="muted-text">{{ user.email }}</p>
          </div>
          <span class="pill">#{{ user.id }}</span>
        </li>
      </ul>
    </section>
  </AppShell>
</template>