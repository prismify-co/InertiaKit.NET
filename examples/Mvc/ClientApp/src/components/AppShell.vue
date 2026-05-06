<script setup>
import { computed } from 'vue';
import { Link, usePage } from '@inertiajs/vue3';

defineProps({
  eyebrow: {
    type: String,
    required: true,
  },
  title: {
    type: String,
    required: true,
  },
  description: {
    type: String,
    required: true,
  },
});

const page = usePage();

const navItems = [
  { href: '/', label: 'Home' },
  { href: '/Home/Privacy', label: 'Privacy' },
  { href: '/users', label: 'Users' },
  { href: '/users/dashboard', label: 'Dashboard' },
];

const flashSuccess = computed(() => page.props.flash?.success ?? '');
const appName = computed(() => page.props.appConfig?.name ?? 'InertiaKit Vue');
const authLabel = computed(() => page.props.auth?.user?.name ?? 'guest');

function isActive(href) {
  return href === '/' ? page.url === href : page.url.startsWith(href);
}
</script>

<template>
  <div class="vue-app-frame" data-framework="vue">
    <div class="vue-app-orb vue-app-orb--top" />
    <div class="vue-app-orb vue-app-orb--bottom" />

    <main class="vue-shell">
      <header class="vue-header panel">
        <div>
          <p class="eyebrow">{{ eyebrow }}</p>
          <h1 class="hero-title">{{ title }}</h1>
          <p class="hero-copy">{{ description }}</p>
        </div>

        <nav class="vue-nav" data-testid="vue-nav">
          <Link
            v-for="item in navItems"
            :key="item.href"
            :href="item.href"
            :class="isActive(item.href) ? 'nav-link nav-link--active' : 'nav-link'"
          >
            {{ item.label }}
          </Link>
        </nav>
      </header>

      <section class="vue-meta-row">
        <div class="pill">{{ appName }}</div>
        <div class="pill">Shared auth: {{ authLabel }}</div>
      </section>

      <transition name="fade-slide">
        <section v-if="flashSuccess" class="flash-banner panel" data-testid="vue-flash">
          {{ flashSuccess }}
        </section>
      </transition>

      <section class="vue-page-content">
        <slot />
      </section>
    </main>
  </div>
</template>