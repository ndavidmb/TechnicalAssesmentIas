import { ZodError } from 'zod'

export function getErrorMessage(error: unknown): string {
  if (error instanceof ZodError) return 'La respuesta del servidor no tiene el formato esperado'
  if (error instanceof Error) return error.message
  return 'Ocurrió un error inesperado'
}
