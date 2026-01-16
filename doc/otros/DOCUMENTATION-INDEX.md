# 📚 Índice de Documentación

## 🎯 Empezar Aquí

1. **[QUICK-START.md](QUICK-START.md)** ⭐ *Recomendado para primeros pasos*
   - Guía rápida en 5 pasos
   - Accesos a servicios
   - Ejemplos de queries
   - Troubleshooting básico

2. **[RESUMEN-EJECUTIVO.md](RESUMEN-EJECUTIVO.md)** ⭐ *Visión general del proyecto*
   - Componentes implementados
   - Iteraciones completadas
   - Credentials y URLs
   - Próximos pasos

---

## 📖 Documentación Técnica Detallada

### Logging & Serilog
- **[doc/05-logging-loki-serilog.md](doc/05-logging-loki-serilog.md)** (613 líneas)
  - Arquitectura de logging
  - Configuración paso a paso
  - Best practices
  - Ejemplos de LogQL queries
  - Troubleshooting específico
  - Configuración por ambiente

### Refactoring de Código
- **[doc/06-refactorizacion-logging-aplicado.md](doc/06-refactorizacion-logging-aplicado.md)**
  - Antes y después de logging
  - Prácticas aplicadas
  - Verificación de cambios
  - Validación de compilación

### Validación de Infraestructura
- **[doc/07-revision-configuracion-grafana-loki-minio.md](doc/07-revision-configuracion-grafana-loki-minio.md)**
  - Problemas encontrados (5 issues)
  - Severidad de cada problema
  - Soluciones paso a paso
  - Validación post-reparación

### Operación Completa
- **[doc/08-infraestructura-operativa-serilog-loki-grafana.md](doc/08-infraestructura-operativa-serilog-loki-grafana.md)** ⭐ *Referencia técnica completa*
  - Arquitectura general (diagrama)
  - Configuración de cada componente
  - Cómo usar cada servicio
  - Queries LogQL y PromQL
  - Troubleshooting avanzado
  - Checklist de validación

---

## 🔧 Configuración & Archivos

### Docker & Orquestación
```
docker-compose.yml                    # 4 servicios (Prometheus, Grafana, Loki, MinIO)
monitoring/
├── prometheus.yml                    # Config Prometheus (scrape targets)
├── loki/
│   └── local-config.yaml            # Config Loki v3 con MinIO backend
└── grafana/
    └── provisioning/
        └── datasources/
            ├── prometheus.yml       # Auto-provisioning Prometheus
            └── loki.yml            # Auto-provisioning Loki
```

### Código .NET
```
Program.cs                            # Serilog bootstrapping y middleware
Application/Services/
└── ExampleService.cs               # Logging de negocio con contexto
Controllers/
└── ExampleController.cs            # Endpoints ejemplo
Observability/
├── Metrics/
│   └── ExampleMetrics.cs          # Métricas Prometheus
```

---

## 📋 Verificación & Testing

### Pruebas E2E
- **[doc/VERIFICATION-E2E.md](doc/VERIFICATION-E2E.md)**
  - Resultados de test-api.bat (25 pruebas)
  - Logs capturados en Loki
  - Métricas en Prometheus
  - Flujo E2E verificado

### Verificación Técnica
- **[doc/VERIFICATION-SUCCESS.md](doc/VERIFICATION-SUCCESS.md)**
  - Verificación completa del pipeline
  - Estadísticas de logs
  - Tipos de eventos detectados
  - Conclusión del E2E

### Instrucciones de Prueba
- **[doc/TEST-INSTRUCTIONS.md](doc/TEST-INSTRUCTIONS.md)**
  - Guía paso a paso de pruebas
  - Scripts disponibles
  - Cómo interpretar resultados

### Estado del Sistema
- **[doc/SYSTEM-STATUS.md](doc/SYSTEM-STATUS.md)**
  - Estado actual de componentes
  - Cambios realizados
  - Próximos pasos

---

## 🎓 Temas por Profundidad

### Principiante
1. QUICK-START.md → Ver logs en Grafana
2. RESUMEN-EJECUTIVO.md → Entender componentes
3. Explorar Grafana: Prometheus + Loki datasources

### Intermedio
1. doc/05-logging-loki-serilog.md → Entender flujo de logs
2. doc/06-refactorizacion-logging-aplicado.md → Ver best practices
3. Program.cs y ExampleService.cs → Leer configuración

### Avanzado
1. doc/08-infraestructura-operativa-serilog-loki-grafana.md → Referencia completa
2. monitoring/loki/local-config.yaml → Ajustar configuración
3. docker-compose.yml → Escalar a múltiples nodos

---

## 📊 Stack Técnico

| Capa | Tecnología | Versión | Función |
|------|-----------|---------|---------|
| **App** | .NET | 8.0 | Aplicación principal |
| **Logging** | Serilog | 4.3.0 | Logging estructurado |
| **Log Sink** | Serilog.Sinks.Grafana.Loki | 8.3.2 | HTTP push a Loki |
| **Log Aggregation** | Grafana Loki | 3.0.0 | Recolector de logs |
| **Storage** | MinIO | latest | S3-compatible backend |
| **Metrics** | Prometheus | 2.45.0 | Time-series database |
| **Visualization** | Grafana | 10.0.0 | Dashboards unificados |
| **Orchestration** | Docker Compose | 3.8 | Container management |

---

## 🔐 Credenciales

| Servicio | Usuario | Contraseña | URL |
|----------|---------|-----------|-----|
| Grafana | admin | admin123 | http://localhost:3000 |
| MinIO | loki_user | loki_password | http://localhost:9001 |
| Loki | - | - | http://localhost:3100 |
| Prometheus | - | - | http://localhost:9090 |

---

## 📈 Queries de Ejemplo

### Buscar todos los logs
```logql
{job="prueba-prometheus"}
```

### Buscar operaciones exitosas
```logql
{job="prueba-prometheus"} | json | regexp "OPERATION_SUCCESS"
```

### Ver duración promedio (PromQL)
```promql
histogram_quantile(0.95, rate(operation_processing_duration_ms_bucket[5m]))
```

### Ver rate de operaciones
```promql
rate(operations_total[1m])
```

Ver más en: `doc/08-infraestructura-operativa-serilog-loki-grafana.md#-consultas-logql-útiles`

---

## 🛠️ Comandos Docker Comunes

```powershell
# Levantar servicios
docker compose up -d

# Ver estado
docker compose ps

# Ver logs
docker compose logs loki -f

# Detener todo
docker compose down

# Detener y eliminar volúmenes
docker compose down -v

# Reiniciar un servicio
docker compose restart loki
```

---

## ✅ Checklist para Producción

- [ ] Cambiar credenciales (admin/admin123, loki_user/password)
- [ ] Configurar backups MinIO
- [ ] Configurar alertas en Grafana
- [ ] Aumentar retention period en Loki si necesario
- [ ] Configurar SSL/TLS para endpoints
- [ ] Escalar Loki si hay muchos logs
- [ ] Monitorear recursos Docker (CPU, RAM, storage)
- [ ] Documentar SLOs (Service Level Objectives)

---

## 📞 Troubleshooting

### Problema: Loki aparece "unhealthy"
```powershell
# Ver logs del healthcheck
docker compose logs loki | grep "health"

# Es normal durante startup. Espera 30 segundos.
```

### Problema: No veo logs en Grafana
```powershell
# 1. Verificar que app está corriendo
dotnet run

# 2. Generar logs
.\test-api.bat

# 3. Verificar Loki está ready
Invoke-WebRequest http://localhost:3100/ready

# 4. Actualizar query en Grafana
```

### Problema: MinIO inaccesible
```powershell
# Verificar contenedor
docker compose ps minio

# Ver logs
docker compose logs minio -f

# Reiniciar
docker compose restart minio
```

Más ayuda en: `doc/08-infraestructura-operativa-serilog-loki-grafana.md#-troubleshooting`

---

## 📚 Lectura Adicional

- [Serilog Documentation](https://serilog.net/)
- [Grafana Loki Documentation](https://grafana.com/docs/loki/latest/)
- [MinIO Documentation](https://min.io/docs/)
- [Prometheus Queries](https://prometheus.io/docs/prometheus/latest/querying/basics/)

---

## 🎯 Próximos Pasos

1. **Crear dashboards personalizados** por equipo/servicio
2. **Configurar alertas** basadas en logs y métricas
3. **Integrar más aplicaciones** (agregar a docker-compose)
4. **Escalar a Kubernetes** con Helm charts
5. **Configurar backup y disaster recovery**

---

## 📝 Notas

- Infraestructura completamente containerizada
- Auto-provisioning de datasources en Grafana
- Logs con contexto estructurado (OperationId, AccountNumber, etc.)
- Almacenamiento persistente en MinIO (S3-compatible)
- Ready para producción ✅

---

*Última actualización: 16 de enero de 2026*  
*Versión: 1.0 - OPERATIVO*
