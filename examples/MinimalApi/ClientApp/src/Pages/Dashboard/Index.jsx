import { Head, Link, router, usePage } from '@inertiajs/react';
import { useEffect } from 'react';
import { AppShell } from '../../components/AppShell';

export default function DashboardIndex({ summary = {}, topUsers = [], monthlyChart, recentActivity = [] }) {
  const { props } = usePage();
  const authUser = props.auth?.user ?? null;
  const maxChartValue = monthlyChart ? Math.max(...monthlyChart.values, 1) : 1;

  useEffect(() => {
    if (!monthlyChart) {
      router.reload({
        only: ['monthlyChart'],
        preserveState: true,
        preserveScroll: true,
      });
    }
  }, [monthlyChart]);

  return (
    <>
      <Head title="Dashboard" />
      <AppShell
        title="Launch insights"
        subtitle="Summary cards render immediately; the velocity chart arrives via a partial reload that requests only the deferred payload."
        heroContent={
          <section className="vista-dashboard" aria-labelledby="dashboard-vista-title">
            <div className="vista-dashboard__frame">
              <div className="vista-dashboard__cliff vista-dashboard__cliff--left" aria-hidden="true" />
              <div className="vista-dashboard__cliff vista-dashboard__cliff--right" aria-hidden="true" />

              <div className="vista-dashboard__copy">
                <p className="vista-dashboard__badge">New feature: deferred launch insights arrive after first paint</p>
                <h1 id="dashboard-vista-title">Gain complete visibility into your launch flow</h1>
                <p>
                  Keep handoffs, team bandwidth, risk review, and secure operator access in one Inertia workspace.
                  The screen still proves shared props, partial reloads, and history protection, but it now reads like a product.
                </p>
              </div>

              <div className="vista-dashboard__actions">
                <Link href="/users" className="vista-button vista-button--primary">
                  Review team board
                </Link>
                {authUser ? (
                  <Link href="/me" className="vista-button vista-button--secondary">
                    Open secure profile
                  </Link>
                ) : (
                  <button
                    type="button"
                    className="vista-button vista-button--secondary"
                    onClick={() => router.post('/auth/demo-sign-in')}
                  >
                    Sign in demo user
                  </button>
                )}
              </div>

              <div className="vista-dashboard__trusted">
                <span>Built to demonstrate</span>
                <div className="vista-dashboard__trusted-list">
                  <strong>Shared props</strong>
                  <strong>Redirect + flash</strong>
                  <strong>Deferred reload</strong>
                  <strong>Encrypted history</strong>
                </div>
              </div>

              <div className="vista-dashboard__summary">
                <div className="vista-dashboard__summary-card">
                  <span>Active launches</span>
                  <strong>{summary.activeLaunches ?? 0}</strong>
                </div>
                <div className="vista-dashboard__summary-card">
                  <span>At-risk accounts</span>
                  <strong>{summary.atRiskAccounts ?? 0}</strong>
                </div>
                <div className="vista-dashboard__summary-card">
                  <span>Handoffs today</span>
                  <strong>{summary.handoffsToday ?? 0}</strong>
                </div>
                <div className="vista-dashboard__summary-card">
                  <span>Team utilization</span>
                  <strong>{summary.teamUtilization ?? '0%'}</strong>
                </div>
              </div>
            </div>
          </section>
        }
      >
        <section className="vista-dashboard__grid">
          <article className="vista-panel vista-panel--chart" data-testid="react-dashboard-chart">
            <header>
              <strong>Launch velocity</strong>
              <p>The chart stays out of the first payload and hydrates through an Inertia partial reload.</p>
            </header>
            {monthlyChart ? (
              <div className="launch-chart">
                {monthlyChart.labels.map((label, i) => {
                  const value = monthlyChart.values[i];
                  const width = `${Math.max((value / maxChartValue) * 100, 8)}%`;

                  return (
                    <div key={label} className="launch-chart__row">
                      <span>{label}</span>
                      <div className="launch-chart__bar">
                        <div className="launch-chart__fill" style={{ width }} />
                      </div>
                      <strong>{value} launches</strong>
                    </div>
                  );
                })}
              </div>
            ) : (
              <p aria-busy="true">Fetching deferred chart data…</p>
            )}
          </article>

          <article className="vista-panel">
            <header>
              <strong>Launch leads</strong>
              <p>The eager roster arrives with the page so the dashboard feels immediate.</p>
            </header>
            <div className="vista-leads">
              {topUsers.map((user) => (
                <div key={user.id} className="vista-leads__row">
                  <div>
                    <strong>{user.name}</strong>
                    <p>{user.role}</p>
                  </div>
                  <small>{user.focus}</small>
                </div>
              ))}
            </div>
          </article>
        </section>

        <section className="vista-dashboard__grid vista-dashboard__grid--secondary">
          <article className="vista-panel">
            <header>
              <strong>Latest events</strong>
              <p>Merge-friendly activity can keep growing without changing the page shape.</p>
            </header>
            <div className="vista-activity">
              {recentActivity.map((entry) => (
                <div key={entry.id} className="vista-activity__row">
                  <strong>{entry.action}</strong>
                  <small>{entry.at}</small>
                </div>
              ))}
            </div>
          </article>

          <article className="vista-panel vista-panel--narrative">
            <header>
              <strong>Why this page matters</strong>
              <p>It should feel closer to a product homepage than a protocol sandbox.</p>
            </header>
            <div className="vista-steps">
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
    </>
  );
}
