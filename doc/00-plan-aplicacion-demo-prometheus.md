# Plan: Aplicación Demo Métricas Prometheus .NET

Crear una aplicación ASP.NET Core Web API desde cero para demostrar el uso de métricas Prometheus con prometheus-net, incluyendo configuración, definición centralizada de métricas y endpoints de ejemplo.

## Pasos

1. **Crear estructura básica del proyecto** - Generar `PruebaPrometheus.csproj` con referencias a `prometheus-net.AspNetCore` y configurar `Program.cs` con middleware de métricas y endpoint `/metrics`

2. **Implementar definición centralizada de métricas** - Crear `Observability/Metrics/ExampleMetrics.cs` con `Counter`, `Histogram` y métricas con labels según especificaciones

3. **Desarrollar servicio de ejemplo** - Implementar `Application/Services/ExampleService.cs` que use las métricas definidas para simular operaciones fast/slow con medición de latencia

4. **Crear controlador de API** - Desarrollar `Controllers/ExampleController.cs` con endpoint `POST /example/process` que consume `ExampleService` y maneja errores

5. **Configurar archivos de apoyo** - Generar `appsettings.json`, `Properties/launchSettings.json` con configuración de puertos y logging

6. **Documentar uso y testing** - Crear ejemplos de requests y verificar que `/metrics` expone las métricas correctamente

## Consideraciones Adicionales

1. **Puerto de aplicación** - ¿Prefieres un puerto específico (ej: 5000, 7000) o usar configuración automática de ASP.NET Core?
2. **Manejo de errores simulados** - ¿Quieres que los errores se generen aleatoriamente, por parámetro, o con una lógica específica?
3. **Nivel de logging** - ¿Incluir logs detallados para fines educativos o mantener logging mínimo?