import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import LoadingSkeleton from '../../components/ui/LoadingSkeleton'
import EmptyState from '../../components/ui/EmptyState'
import ErrorState from '../../components/ui/ErrorState'
import { adminApi } from '../../api/client'
import { formatDate } from '../../lib/utils'
import type { AuditLogPage } from '../../types'


// ─── JSON Diff Viewer ─────────────────────────────────────────────────────────

function JsonDiffViewer({ raw }: { raw: string }) {
  let parsed: { before?: Record<string, unknown>; after?: Record<string, unknown> } | null = null
  try { parsed = JSON.parse(raw) } catch { /* raw display */ }

  if (!parsed || (!parsed.before && !parsed.after)) {
    return (
      <pre className="text-xs bg-gray-50 p-3 rounded-lg overflow-x-auto whitespace-pre-wrap break-all">
        {JSON.stringify(JSON.parse(raw), null, 2)}
      </pre>
    )
  }

  const allKeys = [...new Set([
    ...Object.keys(parsed.before ?? {}),
    ...Object.keys(parsed.after ?? {})
  ])]

  return (
    <div className="text-xs font-mono bg-gray-50 rounded-lg overflow-hidden border border-gray-200">
      <table className="w-full">
        <thead>
          <tr className="bg-gray-100 text-gray-500">
            <th className="px-3 py-1.5 text-left font-medium w-1/4">Alan</th>
            <th className="px-3 py-1.5 text-left font-medium w-5/12 text-red-500">Önce</th>
            <th className="px-3 py-1.5 text-left font-medium w-5/12 text-green-600">Sonra</th>
          </tr>
        </thead>
        <tbody>
          {allKeys.map(k => {
            const before = JSON.stringify(parsed!.before?.[k])
            const after = JSON.stringify(parsed!.after?.[k])
            const changed = before !== after
            return (
              <tr key={k} className={changed ? 'bg-yellow-50' : ''}>
                <td className="px-3 py-1 text-gray-600 font-semibold truncate">{k}</td>
                <td className={`px-3 py-1 ${changed ? 'text-red-600 line-through' : 'text-gray-500'} break-all`}>
                  {before ?? '—'}
                </td>
                <td className={`px-3 py-1 ${changed ? 'text-green-700 font-semibold' : 'text-gray-500'} break-all`}>
                  {after ?? '—'}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function AdminAuditLogs() {
  const [page, setPage] = useState(1)
  const [entityType, setEntityType] = useState('')
  const [action, setAction] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const { data, isLoading, isError, refetch } = useQuery<AuditLogPage>({
    queryKey: ['audit-logs', page, entityType, action, from, to],
    queryFn: () => adminApi.getAuditLogs({
      entityType: entityType || undefined,
      action: action || undefined,
      from: from || undefined,
      to: to || undefined,
      page,
      pageSize: 50,
    }),
  })

  const totalPages = data ? Math.ceil(data.total / data.pageSize) : 1

  const resetFilters = () => {
    setEntityType(''); setAction(''); setFrom(''); setTo(''); setPage(1)
  }

  return (
    <Layout>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-gray-900">Audit Log</h1>
        {data && <span className="text-xs text-gray-500">Toplam {data.total} kayıt</span>}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl border border-gray-200 p-4 mb-4">
        <div className="flex flex-wrap gap-3">
          <div>
            <label className="block text-xs text-gray-500 mb-1">Entity Tipi</label>
            <input value={entityType} onChange={e => { setEntityType(e.target.value); setPage(1) }}
              placeholder="Order, Product…"
              className="px-2 py-1.5 border border-gray-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-blue-500 w-36" />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Aksiyon</label>
            <input value={action} onChange={e => { setAction(e.target.value); setPage(1) }}
              placeholder="OrderConfirmed…"
              className="px-2 py-1.5 border border-gray-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-blue-500 w-40" />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Başlangıç</label>
            <input type="date" value={from} onChange={e => { setFrom(e.target.value); setPage(1) }}
              className="px-2 py-1.5 border border-gray-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Bitiş</label>
            <input type="date" value={to} onChange={e => { setTo(e.target.value); setPage(1) }}
              className="px-2 py-1.5 border border-gray-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          {(entityType || action || from || to) && (
            <div className="flex items-end">
              <button onClick={resetFilters}
                className="px-3 py-1.5 text-xs text-gray-500 border border-gray-300 rounded-lg hover:bg-gray-50">
                Temizle
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Table */}
      {isLoading ? (
        <LoadingSkeleton rows={8} />
      ) : isError ? (
        <ErrorState onRetry={refetch} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState icon="📋" title="Audit kaydı yok" description="Belirtilen kriterlerde kayıt bulunamadı" />
      ) : (
        <>
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            {data.items.map((log, i) => (
              <div key={log.id} className={i < data.items.length - 1 ? 'border-b border-gray-100' : ''}>
                {/* Row */}
                <div
                  className="px-4 py-3 hover:bg-gray-50 cursor-pointer flex items-center gap-4"
                  onClick={() => setExpandedId(expandedId === log.id ? null : log.id)}
                >
                  <span className="text-gray-400 text-xs w-3">{expandedId === log.id ? '▲' : '▼'}</span>
                  <span className="text-xs text-gray-400 whitespace-nowrap w-32 flex-shrink-0">{formatDate(log.createdAt)}</span>
                  <span className="text-xs font-mono text-gray-500 w-20 flex-shrink-0 truncate">{log.userRole}</span>
                  <span className="text-xs font-semibold text-gray-800 w-48 flex-shrink-0 truncate">{log.action}</span>
                  <span className="text-xs text-blue-600 w-20 flex-shrink-0 truncate">{log.entityType}</span>
                  <span className="text-xs font-mono text-gray-400 flex-1 truncate">{log.entityId}</span>
                  {log.ipAddress && (
                    <span className="text-xs text-gray-300 flex-shrink-0">{log.ipAddress}</span>
                  )}
                </div>
                {/* Expanded diff */}
                {expandedId === log.id && log.changes && (
                  <div className="px-12 pb-4">
                    <JsonDiffViewer raw={log.changes} />
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <span className="text-xs text-gray-500">Sayfa {page}/{totalPages}</span>
              <div className="flex gap-2">
                <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}
                  className="px-3 py-1.5 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                  ← Önceki
                </button>
                <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}
                  className="px-3 py-1.5 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                  Sonraki →
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </Layout>
  )
}
