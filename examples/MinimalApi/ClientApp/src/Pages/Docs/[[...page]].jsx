import { Head, Link } from '@inertiajs/react';
import { AppShell } from '../../components/AppShell';

function buildBreadcrumbs(segments) {
  const items = [{ label: 'docs', href: '/docs' }];
  const acc = [];
  for (const seg of segments) {
    acc.push(seg);
    items.push({ label: seg, href: `/docs/${acc.join('/')}` });
  }
  return items;
}

export default function DocsCatchAll({
  componentPattern,
  slug,
  segments = [],
  title,
  summary,
  highlights = [],
  matchedExistingArticle,
  knownPages = [],
}) {
  const breadcrumbs = buildBreadcrumbs(segments);
  const relatedPages = knownPages.filter((p) => p.path !== slug);
  const featuredPage = relatedPages[0] ?? knownPages[0] ?? null;

  return (
    <>
      <Head title={title} />
      <AppShell
        title={title}
        subtitle="One optional catch-all file owns the whole documentation branch."
        actions={
          <>
            <Link href="/docs" role="button" className="secondary">
              Docs index
            </Link>
            {featuredPage && (
              <Link href={featuredPage.href} role="button">
                Explore {featuredPage.path}
              </Link>
            )}
          </>
        }
      >
        <div className="docs-layout">

          {/* Main article */}
          <article data-testid="react-docs-article">
            <header>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                  <small>Guide overview</small>
                  <h2>What this route proves</h2>
                </div>
                <mark>{matchedExistingArticle ? 'Catalog entry' : 'Fallback page'}</mark>
              </div>
            </header>

            <p>{summary}</p>

            {/* Breadcrumbs */}
            <nav aria-label="breadcrumbs" data-testid="react-docs-breadcrumbs">
              <ul>
                {breadcrumbs.map((item, index) => {
                  const isLast = index === breadcrumbs.length - 1;
                  return (
                    <li key={item.href}>
                      {isLast ? (
                        <a href={item.href} aria-current="page">{item.label}</a>
                      ) : (
                        <Link href={item.href}>{item.label}</Link>
                      )}
                    </li>
                  );
                })}
              </ul>
            </nav>

            {/* Component pattern */}
            <p className="pattern-box" data-testid="react-docs-pattern">{componentPattern}</p>
            <p>
              One page file can own the whole documentation branch while the server
              emits a single, stable Inertia component string.
            </p>

            <ol>
              {highlights.map((h) => (
                <li key={h}>{h}</li>
              ))}
            </ol>
          </article>

          {/* Sidebar */}
          <aside>
            <article>
              <header><small>Resolved route</small></header>
              <h3 data-testid="react-docs-slug">{slug ?? '/docs'}</h3>
              <dl className="dl-grid">
                <div>
                  <dt>Segment count</dt>
                  <dd>{segments.length === 0 ? 1 : segments.length}</dd>
                </div>
                <div>
                  <dt>Route mode</dt>
                  <dd>{segments.length === 0 ? 'Index' : 'Nested'}</dd>
                </div>
                <div>
                  <dt>Resolver</dt>
                  <dd>Optional catch-all</dd>
                </div>
              </dl>
            </article>

            <article>
              <header>
                <small>More routes</small>
                <h4>Explore other paths</h4>
              </header>
              <nav>
                <ul>
                  {knownPages.map((page) => (
                    <li key={page.path}>
                      <Link
                        href={page.href}
                        className="side-nav-link"
                        aria-current={page.path === slug ? 'page' : undefined}
                      >
                        <strong>{page.path}</strong>
                        <small>{page.path === slug ? 'Current article' : 'Open article'}</small>
                      </Link>
                    </li>
                  ))}
                </ul>
              </nav>
            </article>
          </aside>

        </div>
      </AppShell>
    </>
  );
}
