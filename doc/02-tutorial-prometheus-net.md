# Tutorial: prometheus-net en ASP.NET Core

Este tutorial completo explica cómo implementar métricas de Prometheus en aplicaciones ASP.NET Core usando la librería `prometheus-net.AspNetCore`, basado en el código del proyecto PruebaPrometheus.

## Índice

1. [Configuración Inicial](#configuración-inicial)
2. [Configuración en Program.cs](#configuración-en-programcs)
3. [Definición Centralizada de Métricas](#definición-centralizada-de-métricas)
4. [Implementación en Servicios](#implementación-en-servicios)
5. [Tipos de Métricas](#tipos-de-métricas)
6. [Uso de Labels](#uso-de-labels)
7. [Buenas Prácticas](#buenas-prácticas)
8. [Consultas de Ejemplo](#consultas-de-ejemplo)

---

## Configuración Inicial

### 1. Instalación del Paquete NuGet

Agregar la librería `prometheus-net.AspNetCore` en el archivo `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="prometheus-net.AspNetCore" Version="8.2.1" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
```

### 2. Importación de Namespaces

```csharp
using Prometheus;
```

---

## Configuración en Program.cs

El archivo `Program.cs` requiere configuración mínima para habilitar las métricas:

```csharp
using Prometheus;
using PruebaPrometheus.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar el servicio de ejemplo
builder.Services.AddScoped<ExampleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANTE: Middleware para capturar métricas HTTP automáticas
app.UseHttpMetrics();

app.UseAuthorization();
app.MapControllers();

// IMPORTANTE: Exponer endpoint /metrics para Prometheus
app.MapMetrics();

app.Run();
```

### Componentes Clave:

1. **`app.UseHttpMetrics()`**: Habilita métricas automáticas de HTTP (requests, duración, códigos de estado)
2. **`app.MapMetrics()`**: Expone el endpoint `/metrics` donde Prometheus puede hacer scraping

---

## Definición Centralizada de Métricas

### Patrón Recomendado

Crear una clase estática centralizada para definir todas las métricas:

```csharp
using Prometheus;

namespace PruebaPrometheus.Observability.Metrics
{
    /// <summary>
    /// Definición centralizada de métricas personalizadas para Prometheus
    /// </summary>
    public static class PrometheusMetrics
    {
        /// <summary>
        /// Contador total de requests procesados
        /// </summary>
        public static readonly Counter RequestsTotal = Prometheus.Metrics
            .CreateCounter(
                "example_requests_total", 
                "Cantidad total de requests procesados"
            );

        /// <summary>
        /// Contador de operaciones por tipo (fast/slow)
        /// </summary>
        public static readonly Counter OperationsTotal = Prometheus.Metrics
            .CreateCounter(
                "example_operations_total", 
                "Cantidad total de operaciones por tipo",
                new[] { "operation_type" }
            );

        /// <summary>
        /// Histograma para medir tiempo de procesamiento
        /// </summary>
        public static readonly Histogram ProcessingDuration = Prometheus.Metrics
            .CreateHistogram(
                "example_processing_seconds", 
                "Tiempo de procesamiento de cada request en segundos",
                new HistogramConfiguration
                {
                    // Buckets personalizados para latencia (en segundos)
                    Buckets = Histogram.LinearBuckets(0.1, 0.1, 10) // 0.1s a 1.0s
                }
            );

        /// <summary>
        /// Contador de errores
        /// </summary>
        public static readonly Counter ErrorsTotal = Prometheus.Metrics
            .CreateCounter(
                "example_errors_total", 
                "Cantidad total de errores ocurridos"
            );
    }
}
```

### Convenciones de Nombres:

- **Sufijos estándar**: `_total` (counters), `_seconds` (duración)
- **Snake_case**: `example_requests_total` (no camelCase)
- **Prefijo consistente**: `example_` para todas las métricas del proyecto

---

## Implementación en Servicios

### Patrón de Instrumentación

```csharp
using PruebaPrometheus.Observability.Metrics;
using System.Diagnostics;

public class ExampleService
{
    private readonly ILogger<ExampleService> _logger;
    private static readonly Random _random = new();

    public ExampleService(ILogger<ExampleService> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> ProcessOperationAsync(string operationType)
    {
        // 1. Incrementar contador ANTES del procesamiento
        PrometheusMetrics.RequestsTotal.Inc();
        PrometheusMetrics.OperationsTotal.WithLabels(operationType).Inc();

        // 2. Iniciar medición de tiempo
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Procesando operación tipo: {OperationType}", operationType);

            // 3. Lógica de negocio
            var processingTime = operationType.ToLowerInvariant() switch
            {
                "fast" => TimeSpan.FromMilliseconds(_random.Next(50, 200)),
                "slow" => TimeSpan.FromMilliseconds(_random.Next(500, 1500)),
                _ => TimeSpan.FromMilliseconds(_random.Next(100, 300))
            };

            await Task.Delay(processingTime);

            // 4. Simular errores ocasionales
            if (_random.NextDouble() < 0.05)
            {
                PrometheusMetrics.ErrorsTotal.Inc();
                throw new InvalidOperationException($"Error simulado durante procesamiento {operationType}");
            }

            var result = new ProcessResult
            {
                Success = true,
                Message = $"Operación {operationType} completada exitosamente",
                ProcessingTimeMs = (int)processingTime.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            };

            // 5. Registrar duración en histograma (caso exitoso)
            PrometheusMetrics.ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando operación {OperationType}", operationType);
            
            // 6. Registrar duración incluso en errores
            PrometheusMetrics.ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
            
            return new ProcessResult
            {
                Success = false,
                Message = $"Error en operación {operationType}: {ex.Message}",
                ProcessingTimeMs = 0,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
```

### Puntos Clave:

1. **Instrumentación temprana**: Incrementar contadores al inicio
2. **Stopwatch para precisión**: Usar `Stopwatch` en lugar de `DateTime`
3. **Manejo de errores**: Registrar métricas en caso de fallo también
4. **Medición completa**: El tiempo incluye toda la operación

---

## Tipos de Métricas

### 1. Counter (Contador)

**Propósito**: Contar eventos que solo aumentan (requests, errores, transacciones)

```csharp
// Sin labels
public static readonly Counter RequestsTotal = Prometheus.Metrics
    .CreateCounter("example_requests_total", "Total de requests procesados");

// Con labels
public static readonly Counter OperationsTotal = Prometheus.Metrics
    .CreateCounter(
        "example_operations_total", 
        "Operaciones por tipo",
        new[] { "operation_type" }
    );

// Uso
RequestsTotal.Inc();                           // Incrementa en 1
RequestsTotal.Inc(5);                          // Incrementa en 5
OperationsTotal.WithLabels("fast").Inc();     // Con label
```

### 2. Histogram (Histograma)

**Propósito**: Medir distribuciones (latencia, tamaños, duraciones)

```csharp
public static readonly Histogram ProcessingDuration = Prometheus.Metrics
    .CreateHistogram(
        "example_processing_seconds", 
        "Tiempo de procesamiento en segundos",
        new HistogramConfiguration
        {
            // Buckets personalizados
            Buckets = Histogram.LinearBuckets(0.1, 0.1, 10) // 0.1, 0.2, 0.3... 1.0
        }
    );

// Uso
ProcessingDuration.Observe(0.245); // Registra 245ms
ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
```

### 3. Gauge (Indicador)

**Propósito**: Valores que suben y bajan (CPU, memoria, conexiones activas)

```csharp
public static readonly Gauge ActiveConnections = Prometheus.Metrics
    .CreateGauge("example_active_connections", "Conexiones activas");

// Uso
ActiveConnections.Set(25);     // Establece valor
ActiveConnections.Inc();       // Incrementa en 1
ActiveConnections.Dec();       // Decrementa en 1
ActiveConnections.Inc(5);      // Incrementa en 5
```

### 4. Summary

**Propósito**: Similar a Histogram pero calcula percentiles en el cliente

```csharp
public static readonly Summary RequestDuration = Prometheus.Metrics
    .CreateSummary(
        "example_request_duration_seconds",
        "Duración de requests",
        new SummaryConfiguration
        {
            Objectives = new[]
            {
                new QuantileEpsilonPair(0.5, 0.01),  // 50th percentile
                new QuantileEpsilonPair(0.9, 0.01),  // 90th percentile
                new QuantileEpsilonPair(0.99, 0.001) // 99th percentile
            }
        }
    );
```

---

## Uso de Labels

### Labels Simples

```csharp
// Definición
public static readonly Counter TransactionsByType = Prometheus.Metrics
    .CreateCounter(
        "example_transactions_by_type_total", 
        "Transacciones por tipo",
        new[] { "transaction_type" }
    );

// Uso
TransactionsByType.WithLabels("debito").Inc();
TransactionsByType.WithLabels("credito").Inc();
```

### Labels Múltiples

```csharp
// Definición
public static readonly Counter TransactionsByPath = Prometheus.Metrics
    .CreateCounter(
        "example_transactions_by_path_total", 
        "Transacciones por camino y tipo",
        new[] { "path", "transaction_type" }
    );

// Uso
TransactionsByPath.WithLabels("premium", "debito").Inc();
TransactionsByPath.WithLabels("standard", "credito").Inc();
```

### Ejemplo Completo con Bifurcación de Caminos

```csharp
public async Task<TransactionResult> ProcessTransactionAsync(decimal amount, string accountType)
{
    // Incrementar contador total
    PrometheusMetrics.TransactionsTotal.Inc();
    
    var stopwatch = Stopwatch.StartNew();

    try
    {
        // Lógica de bifurcación
        string selectedPath;
        string transactionType;
        
        var normalizedAccountType = accountType.ToLowerInvariant();
        
        if (normalizedAccountType == "premium")
        {
            selectedPath = "premium";
            transactionType = amount >= 1000 ? "debito" : "credito";
        }
        else
        {
            selectedPath = "standard";
            transactionType = _random.NextDouble() < 0.6 ? "debito" : "credito";
        }

        // Registrar métricas con labels
        PrometheusMetrics.TransactionsByType.WithLabels(transactionType).Inc();
        PrometheusMetrics.TransactionsByPath.WithLabels(selectedPath, transactionType).Inc();

        // Procesamiento...
        await SimulateProcessing(transactionType, selectedPath);
        
        // Registrar duración
        PrometheusMetrics.TransactionProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        
        return CreateSuccessResult(transactionType, selectedPath);
    }
    catch (Exception ex)
    {
        PrometheusMetrics.ErrorsTotal.Inc();
        PrometheusMetrics.TransactionProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        throw;
    }
}
```

---

## Buenas Prácticas

### 1. Nomenclatura

```csharp
// ✅ CORRECTO
"example_requests_total"           // snake_case con sufijo
"example_processing_seconds"       // unidad de tiempo clara
"example_errors_total"            // sufijo _total para counters

// ❌ INCORRECTO
"ExampleRequestsTotal"            // camelCase
"example_processing_time"         // unidad ambigua
"example_errors"                  // falta sufijo
```

### 2. Labels Estáticos

```csharp
// ✅ CORRECTO - Valores predefinidos
TransactionsByType.WithLabels("debito");     // Conjunto limitado
TransactionsByType.WithLabels("credito");

// ❌ INCORRECTO - Labels dinámicos
TransactionsByUser.WithLabels(userId);       // Cardinalidad infinita
TransactionsByAmount.WithLabels(amount.ToString()); // Memoria infinita
```

### 3. Definición Centralizada

```csharp
// ✅ CORRECTO - Una clase estática central
public static class PrometheusMetrics
{
    public static readonly Counter RequestsTotal = ...;
    public static readonly Histogram ProcessingDuration = ...;
}

// ❌ INCORRECTO - Métricas distribuidas
public class ServiceA 
{
    private static readonly Counter _requests = ...; // Distribuido
}
```

### 4. Manejo de Errores

```csharp
public async Task<Result> ProcessAsync()
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        MetricsClass.RequestsTotal.Inc();
        
        // Lógica de negocio
        var result = await DoWorkAsync();
        
        // ✅ Registrar métricas en caso exitoso
        MetricsClass.ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        return result;
    }
    catch (Exception)
    {
        MetricsClass.ErrorsTotal.Inc();
        
        // ✅ IMPORTANTE: Registrar duración incluso en errores
        MetricsClass.ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        throw;
    }
}
```

### 5. Configuración de Histogramas

```csharp
// Para latencias de API (milisegundos a segundos)
Buckets = Histogram.LinearBuckets(0.1, 0.1, 10)    // 0.1s a 1.0s

// Para operaciones largas (segundos a minutos)  
Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) // 0.1, 0.2, 0.4, 0.8...

// Buckets personalizados
Buckets = new[] { 0.01, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 }
```

---

## Consultas de Ejemplo

### Prometheus PromQL

```promql
# Rate de requests por segundo (últimos 5 minutos)
rate(example_requests_total[5m])

# Percentil 95 de latencia
histogram_quantile(0.95, rate(example_processing_seconds_bucket[5m]))

# Tasa de errores
rate(example_errors_total[5m]) / rate(example_requests_total[5m])

# Transacciones por tipo
sum by (transaction_type) (example_transactions_by_type_total)

# Comparación entre caminos premium vs standard
sum by (path) (rate(example_transactions_by_path_total[5m]))
```

### Grafana Dashboards

```promql
# Panel: Throughput
rate(example_requests_total[5m])

# Panel: Latency Percentiles
histogram_quantile(0.50, rate(example_processing_seconds_bucket[5m])) # P50
histogram_quantile(0.95, rate(example_processing_seconds_bucket[5m])) # P95
histogram_quantile(0.99, rate(example_processing_seconds_bucket[5m])) # P99

# Panel: Error Rate %
(rate(example_errors_total[5m]) / rate(example_requests_total[5m])) * 100

# Panel: Transactions by Type (Pie Chart)
sum by (transaction_type) (increase(example_transactions_by_type_total[1h]))
```

---

## Resultado Final en /metrics

Después de ejecutar algunas operaciones, el endpoint `/metrics` expondrá:

```prometheus
# HELP example_requests_total Cantidad total de requests procesados
# TYPE example_requests_total counter
example_requests_total 42

# HELP example_operations_total Cantidad total de operaciones por tipo
# TYPE example_operations_total counter
example_operations_total{operation_type="fast"} 25
example_operations_total{operation_type="slow"} 17

# HELP example_processing_seconds Tiempo de procesamiento de cada request en segundos
# TYPE example_processing_seconds histogram
example_processing_seconds_bucket{le="0.1"} 8
example_processing_seconds_bucket{le="0.2"} 20
example_processing_seconds_bucket{le="0.3"} 25
example_processing_seconds_bucket{le="0.4"} 25
example_processing_seconds_bucket{le="0.5"} 30
example_processing_seconds_bucket{le="0.6"} 35
example_processing_seconds_bucket{le="0.7"} 38
example_processing_seconds_bucket{le="0.8"} 40
example_processing_seconds_bucket{le="0.9"} 41
example_processing_seconds_bucket{le="1.0"} 42
example_processing_seconds_bucket{le="+Inf"} 42
example_processing_seconds_sum 15.234
example_processing_seconds_count 42

# HELP example_errors_total Cantidad total de errores ocurridos
# TYPE example_errors_total counter
example_errors_total 3

# HELP example_transactions_by_path_total Cantidad total de transacciones por camino de decisión
# TYPE example_transactions_by_path_total counter
example_transactions_by_path_total{path="premium",transaction_type="credito"} 5
example_transactions_by_path_total{path="premium",transaction_type="debito"} 8
example_transactions_by_path_total{path="standard",transaction_type="credito"} 12
example_transactions_by_path_total{path="standard",transaction_type="debito"} 18
```

---

## Conclusión

La librería `prometheus-net.AspNetCore` proporciona una forma sencilla y potente de instrumentar aplicaciones .NET con métricas de observabilidad de clase empresarial. Los patrones mostrados en este tutorial permiten:

- **Monitoreo completo** de throughput, latencia y errores
- **Segmentación avanzada** usando labels para análisis detallado
- **Integración nativa** con ecosistemas de observabilidad (Prometheus + Grafana)
- **Performance óptima** usando buenas prácticas de instrumentación

Este enfoque es fundamental para mantener aplicaciones confiables y observables en entornos de producción.