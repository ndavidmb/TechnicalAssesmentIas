import type { ServiceRequest } from '../interfaces/ServiceRequest'
import { StatusBadge } from './StatusBadge'

interface ServiceRequestRowProps {
  serviceRequest: ServiceRequest
  isOwn: boolean
}

export function ServiceRequestRow({ serviceRequest, isOwn }: ServiceRequestRowProps) {
  const { externalReference, customerId, serviceCode, status } = serviceRequest

  return (
    <tr className={`border-b border-slate-100 last:border-0 ${isOwn ? 'bg-indigo-50/50' : ''}`}>
      <td className="px-4 py-3 font-mono text-xs text-slate-700">{externalReference}</td>
      <td className="px-4 py-3 font-mono text-xs text-slate-700">
        {customerId}
        {isOwn && <span className="ml-2 font-sans text-indigo-600">(tú)</span>}
      </td>
      <td className="px-4 py-3 text-slate-700">{serviceCode}</td>
      <td className="px-4 py-3">
        <StatusBadge status={status} />
      </td>
    </tr>
  )
}
