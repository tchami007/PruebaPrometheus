# Logging con Serilog y Loki - Guía completa

## 📚 Tabla de contenidos

1. [Introducción](#introducción)
2. [Arquitectura de logging](#arquitectura-de-logging)
3. [Configuración de Serilog](#configuración-de-serilog)
4. [Mejores prácticas](#mejores-prácticas)
5. [Ejemplos de implementación](#ejemplos-de-implementación)
6. [Queries en Loki](#queries-en-loki)
7. [Troubleshooting](#troubleshooting)

---

## Introducción

Este documento describe cómo configurar **Serilog** como logger centralizado que envía logs a **Grafana Loki**, facilitando:
- **Búsqueda y filtrado** de logs por propiedades
- **Trazabilidad** de transacciones completas (CorrelationId)
- **Auditoría** con información de quién, qué, cuándo y cómo
- **Análisis de performance** con duraciones y timestamps

### Stack utilizado

| Componente | Versión | Propósito |
|---|---|---|
| Serilog | 4.3.0 | Librería de logging estructurado |
| Serilog.AspNetCore | 10.0.0 | Integración con ASP.NET Core |
| Serilog.Sinks.Grafana.Loki | 8.3.2 | Envío de logs a Loki |
| Grafana Loki | 3.0.0 | Almacenamiento centralizado de logs |
| Grafana | 10.0.0 | Visualización de logs y métricas |

---

## Arquitectura de logging

```
┌─────────────────────────────────────────┐
│   Aplicación ASP.NET Core               │
│  (Controladores, Servicios, Middleware) │
└────────────────┬────────────────────────┘
                 │
                 ▼
        ┌────────────────┐
        │    Serilog     │
        │  (Structured   │
        │   Logging)     │
        └────┬───────┬──┘
             │       │
             ▼       ▼
        ┌──────┐  ┌─────────────────┐
        │      │  │                 │
      Console   HTTP (Push)          
                 │
                 ▼
        ┌──────────────────┐
        │  Grafana Loki    │
        │  (Log Aggregator)│
        └────────┬─────────┘
                 │
                 ▼
        ┌──────────────────┐
        │  MinIO / Storage │
        │  (Persistence)   │
        └──────────────────┘
                 │
                 ▼
        ┌──────────────────┐
        │  Grafana Dashboards
        │  (Visualization) │
        └──────────────────┘
```

---

## Configuración de Serilog

### Instalación de paquetes

```powershell
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Settings.Configuration
dotnet add package Serilog.Sinks.Grafana.Loki
dotnet add package Serilog.Formatting.Compact
```

### Configuración en Program.cs

```csharp
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;

// ⭐ CONFIGURAR ANTES de CreateBuilder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.GrafanaLoki(
        uri: "http://loki:3100",
        labels: new[]
        {
            new LokiLabel { Key = "job", Value = "prueba-prometheus" },
            new LokiLabel { Key = "env", Value = "development" },
            new LokiLabel { Key = "service", Value = "api" }
        }
    )
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ⭐ USAR SERILOG como logger del framework
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // ⭐ MIDDLEWARE IMPORTANTE: Registra requests automáticamente
    app.UseSerilogRequestLogging();

    app.UseHttpMetrics();
    app.UseAuthorization();
    app.MapControllers();
    app.MapMetrics();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
```

### Configuración en docker-compose.yml

```yaml
loki:
  image: grafana/loki:3.0.0
  container_name: loki
  ports:
    - "3100:3100"
  networks:
    - monitoring
```

---

## Mejores prácticas

### 1. Usar propiedades estructuradas con LogContext

**❌ EVITAR:**
```csharp
_logger.LogInformation($"Operación iniciada para cuenta {accountNumber}");
```

**✅ HACER:**
```csharp
using (LogContext.PushProperty("AccountNumber", accountNumber))
using (LogContext.PushProperty("OperationId", operationId))
using (LogContext.PushProperty("Amount", amount))
{
    _logger.LogInformation("OPERATION_START");
    // ... código
    _logger.LogInformation("OPERATION_SUCCESS | duration_ms={Duration}", stopwatch.ElapsedMilliseconds);
}
```

**Ventaja:** Las propiedades se propagan automáticamente a TODOS los logs dentro del bloque, sin necesidad de pasarlas como parámetros.

---

### 2. Eventos y estados claros

Usa prefijos consistentes para eventos:

```csharp
_logger.LogInformation("OPERATION_START");        // Iniciando
_logger.LogInformation("OPERATION_SUCCESS");      // Exitoso
_logger.LogWarning("OPERATION_RETRY");            // Reintentando
_logger.LogError("OPERATION_FAILED");             // Fallo
```

---

### 3. Niveles de log apropiados

| Nivel | Cuándo | Ejemplo |
|-------|--------|---------|
| **Information** | Flujo normal | Operación iniciada/completada |
| **Warning** | Situaciones anómalas | Reintento, saldo bajo |
| **Error** | Errores que interrumpen | Excepción, fallo de operación |
| **Debug** | Solo en desarrollo | Variables internas, pasos intermedios |

---

### 4. Campos obligatorios en cada log

Estas propiedades deben estar presentes:

```
CorrelationId    → Rastrear toda la cadena de una transacción
AccountNumber    → Auditoría: quién ejecutó
Amount           → Auditoría: cuánto se movió
OperationType    → Auditoría: qué se hizo
Status           → started | success | failed
Duration         → Performance: cuánto tardó
ErrorType        → Tipo de excepción (si aplica)
```

---

### 5. CorrelationId para trazabilidad

Implementar middleware que capture/genere un ID único por request:

```csharp
// En Program.cs - después de var app = builder.Build();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.FirstOrDefault("X-Correlation-Id").Value 
        ?? Guid.NewGuid().ToString();
    
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        context.Response.Headers.Add("X-Correlation-Id", correlationId);
        await next();
    }
});
```

**Uso en requests:**
```bash
curl -H "X-Correlation-Id: txn-12345" http://localhost:5000/example/process
```

---

### 6. No loguear datos sensibles

**❌ NUNCA:**
```csharp
_logger.LogInformation("Password: {Password}", userPassword);
_logger.LogInformation("Card: {Card}", creditCard);
_logger.LogInformation("Token: {Token}", apiToken);
```

**✅ SEGURO:**
```csharp
_logger.LogInformation("Authentication attempt for user {UserId}");
_logger.LogInformation("Payment processed for card ending in {CardLast4}", card[-4..]);
```

---

## Ejemplos de implementación

### Ejemplo 1: Servicio simple con logging estructurado

```csharp
public class ExampleService
{
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(ILogger<ExampleService> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> ProcessOperationAsync(string operationType)
    {
        var accountNumber = new Random().Next(100000, 999999);
        var amount = new Random().Next(100, 5001);
        var operationId = Guid.NewGuid().ToString("N")[..8];

        // Enriquecer contexto para TODOS los logs siguientes
        using (LogContext.PushProperty("OperationId", operationId))
        using (LogContext.PushProperty("AccountNumber", accountNumber))
        using (LogContext.PushProperty("Amount", amount))
        using (LogContext.PushProperty("OperationType", operationType))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("OPERATION_START");

                // Simular procesamiento
                await Task.Delay(operationType == "fast" ? 100 : 500);

                _logger.LogInformation(
                    "OPERATION_SUCCESS | duration_ms={Duration}",
                    stopwatch.ElapsedMilliseconds);

                return new ProcessResult
                {
                    Success = true,
                    Message = "Operación completada",
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OPERATION_FAILED | duration_ms={Duration} error_type={ErrorType}",
                    stopwatch.ElapsedMilliseconds, ex.GetType().Name);

                return new ProcessResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}
```

### Ejemplo 2: Controlador con logging de entrada y salida

```csharp
[ApiController]
[Route("[controller]")]
public class ExampleController : ControllerBase
{
    private readonly ExampleService _service;
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(ExampleService service, ILogger<ExampleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<ActionResult<ProcessResult>> ProcessOperation(
        [FromBody] ProcessRequest request)
    {
        using (LogContext.PushProperty("Endpoint", "ProcessOperation"))
        using (LogContext.PushProperty("OperationType", request.OperationType))
        {
            _logger.LogInformation("REQUEST_RECEIVED");

            try
            {
                var result = await _service.ProcessOperationAsync(request.OperationType);

                if (result.Success)
                {
                    _logger.LogInformation("REQUEST_SUCCESS");
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("REQUEST_PARTIAL_FAILURE | message={Message}", result.Message);
                    return StatusCode(500, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REQUEST_FAILED");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
```

---

## Queries en Loki

### Consultas básicas

**Buscar todas las operaciones para una cuenta:**
```promql
{job="prueba-prometheus"} | json | AccountNumber="123456"
```

**Buscar operaciones fallidas:**
```promql
{job="prueba-prometheus"} | json | Status="failed"
```

**Buscar por tipo de error:**
```promql
{job="prueba-prometheus"} | json | ErrorType="InvalidOperationException"
```

---

### Análisis de performance

**Latencia promedio por tipo de operación:**
```promql
{job="prueba-prometheus"} 
| json 
| stats avg(Duration) by OperationType
```

**P95 de latencia (95 percentil):**
```promql
{job="prueba-prometheus"} 
| json 
| stats quantile(0.95, Duration) as p95
```

**Operaciones más lentas:**
```promql
{job="prueba-prometheus"} 
| json 
| sort Duration desc 
| limit 10
```

---

### Auditoría y trazabilidad

**Todas las operaciones de un usuario:**
```promql
{job="prueba-prometheus"} 
| json 
| AccountNumber="123456" 
| order by Timestamp desc
```

**Rastrear una transacción específica:**
```promql
{job="prueba-prometheus"} 
| json 
| CorrelationId="txn-abc123"
```

**Contar operaciones por resultado:**
```promql
{job="prueba-prometheus"} 
| json 
| stats count by Status
```

---

### Alertas (ejemplos)

**Alert: Tasa de errores mayor a 5%:**
```promql
(count({job="prueba-prometheus"} | json | Status="failed") / 
 count({job="prueba-prometheus"})) > 0.05
```

**Alert: Latencia P95 > 2 segundos:**
```promql
quantile(0.95, {job="prueba-prometheus"} | json | Duration) > 2000
```

---

## Configuración avanzada

### Enriquecimiento global en Program.cs

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "PruebaPrometheus")
    .Enrich.WithProperty("Version", "1.0.0")
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        uri: "http://loki:3100",
        labels: new[]
        {
            new LokiLabel { Key = "job", Value = "prueba-prometheus" },
            new LokiLabel { Key = "env", Value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development" },
            new LokiLabel { Key = "service", Value = "api" }
        }
    )
    .CreateLogger();
```

### Configuración por ambiente (appsettings.json)

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "GrafanaLoki",
        "Args": {
          "uri": "http://loki:3100",
          "labels": [
            { "key": "job", "value": "prueba-prometheus" },
            { "key": "env", "value": "development" }
          ]
        }
      }
    ]
  }
}
```

---

## Troubleshooting

### Los logs no llegan a Loki

1. **Verificar conectividad a Loki:**
   ```bash
   docker compose exec loki curl localhost:3100/ready
   ```

2. **Verificar que el servicio Loki está corriendo:**
   ```bash
   docker compose ps
   ```

3. **Ver logs de la aplicación:**
   ```bash
   docker compose logs app
   ```

4. **Incrementar nivel de logging de Serilog a Debug (temporalmente):**
   ```csharp
   .MinimumLevel.Debug()
   ```

---

### Logs aparecen en consola pero no en Grafana

1. Los logs pueden tener un delay de 1-5 segundos
2. Verificar que el filtro en Grafana incluya el job: `{job="prueba-prometheus"}`
3. Revisar que el label `env` coincida con el valor configurado

---

### Loki crece demasiado (almacenamiento)

1. Ajustar retention en loki/config.yaml:
   ```yaml
   limits_config:
     retention_period: 7d  # Mantener 7 días
   ```

2. Reducir MinimumLevel a Warning (solo errores importantes):
   ```csharp
   .MinimumLevel.Warning()
   ```

3. Usar filtros en el sink:
   ```csharp
   .WriteTo.GrafanaLoki(
       uri: "http://loki:3100",
       minLevel: LogEventLevel.Warning  // Solo Warning y superior
   )
   ```

---

## Checklist de implementación

- ✅ Serilog configurado en Program.cs
- ✅ UseSerilog() en builder.Host
- ✅ UseSerilogRequestLogging() en pipeline
- ✅ LogContext.PushProperty() para propiedades estructuradas
- ✅ Propiedades obligatorias: CorrelationId, AccountNumber, Amount, Status
- ✅ Eventos con prefijos claros: OPERATION_START, OPERATION_SUCCESS, OPERATION_FAILED
- ✅ Niveles de log correctos: Information, Warning, Error
- ✅ Sin datos sensibles en logs
- ✅ Try-catch-finally con manejo de excepciones
- ✅ Log.CloseAndFlush() en finally

---

## Referencias

- [Serilog Documentation](https://serilog.net/)
- [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore)
- [Grafana Loki](https://grafana.com/oss/loki/)
- [LogQL - Loki Query Language](https://grafana.com/docs/loki/latest/logql/)

---

**Última actualización:** 15 de enero de 2026  
**Versión:** 1.0.0
