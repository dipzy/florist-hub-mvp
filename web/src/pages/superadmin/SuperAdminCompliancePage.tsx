import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Shield } from 'lucide-react';
import { api } from '../../api/client';
import type { ComplianceAuditEntry } from '../../api/types';
import { EmptyState } from '../../components/ui/EmptyState';
import { Table } from '../../components/ui/Table';
import { formatDateTime } from '../../lib/locale';

export function SuperAdminCompliancePage() {
  const { t } = useTranslation();
  const [items, setItems] = useState<ComplianceAuditEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api
      .complianceAuditPlatform()
      .then((res) => {
        setItems(res.items);
        setTotal(res.total);
        setLoaded(true);
      })
      .catch(() => setLoaded(true));
  }, []);

  return (
    <div className="page">
      <header className="page-header">
        <h1>{t('superadmin.compliance.title')}</h1>
        <p className="lede muted">{t('superadmin.compliance.lede', { count: total })}</p>
      </header>
      {loaded && items.length === 0 ? (
        <EmptyState
          title={t('superadmin.compliance.emptyTitle')}
          description={t('superadmin.compliance.emptyDesc')}
          icon={<Shield size={40} />}
        />
      ) : items.length > 0 ? (
        <Table>
          <thead>
            <tr>
              <th>{t('superadmin.compliance.colDate')}</th>
              <th>{t('superadmin.compliance.colStore')}</th>
              <th>{t('superadmin.compliance.colAction')}</th>
              <th>{t('superadmin.compliance.colDetail')}</th>
            </tr>
          </thead>
          <tbody>
            {items.map((row) => (
              <tr key={row.id}>
                <td>{formatDateTime(row.occurredAt)}</td>
                <td>{row.tenantName?.trim() || row.tenantId?.slice(0, 8) || t('common.dash')}</td>
                <td>{row.action}</td>
                <td>{row.details ?? t('common.dash')}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      ) : null}
    </div>
  );
}
