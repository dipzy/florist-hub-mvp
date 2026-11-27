import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminLoadGate, useAdminLoad } from '../../components/admin/AdminLoadGate';
import { api } from '../../api/client';
import type { ComplianceAuditEntry } from '../../api/types';
import { formatDateTime } from '../../lib/locale';

export function AdminCompliancePage() {
  const { t } = useTranslation();
  const [items, setItems] = useState<ComplianceAuditEntry[]>([]);
  const [total, setTotal] = useState(0);

  const screen = useAdminLoad(async () => {
    const res = await api.complianceAudit();
    setItems(res.items);
    setTotal(res.total);
  }, []);

  if (screen.phase !== 'ready') {
    return <AdminLoadGate phase={screen.phase} onRetry={screen.retry}>{null}</AdminLoadGate>;
  }

  return (
    <div className="page">
      <h1>{t('admin.compliance.title')}</h1>
      <p className="muted">{t('admin.compliance.lede', { count: total })}</p>
      <table className="data-table">
        <thead>
          <tr>
            <th>{t('admin.compliance.colDate')}</th>
            <th>{t('admin.compliance.colAction')}</th>
            <th>{t('admin.compliance.colDetail')}</th>
          </tr>
        </thead>
        <tbody>
          {items.map((row) => (
            <tr key={row.id}>
              <td>{formatDateTime(row.occurredAt)}</td>
              <td>{row.action}</td>
              <td>{row.details ?? t('common.dash')}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
