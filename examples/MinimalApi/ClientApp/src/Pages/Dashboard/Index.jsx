import { Head, Link, router } from '@inertiajs/react';
import { useEffect } from 'react';
import { AppShell } from '../../components/AppShell';

function formatCurrency(value) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value ?? 0);
}

export default function DashboardIndex({ summary = {}, topUsers = [], monthlyChart, recentActivity = [] }) {
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
        title="Operations dashboard"
        subtitle="Eager props render immediately; deferred chart data arrives via a partial reload."
        actions={
          <Link href="/users" role="button" className="secondary">
            Back to users
          </Link>
        }
      >
        {/* KPI row */}
        <div className="grid">
          <article>
            <header><strong>Users</strong></header>
            <p style={{ fontSize: '2.5rem', fontWeight: 700, margin: 0 }}>{summary.users ?? 0}</p>
          </article>
          <article>
            <header><strong>Revenue</strong></header>
            <p style={{ fontSize: '1.75rem', fontWeight: 700, margin: 0 }}>{formatCurrency(summary.revenue)}</p>
          </article>
        </div>

        {/* Chart + top users */}
        <div className="grid">
          <article>
            <header><strong>Active accounts</strong></header>
            <ul>
              {topUsers.map((user) => (
                <li key={user.id}>{user.name}</li>
              ))}
            </ul>
          </article>

          <article data-testid="react-dashboard-chart">
            <header><strong>Monthly chart</strong></header>
            {monthlyChart ? (
              <div>
                {monthlyChart.labels.map((label, i) => (
                  <div key={label} className="chart-row">
                    <span>{label}</span>
                    <strong>{monthlyChart.values[i]}</strong>
                  </div>
                ))}
              </div>
            ) : (
              <p aria-busy="true">Fetching deferred chart data…</p>
            )}
          </article>
        </div>

        {/* Recent activity */}
        <article>
          <header><strong>Latest events</strong></header>
          <div>
            {recentActivity.map((entry) => (
              <div key={entry.id} className="activity-row">
                <strong>{entry.action}</strong>
                <small>{entry.at}</small>
              </div>
            ))}
          </div>
        </article>
      </AppShell>
    </>
  );
}
