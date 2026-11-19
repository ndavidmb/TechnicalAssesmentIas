import { useEffect, useState } from 'react'
import { ServiceRequestStatus, ServiceRequestStatusLabel } from '../enums/ServiceRequestStatus'
import { useServiceRequestStore } from '../store/serviceRequest.store'

const SEARCH_DEBOUNCE_MS = 400

export function ServiceRequestFilters() {
  const status = useServiceRequestStore((state) => state.filters.status)
  const setStatusFilter = useServiceRequestStore((state) => state.setStatusFilter)
  const setSearchFilter = useServiceRequestStore((state) => state.setSearchFilter)
  const [search, setSearch] = useState('')

  // Debounce: solo se aplica el filtro cuando el usuario deja de escribir SEARCH_DEBOUNCE_MS
  useEffect(() => {
    const timeout = setTimeout(() => setSearchFilter(search), SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timeout)
  }, [search, setSearchFilter])

  return (
    <div className="flex flex-col gap-3 sm:flex-row">
      <input
        type="search"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        placeholder="Buscar por external reference, customer ID o service code…"
        className="flex-1 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-200"
      />
      <select
        value={status ?? ''}
        onChange={(event) =>
          setStatusFilter(event.target.value ? (Number(event.target.value) as ServiceRequestStatus) : null)
        }
        className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-200"
      >
        <option value="">Todos los estados</option>
        {Object.values(ServiceRequestStatus).map((value) => (
          <option key={value} value={value}>
            {ServiceRequestStatusLabel[value]}
          </option>
        ))}
      </select>
    </div>
  )
}
