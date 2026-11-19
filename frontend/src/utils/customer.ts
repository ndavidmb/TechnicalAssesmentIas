import { StorageKeys } from '../enums/StorageKeys'

// Un cliente = una sesión: el guid se crea una sola vez y se reutiliza.
export function getOrCreateCustomerId(): string {
  const stored = localStorage.getItem(StorageKeys.CustomerId)
  if (stored) return stored

  const customerId = crypto.randomUUID()
  localStorage.setItem(StorageKeys.CustomerId, customerId)
  return customerId
}
