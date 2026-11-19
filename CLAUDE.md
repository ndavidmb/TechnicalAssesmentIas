# Contexto del proyecto: Prueba técnica Full Stack .NET

Portal de solicitudes de servicio con procesamiento en segundo plano contra un proveedor externo simulado.
Tiempo máximo de la prueba: 2 horas. Entrega: repositorio Git con historial de commits que muestre la evolución.

> El enunciado original está en `docs/` (PDF). Si algo de este archivo contradice el enunciado, manda el enunciado.

## Requisitos del enunciado (resumen)

- **API** en C# + ASP.NET Core Web API: registrar una solicitud y consultar solicitudes por estado. Contrato y errores comprensibles.
- **Frontend mínimo** con UNA de estas opciones: Vue.js, React.js o AngularJS. Crear solicitud, listar y filtrar por estado. Mostrar al menos `externalReference`, `customerId`, `serviceCode` y estado.
- **Persistencia real** (SQL o NoSQL) con forma reproducible de inicializarla. Documentar en 3-5 líneas qué cambiaría con la alternativa no elegida.
- **Proceso en segundo plano** que tome solicitudes pendientes, invoque un proveedor simulado y actualice el estado. Debe poder ejecutarse manualmente y quedar documentado cómo llevarlo a Windows Service.
- **Pruebas automatizadas** sobre el comportamiento crítico más riesgoso, con nota de qué escenarios cubren.
- **Documentación**: instrucciones de ejecución, registro de decisiones (contexto, alternativas, decisión, trade-offs, riesgos, validación), supuestos, nota de uso de IA, revisión de código legado (fragmentos WebForms y MVC del enunciado: riesgo concreto + cambio mínimo seguro).
- No se exige autenticación, Docker, nube ni CI/CD. Sí se exige manejo seguro de entradas, errores, configuración y secretos.
- Priorizar una ruta ejecutable. Si algo queda incompleto, documentar estado actual, decisión pendiente y siguiente paso.

### Reglas funcionales

1. Campos: `externalReference`, `customerId`, `serviceCode`, `description`, estado.
2. Estados mínimos: `Pending`, `Processing`, `Completed`, `Failed` (se pueden agregar otros justificándolos).
3. `externalReference` identifica la operación de negocio del cliente. Un reintento de la misma operación NO debe generar dos efectos en el proveedor.
4. El proveedor simulado puede responder éxito o fallo transitorio.
5. Una solicitud `Completed` nunca regresa a un estado anterior.
6. Ante ambigüedades, documentar el supuesto tomado; no inventar reglas no escritas.

## Lineamientos de diseño discutidos

### Nombres
- Entidad: **`ServiceRequest`** (se alinea con la tabla `ServiceRequests` del código legado del enunciado y evita colisiones con `HttpRequest` / `ControllerBase.Request` / `IRequest<T>`).
- Enum: `ServiceRequestStatus`.
- DTOs sin sufijo `Request` para evitar `CreateServiceRequestRequest`: entrada `CreateServiceRequestCommand`, salida `ServiceRequestDto`.
- Controlador: `ServiceRequestsController`, ruta `/api/service-requests`.
- Caso de uso del worker: `ProcessPendingServiceRequests`.

### Estructura (DDD por capas)
```
src/
  <Nombre>.Domain/          Entidad ServiceRequest, estados, reglas de transición (Completed no retrocede)
  <Nombre>.Application/     Casos de uso (CreateServiceRequest, ProcessPendingServiceRequests) e interfaces (repositorio, IProviderClient)
  <Nombre>.Infrastructure/  Persistencia, proveedor simulado (implementa IProviderClient)
  <Nombre>.Api/             ASP.NET Core Web API
  <Nombre>.Worker/          Worker Service (BackgroundService) que invoca el caso de uso de Application
tests/
```
- Application define la interfaz del proveedor; Infrastructure la implementa con un simulado. Cambiar a un proveedor real = nueva implementación + cambio en DI.
- El Worker NO contiene lógica de negocio: solo un bucle que crea un scope (`IServiceScopeFactory`, porque el DbContext es scoped), ejecuta el caso de uso, captura/loguea excepciones y espera un intervalo.

### Idempotencia y `externalReference`
- `externalReference` lo genera el **cliente (frontend)**, NO el backend al crear la entidad. El frontend genera un UUID (`crypto.randomUUID()`) al iniciar el formulario y reutiliza el mismo valor en reintentos/doble clic; genera uno nuevo solo tras una creación exitosa o un formulario nuevo.
- Backend: tratarlo como `string` validado (formato/longitud). Que sea GUID es un supuesto a documentar.
- **Índice único** en `ExternalReference` en la base de datos (la validación en código sola no resiste concurrencia).
- `POST` con `externalReference` existente: mismo payload → devolver la solicitud existente (200) sin crear otra; payload distinto → `409 Conflict`. Documentar como decisión.
- Hay dos puntos de duplicación:
  1. Frontend → API (doble POST): se resuelve con lo anterior.
  2. Worker → proveedor (caída entre "proveedor respondió OK" y "guardé Completed", o timeout cuando el proveedor sí procesó): se resuelve enviando `externalReference` como clave de idempotencia al proveedor. El proveedor simulado guarda las referencias ya procesadas y, ante un repetido, devuelve el mismo resultado sin repetir el efecto.
- Mensaje para la documentación: las llamadas al proveedor son "al menos una vez"; el efecto es "exactamente una vez" gracias a la idempotencia. Supuesto: el proveedor real acepta una clave de idempotencia.

### Concurrencia y estados en el worker
- La transición `Pending → Processing` debe ser atómica para que dos instancias no tomen la misma solicitud (p. ej. `UPDATE ... SET Status='Processing' WHERE Id=@id AND Status='Pending'` verificando filas afectadas, o concurrencia optimista con `rowversion`/token de concurrencia).
- Definir/documentar qué pasa con solicitudes atascadas en `Processing` si el proceso se cae (p. ej. timeout tipo lease). Puede quedar como supuesto o siguiente paso si no se implementa.
- Fallos transitorios: reintentos con límite de intentos (documentar el número y la política).

### Ejecución como Windows Service
- Proyecto Worker separado de la API (no hospedar el BackgroundService dentro de la API: se acopla al ciclo de vida de IIS/app pool).
- Usar `Microsoft.Extensions.Hosting.WindowsServices` + `builder.Services.AddWindowsService(o => o.ServiceName = "...")`. No afecta cuando corre como consola.
- Demo: `dotnet run --project src/<Nombre>.Worker`.
- Producción (documentar): `dotnet publish -c Release -o <ruta>`, `sc.exe create <ServiceName> binPath= "<ruta>\<Nombre>.Worker.exe" start= auto`, `sc.exe start <ServiceName>`; cuenta de servicio con permisos mínimos, connection string fuera del repo (variables de entorno/secretos), logs al Event Log. Equivalente Linux: `AddSystemd()`.

### Frontend
- No necesita ningún worker. Solo crea solicitudes y consulta estado; para ver el avance del worker basta con polling (`setInterval` sobre el GET).

### Pruebas prioritarias
- Idempotencia: dos POST con el mismo `externalReference` → un solo registro.
- Reintento del worker tras fallo → el proveedor no produce efecto duplicado.
- `Completed` no retrocede a otro estado.
- Proveedor que falla de forma transitoria → estado y reintentos correctos.
- Usar un proveedor falso determinista en las pruebas (nada aleatorio ni de red).

## Decisiones aún pendientes
- Versión de .NET.
- Persistencia: motor concreto (SQL o NoSQL) y forma de inicializarla.
- Framework de frontend (Vue / React / AngularJS).
- Nombre de la solución/proyectos.

## Cómo trabajar en este repo
- Commits pequeños y descriptivos que muestren la evolución.
- Toda ambigüedad que afecte una decisión se anota en la documentación de supuestos; no inventar reglas no escritas.
- No incluir secretos en el repositorio.
