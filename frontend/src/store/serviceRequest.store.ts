import { create } from 'zustand'
import type { ServiceRequestStatus } from '../enums/ServiceRequestStatus'
import type { ServiceRequest, ServiceRequestFilters } from '../interfaces/ServiceRequest'
import { createServiceRequest, getServiceRequests } from '../services/serviceRequest.service'
import { getOrCreateCustomerId } from '../utils/customer'
import { getErrorMessage } from '../utils/errors'

interface ServiceRequestState {
  customerId: string
  items: ServiceRequest[]
  isLoading: boolean
  error: string | null

  filters: ServiceRequestFilters

  // Se genera en el primer intento de envío y se conserva entre reintentos
  // hasta que el backend confirme la creación (clave de idempotencia).
  externalReference: string | null
  isCreating: boolean
  createError: string | null

  fetchServiceRequests: (signal?: AbortSignal) => Promise<void>
  submitServiceRequest: (description: string) => Promise<boolean>
  setStatusFilter: (status: ServiceRequestStatus | null) => void
  setSearchFilter: (search: string) => void
  clearCreateError: () => void
}

export const useServiceRequestStore = create<ServiceRequestState>((set, get) => ({
  customerId: getOrCreateCustomerId(),
  items: [],
  isLoading: false,
  error: null,

  filters: { status: null, search: '' },

  externalReference: null,
  isCreating: false,
  createError: null,

  fetchServiceRequests: async (signal) => {
    set({ isLoading: true, error: null })
    try {
      const items = await getServiceRequests(get().filters.status, signal)
      set({ items, isLoading: false })
    } catch (error) {
      if (signal?.aborted) return
      set({ error: getErrorMessage(error), isLoading: false })
    }
  },

  submitServiceRequest: async (description) => {
    const externalReference = get().externalReference ?? crypto.randomUUID()
    set({ externalReference, isCreating: true, createError: null })

    try {
      await createServiceRequest({
        externalReference,
        customerId: get().customerId,
        description,
      })
      set({ externalReference: null, isCreating: false })
      await get().fetchServiceRequests()
      return true
    } catch (error) {
      set({ createError: getErrorMessage(error), isCreating: false })
      return false
    }
  },

  setStatusFilter: (status) => set((state) => ({ filters: { ...state.filters, status } })),
  setSearchFilter: (search) => set((state) => ({ filters: { ...state.filters, search } })),
  clearCreateError: () => set({ createError: null }),
}))

// El backend solo filtra por estado; la búsqueda de texto se aplica en cliente.
export const selectFilteredItems = (state: ServiceRequestState): ServiceRequest[] => {
  const search = state.filters.search.trim().toLowerCase()
  if (!search) return state.items

  return state.items.filter((item) =>
    [item.externalReference, item.customerId, String(item.serviceCode), item.description].some((value) =>
      value.toLowerCase().includes(search),
    ),
  )
}
