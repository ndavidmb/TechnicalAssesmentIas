// Espejo de Domain.Entities.ServiceRequestStatus (el backend lo serializa como número).
// Se usa un objeto `as const` porque tsconfig tiene `erasableSyntaxOnly` (no permite `enum`).
export const ServiceRequestStatus = {
  Pending: 1,
  Processing: 2,
  Completed: 3,
  Failed: 4,
} as const

export type ServiceRequestStatus = (typeof ServiceRequestStatus)[keyof typeof ServiceRequestStatus]

export const ServiceRequestStatusLabel: Record<ServiceRequestStatus, string> = {
  [ServiceRequestStatus.Pending]: 'Pending',
  [ServiceRequestStatus.Processing]: 'Processing',
  [ServiceRequestStatus.Completed]: 'Completed',
  [ServiceRequestStatus.Failed]: 'Failed',
}
