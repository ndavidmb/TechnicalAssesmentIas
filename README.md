# Portal de solicitudes de servicio

API ASP.NET Core + Worker Service (.NET 10) + frontend React, sobre PostgreSQL. Las solicitudes se registran
como `Pending`; un proceso en segundo plano las envía a un proveedor externo simulado y actualiza su estado.

## Estructura (DDD por capas)

```
NetInterview/
  Domain/          Entidad ServiceRequest y sus reglas de transición, excepciones de dominio, interfaces
                   (repositorio, unidad de trabajo, proveedor externo)
  Application/     Casos de uso: ServiceRequestService (crear/consultar) y ProcessPendingServiceRequests (worker)
  Infraestructure/ EF Core + Npgsql, migraciones, repositorio, proveedor simulado
  WebApi/          Controlador /api/service-requests, DTOs, traducción de errores a ProblemDetails
  Worker/          BackgroundService que solo orquesta: scope → caso de uso → log → espera
  Tests/           xUnit sobre dominio y casos de uso, con dobles deterministas
frontend/          React + Vite (crear, listar, filtrar por estado, polling)
```

## Cómo iniciar el proyecto

### Requisitos

| Herramienta | Versión | Para qué |
|---|---|---|
| .NET SDK | 10 | API, worker y pruebas |
| Herramienta `dotnet-ef` | 10+ | Aplicar migraciones manualmente (opcional): `dotnet tool install --global dotnet-ef` |
| Node.js | 20+ | Frontend |
| PostgreSQL | 15+ | Persistencia (en local, con Docker) |

### Backend

Todos los comandos se ejecutan desde la carpeta `NetInterview/`.

1. **Levantar PostgreSQL** (si no tienes uno disponible):
   ```bash
   docker run -d --name postgres-container \
     -e POSTGRES_USER=admin -e POSTGRES_PASSWORD=<clave> -e POSTGRES_DB=ias \
     -p 5432:5432 postgres:17
   ```

2. **Configurar la cadena de conexión.** No está en el repositorio: en desarrollo se guarda con user-secrets, una vez para la API y otra para el worker:
   ```bash
   cd NetInterview
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=ias;Username=admin;Password=<clave>" --project WebApi
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=ias;Username=admin;Password=<clave>" --project Worker
   ```
   Fuera de desarrollo se usa la variable de entorno `ConnectionStrings__Default`. Si falta, la API y el worker se detienen al arrancar con un mensaje que lo indica.

3. **Crear la base de datos.** La API aplica las migraciones pendientes al arrancar en `Development`, así que basta con el paso 4. Para aplicarlas a mano:
   ```bash
   dotnet ef database update --project Infraestructure --startup-project WebApi
   ```

4. **Iniciar la API** (terminal 1):
   ```bash
   dotnet run --project WebApi --launch-profile http
   ```
   Queda en `http://localhost:5062`. Para probarla: `curl http://localhost:5062/api/service-requests`, o los ejemplos de `WebApi/WebApi.http`. La especificación OpenAPI está en `http://localhost:5062/openapi/v1.json`.

5. **Iniciar el worker** (terminal 2):
   ```bash
   dotnet run --project Worker
   ```
   Revisa las solicitudes `Pending` cada 5 segundos. Para ejecutarlo manualmente una sola vez (un lote y termina):
   ```bash
   dotnet run --project Worker -- --Worker:RunOnce=true
   ```
   El intervalo, los intentos, el lease y la tasa de fallo del proveedor simulado se configuran en `Worker/appsettings.json`.

6. **Ejecutar las pruebas:**
   ```bash
   dotnet test Tests
   ```

### Frontend

Todos los comandos se ejecutan desde la carpeta `frontend/`, con la API ya iniciada.

1. **Instalar dependencias:**
   ```bash
   cd frontend
   npm install
   ```

2. **Iniciar en modo desarrollo** (terminal 3):
   ```bash
   npm run dev
   ```
   Abre `http://localhost:5173`. Vite redirige las llamadas a `/api` hacia `http://localhost:5062`, así que no hace falta configurar CORS ni variables de entorno.

   Si la API corre en otra URL, crea `frontend/.env` a partir de `.env.example` y define `VITE_API_URL` (por ejemplo `VITE_API_URL=http://localhost:5062`). En ese caso la API debe permitir CORS para el origen del frontend.

3. **Otros comandos:**
   - `npm run build`: compilación de producción en `frontend/dist`.
   - `npm run lint`: análisis estático con oxlint.

### Recorrido rápido

1. Con la API, el worker y el frontend iniciados, crea una solicitud desde **Nueva solicitud**. Aparece en estado `Pending`.
2. La lista se actualiza sola cada 5 segundos: en pocos segundos la solicitud pasa a `Completed`, o vuelve a `Pending` si el proveedor simulado falló y aún quedan intentos.
3. Usa el filtro de estado para consultar solo las solicitudes de un estado.

### Problemas frecuentes

- **El puerto 5062 está ocupado.** Puede haber otra instancia de la API corriendo: `lsof -i :5062` en macOS/Linux o `netstat -ano | findstr 5062` en Windows. Si se queda abierta una versión anterior, las peticiones llegan a ese código y no al nuevo.
- **"Falta ConnectionStrings:Default".** No se configuró el paso 2 para ese proyecto (API o worker).
- **Error al aplicar migraciones por índice único.** Hay filas antiguas con `externalReference` repetido, creadas antes de esa regla. Hay que eliminar los duplicados antes de migrar.

## Contrato de la API

| Método | Ruta | Respuestas |
|---|---|---|
| `POST` | `/api/service-requests` | `201` creada · `200` reintento con mismo `externalReference` y mismos datos (devuelve la existente) · `400` validación · `409` mismo `externalReference` con datos distintos |
| `GET` | `/api/service-requests?status=Pending` | `200` lista (más recientes primero); `status` acepta nombre o número; `400` si no es válido |
| `GET` | `/api/service-requests/{id}` | `200` · `404` |

Errores en formato ProblemDetails. Los 500 no exponen detalles internos (se registran en el log).
Estados en JSON como número: `1 Pending`, `2 Processing`, `3 Completed`, `4 Failed`.

## Reglas de dominio implementadas

- Transiciones válidas: `Pending → Processing → Completed`, `Processing → Pending` (fallo transitorio con intentos disponibles),
  `Processing → Failed` (intentos agotados). Cualquier otra lanza `InvalidStatusTransitionException`.
- `Completed` es final: ninguna operación la saca de ese estado. `Failed` también es final (no hay reintento manual).
- Validación de entrada en la entidad: `externalReference` y `customerId` GUID no vacíos, `description` requerida (≤ 500 caracteres, se recorta).
- Cada paso a `Processing` cuenta un intento; con `MaxAttempts = 3` (configurable) la solicitud queda en `Failed` tras el tercer fallo.

## Registro de decisiones

### 1. Idempotencia de la creación (frontend → API)
- **Contexto**: un doble clic o un reintento tras timeout no debe crear dos solicitudes.
- **Alternativas**: generar `externalReference` en el backend; deduplicar solo en código; índice único + comparación de payload.
- **Decisión**: el cliente genera `externalReference` (UUID) y lo reutiliza en reintentos. Índice único en BD. Mismo payload → `200` con la existente; payload distinto → `409`. Si dos POST concurrentes compiten, el perdedor captura la violación de unicidad y resuelve igual.
- **Trade-offs**: el cliente asume la responsabilidad de generar la clave; `200` vs `201` obliga al cliente a aceptar ambos.
- **Riesgos**: un cliente que genere una clave nueva por reintento rompe la idempotencia (no es detectable en el servidor).
- **Validación**: pruebas `CreateServiceRequestTests`; verificado con 5 POST en paralelo contra Postgres → 1×201 + 4×200, un solo registro.

### 2. Idempotencia hacia el proveedor (worker → proveedor)
- **Contexto**: el worker puede caerse entre "el proveedor respondió OK" y "guardé `Completed`", o recibir timeout cuando el proveedor sí procesó.
- **Decisión**: se envía `externalReference` como clave de idempotencia. Las llamadas son "al menos una vez"; el efecto es "exactamente una vez". Timeouts/errores de red se tratan como fallo transitorio.
- **Riesgos**: depende de que el proveedor real acepte una clave de idempotencia (supuesto).
- **Validación**: pruebas `Timeout_after_provider_applied_the_effect…` y `Request_abandoned_in_processing…`.

### 3. Concurrencia entre instancias del worker
- **Alternativas**: `UPDATE … WHERE Status='Pending'` contando filas; `SELECT … FOR UPDATE SKIP LOCKED`; concurrencia optimista.
- **Decisión**: concurrencia optimista con la columna de sistema `xmin` de Postgres. La transición la hace la entidad (`StartProcessing`) y el `UPDATE` incluye `WHERE xmin = @original`; si otra instancia ganó, se omite sin llamar al proveedor. Mantiene las reglas en el dominio en vez de en SQL.
- **Trade-offs**: con muchas instancias habrá conflictos desperdiciados (cada una lee el mismo lote); `SKIP LOCKED` escalaría mejor.
- **Validación**: prueba `Request_claimed_by_another_worker…`; verificado con dos workers simultáneos contra Postgres (cada solicitud tomada por uno solo).

### 4. Solicitudes atascadas en `Processing`
- **Decisión**: lease. Al inicio de cada lote, las solicitudes con `ProcessingStartedAt` más antiguo que `LeaseTimeout` (5 min) se liberan contando como intento fallido (vuelven a `Pending` o pasan a `Failed`). Reintentarlas es seguro por la decisión 2.
- **Riesgo**: si una llamada al proveedor tarda más que el lease, otra instancia podría reenviarla (sin doble efecto, pero con llamada extra). El lease debe ser mayor que el timeout del cliente HTTP real.

### 5. Worker separado de la API
- El `BackgroundService` vive en su propio proyecto para no depender del ciclo de vida de IIS/app pool, y escalar o detener el procesamiento sin tocar la API. No contiene lógica de negocio.

### 6. Identidad del cliente sin login (frontend)
- **Contexto**: el enunciado no exige autenticación, pero cada solicitud necesita un `customerId`.
- **Alternativas**: implementar login; pedir el `customerId` en el formulario; generar un identificador por navegador.
- **Decisión**: al iniciar la sesión, el frontend genera un GUID (`crypto.randomUUID()`), lo guarda en `localStorage` y lo reutiliza como `customerId` en todas las solicitudes de ese navegador. La tabla resalta las solicitudes propias.
- **Trade-offs**: evita todo el flujo de autenticación y deja el contrato de la API listo para que, más adelante, el `customerId` salga de la identidad del usuario autenticado.
- **Riesgos**: no es un mecanismo de seguridad. Cualquiera puede enviar otro `customerId`, y borrar el `localStorage` o cambiar de navegador crea un "cliente" nuevo.

### 7. Persistencia: PostgreSQL + EF Core
- Migraciones versionadas como forma reproducible de inicialización.
- **Si se hubiera elegido NoSQL (p. ej. MongoDB)**: la unicidad de `externalReference` se garantizaría con un índice único en la colección y la toma atómica con `findOneAndUpdate({_id, status:'Pending'}, {$set:{status:'Processing'}})`, en lugar del token `xmin`. Se perdería el esquema/migraciones (la inicialización sería un script de índices) y las consultas por estado necesitarían su propio índice. El dominio y Application no cambiarían: solo el repositorio y la unidad de trabajo.

## Ejecutar el worker como Windows Service

El worker ya llama a `AddWindowsService` (sin efecto al correr como consola).

```powershell
dotnet publish NetInterview/Worker -c Release -o C:\Services\ServiceRequestsWorker
sc.exe create NetInterview.ServiceRequestsWorker binPath= "C:\Services\ServiceRequestsWorker\Worker.exe" start= auto
sc.exe config NetInterview.ServiceRequestsWorker obj= "NT SERVICE\NetInterview.ServiceRequestsWorker"
setx /M ConnectionStrings__Default "Host=...;Password=..."   # o un almacén de secretos; nunca en appsettings
sc.exe start NetInterview.ServiceRequestsWorker
```

- Cuenta de servicio virtual con permisos mínimos (solo lectura de la carpeta y acceso a la BD).
- Como servicio, los logs van al Event Log. `sc.exe failure` para reinicio automático.
- Linux: `AddSystemd()` + unidad systemd.

## Supuestos

- `externalReference` es un GUID generado por el cliente. Se valida como GUID no vacío.
- Un reintento "con el mismo payload" significa mismo `customerId` y misma `description` (recortada).
- `serviceCode` lo asigna la base de datos (secuencia) y no lo envía el cliente. *Si el enunciado espera que el cliente elija un código de servicio, hay que añadirlo al comando y quitar la generación automática.*
- `customerId` lo genera el frontend una vez por navegador (no hay autenticación).
- Política de reintentos: 3 intentos totales, sin backoff exponencial (el intervalo del worker, 5 s, actúa como espera). `Failed` es final.
- El proveedor simulado guarda las claves procesadas en memoria: si el worker se reinicia, "olvida" lo procesado. Es aceptable para la simulación; el proveedor real mantiene su propio registro.
- El proveedor simulado falla de forma transitoria con probabilidad configurable (`SimulatedProvider:TransientFailureRate`, 0.3). Las pruebas no usan aleatoriedad.

## Pruebas

`dotnet test NetInterview/Tests` — 17 pruebas, sin red ni base de datos:

| Escenario | Prueba |
|---|---|
| Dos POST con el mismo `externalReference` → un solo registro | `Retrying_with_same_external_reference_and_payload_creates_a_single_record` |
| Mismo `externalReference`, datos distintos → conflicto | `Same_external_reference_with_different_payload_is_a_conflict` |
| Carrera resuelta por el índice único | `Concurrent_insert_rejected_by_unique_index_returns_the_existing_request` |
| `Completed` no retrocede | `Completed_never_returns_to_a_previous_status`, `Completed_requests_are_never_reprocessed` |
| Fallo transitorio → reintento → `Completed` con un solo efecto | `Transient_failure_is_retried_on_next_run_and_completes_with_a_single_effect` |
| Intentos agotados → `Failed` y no se reenvía | `Request_fails_after_max_attempts_and_is_not_sent_again` |
| Timeout cuando el proveedor sí procesó → sin efecto duplicado | `Timeout_after_provider_applied_the_effect_does_not_duplicate_it_on_retry` |
| Caída del worker en `Processing` → recuperación por lease sin duplicar | `Request_abandoned_in_processing_is_recovered_after_lease_without_duplicating_the_effect` |
| Dos workers compiten por la misma solicitud | `Request_claimed_by_another_worker_is_skipped_without_calling_the_provider` |
| Validaciones de entrada | `Create_rejects_invalid_input`, `Create_rejects_description_longer_than_max` |

No cubiertas por pruebas automáticas (verificadas manualmente contra Postgres): el índice único real y el token `xmin`. Siguiente paso: pruebas de integración con Testcontainers.

## Nota de uso de IA

Quiero ser transparente sobre cómo repartí el trabajo entre lo que hice yo y lo que hizo la IA (Claude Code).

**Lo que hice yo**
- Dediqué la mayor parte del tiempo a diagramar y definir la arquitectura: las capas DDD (Domain, Application, Infraestructure, WebApi), la separación de responsabilidades entre ellas y los lineamientos de diseño (nombres, idempotencia con `externalReference`, worker separado de la API) que dejé escritos en `CLAUDE.md` para guiar el resto del trabajo.
- Monté la solución con esa estructura: entidad, repositorio, unidad de trabajo, `DbContext` y configuración de PostgreSQL, migración inicial, servicio de aplicación y controlador.
- Elegí el stack del frontend: React con Zustand para el estado, Tailwind para los estilos y Zod para validar las respuestas de la API, de modo que un cambio en el contrato del backend se detecte como error explícito en vez de fallar en silencio en la interfaz.
- Decidí que, en el frontend, cada usuario quede atado a un único GUID guardado en `localStorage` al iniciar su sesión, para no tener que implementar login (decisión 6).
- Al frontend le dediqué poco tiempo a propósito: por el tiempo disponible y porque me siento tranquilo con mis conocimientos en esa área, así que preferí invertir el tiempo en el backend.

**Lo que hizo la IA, a partir de mis lineamientos**
- Las reglas de transición de estados en la entidad, el proyecto Worker y el caso de uso `ProcessPendingServiceRequests`.
- La idempotencia en la creación y hacia el proveedor, la concurrencia optimista con `xmin` y la liberación de solicitudes atascadas.
- El proveedor simulado, el manejo de errores con ProblemDetails, la segunda migración, las pruebas automatizadas y la mayor parte de este README.

Revisé ese código y lo verifiqué ejecutando las pruebas y probando la API y el worker contra PostgreSQL.

## Estado de la entrega

Hoy estuve en varios procesos en paralelo y no me alcanzó el tiempo para completar todo lo que pide la prueba. Quedan pendientes:

- **Revisión de código legado** (fragmentos WebForms y MVC del enunciado).
- Backoff exponencial entre reintentos (`NextAttemptAt`) y reintento manual de solicitudes `Failed`.
- Pruebas de integración contra PostgreSQL real (Testcontainers) para el índice único y el token de concurrencia.

Estoy abierto a una entrevista para profundizar en las decisiones de arquitectura, completar lo pendiente y ampliar sobre mis conocimientos.
