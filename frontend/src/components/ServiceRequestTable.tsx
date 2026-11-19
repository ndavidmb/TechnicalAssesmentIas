import type { ServiceRequest } from '../interfaces/ServiceRequest'
import { ServiceRequestRow } from './ServiceRequestRow'

interface ServiceRequestTableProps {
  items: ServiceRequest[]
  customerId: string
  isLoading: boolean
}

export function ServiceRequestTable({ items, customerId, isLoading }: ServiceRequestTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
      <table className="w-full text-left text-sm">
        <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th className="px-4 py-3 font-medium">External reference</th>
            <th className="px-4 py-3 font-medium">Customer ID</th>
            <th className="px-4 py-3 font-medium">Service code</th>
            <th className="px-4 py-3 font-medium">Estado</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <ServiceRequestRow key={item.id} serviceRequest={item} isOwn={item.customerId === customerId} />
          ))}
          {items.length === 0 && (
            <tr>
              <td colSpan={4} className="px-4 py-10 text-center text-slate-400">
                {isLoading ? 'Cargando…' : 'No hay solicitudes'}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
