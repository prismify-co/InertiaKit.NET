<script setup>
import { Head, Link } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';
</script>

<template>
  <Head title="Privacy" />

  <AppShell>
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Vue pages</p>
        <h1>Privacy page</h1>
        <p class="page-subtitle">A simple secondary page confirms client-side navigation across multiple MVC controller actions.</p>
      </div>
    </section>

    <article>
      <header><strong>Secondary route</strong></header>
      <h2 data-testid="vue-privacy-title">The client reached a second controller action without a full refresh.</h2>
      <p class="muted-text">
        That matters because it proves the browser-side adapter is driving real Inertia navigation rather than relying only on protocol-level tests.
      </p>

      <div class="button-row">
        <Link href="/" class="button button--secondary">Back home</Link>
        <Link href="/users" class="button button--primary">Open users</Link>
      </div>
    </article>
  </AppShell>
</template>