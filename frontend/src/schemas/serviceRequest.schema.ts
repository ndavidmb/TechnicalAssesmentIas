import { z } from 'zod'
import { ServiceRequestStatus } from '../enums/ServiceRequestStatus'

// Respuesta del backend (ServiceRequestDto).
export const serviceRequestSchema = z.object({
  id: z.guid(),
  externalReference: z.guid(),
  customerId: z.guid(),
  serviceCode: z.number().int(),
  description: z.string(),
  status: z.enum(ServiceRequestStatus),
  attemptCount: z.number().int(),
  lastError: z.string().nullable(),
  createdAt: z.string(),
  updatedAt: z.string(),
})

export const serviceRequestListSchema = z.array(serviceRequestSchema)

export const createServiceRequestFormSchema = z.object({
  description: z.string().trim().min(1, 'La descripción es requerida'),
})
