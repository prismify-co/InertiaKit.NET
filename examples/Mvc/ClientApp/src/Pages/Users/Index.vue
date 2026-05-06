<script setup>
import { Head, Link } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';
defineProps({ users: Array, total: Number, overview: Array });
</script>
<template>
  <Head title="Users" />
  <AppShell>
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Team board</p>
        <h1>Launch team board</h1>
        <p class="page-subtitle">This is an eager Inertia index: the roster, coverage summary, and any redirect flash message arrive together from the server.</p>
      </div>
    </section>

    <div class="action-row">
      <Link href="/users/create" class="button button--primary" data-testid="vue-create-link">Invite teammate</Link>
      <Link href="/dashboard" class="button button--secondary">View insights</Link>
    </div>

    <div class="metric-grid">
      <div class="metric-card metric-card--featured">
        <span>Total specialists</span>
        <strong data-testid="vue-users-total">{{ total }}</strong>
        <small>Regional launch coverage</small>
      </div>
      <div v-for="item in overview" :key="item.label" class="metric-card">
        <span>{{ item.label }}</span>
        <strong>{{ item.value }}</strong>
        <small>{{ item.detail }}</small>
      </div>
    </div>

    <article>
      <header><strong>Delivery roster</strong></header>
      <h2>Current launch specialists</h2>
      <div data-testid="vue-users-list">
        <div v-for="user in users" :key="user.id" class="user-row">
          <div class="user-row__primary">
            <strong>{{ user.name }}</strong>
            <p>{{ user.role }}</p>
            <small>{{ user.email }}</small>
          </div>
          <div class="user-row__details">
            <span class="tag">{{ user.region }}</span>
            <span class="tag">{{ user.focus }}</span>
            <span class="tag">{{ user.status }}</span>
          </div>
          <small>{{ user.activeLaunches }} active launches</small>
        </div>
      </div>
    </article>
  </AppShell>
</template>
