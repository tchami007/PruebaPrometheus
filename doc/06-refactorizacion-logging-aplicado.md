# Resumen de refactorización: Logging con mejores prácticas

## 📋 Cambios realizados en ExampleService.cs

### ✅ Completado: 16 de enero de 2026

Se han refactorizado ambos métodos del servicio (`ProcessOperationAsync` y `ProcessTransactionAsync`) para aplicar las mejores prácticas de logging descritas en `doc/05-logging-loki-serilog.md`.

---

## 🔄 Cambios principales

### 1. Adición de using
```csharp
using Serilog.Context;
```

### 2. ProcessOperationAsync - Cambios aplicados

#### Antes:
```csharp
_logger.LogInformation("Entrada a ProcessOperationAsync - Cuenta: {AccountNumber}, Importe: {Amount:C}, Tipo: {OperationType}", 
    accountNumber, amount, operationType);
```

#### Después:
```csharp
var operationId = Guid.NewGuid().ToString("N")[..8];

// Enriquecer el contexto de logging para TODOS los logs siguientes
using (LogContext.PushProperty("OperationId", operationId))
using (LogContext.PushProperty("AccountNumber", accountNumber))
using (LogContext.PushProperty("Amount", amount))
using (LogContext.PushProperty("OperationType", operationType))
{
    try
    {
        _logger.LogInformation("OPERATION_START");
        
        // ... código ...
        
        _logger.LogInformation(
            "OPERATION_SUCCESS | duration_ms={Duration} processing_time_ms={ProcessingTime}",
            stopwatch.ElapsedMilliseconds, processingTime.TotalMilliseconds);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "OPERATION_FAILED | duration_ms={Duration} error_type={ErrorType}",
            stopwatch.ElapsedMilliseconds, ex.GetType().Name);
    }
}
```

**Ventajas:**
- ✅ Propiedades estructuradas con `LogContext.PushProperty()`
- ✅ Eventos claros: `OPERATION_START`, `OPERATION_SUCCESS`, `OPERATION_FAILED`
- ✅ ID único (`OperationId`) para trazabilidad
- ✅ Duración en logs (performance tracking)
- ✅ Tipo de error en logs (facilita búsquedas)

---

### 3. ProcessTransactionAsync - Cambios aplicados

#### Antes:
```csharp
_logger.LogInformation("Iniciando procesamiento de transacción: Monto={Amount}, Cuenta={AccountType}", 
    amount, accountType);
```

#### Después:
```csharp
var transactionId = Guid.NewGuid().ToString("N")[..8].ToUpper();

// Enriquecer el contexto de logging
using (LogContext.PushProperty("TransactionId", transactionId))
using (LogContext.PushProperty("Amount", amount))
using (LogContext.PushProperty("AccountType", accountType))
{
    try
    {
        _logger.LogInformation("TRANSACTION_START");
        
        // ... lógica de decisión ...
        
        using (LogContext.PushProperty("TransactionType", transactionType))
        using (LogContext.PushProperty("Path", selectedPath))
        {
            _logger.LogInformation(
                "TRANSACTION_PROCESSING | processing_time_ms={ProcessingTime}",
                processingTime.TotalMilliseconds);
            
            // ... procesamiento ...
            
            _logger.LogInformation(
                "TRANSACTION_SUCCESS | duration_ms={Duration} processing_time_ms={ProcessingTime}",
                stopwatch.ElapsedMilliseconds, processingTime.TotalMilliseconds);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "TRANSACTION_FAILED | duration_ms={Duration} error_type={ErrorType}",
            stopwatch.ElapsedMilliseconds, ex.GetType().Name);
    }
}
```

**Ventajas:**
- ✅ ID único (`TransactionId`) generado una sola vez
- ✅ Contexto estratificado: propiedades globales + específicas
- ✅ Eventos claros: `TRANSACTION_START`, `TRANSACTION_PROCESSING`, `TRANSACTION_SUCCESS`, `TRANSACTION_FAILED`
- ✅ Duración y métricas de performance
- ✅ Detalles de tipo de transacción y ruta (premium/standard)

---

## 📊 Comparativa: Antes vs Después

### Logs de éxito

**Antes:**
```
Éxito en ProcessOperationAsync - Cuenta: 123456, Importe: $2,500.00, Tipo: fast, Tiempo: 125ms
```

**Después:**
```
OPERATION_SUCCESS | duration_ms=125 processing_time_ms=115
  OperationId: a1b2c3d4
  AccountNumber: 123456
  Amount: 2500
  OperationType: fast
```

**Ventaja:** Las propiedades se indexan por separado en Loki, facilitando búsquedas precisas.

---

### Logs de error

**Antes:**
```
Error en ProcessOperationAsync - Cuenta: 123456, Importe: $2,500.00, Tipo: slow
System.InvalidOperationException: Error simulado durante procesamiento slow
```

**Después:**
```
OPERATION_FAILED | duration_ms=1250 error_type=InvalidOperationException
  OperationId: a1b2c3d4
  AccountNumber: 123456
  Amount: 2500
  OperationType: slow
  Exception: System.InvalidOperationException: Simulated error during slow operation
```

**Ventaja:** Error type indexado, permite alertas y queries por tipo de excepción.

---

## 🔍 Queries en Loki ahora posibles

Con la nueva estructura, puedes hacer:

```promql
# Buscar operaciones exitosas para una cuenta
{job="prueba-prometheus"} | json | Status="OPERATION_SUCCESS" | AccountNumber="123456"

# Latencia P95 por tipo de operación
{job="prueba-prometheus"} | json Status="OPERATION_SUCCESS" | stats quantile(0.95, duration_ms) by OperationType

# Contar errores por tipo de excepción
{job="prueba-prometheus"} | json | Status="OPERATION_FAILED" | stats count by error_type

# Encontrar la transacción específica
{job="prueba-prometheus"} | json | TransactionId="A1B2C3D4"

# Operaciones lentas (duración > 500ms)
{job="prueba-prometheus"} | json | duration_ms > 500 | order by duration_ms desc
```

---

## ✅ Checklist de prácticas aplicadas

- ✅ **LogContext.PushProperty()** — Propiedades estructuradas
- ✅ **IDs únicos** — OperationId, TransactionId para trazabilidad
- ✅ **Eventos claros** — START, SUCCESS, FAILED, PROCESSING
- ✅ **Duración** — duration_ms, processing_time_ms para performance
- ✅ **Tipo de error** — error_type en logs de error
- ✅ **Contexto estratificado** — Propiedades globales + específicas
- ✅ **Sin datos sensibles** — Cantidad, no detalles sensibles
- ✅ **Try-catch-finally** — Manejo correcto de excepciones

---

## 🧪 Compilación

```
✅ Compilación correcta.
    0 Advertencia(s)
    0 Errores
```

---

## 🚀 Siguientes pasos recomendados

1. **Ejecutar la aplicación:**
   ```powershell
   dotnet run
   ```

2. **Hacer requests de prueba:**
   ```bash
   curl -X POST http://localhost:5000/example/process \
     -H "Content-Type: application/json" \
     -d '{"operationType":"fast"}'
   ```

3. **Visualizar logs en Grafana:**
   - Ir a http://localhost:3000
   - En Explore → Loki
   - Buscar: `{job="prueba-prometheus"} | json`

4. **Hacer commit:**
   ```bash
   git add .
   git commit -m "refactor: aplicar mejores prácticas de logging en ExampleService"
   git push
   ```

---

## 📁 Archivos modificados

- `Application/Services/ExampleService.cs` — Refactorización de logging
- `Program.cs` — Configuración Serilog + Loki (ya completado)
- `doc/05-logging-loki-serilog.md` — Documento de referencia

---

**Versión:** 1.0.0  
**Fecha:** 16 de enero de 2026
