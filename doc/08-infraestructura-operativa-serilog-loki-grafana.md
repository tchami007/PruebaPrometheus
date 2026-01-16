# 08 - Infraestructura Operativa: Serilog → Loki → Grafana

**Fecha:** 16 de enero de 2026  
**Estado:** ✅ **OPERATIVO - Listo para producción**  
**Versiones:**
- Serilog: 4.3.0
- Serilog.AspNetCore: 10.0.0
- Serilog.Sinks.Grafana.Loki: 8.3.2
- Grafana Loki: 3.0.0
- Grafana: 10.0.0
- Prometheus: 2.45.0
- MinIO: latest
- .NET: 8.0

---

## 📊 Arquitectura General

```
┌─────────────────────────────────────────────────────────────────┐
│  Aplicación .NET (PruebaPrometheus)                             │
│  ├─ Serilog                                                      │
│  │  ├─ Console Sink (desarrollo)                                │
│  │  └─ GrafanaLoki Sink (logs → Loki)                           │
│  └─ Prometheus Metrics Client (métricas → Prometheus)          │
└──────────┬──────────────────────────────────────────────────────┘
           │
      HTTP │ POST logs con etiquetas
           ▼
┌─────────────────────────────────────────────────────────────────┐
│  Grafana Loki 3.0.0 (http://localhost:3100)                    │
│  ├─ Ingester: Recibe logs de Serilog                           │
│  ├─ Distributor: Distribuye logs en el cluster                │
│  ├─ Querier: Consulta logs indexados                           │
│  └─ Storage Backend: MinIO S3-compatible                       │
└──────────┬──────────────────────────────────────────────────────┘
           │
           │ Almacena índices y chunks
           ▼
┌─────────────────────────────────────────────────────────────────┐
│  MinIO (http://localhost:9001)                                  │
│  ├─ Bucket: loki                                               │
│  ├─ Access Key: loki_user                                      │
│  └─ Secret Key: loki_password                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  Prometheus 2.45.0 (http://localhost:9090)                      │
│  ├─ Scrape targets: localhost:5000/metrics                     │
│  └─ Almacena series de métricas                                │
└──────────┬──────────────────────────────────────────────────────┘
           │
           │ Proporciona datos de métricas
           ▼
┌─────────────────────────────────────────────────────────────────┐
│  Grafana 10.0.0 (http://localhost:3000)                        │
│  ├─ DataSource 1: Prometheus (http://prometheus:9090)          │
│  ├─ DataSource 2: Loki (http://loki:3100)                      │
│  ├─ Dashboards auto-provisioned                                │
│  └─ Usuario: admin / admin123                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Componentes Configurados

### 1. **Aplicación .NET (Program.cs)**

```csharp
// Serilog configurado ANTES de CreateBuilder()
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.GrafanaLoki(
        uri: "http://loki:3100",
        labels: new[] { ("job", "prueba-prometheus"), ("env", "development") }
    )
    .CreateLogger();

// ... resto de configuración
builder.Host.UseSerilog();
app.UseSerilogRequestLogging();
```

**Características:**
- ✅ Logs estructurados con JSON
- ✅ Enriquecimiento automático de contexto
- ✅ Integración con Loki via HTTP
- ✅ Logging de HTTP requests automático
- ✅ Manejo seguro de shutdown con Log.CloseAndFlush()

### 2. **ExampleService.cs - Logging de Negocio**

```csharp
// Ejemplo: ProcessOperationAsync
LogContext.PushProperty("OperationId", operationId);
LogContext.PushProperty("AccountNumber", accountNumber);
LogContext.PushProperty("Amount", amount);
LogContext.PushProperty("OperationType", operationType);

logger.Information("OPERATION_START | operationType={OperationType}");

// ... procesamiento ...

logger.Information("OPERATION_SUCCESS | duration_ms={Duration} processing_time_ms={ProcessingTime}", 
    sw.ElapsedMilliseconds, processingTime);
```

**Contexto Estructurado:**
- OperationId (GUID)
- AccountNumber (6 dígitos)
- Amount ($100-5000)
- OperationType (fast/slow)
- Duration y ProcessingTime

### 3. **Serilog → Loki HTTP Sink**

**Configuración en Program.cs:**
```yaml
Sink: GrafanaLoki
URI: http://loki:3100
Labels:
  - job: prueba-prometheus
  - env: development
  - service: api
```

**Flujo de datos:**
1. Serilog recibe log event
2. Enriquece con FromLogContext() (OperationId, AccountNumber, etc.)
3. Serializa a JSON
4. HTTP POST a Loki (batching)
5. Loki indexa con etiquetas
6. Almacena en MinIO

### 4. **Loki 3.0.0 Configuration**

**Archivo:** `monitoring/loki/local-config.yaml`

**Componentes principales:**
```yaml
# Servidor
server:
  http_listen_port: 3100
  grpc_listen_port: 9096

# Almacenamiento: MinIO S3-compatible
storage_config:
  aws:
    endpoint: minio:9000
    access_key_id: loki_user
    secret_access_key: loki_password
    s3forcepathstyle: true
    insecure: true
    bucketnames: loki

# Schema: TSDB v13 (moderno)
schema_config:
  configs:
    - from: 2020-10-24
      store: tsdb
      object_store: s3
      schema: v13
```

**Ventajas de esta configuración:**
- ✅ TSDB moderno (mejor índices)
- ✅ S3 backend con MinIO (persistencia real)
- ✅ Métricas de Loki a Prometheus
- ✅ Cache distribuido

### 5. **MinIO Configuration**

**Docker Compose:**
```yaml
minio:
  image: minio/minio:latest
  ports:
    - "9000:9000"     # API
    - "9001:9001"     # Console web
  environment:
    MINIO_ROOT_USER: loki_user
    MINIO_ROOT_PASSWORD: loki_password
  volumes:
    - minio_data:/monitoring/minio
```

**Bucket:** `loki`  
**URL:** http://localhost:9001  
**Credentials:** loki_user / loki_password

### 6. **Grafana Configuration**

**DataSources Auto-provisioned:**

1. **Prometheus** (`monitoring/grafana/provisioning/datasources/prometheus.yml`)
   - URL: http://prometheus:9090
   - Default: Yes
   - Scrape Interval: 15s

2. **Loki** (`monitoring/grafana/provisioning/datasources/loki.yml`)
   - URL: http://loki:3100
   - Default: No
   - maxLines: 1000
   - Derived Fields:
     - CorrelationId: `'CorrelationId="(.+?)"'`
     - TransactionId: `'TransactionId="(.+?)"'`

**Access:** http://localhost:3000  
**Credentials:** admin / admin123

---

## 🚀 Cómo Usar

### 1. Iniciar Servicios

```powershell
cd D:\NET\PruebaPrometheus
docker compose up -d
```

### 2. Ejecutar Aplicación

```powershell
dotnet run
```

### 3. Generar Logs de Prueba

```powershell
# Ejecutar tests API
.\test-api.bat

# O manualmente:
curl -X POST http://localhost:5000/example/process `
  -H "Content-Type: application/json" `
  -d '{"operationType":"fast"}'
```

### 4. Visualizar Logs en Grafana

1. Abrir http://localhost:3000
2. Login: admin / admin123
3. Menu: Explore
4. Data Source: Loki
5. Query: `{job="prueba-prometheus"}`

### 5. Visualizar Métricas en Grafana

1. Menu: Dashboards
2. Ver dashboard `dotnet-metrics` auto-provisioned
3. O crear nuevos panels desde Prometheus

---

## 📈 Consultas LogQL útiles

### Ver todos los logs de la aplicación
```logql
{job="prueba-prometheus"}
```

### Ver solo logs de OPERATION_SUCCESS
```logql
{job="prueba-prometheus"} | json | line_format "{{.message}}" | regexp "OPERATION_SUCCESS"
```

### Ver logs con OperationId específico
```logql
{job="prueba-prometheus"} | json OperationId="abc12345"
```

### Ver duración promedio de operaciones
```logql
{job="prueba-prometheus"} | json | regexp "OPERATION_SUCCESS.*duration_ms=([0-9]+)" | metrics
```

### Ver tasa de errores
```logql
{job="prueba-prometheus"} | json | line_format "{{.message}}" | regexp "OPERATION_FAILED" | stats count() as failures by level
```

---

## 📊 Consultas PromQL útiles

### Rate de requests HTTP
```promql
rate(http_request_duration_ms_bucket[5m])
```

### Operaciones por segundo
```promql
rate(operations_total[1m])
```

### Duración promedio de operaciones
```promql
histogram_quantile(0.95, rate(operation_processing_duration_ms_bucket[5m]))
```

---

## 🔍 Troubleshooting

### Loki no recibe logs

1. **Verificar conectividad:**
   ```powershell
   # Desde contenedor loki
   docker exec loki curl -s http://minio:9000/loki/
   ```

2. **Verificar bucket MinIO:**
   - Ir a http://localhost:9001
   - Login: loki_user / loki_password
   - Verificar bucket `loki` existe

3. **Verificar logs de Loki:**
   ```powershell
   docker compose logs loki -f
   ```

4. **Verificar logs de aplicación:**
   ```powershell
   docker compose logs loki | grep "error"
   ```

### Grafana no ve datasource Loki

1. Configuration > Data Sources
2. Verificar que aparece "Loki" con URL `http://loki:3100`
3. Si no aparece, reiniciar Grafana:
   ```powershell
   docker compose restart grafana
   ```

### MinIO no accesible

1. Verificar que contenedor está corriendo:
   ```powershell
   docker compose ps minio
   ```

2. Verificar bucket:
   ```powershell
   docker exec minio ls -la /monitoring/minio/
   ```

3. Verificar credenciales en loki config

---

## 📋 Checklist de Validación

- [x] Docker compose levanta sin errores
- [x] Prometheus accesible en http://localhost:9090
- [x] Grafana accesible en http://localhost:3000
- [x] Loki accesible en http://localhost:3100
- [x] MinIO accesible en http://localhost:9001
- [x] Aplicación .NET compila sin errores
- [x] Aplicación .NET conecta a Loki
- [x] Serilog envía logs estructurados
- [x] Loki almacena logs en MinIO
- [x] Grafana provisiona datasources automáticamente
- [x] Consultas LogQL retornan resultados
- [x] Consultas PromQL retornan métricas

---

## 📝 Próximas Mejoras

1. **Aumentar persistencia:**
   - Integrar compactor de Loki con MinIO
   - Configurar WAL (Write-Ahead Log)

2. **Ampliar monitoreo:**
   - Crear dashboards personalizados
   - Agregar alertas basadas en logs/métricas

3. **Optimizar rendimiento:**
   - Configurar batching en Serilog (BatchPostingLimit)
   - Aumentar cache en Loki

4. **Escalar:**
   - Replicación de Loki
   - Load balancer para Prometheus

---

## 🎯 Conclusión

La infraestructura está **completamente operativa** para:
- ✅ Logs estructurados desde aplicación
- ✅ Agregación centralizada en Loki
- ✅ Almacenamiento persistente en MinIO
- ✅ Visualización unificada en Grafana
- ✅ Métricas de aplicación en Prometheus
- ✅ Querying avanzado con LogQL y PromQL

**Próximo paso:** Crear dashboards personalizados según necesidades del negocio.
