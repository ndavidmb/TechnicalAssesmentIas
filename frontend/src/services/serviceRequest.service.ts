import type { ServiceRequestStatus } from '../enums/ServiceRequestStatus'
import type { CreateServiceRequestPayload, ServiceRequest } from '../interfaces/ServiceRequest'
import { serviceRequestListSchema, serviceRequestSchema } from '../schemas/serviceRequest.schema'

const BASE_URL = `${import.meta.env.VITE_API_URL ?? ''}/api/service-requests`

async function request(url: string, init?: RequestInit): Promise<unknown> {
  let response: Response
  try {
    response = await fetch(url, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...init?.headers },
    })
  } catch (error) {
    if (init?.signal?.aborted) throw error
    throw new Error('No fue posible conectar con el servidor')
  }

  if (!response.ok) {
    throw new Error(await getProblemMessage(response))
  }
  return response.json()
}

// El backend responde errores como ProblemDetails (RFC 9457): se muestra su detalle si existe.
async function getProblemMessage(response: Response): Promise<string> {
  const fallback = `Error del servidor (${response.status})`
  try {
    const problem = await response.json()
    const validationErrors = problem?.errors ? Object.values(problem.errors).flat().join(' ') : ''
    return problem?.detail || validationErrors || problem?.title || fallback
  } catch {
    return fallback
  }
}

export async function getServiceRequests(
  status: ServiceRequestStatus | null,
  signal?: AbortSignal,
): Promise<ServiceRequest[]> {
  const url = status ? `${BASE_URL}?status=${status}` : BASE_URL
  const data = await request(url, { signal })
  return serviceRequestListSchema.parse(data)
}

export async function createServiceRequest(payload: CreateServiceRequestPayload): Promise<ServiceRequest> {
  const data = await request(BASE_URL, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
  return serviceRequestSchema.parse(data)
}
