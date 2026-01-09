# 🐋 Guía Completa: Instalación Docker Desktop + Prometheus + Grafana desde Cero

Esta guía te llevará paso a paso desde la instalación de Docker Desktop hasta tener funcionando Prometheus y Grafana monitoreando tu aplicación .NET.

## 📋 Requisitos del Sistema

### Verificar Requisitos Mínimos

**Windows 10/11:**
- Windows 10 64-bit: Pro, Enterprise, o Education (Build 19041 o superior)
- Windows 11 64-bit: Cualquier edición
- **RAM**: Mínimo 4GB (recomendado 8GB+)
- **Espacio en disco**: Mínimo 4GB libres

### Verificar WSL2 (Windows Subsystem for Linux)

```powershell
# Abrir PowerShell como Administrador y verificar WSL
wsl --list --verbose

# Si no está instalado o es versión 1, instalar WSL2:
wsl --install
# Reiniciar la computadora después de la instalación
```

## 🚀 Parte 1: Instalación de Docker Desktop

### Paso 1: Descargar Docker Desktop

1. Ve a la página oficial: https://www.docker.com/products/docker-desktop/
2. Click en **"Download Docker Desktop for Windows"**
3. Se descargará el archivo: `Docker Desktop Installer.exe` (~500MB)

### Paso 2: Instalar Docker Desktop

1. **Ejecutar como Administrador** el archivo descargado
2. En el instalador, **verificar que estén marcadas estas opciones**:
   - ✅ **Use WSL 2 instead of Hyper-V** (recomendado)
   - ✅ **Add shortcut to desktop**

3. Click **"Ok"** para comenzar la instalación
4. **Esperar** ~5-10 minutos (descarga componentes adicionales)
5. Click **"Close and restart"** cuando termine

### Paso 3: Configuración Inicial de Docker Desktop

1. **Reiniciar** la computadora (importante)
2. **Abrir Docker Desktop** desde el escritorio o menú inicio
3. **Aceptar** los términos de servicio
4. **Configurar cuenta** (opcional, puedes omitir por ahora)
5. **Tutorial opcional**: Puedes omitir el tutorial

### Paso 4: Verificar Instalación

```cmd
# Abrir Command Prompt o PowerShell y verificar:

# Verificar Docker
docker --version
# Debe mostrar: Docker version 24.x.x, build...

# Verificar Docker Compose
docker compose version  
# Debe mostrar: Docker Compose version v2.x.x

# Probar que funciona
docker run hello-world
# Debe descargar y ejecutar contenedor de prueba
```

### Paso 5: Configuración Recomendada

1. **Abrir Docker Desktop**
2. Click en ⚙️ **Settings** (arriba derecha)
3. **General**:
   - ✅ Start Docker Desktop when you sign in
   - ✅ Use the WSL 2 based engine
4. **Resources > Advanced**:
   - **Memory**: Asignar al menos 4GB (más si tienes RAM disponible)
   - **CPUs**: Usar 2-4 cores
5. Click **"Apply & Restart"**

## 🔧 Parte 2: Crear Estructura de Monitoreo

### Paso 6: Preparar Archivos de Configuración

Desde tu proyecto `d:\NET\PruebaPrometheus`, crear la estructura:

```cmd
# Ir a la carpeta del proyecto
cd d:\NET\PruebaPrometheus

# Crear estructura de carpetas
mkdir monitoring
mkdir monitoring\grafana
mkdir monitoring\grafana\provisioning  
mkdir monitoring\grafana\provisioning\datasources
mkdir monitoring\grafana\provisioning\dashboards
```

### Paso 7: Crear docker-compose.yml

Crear archivo `docker-compose.yml` en la raíz del proyecto:

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
    restart: unless-stopped

  grafana:
    image: grafana/grafana:10.0.0
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=admin123
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning
    networks:
      - monitoring
    restart: unless-stopped

volumes:
  prometheus_data:
  grafana_data:

networks:
  monitoring:
    driver: bridge
```

### Paso 8: Crear monitoring/prometheus.yml

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

### Paso 9: Crear monitoring/grafana/provisioning/datasources/prometheus.yml

```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: true
```

### Paso 10: Crear monitoring/grafana/provisioning/dashboards/dashboard.yml

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

### Paso 11: Crear monitoring/grafana/provisioning/dashboards/dotnet-metrics.json

```json
{
  "dashboard": {
    "id": null,
    "title": "Prueba Prometheus .NET Metrics",
    "tags": ["dotnet", "prometheus", "aspnetcore"],
    "timezone": "browser",
    "panels": [
      {
        "id": 1,
        "title": "Total Requests",
        "type": "stat",
        "targets": [
          {
            "expr": "example_requests_total",
            "legendFormat": "Total Requests",
            "refId": "A"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "color": {
              "mode": "thresholds"
            },
            "thresholds": {
              "steps": [
                {"color": "green", "value": null},
                {"color": "red", "value": 80}
              ]
            }
          }
        },
        "gridPos": {"h": 8, "w": 6, "x": 0, "y": 0}
      },
      {
        "id": 2,
        "title": "Request Rate (per minute)",
        "type": "timeseries",
        "targets": [
          {
            "expr": "rate(example_requests_total[1m]) * 60",
            "legendFormat": "Requests/min",
            "refId": "A"
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
            "legendFormat": "{{operation_type}}",
            "refId": "A"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 0, "y": 8}
      },
      {
        "id": 4,
        "title": "Processing Duration (95th percentile)",
        "type": "timeseries",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(example_processing_seconds_bucket[5m]))",
            "legendFormat": "95th percentile",
            "refId": "A"
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
            "legendFormat": "{{transaction_type}}",
            "refId": "A"
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
            "legendFormat": "{{path}} - {{transaction_type}}",
            "refId": "A"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 12, "y": 16}
      },
      {
        "id": 7,
        "title": "Error Rate",
        "type": "timeseries",
        "targets": [
          {
            "expr": "rate(example_errors_total[1m])",
            "legendFormat": "Errors/sec",
            "refId": "A"
          }
        ],
        "gridPos": {"h": 8, "w": 24, "x": 0, "y": 24}
      }
    ],
    "time": {
      "from": "now-1h",
      "to": "now"
    },
    "refresh": "5s",
    "schemaVersion": 30,
    "version": 1
  }
}
```

## 🚀 Parte 3: Ejecutar el Stack de Monitoreo

### Paso 12: Verificar Estructura Final

Tu proyecto debe tener esta estructura:

```
PruebaPrometheus/
├── docker-compose.yml
├── Program.cs
├── PruebaPrometheus.csproj
├── test-api.bat
├── monitoring/
│   ├── prometheus.yml
│   └── grafana/
│       └── provisioning/
│           ├── datasources/
│           │   └── prometheus.yml
│           └── dashboards/
│               ├── dashboard.yml
│               └── dotnet-metrics.json
└── (resto de archivos del proyecto...)
```

### Paso 13: Ejecutar tu Aplicación .NET

```cmd
# En una terminal, desde d:\NET\PruebaPrometheus
dotnet run
```

**Verificar que esté funcionando:**
- Aplicación: https://localhost:7001/swagger
- Métricas: https://localhost:7001/metrics

### Paso 14: Descargar e Iniciar Prometheus + Grafana

```cmd
# En otra terminal, desde la misma carpeta d:\NET\PruebaPrometheus
docker compose up -d
```

**Este comando va a:**
1. ⬇️ **Descargar** Prometheus v2.45.0 (~200MB)
2. ⬇️ **Descargar** Grafana 10.0.0 (~350MB)  
3. 🚀 **Crear** las redes y volúmenes necesarios
4. ▶️ **Iniciar** ambos contenedores

### Paso 15: Verificar que Todo Funciona

```cmd
# Ver estado de contenedores
docker compose ps

# Debe mostrar:
# NAME        IMAGE                    STATUS
# grafana     grafana/grafana:10.0.0   Up
# prometheus  prom/prometheus:v2.45.0  Up
```

**URLs para verificar:**
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin123)

## 🎯 Parte 4: Verificación y Primer Uso

### Paso 16: Verificar Prometheus

1. Abrir http://localhost:9090
2. Ir a **Status > Targets**
3. Verificar que `prueba-prometheus-dotnet` esté **UP** (verde)
4. Si está DOWN, verificar que tu app .NET esté corriendo

### Paso 17: Acceder a Grafana

1. Abrir http://localhost:3000
2. Login: `admin` / Password: `admin123`
3. Ir a **Dashboards** en el menú lateral
4. Debe aparecer **"Prueba Prometheus .NET Metrics"**
5. Click en el dashboard para abrirlo

### Paso 18: Generar Datos de Prueba

```cmd
# Ejecutar script de prueba para generar métricas
test-api.bat
```

### Paso 19: Ver Métricas en Tiempo Real

En Grafana deberías ver:
- ✅ **Total Requests** aumentando
- ✅ **Operations by Type** con distribución fast/slow
- ✅ **Transactions** por tipo y camino
- ✅ **Processing Duration** con latencias
- ✅ **Error Rate** con errores simulados

## 🔧 Comandos Útiles para Mantenimiento

### Gestión de Contenedores

```cmd
# Ver logs de Prometheus
docker compose logs -f prometheus

# Ver logs de Grafana  
docker compose logs -f grafana

# Reiniciar servicios
docker compose restart

# Parar servicios (conserva datos)
docker compose stop

# Parar y eliminar todo (incluyendo datos)
docker compose down -v

# Ver uso de recursos
docker stats
```

### Backup de Configuraciones

```cmd
# Backup de dashboards de Grafana
docker cp grafana:/var/lib/grafana/grafana.db ./backup-grafana.db

# Backup de datos de Prometheus
docker cp prometheus:/prometheus ./backup-prometheus
```

## 🚨 Troubleshooting Común

### Problema: Docker Desktop no inicia

**Síntomas**: Error al abrir Docker Desktop

**Soluciones**:
1. Verificar que WSL2 esté instalado: `wsl --list --verbose`
2. Reiniciar servicio: Services → Docker Desktop Service → Restart
3. Reinstalar Docker Desktop con opción "Reset to factory defaults"

### Problema: Target DOWN en Prometheus

**Síntomas**: `prueba-prometheus-dotnet` aparece como DOWN

**Soluciones**:
1. Verificar app .NET: https://localhost:7001/metrics
2. Verificar puertos: `netstat -an | findstr 7001`
3. Revisar certificados SSL en prometheus.yml

### Problema: No aparecen métricas en Grafana

**Pasos de verificación**:
1. ✅ App .NET funcionando
2. ✅ Métricas accesibles: https://localhost:7001/metrics
3. ✅ Target UP en Prometheus: http://localhost:9090/targets
4. ✅ Datasource configurado en Grafana
5. ✅ Dashboard importado correctamente

### Problema: Error de memoria

**Síntomas**: Contenedores se reinician constantemente

**Soluciones**:
1. Aumentar memoria en Docker Desktop Settings
2. Cerrar aplicaciones innecesarias
3. Usar `docker system prune` para limpiar espacio

## 🎉 ¡Éxito!

Si has llegado hasta aquí, deberías tener:

✅ **Docker Desktop** instalado y funcionando  
✅ **Prometheus** corriendo en http://localhost:9090  
✅ **Grafana** corriendo en http://localhost:3000  
✅ **Tu aplicación .NET** generando métricas  
✅ **Dashboard** mostrando datos en tiempo real  

## 📚 Próximos Pasos

1. **Personalizar dashboard** agregando más paneles
2. **Configurar alertas** en Grafana  
3. **Explorar PromQL** para queries más complejas
4. **Optimizar rendimiento** ajustando intervals y retention

## 🔗 Recursos Adicionales

- [Docker Desktop Documentation](https://docs.docker.com/desktop/)
- [Prometheus Getting Started](https://prometheus.io/docs/prometheus/latest/getting_started/)
- [Grafana Documentation](https://grafana.com/docs/grafana/latest/)
- [PromQL Tutorial](https://prometheus.io/docs/prometheus/latest/querying/basics/)

¡Felicidades! Ahora tienes un stack completo de monitoreo profesional funcionando.