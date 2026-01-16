# Revisión: Configuración Grafana, Loki y MinIO

## 📋 Resumen de la revisión

**Fecha:** 16 de enero de 2026  
**Estado:** ⚠️ **PARCIALMENTE CONFIGURADO** — Se necesitan ajustes

---

## 🔍 Hallazgos

### ✅ Configurado correctamente

| Componente | Aspecto | Estado |
|---|---|---|
| **Docker Compose** | Red `monitoring` | ✅ Correcta |
| **Docker Compose** | Volúmenes persistentes | ✅ Correctos |
| **Docker Compose** | Puertos expuestos | ✅ Correctos |
| **Grafana** | Datasource Prometheus | ✅ Aprovisionado |
| **Grafana** | Provisioning de dashboards | ✅ Configurado |
| **Loki** | Servidor HTTP | ✅ Puerto 3100 |
| **MinIO** | Volumen montado | ✅ `/monitoring/minio` |

### ⚠️ Problemas encontrados

| Componente | Problema | Severidad |
|---|---|---|
| **Loki** | NO tiene datasource en Grafana | 🔴 **CRÍTICO** |
| **Loki** | Config usa `filesystem` pero no MinIO | 🟡 **IMPORTANTE** |
| **MinIO** | NO está configurado para ser usado por Loki | 🟡 **IMPORTANTE** |
| **Grafana** | NO tiene credentials para MinIO (si se usa) | 🟡 **IMPORTANTE** |

---

## 📊 Estado actual de la arquitectura

```
┌─────────────────────────────────────────────┐
│         Aplicación .NET Core                │
│      (Serilog → Loki HTTP)                  │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
        ┌──────────────────────┐
        │      Loki 3.0.0      │
        │  ❌ No conectado a    │
        │     Grafana UI       │
        │  ⚠️ Almacenamiento:   │
        │     filesystem       │
        └──────────┬───────────┘
                   │
        ┌──────────▼───────────┐
        │   Filesystem Local   │ ❌ No persisten entre reinicios
        │    (En contenedor)   │     Sin redundancia
        │                      │
        └──────────────────────┘

┌──────────────────────────────────────────────┐
│    MinIO (Aislado, no usado)                 │
│    ❌ No conectado a Loki                    │
│    ❌ No visible desde Grafana               │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│         Grafana 10.0.0                       │
│  ✅ Datasource: Prometheus                   │
│  ❌ Datasource: Loki (FALTA)                 │
│  ❌ Datasource: MinIO (No necesario)         │
└──────────────────────────────────────────────┘
```

---

## 🔧 Problemas específicos

### Problema 1: Loki no está configurado como DataSource en Grafana

**Ubicación:** `monitoring/grafana/provisioning/datasources/prometheus.yml`

**Actual:**
```yaml
datasources:
  - name: Prometheus
    type: prometheus
    url: http://prometheus:9090
    isDefault: true
```

**Falta:** DataSource de Loki

**Impacto:** 
- No puedes consultar logs desde Grafana UI
- Aunque Serilog envíe logs a Loki, no son accesibles visualmente
- Perderías toda la capacidad de searching/filtering en Grafana

---

### Problema 2: Loki está configurado con `filesystem` pero debería usar MinIO

**Ubicación:** `monitoring/loki/local-config.yaml`

**Actual:**
```yaml
storage_config:
  boltdb_shipper:
    active_index_directory: /loki/index
    shared_store: filesystem    # ❌ Almacenamiento local
  filesystem:
    directory: /loki/chunks
```

**Problemas:**
- Los logs se almacenan DENTRO del contenedor (se pierden si se reinicia)
- Sin respaldo externo (MinIO)
- Sin persistencia entre deployments
- No escalable

---

### Problema 3: MinIO no está integrado con Loki

**Ubicación:** `docker-compose.yml` + `local-config.yaml`

**Actual:**
- MinIO corre pero está **aislado**
- Loki no tiene credenciales de MinIO
- Loki no está configurado para usar MinIO como backend

**Impacto:**
- MinIO es solo contenedor vacío, sin propósito
- Loki pierde logs entre reinicios

---

## ✅ Solución: Pasos recomendados

### Paso 1: Crear DataSource de Loki en Grafana

Crea el archivo: `monitoring/grafana/provisioning/datasources/loki.yml`

```yaml
apiVersion: 1

datasources:
  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    isDefault: false
    jsonData:
      maxLines: 1000
```

### Paso 2: Configurar Loki para usar MinIO

Actualiza: `monitoring/loki/local-config.yaml`

```yaml
auth_enabled: false

server:
  http_listen_port: 3100

ingester:
  wal:
    enabled: true          # ✅ Habilitar WAL
    dir: /loki/wal
  chunk_idle_period: 3m
  max_chunk_age: 1h
  chunk_retain_period: 1m

limits_config:
  enforce_metric_name: false
  reject_old_samples: true
  reject_old_samples_max_age: 168h
  retention_period: 720h    # 30 días de retención

storage_config:
  aws:                      # ✅ Usar MinIO como S3-compatible
    s3: s3://loki_user:loki_password@minio:9000/loki
    s3forcepathstyle: true
  boltdb_shipper:
    active_index_directory: /loki/index
    shared_store: s3

schema_config:
  configs:
    - from: 2020-10-15
      store: boltdb-shipper
      object_store: s3
      schema: v12
      index:
        prefix: loki_index_
        period: 24h

compactor:
  working_directory: /loki/compactor
  shared_store: s3
```

### Paso 3: Actualizar docker-compose.yml

```yaml
loki:
  image: grafana/loki:3.0.0
  container_name: loki
  ports:
    - "3100:3100"
  volumes:
    - ./monitoring/loki/local-config.yaml:/etc/loki/local-config.yaml
    - loki_data:/loki
  command: -config.file=/etc/loki/local-config.yaml
  depends_on:
    - minio
  networks:
    - monitoring
  environment:
    - LOKI_MINIO_ENDPOINT=minio:9000
    - LOKI_MINIO_USER=loki_user
    - LOKI_MINIO_PASSWORD=loki_password
```

### Paso 4: Crear bucket en MinIO

```bash
# Entrar al contenedor de MinIO
docker exec -it minio /bin/sh

# Crear bucket usando mc (MinIO client)
mc alias set minio http://localhost:9000 loki_user loki_password
mc mb minio/loki
```

### Paso 5: Reiniciar servicios

```bash
# Detener y eliminar contenedores
docker compose down --volumes --remove-orphans

# Levantar de nuevo
docker compose up -d --build

# Verificar que todo está corriendo
docker compose ps
```

### Paso 6: Verificar DataSources en Grafana

1. Ir a http://localhost:3000 (admin/admin123)
2. Configuration → Data Sources
3. Debería ver:
   - ✅ Prometheus (existente)
   - ✅ Loki (nuevo)

4. Hacer clic en Loki y probar connection ("Save & Test")

---

## 🧪 Testing post-configuración

### Test 1: Verificar Loki está accessible

```bash
curl http://localhost:3100/ready
# Debería responder: ready
```

### Test 2: Enviar logs de prueba

```bash
curl -X POST http://localhost:3100/loki/api/v1/push \
  -H "Content-Type: application/json" \
  -d '{
    "streams": [{
      "stream": {"job": "test"},
      "values": [["'$(date +%s%N)'", "Test log message"]]
    }]
  }'
```

### Test 3: Consultar logs en Grafana

1. Ir a Explore (icono de brújula)
2. Seleccionar Loki
3. En LogQL, escribe: `{job="prueba-prometheus"}`
4. Ver logs de tu aplicación

---

## 📊 Configuración final esperada

```
┌──────────────────────────────────────────────────────┐
│         Aplicación .NET Core                         │
│    (Serilog → Loki HTTP puerto 3100)                 │
└──────────────────────┬───────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────────┐
        │      Loki 3.0.0                  │
        │  ✅ Escucha puerto 3100          │
        │  ✅ Almacenamiento: MinIO/S3     │
        │  ✅ Indexación: BoltDB Shipper   │
        └──────────────────┬───────────────┘
                           │
                           ▼
        ┌──────────────────────────────────┐
        │    MinIO S3-compatible           │
        │  ✅ Bucket: loki                 │
        │  ✅ User: loki_user              │
        │  ✅ Password: loki_password      │
        │  ✅ Accesible en 9000/9001       │
        └──────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│         Grafana 10.0.0                               │
│  ✅ DataSource: Prometheus                           │
│  ✅ DataSource: Loki                                 │
│  ✅ Dashboards: dotnet-metrics.json                  │
│  ✅ Explore: Consultar logs por etiquetas            │
└──────────────────────────────────────────────────────┘
```

---

## ⚠️ Notas importantes

### Seguridad

**Actual (NO SEGURO para producción):**
- Grafana: credentials por defecto (admin/admin123)
- MinIO: credentials en plain text
- auth_enabled: false en Loki

**Para producción:**
```yaml
# En loki local-config.yaml
auth_enabled: true
auth:
  type: enterprise

# En docker-compose.yml (Grafana)
environment:
  - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
  - GF_SECURITY_ADMIN_USER=${GRAFANA_USER}
```

### Performance

**Configuración recomendada por volumen:**

| Volumen | Retención | Config |
|---------|-----------|--------|
| **Bajo** (< 100MB/día) | 30 días | `retention_period: 720h` |
| **Medio** (100MB-1GB/día) | 7 días | `retention_period: 168h` |
| **Alto** (> 1GB/día) | 3 días | `retention_period: 72h` |

### Escalabilidad

Si necesitas escalar:

1. **Distribuido:** Usar Loki en modo distribuido (ingesters, queriers, distributors)
2. **Storage:** MinIO en cluster (3+ nodos)
3. **Replicación:** Configurar MinIO con Erasure Coding

---

## 🔗 Referencias

- [Loki Storage Config](https://grafana.com/docs/loki/latest/storage/)
- [Loki S3 Backend](https://grafana.com/docs/loki/latest/storage/aws/)
- [MinIO S3 Compatibility](https://min.io/docs/minio/linux/developers/minio-client/minio-client-complete-guide.html)
- [Grafana DataSources Provisioning](https://grafana.com/docs/grafana/latest/administration/provisioning/#data-sources)

---

## ✅ Checklist de implementación

- [ ] Crear `monitoring/grafana/provisioning/datasources/loki.yml`
- [ ] Actualizar `monitoring/loki/local-config.yaml` con config de MinIO
- [ ] Actualizar `docker-compose.yml` con volúmenes y variables de Loki
- [ ] Crear bucket en MinIO: `loki`
- [ ] `docker compose down --volumes --remove-orphans`
- [ ] `docker compose up -d --build`
- [ ] Verificar: `curl http://localhost:3100/ready`
- [ ] Verificar DataSources en Grafana (http://localhost:3000)
- [ ] Hacer request a la API para generar logs
- [ ] Buscar logs en Grafana Explore con: `{job="prueba-prometheus"}`

---

**Status:** 🟡 Listo con recomendaciones críticas  
**Próximo paso:** Implementar los cambios del "Paso 1 al 6"
