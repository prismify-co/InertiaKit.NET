<script setup>
import { Head, Link } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';
</script>

<template>
  <Head title="Privacy" />

  <AppShell
    eyebrow="Vue pages"
    title="Privacy page"
    description="A simple secondary page confirms client-side navigation across multiple MVC controller actions."
  >
    <section class="panel narrative-panel">
      <p class="section-label">Secondary route</p>
      <h2 data-testid="vue-privacy-title">The client reached a second controller action without a full refresh.</h2>
      <p class="muted-text">
        That matters because it proves the browser-side adapter is driving real Inertia navigation rather than relying only on protocol-level tests.
      </p>

      <div class="button-row">
        <Link href="/" class="button button--secondary">Back home</Link>
        <Link href="/users" class="button button--primary">Open users</Link>
      </div>
    </section>
  </AppShell>
</template>