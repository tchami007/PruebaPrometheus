# 🚀 GUÍA RÁPIDA DE PRUEBA

## Paso 1: Ejecuta la aplicación

Abre una terminal PowerShell en `D:\NET\PruebaPrometheus` y ejecuta:

```powershell
dotnet run --no-build
```

**Espera a que veas el mensaje:**
```
Now listening on: http://localhost:5000
```

## Paso 2: Prueba los endpoints (en otra terminal)

Una vez que la app esté corriendo, abre **OTRA TERMINAL** y ejecuta:

```powershell
.\test-requests.ps1
```

Esto hará 3 requests a la aplicación:
- `GET /example/info`
- `POST /example/process-operation`
- `POST /example/process-transaction`

## Paso 3: Verifica los logs en Loki

Después de ejecutar los requests, en la **misma terminal** ejecuta:

```powershell
.\test-logs.ps1
```

Esto:
1. Verifica que la app responde
2. Espera 5 segundos (para que Serilog envíe los logs)
3. Consulta Loki por los logs
4. Te muestra si los logs llegaron correctamente

## Flujo esperado

```
APP (localhost:5000)
    ↓ logs vía Serilog
LOKI (localhost:3100)
    ↓ visualización
GRAFANA (localhost:3000)
```

## Direcciones útiles

| Servicio | URL |
|----------|-----|
| Aplicación | http://localhost:5000 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |
| Loki | http://localhost:3100 |
| MinIO | http://localhost:9000 |

**Credenciales Grafana:** admin / admin123

---

**¡Avísame cuando hayas ejecutado los scripts para revisar los resultados!**
