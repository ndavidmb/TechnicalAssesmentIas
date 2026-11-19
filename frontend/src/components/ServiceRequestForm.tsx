import { useState, type FormEvent } from 'react'
import { createServiceRequestFormSchema } from '../schemas/serviceRequest.schema'
import { useServiceRequestStore } from '../store/serviceRequest.store'
import { ErrorMessage } from './ErrorMessage'

interface ServiceRequestFormProps {
  onClose: () => void
}

export function ServiceRequestForm({ onClose }: ServiceRequestFormProps) {
  const submitServiceRequest = useServiceRequestStore((state) => state.submitServiceRequest)
  const isCreating = useServiceRequestStore((state) => state.isCreating)
  const createError = useServiceRequestStore((state) => state.createError)
  const clearCreateError = useServiceRequestStore((state) => state.clearCreateError)

  const [description, setDescription] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)

  const handleClose = () => {
    clearCreateError()
    onClose()
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const result = createServiceRequestFormSchema.safeParse({ description })
    if (!result.success) {
      setValidationError(result.error.issues[0].message)
      return
    }
    setValidationError(null)

    const created = await submitServiceRequest(result.data.description)
    if (created) onClose()
  }

  return (
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-slate-900/40 p-4" onClick={handleClose}>
      <form
        onSubmit={handleSubmit}
        onClick={(event) => event.stopPropagation()}
        noValidate
        className="w-full max-w-md space-y-4 rounded-lg bg-white p-6 shadow-xl"
      >
        <h2 className="text-lg font-semibold text-slate-900">Nueva solicitud</h2>

        <div className="space-y-1">
          <label htmlFor="description" className="block text-sm font-medium text-slate-700">
            Descripción <span className="text-red-500">*</span>
          </label>
          <textarea
            id="description"
            rows={4}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            aria-invalid={validationError !== null}
            autoFocus
            className={`w-full rounded-md border px-3 py-2 text-sm outline-none focus:ring-2 ${
              validationError
                ? 'border-red-400 focus:ring-red-200'
                : 'border-slate-300 focus:border-indigo-500 focus:ring-indigo-200'
            }`}
          />
          {validationError && <p className="text-sm text-red-600">{validationError}</p>}
        </div>

        {createError && <ErrorMessage message={`No se pudo crear la solicitud. ${createError}`} />}

        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={handleClose}
            className="rounded-md px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isCreating}
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-60"
          >
            {isCreating ? 'Enviando…' : 'Enviar'}
          </button>
        </div>
      </form>
    </div>
  )
}
