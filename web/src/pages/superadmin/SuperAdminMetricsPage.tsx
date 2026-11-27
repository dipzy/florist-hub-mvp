import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis, Line, ComposedChart } from 'recharts';
import { api } from '../../api/client';
import type { PlatformMetrics } from '../../api/types';
import { ErrorState } from '../../components/ui/ErrorState';
import { LoadingState } from '../../components/ui/LoadingState';
import { formatMoney, formatDate } from '../../lib/locale';

type Period = 7 | 30 | 90;

export function SuperAdminMetricsPage() {
  const { t } = useTranslation();
  const [period, setPeriod] = useState<Period>(30);
  const [metrics, setMetrics] = useState<PlatformMetrics | null>(null);
  const [err, setErr] = useState<string | null>(null);

  function loadMetrics() {
    setErr(null);
    return api
      .platformMetrics(period)
      .then(setMetrics)
      .catch(() => setErr(t('superadmin.metrics.loadError')));
  }

  useEffect(() => {
    loadMetrics().catch(() => undefined);
  }, [period, t]);

  if (err) return <ErrorState message={err} onRetry={() => loadMetrics()} />;
  if (!metrics) return <LoadingState message={t('superadmin.metrics.loading')} />;

  const chartData = metrics.ordersByDay.map((row, i) => ({
    date: row.date,
    encomendas: row.count,
    gmv: metrics.gmvByDay[i]?.amount ?? 0,
  }));

  const tickDate = (d: string) =>
    formatDate(new Date(`${d}T12:00:00`), { day: '2-digit', month: 'short' });

  return (
    <div className="page">
      <header className="page-header">
        <h1>{t('superadmin.metrics.title')}</h1>
        <p className="lede muted">{t('superadmin.metrics.lede')}</p>
      </header>
      <PeriodSelector period={period} onChange={setPeriod} label={(d) => t('superadmin.metrics.daysChip', { days: d })} />

      <div className="grid stats">
        <article className="card stat-card">
          <p className="stat-label">{t('superadmin.metrics.activeStores')}</p>
          <p className="stat">{metrics.activeStores}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">{t('superadmin.metrics.ordersPeriod', { days: metrics.days })}</p>
          <p className="stat">{metrics.ordersInPeriod}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">{t('superadmin.metrics.gmvPeriod', { days: metrics.days })}</p>
          <p className="stat">{formatMoney(metrics.gmvInPeriod)}</p>
          <p className="muted stat-footnote">{t('superadmin.metrics.gmvNote')}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">{t('superadmin.metrics.commissionPeriod', { days: metrics.days })}</p>
          <p className="stat">{formatMoney(metrics.commissionEarned)}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">{t('superadmin.metrics.stripeConnected')}</p>
          <p className="stat">{metrics.storesWithStripeConnected}</p>
        </article>
        <article className="card stat-card">
          <p className="stat-label">{t('superadmin.metrics.newStoresMonth')}</p>
          <p className="stat">{metrics.newStoresThisMonth}</p>
        </article>
      </div>

      <section className="card platform-metrics-chart">
        <h2>{t('superadmin.metrics.chartTitle')}</h2>
        <ResponsiveContainer width="100%" height={280}>
          <ComposedChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tickFormatter={tickDate} />
            <YAxis yAxisId="left" allowDecimals={false} />
            <YAxis yAxisId="right" orientation="right" />
            <Tooltip
              formatter={(value, name) =>
                name === 'gmv' || name === 'GMV (€)' ? formatMoney(Number(value)) : value
              }
            />
            <Bar yAxisId="left" dataKey="encomendas" fill="#2d6a4f" name={t('superadmin.metrics.colOrders')} />
            <Line yAxisId="right" type="monotone" dataKey="gmv" stroke="#95d5b2" name="GMV" />
          </ComposedChart>
        </ResponsiveContainer>
      </section>

      <section className="card" style={{ marginTop: '1.5rem' }}>
        <h2>{t('superadmin.metrics.topStores')}</h2>
        <table className="data-table">
          <thead>
            <tr>
              <th>{t('superadmin.metrics.colStore')}</th>
              <th>{t('superadmin.metrics.colOrders')}</th>
              <th>{t('superadmin.metrics.colGmv')}</th>
            </tr>
          </thead>
          <tbody>
            {metrics.topStores.map((row) => (
              <tr key={row.tenantId}>
                <td>{row.name}</td>
                <td>{row.orderCount}</td>
                <td>{formatMoney(row.gmv)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="card platform-metrics-chart" style={{ marginTop: '1.5rem' }}>
        <h2>{t('superadmin.metrics.newByMonth')}</h2>
        <ResponsiveContainer width="100%" height={240}>
          <BarChart data={metrics.newStoresByMonth}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" />
            <YAxis allowDecimals={false} />
            <Tooltip />
            <Bar dataKey="count" fill="#40916c" name={t('superadmin.metrics.colStore')} />
          </BarChart>
        </ResponsiveContainer>
      </section>
    </div>
  );
}

function PeriodSelector({
  period,
  onChange,
  label,
}: {
  period: Period;
  onChange: (p: Period) => void;
  label: (d: Period) => string;
}) {
  return (
    <div className="row gap-sm" style={{ marginBottom: '1rem' }}>
      {([7, 30, 90] as Period[]).map((d) => (
        <button
          key={d}
          type="button"
          className={period === d ? 'chip active' : 'chip'}
          onClick={() => onChange(d)}
        >
          {label(d)}
        </button>
      ))}
    </div>
  );
}
