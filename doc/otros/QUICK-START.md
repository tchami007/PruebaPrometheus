# 🚀 Quick Start Guide - PruebaPrometheus

**Infraestructura Observable Completa: Serilog → Loki → Grafana**

---

## ⚡ Start en 5 pasos

### 1️⃣ Levantar servicios Docker
```powershell
cd D:\NET\PruebaPrometheus
docker compose up -d
```

✅ Espera 30 segundos para que todos los servicios inicien completamente.

### 2️⃣ Compilar y ejecutar aplicación
```powershell
dotnet build
dotnet run
```

O en una terminal separada:
```powershell
cd D:\NET\PruebaPrometheus
.\test-api.bat
```

### 3️⃣ Abrir Grafana
```
http://localhost:3000
Usuario: admin
Contraseña: admin123
```

### 4️⃣ Ver logs en Grafana
1. Menu **Explore** (left sidebar)
2. Data Source: **Loki**
3. Query: `{job="prueba-prometheus"}`
4. Click **Run Query**

### 5️⃣ Ver métricas en Grafana
1. Menu **Dashboards**
2. Buscar `dotnet-metrics`
3. O crear nuevo panel desde **Prometheus**

---

## 📊 Accesos

| Servicio | URL | Credenciales |
|----------|-----|--------------|
| **Grafana** | http://localhost:3000 | admin / admin123 |
| **Prometheus** | http://localhost:9090 | - |
| **MinIO Console** | http://localhost:9001 | loki_user / loki_password |
| **Loki API** | http://localhost:3100 | - |

---

## 🔍 Ejemplos de Queries

### LogQL (Loki)
```logql
# Ver todos los logs
{job="prueba-prometheus"}

# Ver solo errores
{job="prueba-prometheus"} | json | line_format "{{.level}}" = "Error"

# Ver operaciones exitosas
{job="prueba-prometheus"} 
| json 
| regexp "OPERATION_SUCCESS"

# Ver OperationId específico
{job="prueba-prometheus"} 
| json OperationId="a1b2c3d4"
```

### PromQL (Prometheus)
```promql
# Rate de requests HTTP
rate(http_requests_total[1m])

# Duración promedio de operaciones
histogram_quantile(0.95, rate(operation_processing_duration_ms_bucket[5m]))

# Operaciones por segundo
rate(operations_total[1m])
```

---

## 📝 Estructura de Logs

Cada log es JSON estructurado:
```json
{
  "Timestamp": "2026-01-16T14:52:00.000Z",
  "Level": "Information",
  "Message": "OPERATION_SUCCESS | duration_ms=150",
  "OperationId": "abc12345",
  "AccountNumber": "123456",
  "Amount": 2500,
  "OperationType": "fast",
  "Environment": "development"
}
```

---

## 🛠️ Comandos Útiles

### Ver estado de servicios
```powershell
docker compose ps
```

### Ver logs de un servicio
```powershell
docker compose logs loki -f          # Loki logs (follow mode)
docker compose logs grafana -n 50    # Últimos 50 logs de Grafana
docker compose logs -f               # Todos los servicios
```

### Detener servicios
```powershell
docker compose down           # Detener sin eliminar volúmenes
docker compose down -v        # Detener y eliminar volúmenes
```

### Reiniciar Loki
```powershell
docker compose restart loki
```

### Ejecutar comando en MinIO
```powershell
docker compose exec minio mc ls local/
```

---

## 🐛 Troubleshooting Rápido

**Problem:** Loki aparece "unhealthy"  
**Solución:** Es normal durante startup. Espera 30 segundos.

**Problem:** No veo logs en Grafana  
**Solución:** 
1. Verifica que la app está corriendo: `dotnet run`
2. Ejecuta `test-api.bat` para generar logs
3. Espera 5 segundos
4. Actualiza la query en Grafana

**Problem:** MinIO no accesible  
**Solución:** Verifica que el contenedor está corriendo
```powershell
docker compose ps minio
```

**Problem:** Error de conexión a Loki desde app  
**Solución:** Verifica que Loki está ready
```powershell
curl http://localhost:3100/ready
```

---

## 📚 Documentación Adicional

Archivos de referencia en `/doc/`:
- `05-logging-loki-serilog.md` - Guía completa Serilog+Loki
- `06-refactorizacion-logging-aplicado.md` - Ejemplos de código
- `07-revision-configuracion-grafana-loki-minio.md` - Detalles técnicos
- `08-infraestructura-operativa-serilog-loki-grafana.md` - Guía operativa
- `RESUMEN-EJECUTIVO.md` - Overview de toda la solución

---

## ✨ Tips & Tricks

1. **Aumentar log verbosity:**
   En `Program.cs`:
   ```csharp
   .MinimumLevel.Debug()    // Antes era Information
   ```

2. **Ver logs en consola:**
   Ya está configurado. Descomenta en Program.cs:
   ```csharp
   .WriteTo.Console(new CompactJsonFormatter())
   ```

3. **Crear dashboards personalizados:**
   - Explore → Build your own panel
   - Selecciona Prometheus
   - Escribe tu PromQL

4. **Exportar logs a CSV:**
   En Grafana Explore → ... → Download as CSV

5. **Configurar alertas:**
   - Menu: Alerting → Alert Rules
   - Create new alert rule
   - Condition: LogQL o PromQL

---

## 🎯 Próximas Acciones

1. **Crear dashboard del negocio** con KPIs
2. **Configurar alertas** para eventos críticos
3. **Integrar más aplicaciones** (agregar más servicios a docker-compose)
4. **Configurar backups** de MinIO
5. **Escalar a Kubernetes** (Helm charts disponibles)

---

## 📞 Soporte

Para problemas específicos:
1. Ver logs: `docker compose logs [servicio]`
2. Consultar doc: `/doc/08-infraestructura-operativa-serilog-loki-grafana.md`
3. Revisar config: `monitoring/loki/local-config.yaml`

---

**¡Listo para observar tu aplicación! 🎉**
