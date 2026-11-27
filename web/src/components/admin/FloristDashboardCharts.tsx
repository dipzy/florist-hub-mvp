import { useMemo, type PropsWithChildren, type HTMLAttributes } from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { FloristDashboard, FloristDashboardMetrics } from '../../api/types';
import { formatMoney } from '../../lib/locale';
import { orderStatusLabel } from '../../lib/enumLabels';
import { useTranslation } from 'react-i18next';
import { OrderStatusBadge } from '../ui/Badge';
import { countLabel } from '../../lib/pluralPt';
import { orderStatusVariant } from '../../lib/orderStatus';

export type DashboardPeriod = 7 | 30 | 90;

type Props = {
  summary: FloristDashboard;
  metrics: FloristDashboardMetrics;
  period: DashboardPeriod;
  onPeriodChange: (days: DashboardPeriod) => void;
  primaryColor: string;
  accentColor: string;
};

const periodOptions: DashboardPeriod[] = [7, 30, 90];

function cssVar(name: string, fallback: string) {
  if (typeof document === 'undefined') return fallback;
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return value || fallback;
}

function formatDayLabel(iso: string, locale: string) {
  const date = new Date(`${iso}T12:00:00`);
  const intl = locale.startsWith('en') ? 'en-GB' : 'pt-PT';
  return date.toLocaleDateString(intl, { day: '2-digit', month: 'short' });
}

function formatDayTooltip(iso: string, locale: string) {
  const date = new Date(`${iso}T12:00:00`);
  const intl = locale.startsWith('en') ? 'en-GB' : 'pt-PT';
  return date.toLocaleDateString(intl, { weekday: 'long', day: 'numeric', month: 'long' });
}

export function FloristDashboardCharts({
  summary,
  metrics,
  period,
  onPeriodChange,
  primaryColor,
  accentColor,
}: Props) {
  const { t, i18n } = useTranslation();
  const series = useMemo(
    () =>
      metrics.ordersByDay.map((row, index) => ({
        date: row.date,
        encomendas: row.count,
        receita: metrics.revenueByDay[index]?.revenue ?? 0,
      })),
    [metrics],
  );

  const statusRows = useMemo(() => {
    return Object.entries(summary.ordersByStatus)
      .map(([key, count]) => {
        const status = Number(key);
        const label = Number.isNaN(status) ? key : orderStatusLabel(status, t);
        return { key, status, label, count };
      })
      .sort((a, b) => {
        const as = Number.isNaN(a.status) ? 999 : a.status;
        const bs = Number.isNaN(b.status) ? 999 : b.status;
        return as - bs;
      });
  }, [summary.ordersByStatus, t]);

  const statusFill = (status: number) => {
    if (Number.isNaN(status)) return cssVar('--fh-neutral-400', '#94a3b8');
    const variant = orderStatusVariant(status);
    const map: Record<string, string> = {
      pending: cssVar('--fh-warning', '#f59e0b'),
      confirmed: cssVar('--fh-success', '#16a34a'),
      progress: '#2563eb',
      ready: '#c45c6a',
      delivered: cssVar('--fh-sage-600', primaryColor),
      cancelled: cssVar('--fh-danger', '#dc2626'),
      rejected: cssVar('--fh-danger', '#dc2626'),
      neutral: cssVar('--fh-neutral-400', '#94a3b8'),
    };
    return map[variant] ?? primaryColor;
  };

  const topFlowers = metrics.topFlowers.length > 0 ? metrics.topFlowers : summary.topItems;

  return (
    <div className="dashboard-analytics stack">
      <div className="dashboard-period card">
        <div className="dashboard-period-head">
          <h2>Resumo do período</h2>
          <p className="muted">
            {metrics.pendingOrders} pendentes · {formatMoney(metrics.periodRevenue)} de receita (pagamentos simulados
            capturados)
          </p>
        </div>
        <div className="dashboard-period-tabs" role="tablist" aria-label="Período do dashboard">
          {periodOptions.map((days) => (
            <button
              key={days}
              type="button"
              role="tab"
              aria-selected={period === days}
              className={period === days ? 'active' : 'secondary'}
              onClick={() => onPeriodChange(days)}
            >
              {days} dias
            </button>
          ))}
        </div>
      </div>

      <ChartSection className="dashboard-chart card" aria-label="Encomendas e receita por dia">
        <h2>Encomendas e receita por dia</h2>
        <div className="dashboard-recharts">
          <ResponsiveContainer width="100%" height={280}>
            <ComposedChart data={series} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(0,0,0,0.08)" />
              <XAxis
                dataKey="date"
                tickFormatter={(v) => formatDayLabel(String(v), i18n.language)}
                minTickGap={16}
              />
              <YAxis yAxisId="left" allowDecimals={false} width={36} />
              <YAxis yAxisId="right" orientation="right" width={48} tickFormatter={(v) => `${v}€`} />
              <Tooltip
                labelFormatter={(v) => formatDayTooltip(String(v), i18n.language)}
                formatter={(value: number, name: string) => [
                  name === 'receita' ? formatMoney(Number(value)) : value,
                  name === 'receita' ? 'Receita' : 'Encomendas',
                ]}
              />
              <Legend />
              <Bar yAxisId="left" dataKey="encomendas" name="Encomendas" fill={primaryColor} radius={[4, 4, 0, 0]} />
              <Line
                yAxisId="right"
                type="monotone"
                dataKey="receita"
                name="Receita"
                stroke={accentColor}
                strokeWidth={2}
                dot={{ r: 3, fill: accentColor }}
                activeDot={{ r: 5 }}
              />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      </ChartSection>

      <div className="dashboard-split">
        <div className="dashboard-chart card" aria-label="Encomendas por estado">
          <h2>Encomendas por estado</h2>
          {statusRows.length === 0 ? (
            <p className="muted">Sem encomendas registadas.</p>
          ) : (
            <div className="dashboard-recharts">
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={statusRows} layout="vertical" margin={{ left: 8, right: 16 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="rgba(0,0,0,0.08)" />
                  <XAxis type="number" allowDecimals={false} />
                  <YAxis type="category" dataKey="label" width={140} />
                  <Tooltip
                    formatter={(value: number) => [value, 'Encomendas']}
                    labelFormatter={(_, payload) => {
                      const row = payload?.[0]?.payload as (typeof statusRows)[number] | undefined;
                      if (!row) return '';
                      return Number.isNaN(row.status) ? row.label : orderStatusLabel(row.status, t);
                    }}
                  />
                  <Bar dataKey="count" radius={[0, 4, 4, 0]}>
                    {statusRows.map((row) => (
                      <Cell key={row.key} fill={statusFill(row.status)} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
          <ul className="dashboard-status-legend">
            {statusRows.map((row) => (
              <li key={row.key}>
                {Number.isNaN(row.status) ? (
                  row.label
                ) : (
                  <OrderStatusBadge
                    status={row.status}
                    label={Number.isNaN(row.status) ? row.label : orderStatusLabel(row.status, t)}
                  />
                )}
                <span className="muted">{row.count}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="dashboard-chart card" aria-label="Flores mais vendidas">
          <h2>Flores mais vendidas</h2>
          {topFlowers.length === 0 ? (
            <p className="muted">Ainda não há linhas com flores do catálogo.</p>
          ) : (
            <ChartSection className="dashboard-recharts">
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={topFlowers} layout="vertical" margin={{ left: 8, right: 16 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="rgba(0,0,0,0.08)" />
                  <XAxis type="number" allowDecimals={false} />
                  <YAxis type="category" dataKey="name" width={120} />
                  <Tooltip
                    formatter={(value: number, _name, item) => {
                      const row = item.payload as (typeof topFlowers)[number];
                      return [
                        `${value} ${countLabel(value, 'linha', 'linhas')} · ${formatMoney(row.totalValue)}`,
                        row.name,
                      ];
                    }}
                  />
                  <Bar dataKey="orderLineCount" fill={primaryColor} radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </ChartSection>
          )}
        </div>
      </div>
    </div>
  );
}

/** Evita importar framer-motion — wrapper semântico apenas para agrupar. */
function ChartSection({
  children,
  className,
  ...rest
}: PropsWithChildren<{ className?: string } & HTMLAttributes<HTMLDivElement>>) {
  return (
    <div className={className} {...rest}>
      {children}
    </div>
  );
}
