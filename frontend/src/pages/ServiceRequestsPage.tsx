import { useEffect, useState } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { ErrorMessage } from '../components/ErrorMessage'
import { ServiceRequestFilters } from '../components/ServiceRequestFilters'
import { ServiceRequestForm } from '../components/ServiceRequestForm'
import { ServiceRequestTable } from '../components/ServiceRequestTable'
import { selectFilteredItems, useServiceRequestStore } from '../store/serviceRequest.store'

const POLLING_INTERVAL_MS = 5000

export function ServiceRequestsPage() {
  const customerId = useServiceRequestStore((state) => state.customerId)
  const status = useServiceRequestStore((state) => state.filters.status)
  const isLoading = useServiceRequestStore((state) => state.isLoading)
  const error = useServiceRequestStore((state) => state.error)
  const fetchServiceRequests = useServiceRequestStore((state) => state.fetchServiceRequests)
  const items = useServiceRequestStore(useShallow(selectFilteredItems))

  const [isFormOpen, setIsFormOpen] = useState(false)

  // El estado se filtra en el backend: se vuelve a consultar cuando cambia.
  // Polling para ver el avance del worker (Pending → Processing → Completed/Failed).
  useEffect(() => {
    const controller = new AbortController()
    fetchServiceRequests(controller.signal)
    const interval = setInterval(() => fetchServiceRequests(controller.signal), POLLING_INTERVAL_MS)
    return () => {
      clearInterval(interval)
      controller.abort()
    }
  }, [status, fetchServiceRequests])

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-10">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Solicitudes de servicio</h1>
          <p className="mt-1 text-sm text-slate-500">
            Customer ID: <span className="font-mono">{customerId}</span>
          </p>
        </div>
        <button
          type="button"
          onClick={() => setIsFormOpen(true)}
          className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
        >
          Nueva solicitud
        </button>
      </header>

      <ServiceRequestFilters />

      {error && <ErrorMessage message={error} onRetry={() => fetchServiceRequests()} />}

      <ServiceRequestTable items={items} customerId={customerId} isLoading={isLoading} />

      {isFormOpen && <ServiceRequestForm onClose={() => setIsFormOpen(false)} />}
    </main>
  )
}
