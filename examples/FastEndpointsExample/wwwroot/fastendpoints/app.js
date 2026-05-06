var e=[{href:`/`,label:`Overview`},{href:`/users`,label:`Team`},{href:`/users/dashboard`,label:`Insights`}];function t(){return JSON.parse(document.getElementById(`app-data`)?.textContent??`{}`)}function n(){let e=document.getElementById(`app`);if(!e)throw Error(`Missing #app mount element`);return e}function r(e){return String(e??``).replaceAll(`&`,`&amp;`).replaceAll(`<`,`&lt;`).replaceAll(`>`,`&gt;`).replaceAll(`"`,`&quot;`).replaceAll(`'`,`&#39;`)}function i(t){let n=t.url?.split(`?`)[0]||`/`;return`
    <header class="shell-header">
      <div class="container shell-header__inner">
        <a href="/" class="brand">
          <strong>InertiaKit</strong>
          <span>FastEndpoints</span>
        </a>
        <nav class="shell-nav" data-testid="fastendpoints-nav">
          ${e.map(e=>`
            <a href="${e.href}" class="nav-pill ${n===e.href?`nav-pill--active`:``}" ${n===e.href?`aria-current="page"`:``}>
              ${e.label}
            </a>
          `).join(``)}
        </nav>
      </div>
    </header>
  `}function a(e){let t=e.props?.users||[],n=e.props?.stats,i=e.props?.countries||[];return`
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Team directory</p>
        <h1 data-testid="fastendpoints-users-panel">Launch specialists</h1>
        <p class="page-subtitle">Eager, optional, deferred, and once props demonstrate the full Inertia protocol from FastEndpoints handlers.</p>
      </div>
    </section>

    <section class="stats-grid">
      <div class="stat-card">
        <div class="stat-card__value">${t.length}</div>
        <div class="stat-card__label">Team members</div>
      </div>
      <div class="stat-card">
        <div class="stat-card__value">${n?.activeToday??`—`}</div>
        <div class="stat-card__label">Active today</div>
      </div>
      <div class="stat-card">
        <div class="stat-card__value">${i.length}</div>
        <div class="stat-card__label">Regions covered</div>
      </div>
    </section>

    <section class="card-grid">
      <article>
        <header><strong>Team roster</strong></header>
        ${t.length>0?`
          <div class="user-rows">
            ${t.map(e=>`
              <div class="user-row">
                <div class="user-row__avatar">${r(e.name?.charAt(0)||`?`)}</div>
                <div class="user-row__info">
                  <div class="user-row__name">${r(e.name)}</div>
                  <div class="user-row__email">${r(e.email)}</div>
                </div>
              </div>
            `).join(``)}
          </div>
        `:`<p class="muted-text">No team members found.</p>`}
      </article>

      <article data-testid="fastendpoints-protocol-panel">
        <header><strong>Protocol features</strong></header>
        <ul class="feature-list">
          <li><strong>Eager props</strong> resolve on every request</li>
          <li><strong>Optional props</strong> only evaluate on partial reloads</li>
          <li><strong>Deferred props</strong> load after initial render</li>
          <li><strong>Once props</strong> cache client-side permanently</li>
        </ul>
      </article>
    </section>
  `}function o(e){let t=e.props?.summary||{},n=e.props?.activities||[];return`
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Analytics</p>
        <h1 data-testid="fastendpoints-dashboard-summary">Operations dashboard</h1>
        <p class="page-subtitle">Merge props enable infinite scroll pagination with automatic activity deduplication.</p>
      </div>
    </section>

    <section class="stats-grid">
      <div class="stat-card">
        <div class="stat-card__value">${t.total??0}</div>
        <div class="stat-card__label">Total users</div>
      </div>
      <div class="stat-card">
        <div class="stat-card__value">${n.length}</div>
        <div class="stat-card__label">Recent events</div>
      </div>
    </section>

    <section class="card-grid">
      <article data-testid="fastendpoints-dashboard-activities">
        <header><strong>Activity feed</strong></header>
        ${n.length>0?`
          <div class="activity-feed">
            ${n.map(e=>`
              <div class="list-row">
                <div class="list-row__main">
                  <strong>${r(e.action)}</strong>
                </div>
                <div class="list-row__aside">
                  <span class="muted-text">${r(e.at)}</span>
                </div>
              </div>
            `).join(``)}
          </div>
        `:`<p class="muted-text">No recent activity.</p>`}
      </article>

      <article>
        <header><strong>Merge props</strong></header>
        <ul class="feature-list">
          <li>Activities are merged by <code>id</code> field</li>
          <li>Enables infinite scroll without duplicates</li>
          <li>Preserves history encryption for sensitive data</li>
        </ul>
      </article>
    </section>
  `}function s(e){return`
    <section class="page-hero">
      <div class="page-hero__body">
        <p class="eyebrow">Asset shell example</p>
        <h1 data-testid="fastendpoints-home-title">FastEndpoints + Inertia</h1>
        <p class="page-subtitle">A lightweight browser shell demonstrating the Inertia protocol with FastEndpoints handlers. The server adapter handles asset shells, partial reloads, and deferred props.</p>
      </div>
    </section>

    <section class="card-grid">
      <article>
        <header><strong>Protocol features</strong></header>
        <ul class="feature-list">
          <li><strong>Asset shell</strong> — First visits render HTML server-side</li>
          <li><strong>Partial reloads</strong> — Navigate without full page loads</li>
          <li><strong>Deferred props</strong> — Load data after initial render</li>
          <li><strong>Merge props</strong> — Combine data for infinite scroll</li>
        </ul>
      </article>

      <article>
        <header><strong>FastEndpoints integration</strong></header>
        <ul class="feature-list">
          <li>Typed request/response models</li>
          <li>Built-in validation support</li>
          <li>Endpoint groups and routing</li>
          <li>History encryption middleware</li>
        </ul>
      </article>
    </section>

    <section class="code-panel" data-testid="fastendpoints-page-object">
      <header><strong>Embedded page object</strong></header>
      <pre><code>${r(JSON.stringify(e,null,2))}</code></pre>
    </section>
  `}function c(e){switch(e.component){case`Users/Index`:return a(e);case`Users/Dashboard`:return o(e);default:return s(e)}}function l(e){n().innerHTML=`
    ${i(e)}
    <main class="container page-shell" data-testid="fastendpoints-shell">
      ${c(e)}
    </main>
  `}l(t());