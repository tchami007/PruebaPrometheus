# Aplicación Demo Métricas Prometheus .NET

Esta es una aplicación de demostración que muestra cómo implementar y exponer métricas personalizadas para Prometheus en una API de ASP.NET Core.

## 🚀 Ejecución

### Prerrequisitos
- .NET 8 SDK
- Visual Studio Code o Visual Studio

### Ejecutar la aplicación
```bash
cd d:\NET\PruebaPrometheus
dotnet restore
dotnet run
```

La aplicación estará disponible en:
- **API**: https://localhost:7001 o http://localhost:5001
- **Swagger**: https://localhost:7001/swagger
- **Métricas Prometheus**: https://localhost:7001/metrics

## 📊 Endpoints

### 1. Información de la API
```
GET /example/info
```

### 2. Procesar Operación (genera métricas)
```
POST /example/process
Content-Type: application/json

{
    "operationType": "fast"  // o "slow"
}
```

### 3. Métricas Prometheus
```
GET /metrics
```

## 🎯 Métricas Implementadas

### Counter: `example_requests_total`
- **Descripción**: Cuenta el número total de requests procesados
- **Tipo**: Counter sin labels

### Counter con Labels: `example_operations_total`
- **Descripción**: Cuenta operaciones por tipo
- **Labels**: 
  - `operation_type`: "fast" | "slow"

### Histogram: `example_processing_seconds`
- **Descripción**: Mide la duración del procesamiento en segundos
- **Buckets**: De 0.1s a 1.0s en incrementos de 0.1s

### Counter de Errores: `example_errors_total`
- **Descripción**: Cuenta errores ocurridos durante el procesamiento
- **Tipo**: Counter sin labels

## 🧪 Ejemplos de Uso

### Ejemplo 1: Operación Rápida
```bash
curl -X POST https://localhost:7001/example/process \
  -H "Content-Type: application/json" \
  -d '{"operationType": "fast"}'
```

### Ejemplo 2: Operación Lenta
```bash
curl -X POST https://localhost:7001/example/process \
  -H "Content-Type: application/json" \
  -d '{"operationType": "slow"}'
```

### Ejemplo 3: Ver Métricas
```bash
curl https://localhost:7001/metrics
```

## 📈 Interpretación de Métricas

Después de hacer algunas llamadas a `/example/process`, puedes ver las métricas en `/metrics`:

```prometheus
# HELP example_requests_total Cantidad total de requests procesados
# TYPE example_requests_total counter
example_requests_total 15

# HELP example_operations_total Cantidad total de operaciones por tipo
# TYPE example_operations_total counter
example_operations_total{operation_type="fast"} 8
example_operations_total{operation_type="slow"} 7

# HELP example_processing_seconds Tiempo de procesamiento de cada request en segundos
# TYPE example_processing_seconds histogram
example_processing_seconds_bucket{le="0.1"} 3
example_processing_seconds_bucket{le="0.2"} 8
example_processing_seconds_bucket{le="0.3"} 8
example_processing_seconds_bucket{le="0.4"} 8
example_processing_seconds_bucket{le="0.5"} 8
example_processing_seconds_bucket{le="0.6"} 10
example_processing_seconds_bucket{le="0.7"} 12
example_processing_seconds_bucket{le="0.8"} 14
example_processing_seconds_bucket{le="0.9"} 15
example_processing_seconds_bucket{le="1.0"} 15
example_processing_seconds_bucket{le="+Inf"} 15
example_processing_seconds_sum 7.234
example_processing_seconds_count 15

# HELP example_errors_total Cantidad total de errores ocurridos
# TYPE example_errors_total counter
example_errors_total 1
```

## 🏗️ Arquitectura del Código

### `Program.cs`
- Configuración básica de ASP.NET Core
- Registro de servicios
- Configuración de middleware de métricas
- Exposición del endpoint `/metrics`

### `Observability/Metrics/ExampleMetrics.cs`
- **Definición centralizada** de todas las métricas
- Uso de tipos Prometheus: Counter, Histogram
- Configuración de labels y buckets

### `Application/Services/ExampleService.cs`
- **Lógica de negocio** que utiliza las métricas
- Simulación de operaciones "fast" y "slow"
- Manejo de errores con métricas correspondientes

### `Controllers/ExampleController.cs`
- **API endpoints** que consumen el servicio
- Validación de parámetros
- Manejo de respuestas HTTP

## 🎓 Conceptos Clave Aprendidos

1. **Definición Centralizada**: Todas las métricas se definen en un solo lugar
2. **Labels Estáticos**: Se usan valores predefinidos ("fast", "slow"), no dinámicos
3. **Tipos de Métricas**:
   - Counter: Para contar eventos
   - Histogram: Para medir distribuciones (latencia)
4. **Separación de Responsabilidades**: El servicio usa métricas, no las define
5. **Middleware Automático**: `UseHttpMetrics()` agrega métricas HTTP automáticas

## 🔍 Buenas Prácticas Implementadas

- ✅ Métricas definidas centralizadamente
- ✅ Labels estáticos (no dinámicos)
- ✅ Nombres descriptivos y consistentes
- ✅ Separación entre definición y uso
- ✅ Manejo de errores con métricas
- ✅ Configuración de buckets apropiada para histogramas
- ✅ Documentación clara de cada métrica