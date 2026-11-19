import { ServiceRequestStatus, ServiceRequestStatusLabel } from '../enums/ServiceRequestStatus'

const statusStyles: Record<ServiceRequestStatus, string> = {
  [ServiceRequestStatus.Pending]: 'bg-amber-100 text-amber-800',
  [ServiceRequestStatus.Processing]: 'bg-blue-100 text-blue-800',
  [ServiceRequestStatus.Completed]: 'bg-emerald-100 text-emerald-800',
  [ServiceRequestStatus.Failed]: 'bg-red-100 text-red-800',
}

interface StatusBadgeProps {
  status: ServiceRequestStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${statusStyles[status]}`}>
      {ServiceRequestStatusLabel[status]}
    </span>
  )
}
