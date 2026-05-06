<script setup>
import { Link, usePage, router } from '@inertiajs/vue3';
import { computed } from 'vue';

const page = usePage();
const flashSuccess = computed(() => page.props.flash?.success ?? '');
const authUser = computed(() => page.props.auth?.user);

const NAV_ITEMS = [
  { href: '/', label: 'Overview' },
  { href: '/docs/guides/getting-started', label: 'Runbooks' },
  { href: '/users', label: 'Team' },
  { href: '/dashboard', label: 'Insights' },
];

const navItems = computed(() =>
  authUser.value
    ? [...NAV_ITEMS, { href: '/me', label: 'Account' }]
    : NAV_ITEMS
);

function isActive(currentUrl, href) {
  return href === '/' ? currentUrl === href : currentUrl.startsWith(href);
}
</script>

<template>
  <div>
    <header class="shell-header">
      <div class="container shell-header__inner">
        <Link href="/" class="brand">
          <strong>InertiaKit</strong>
          <small>MVC + Vue</small>
        </Link>

        <nav class="shell-nav" aria-label="Primary">
          <ul data-testid="vue-nav">
            <li v-for="item in navItems" :key="item.href">
              <Link
                :href="item.href"
                class="nav-pill"
                :aria-current="isActive(page.url, item.href) ? 'page' : undefined"
              >
                {{ item.label }}
              </Link>
            </li>
          </ul>
        </nav>

        <div class="shell-session">
          <small class="auth-label" data-testid="vue-auth-label">
            {{ authUser ? authUser.name : 'guest' }}
          </small>
          <template v-if="authUser">
            <Link href="/me" class="button button--secondary">Account</Link>
            <button
              type="button"
              class="button button--secondary"
              data-testid="vue-sign-out-button"
              @click="router.post('/auth/demo-sign-out')"
            >
              Sign out
            </button>
          </template>
          <button
            v-else
            type="button"
            class="button"
            data-testid="vue-sign-in-button"
            @click="router.post('/auth/demo-sign-in')"
          >
            Sign in
          </button>
        </div>
      </div>
    </header>

    <main class="container page-shell">
      <p v-if="flashSuccess" class="flash-banner" data-testid="vue-flash-success">{{ flashSuccess }}</p>
      <slot />
    </main>
  </div>
</template>
