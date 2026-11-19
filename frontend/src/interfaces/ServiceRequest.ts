import type { z } from 'zod'
import type { createServiceRequestFormSchema, serviceRequestSchema } from '../schemas/serviceRequest.schema'
import type { ServiceRequestStatus } from '../enums/ServiceRequestStatus'

export type ServiceRequest = z.infer<typeof serviceRequestSchema>

export type CreateServiceRequestForm = z.infer<typeof createServiceRequestFormSchema>

export interface CreateServiceRequestPayload {
  externalReference: string
  customerId: string
  description: string
}

export interface ServiceRequestFilters {
  status: ServiceRequestStatus | null
  search: string
}
