<script setup>
import { Head, Link, router, usePage } from '@inertiajs/vue3';
import { onMounted, computed } from 'vue';
import AppShell from '../../components/AppShell.vue';

const page = usePage();
const authUser = page.props.auth?.user;

const props = defineProps({ summary: Object, topUsers: Array, monthlyChart: Object, recentActivity: Array });

const maxChartValue = computed(() =>
  props.monthlyChart ? Math.max(...props.monthlyChart.values, 1) : 1
);

onMounted(() => {
  if (!props.monthlyChart) {
    router.reload({
      only: ['monthlyChart'],
      preserveState: true,
      preserveScroll: true,
    });
  }
});
</script>
<template>
  <Head title="Dashboard" />
  <AppShell>
    <section class="vista-dashboard" aria-labelledby="dashboard-vista-title">
      <div class="vista-dashboard__frame">
        <p class="vista-dashboard__badge">New feature: deferred launch insights arrive after first paint</p>
        <h1 id="dashboard-vista-title">Gain complete visibility into your launch flow</h1>
        <p>
          Keep handoffs, team bandwidth, risk review, and secure operator access in one Inertia workspace.
        </p>

        <div class="vista-dashboard__actions">
          <Link href="/users" class="vista-button vista-button--primary">Review team board</Link>
          <template v-if="authUser">
            <Link href="/me" class="vista-button vista-button--secondary">Open secure profile</Link>
          </template>
          <template v-else>
            <button
              type="button"
              class="vista-button vista-button--secondary"
              @click="router.post('/auth/demo-sign-in')"
            >
              Sign in demo user
            </button>
          </template>
        </div>

        <div class="vista-dashboard__trusted">
          <span>Built to demonstrate</span>
          <div class="vista-dashboard__trusted-list">
            <strong>Shared props</strong>
            <strong>Redirect + flash</strong>
            <strong>Deferred reload</strong>
            <strong>Encrypted history</strong>
          </div>
        </div>

        <div class="vista-dashboard__summary">
          <div class="vista-dashboard__summary-card">
            <span>Active launches</span>
            <strong>{{ summary?.activeLaunches ?? 0 }}</strong>
          </div>
          <div class="vista-dashboard__summary-card">
            <span>At-risk accounts</span>
            <strong>{{ summary?.atRiskAccounts ?? 0 }}</strong>
          </div>
          <div class="vista-dashboard__summary-card">
            <span>Handoffs today</span>
            <strong>{{ summary?.handoffsToday ?? 0 }}</strong>
          </div>
          <div class="vista-dashboard__summary-card">
            <span>Team utilization</span>
            <strong>{{ summary?.teamUtilization ?? '0%' }}</strong>
          </div>
        </div>
      </div>
    </section>

    <section class="vista-dashboard__grid">
      <article class="vista-panel vista-panel--chart" data-testid="vue-dashboard-chart">
        <header>
          <strong>Launch velocity</strong>
          <p>The chart stays out of the first payload and hydrates through an Inertia partial reload.</p>
        </header>
        <div v-if="monthlyChart" class="launch-chart">
          <div v-for="(label, i) in monthlyChart.labels" :key="label" class="launch-chart__row">
            <span>{{ label }}</span>
            <div class="launch-chart__bar">
              <div
                class="launch-chart__fill"
                :style="{ width: `${Math.max((monthlyChart.values[i] / maxChartValue) * 100, 8)}%` }"
              />
            </div>
            <strong>{{ monthlyChart.values[i] }} launches</strong>
          </div>
        </div>
        <p v-else class="loading-pill" :aria-busy="!monthlyChart">Fetching deferred chart data…</p>
      </article>

      <article class="vista-panel">
        <header>
          <strong>Launch leads</strong>
          <p>The eager roster arrives with the page so the dashboard feels immediate.</p>
        </header>
        <div class="vista-leads">
          <div v-for="user in topUsers" :key="user.id" class="vista-leads__row">
            <div>
              <strong>{{ user.name }}</strong>
              <p>{{ user.role }}</p>
            </div>
            <small>{{ user.focus }}</small>
          </div>
        </div>
      </article>
    </section>

    <section class="vista-dashboard__grid vista-dashboard__grid--secondary">
      <article class="vista-panel">
        <header>
          <strong>Latest events</strong>
          <p>Merge-friendly activity can keep growing without changing the page shape.</p>
        </header>
        <div class="vista-activity">
          <div v-for="entry in recentActivity" :key="entry.id" class="vista-activity__row">
            <strong>{{ entry.action }}</strong>
            <small>{{ entry.at }}</small>
          </div>
        </div>
      </article>

      <article class="vista-panel vista-panel--narrative">
        <header>
          <strong>Why this page matters</strong>
          <p>It should feel closer to a product homepage than a protocol sandbox.</p>
        </header>
        <div class="vista-steps">
          <div>
            <span>1</span>
            <p>Open the team board to see eager props and redirect-driven flash messaging.</p>
          </div>
          <div>
            <span>2</span>
            <p>Stay here to watch the deferred insight payload hydrate after the first paint.</p>
          </div>
          <div>
            <span>3</span>
            <p>Sign in to open the protected profile and verify encrypted history behavior.</p>
          </div>
        </div>
      </article>
    </section>
  </AppShell>
</template>
