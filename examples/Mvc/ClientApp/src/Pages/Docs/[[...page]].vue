<script setup>
import { Head, Link } from '@inertiajs/vue3';
import AppShell from '../../components/AppShell.vue';
import { computed } from 'vue';

const props = defineProps({ article: Object, breadcrumbs: Array, componentPattern: String, slug: String, segments: Array, matchedExistingArticle: Boolean, knownPages: Array, title: String, summary: String, highlights: Array });

function buildBreadcrumbs(segments) {
  const items = [{ label: 'docs', href: '/docs' }];
  const acc = [];
  for (const seg of segments) {
    acc.push(seg);
    items.push({ label: seg, href: `/docs/${acc.join('/')}` });
  }
  return items;
}

const navBreadcrumbs = computed(() => buildBreadcrumbs(props.segments ?? []));
</script>
<template>
  <Head title="Docs" />
  <AppShell>
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Runbooks</p>
        <h1>{{ title }}</h1>
        <p class="page-subtitle">{{ summary }}</p>
      </div>
    </section>

    <div class="docs-layout">
      <article data-testid="vue-docs-article">
        <header>
          <div class="article-header-row">
            <div>
              <small>Runbook overview</small>
              <h2>What this route proves</h2>
            </div>
            <mark>{{ matchedExistingArticle ? 'Catalog entry' : 'Fallback page' }}</mark>
          </div>
        </header>

        <p>One page file can own the whole documentation branch while the server emits a single, stable Inertia component string.</p>

        <nav aria-label="breadcrumbs" data-testid="vue-docs-breadcrumbs">
          <ul>
            <li v-for="(crumb, index) in navBreadcrumbs" :key="crumb.href">
              <Link :href="crumb.href" :aria-current="index === navBreadcrumbs.length - 1 ? 'page' : undefined">{{ crumb.label }}</Link>
            </li>
          </ul>
        </nav>

        <p class="pattern-box" data-testid="vue-docs-pattern">{{ componentPattern }}</p>

        <ol v-if="highlights?.length">
          <li v-for="h in highlights" :key="h">{{ h }}</li>
        </ol>

        <div v-html="article.content"></div>
      </article>

      <aside>
        <article>
          <header><small>Resolved route</small></header>
          <h3 data-testid="vue-docs-slug">{{ slug ?? '/docs' }}</h3>
          <div class="dl-grid">
            <div>
              <dt>Segment count</dt>
              <dd>{{ (segments?.length ?? 0) === 0 ? 1 : segments.length }}</dd>
            </div>
            <div>
              <dt>Route mode</dt>
              <dd>{{ (segments?.length ?? 0) === 0 ? 'Index' : 'Nested' }}</dd>
            </div>
            <div>
              <dt>Resolver</dt>
              <dd>Optional catch-all</dd>
            </div>
          </div>
        </article>

        <article v-if="knownPages?.length">
          <header>
            <small>More runbooks</small>
            <h4>Explore other paths</h4>
          </header>
          <nav>
            <ul>
              <li v-for="p in knownPages" :key="p.path">
                <Link
                  :href="p.href"
                  class="side-nav-link"
                  :aria-current="p.path === slug ? 'page' : undefined"
                >
                  <strong>{{ p.path }}</strong>
                  <small>{{ p.path === slug ? 'Current article' : 'Open article' }}</small>
                </Link>
              </li>
            </ul>
          </nav>
        </article>
      </aside>
    </div>
  </AppShell>
</template>
