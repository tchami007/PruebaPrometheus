# Guía de Instalación: Prometheus + Grafana para Monitoreo

## 📋 Requisitos Previos

- Docker Desktop instalado y funcionando
- Tu aplicación .NET corriendo en https://localhost:7001
- PowerShell o CMD disponible

## 📦 Stack Completo Requerido

### 1. Tu Aplicación .NET (✅ Ya tienes)
- **Puerto**: https://localhost:7001
- **Métricas**: https://localhost:7001/metrics
- **Estado**: ✅ Funcionando

### 2. Prometheus Server
- **Función**: Scraping y almacenamiento de métricas
- **Puerto**: 9090 (por defecto)

### 3. Grafana
- **Función**: Visualización y dashboards
- **Puerto**: 3000 (por defecto)

## 🚀 Instalación con Docker Compose

### Paso 1: Estructura de Archivos

Asegúrate de tener esta estructura en tu proyecto:
```
PruebaPrometheus/
├── docker-compose.yml
├── monitoring/
│   ├── prometheus.yml
│   └── grafana/
│       └── provisioning/
│           ├── datasources/
│           │   └── prometheus.yml
│           └── dashboards/
│               ├── dashboard.yml
│               └── dotnet-metrics.json
```

### Paso 2: Crear docker-compose.yml

```yaml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:v2.45.0
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=200h'
      - '--web.enable-lifecycle'
    networks:
      - monitoring

  grafana:
    image: grafana/grafana:10.0.0
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=admin123
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning
    networks:
      - monitoring

volumes:
  prometheus_data:
  grafana_data:

networks:
  monitoring:
    driver: bridge
```

### Paso 3: Crear monitoring/prometheus.yml

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  # - "first_rules.yml"
  # - "second_rules.yml"

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'prueba-prometheus-dotnet'
    static_configs:
      - targets: ['host.docker.internal:7001']
    metrics_path: '/metrics'
    scrape_interval: 5s
    scrape_timeout: 5s
    scheme: 'https'
    tls_config:
      insecure_skip_verify: true
```

### Paso 4: Crear monitoring/grafana/provisioning/datasources/prometheus.yml

```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
```

### Paso 5: Crear monitoring/grafana/provisioning/dashboards/dashboard.yml

```yaml
apiVersion: 1

providers:
  - name: 'default'
    orgId: 1
    folder: ''
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /etc/grafana/provisioning/dashboards
```

### Paso 6: Crear monitoring/grafana/provisioning/dashboards/dotnet-metrics.json

```json
{
  "dashboard": {
    "id": null,
    "title": "Prueba Prometheus .NET Metrics",
    "tags": ["dotnet", "prometheus"],
    "timezone": "browser",
    "panels": [
      {
        "id": 1,
        "title": "Total Requests",
        "type": "stat",
        "targets": [
          {
            "expr": "example_requests_total",
            "legendFormat": "Total Requests"
          }
        ],
        "gridPos": {"h": 8, "w": 6, "x": 0, "y": 0}
      },
      {
        "id": 2,
        "title": "Request Rate (per minute)",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(example_requests_total[1m]) * 60",
            "legendFormat": "Requests/min"
          }
        ],
        "gridPos": {"h": 8, "w": 18, "x": 6, "y": 0}
      },
      {
        "id": 3,
        "title": "Operations by Type",
        "type": "piechart",
        "targets": [
          {
            "expr": "example_operations_total",
            "legendFormat": "{{operation_type}}"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 0, "y": 8}
      },
      {
        "id": 4,
        "title": "Processing Duration (95th percentile)",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(example_processing_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 12, "y": 8}
      },
      {
        "id": 5,
        "title": "Transactions by Type",
        "type": "barchart",
        "targets": [
          {
            "expr": "example_transactions_by_type_total",
            "legendFormat": "{{transaction_type}}"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 0, "y": 16}
      },
      {
        "id": 6,
        "title": "Transactions by Path and Type",
        "type": "table",
        "targets": [
          {
            "expr": "example_transactions_by_path_total",
            "legendFormat": "{{path}} - {{transaction_type}}"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 12, "y": 16}
      }
    ],
    "time": {
      "from": "now-1h",
      "to": "now"
    },
    "refresh": "5s"
  }
}
```

### Paso 7: Iniciar los Servicios

```cmd
# Desde la raíz del proyecto
docker-compose up -d
```

### Paso 8: Verificar Instalación

**Prometheus:**
- URL: http://localhost:9090
- Verificar targets: Status > Targets
- Debe aparecer `prueba-prometheus-dotnet` en estado UP

**Grafana:**
- URL: http://localhost:3000
- Usuario: `admin`
- Contraseña: `admin123`

## 🎯 Configuración Detallada

### Prometheus Configuration

El archivo `monitoring/prometheus.yml` configura:

```yaml
scrape_configs:
  - job_name: 'prueba-prometheus-dotnet'
    static_configs:
      - targets: ['host.docker.internal:7001']  # Tu aplicación
    metrics_path: '/metrics'                     # Endpoint de métricas
    scrape_interval: 5s                         # Frecuencia de scraping
    scheme: 'https'                             # Protocolo HTTPS
    tls_config:
      insecure_skip_verify: true                # Ignorar certificados SSL
```

**Explicación de host.docker.internal:**
- Permite que contenedores Docker accedan a servicios del host
- Equivale a `localhost` desde dentro del contenedor
- Funciona en Docker Desktop para Windows/Mac

### Grafana Configuration

**Datasource automático:**
- Se configura automáticamente via `provisioning/datasources/prometheus.yml`
- Apunta a `http://prometheus:9090` (comunicación entre contenedores)

**Dashboard pre-configurado:**
- Se carga automáticamente desde `provisioning/dashboards/dotnet-metrics.json`
- Incluye métricas básicas de tu aplicación

## 📊 Métricas Disponibles

### En Prometheus (http://localhost:9090)

Puedes consultar directamente:

```promql
# Total de requests
example_requests_total

# Rate de requests por minuto  
rate(example_requests_total[1m]) * 60

# Operaciones por tipo
example_operations_total

# Percentil 95 de latencia
histogram_quantile(0.95, rate(example_processing_seconds_bucket[5m]))

# Transacciones por tipo y camino
example_transactions_by_path_total

# Total de transacciones
example_transactions_total

# Rate de errores
rate(example_errors_total[1m])
```

### En Grafana (http://localhost:3000)

El dashboard incluye:
- **Total Requests**: Contador total acumulado
- **Request Rate**: Requests por minuto en tiempo real
- **Operations by Type**: Distribución fast vs slow
- **Processing Duration**: Latencia percentil 95
- **Transactions by Type**: Débito vs crédito
- **Transactions by Path and Type**: Tabla detallada con labels múltiples

## 🔍 Troubleshooting

### Problema: Target DOWN en Prometheus

**Síntomas:** El target `prueba-prometheus-dotnet` aparece como DOWN

**Soluciones:**
1. Verificar que tu app .NET esté corriendo en puerto 7001
2. Probar acceso manual: https://localhost:7001/metrics
3. Verificar logs de Prometheus:
   ```cmd
   docker logs prometheus
   ```

### Problema: No aparecen métricas

**Verificar paso a paso:**
1. App .NET funcionando: ✅
2. Endpoint /metrics accesible: ✅
3. Target UP en Prometheus: ✅
4. Métricas visibles en Prometheus: ✅
5. Datasource configurado en Grafana: ✅

### Problema: Certificados HTTPS

Si tienes problemas con HTTPS, puedes modificar tu app para exponer métricas también en HTTP:

```csharp
// En Program.cs, agregar listener HTTP adicional
builder.WebHost.UseUrls("https://localhost:7001", "http://localhost:5001");
```

Luego cambiar en `prometheus.yml`:
```yaml
- targets: ['host.docker.internal:5001']
scheme: 'http'  # Cambiar a HTTP
```

## 🎮 Generar Datos de Prueba

Una vez todo configurado:

```cmd
# Ejecutar script de prueba
test-api.bat

# Ver métricas en tiempo real
# Prometheus: http://localhost:9090/graph
# Grafana: http://localhost:3000/d/dotnet-metrics
```

## 🔄 Comandos Útiles

```cmd
# Iniciar servicios
docker-compose up -d

# Ver logs
docker-compose logs -f prometheus
docker-compose logs -f grafana

# Parar servicios
docker-compose down

# Reiniciar con limpieza
docker-compose down -v
docker-compose up -d

# Ver estado de contenedores
docker-compose ps
```

## 📈 Próximos Pasos

1. **Personalizar Dashboard**: Agregar más paneles en Grafana
2. **Alertas**: Configurar alertas en Grafana para métricas críticas
3. **Retention**: Ajustar retención de datos en Prometheus
4. **Seguridad**: Cambiar credenciales por defecto de Grafana

## 🎯 URLs de Referencia

- **Tu App**: https://localhost:7001/swagger
- **Métricas Raw**: https://localhost:7001/metrics
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin123)

## 📝 Notas Importantes

### Versiones Recomendadas
- **Prometheus**: v2.45.0 (estable y probada)
- **Grafana**: 10.0.0 (soporte completo para features modernas)

### Consideraciones de Performance
- **Scrape interval**: 5s para desarrollo, 15-30s para producción
- **Retention**: 200h configurado (8+ días de historia)
- **Storage**: Volúmenes Docker persistentes para datos

### Seguridad
- Cambiar credenciales de Grafana en producción
- Considerar autenticación adicional si expones públicamente
- Revisar configuración de red para ambientes productivos