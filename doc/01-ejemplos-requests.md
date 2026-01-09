# Requests de Ejemplo para Pruebas

Este archivo contiene ejemplos de requests HTTP que puedes usar para probar la aplicación y generar métricas.

## 📋 Preparación

1. Asegúrate de que la aplicación esté ejecutándose:
   ```bash
   dotnet run
   ```

2. La aplicación estará disponible en: `https://localhost:7001`

## 🧪 Requests de Prueba

### 1. Información de la API
```http
GET https://localhost:7001/example/info
Accept: application/json
```

### 2. Operación Rápida
```http
POST https://localhost:7001/example/process
Content-Type: application/json

{
    "operationType": "fast"
}
```

### 3. Operación Lenta
```http
POST https://localhost:7001/example/process
Content-Type: application/json

{
    "operationType": "slow"
}
```

### 4. Transacción Premium con Débito
```http
POST https://localhost:7001/example/transaction
Content-Type: application/json

{
    "amount": 1500.00,
    "accountType": "premium"
}
```

### 5. Transacción Premium con Crédito
```http
POST https://localhost:7001/example/transaction
Content-Type: application/json

{
    "amount": 500.00,
    "accountType": "premium"
}
```

### 6. Transacción Standard (tipo aleatorio)
```http
POST https://localhost:7001/example/transaction
Content-Type: application/json

{
    "amount": 750.00,
    "accountType": "standard"
}
```

### 7. Ver Métricas Prometheus
```http
GET https://localhost:7001/metrics
Accept: text/plain
```

### 8. Request Inválido - Operación (para probar validación)
```http
POST https://localhost:7001/example/process
Content-Type: application/json

{
    "operationType": "invalid"
}
```

### 9. Request Inválido - Transacción (para probar validación)
```http
POST https://localhost:7001/example/transaction
Content-Type: application/json

{
    "amount": -100.00,
    "accountType": "invalid"
}
```

## 🔄 Script para Generar Múltiples Requests

### PowerShell - Operaciones y Transacciones
```powershell
# Generar 10 operaciones rápidas
for ($i = 1; $i -le 10; $i++) {
    Invoke-RestMethod -Uri "https://localhost:7001/example/process" -Method POST -ContentType "application/json" -Body '{"operationType":"fast"}' -SkipCertificateCheck
    Write-Host "Fast operation $i completed"
    Start-Sleep -Seconds 1
}

# Generar 5 operaciones lentas
for ($i = 1; $i -le 5; $i++) {
    Invoke-RestMethod -Uri "https://localhost:7001/example/process" -Method POST -ContentType "application/json" -Body '{"operationType":"slow"}' -SkipCertificateCheck
    Write-Host "Slow operation $i completed"
    Start-Sleep -Seconds 2
}

# Generar 8 transacciones premium (mix de débito y crédito)
for ($i = 1; $i -le 8; $i++) {
    $amount = Get-Random -Minimum 100 -Maximum 2000
    $body = '{"amount":' + $amount + ',"accountType":"premium"}'
    Invoke-RestMethod -Uri "https://localhost:7001/example/transaction" -Method POST -ContentType "application/json" -Body $body -SkipCertificateCheck
    Write-Host "Premium transaction $i completed: $amount"
    Start-Sleep -Seconds 1
}

# Generar 12 transacciones standard
for ($i = 1; $i -le 12; $i++) {
    $amount = Get-Random -Minimum 50 -Maximum 1500
    $body = '{"amount":' + $amount + ',"accountType":"standard"}'
    Invoke-RestMethod -Uri "https://localhost:7001/example/transaction" -Method POST -ContentType "application/json" -Body $body -SkipCertificateCheck
    Write-Host "Standard transaction $i completed: $amount"
    Start-Sleep -Seconds 1
}

# Ver métricas finales
Invoke-RestMethod -Uri "https://localhost:7001/metrics" -Method GET -SkipCertificateCheck
```

### curl (Bash/CMD) - Operaciones y Transacciones
```bash
# Generar operaciones rápidas
for i in {1..10}; do
    curl -k -X POST https://localhost:7001/example/process \
         -H "Content-Type: application/json" \
         -d '{"operationType":"fast"}'
    echo "Fast operation $i completed"
    sleep 1
done

# Generar operaciones lentas
for i in {1..5}; do
    curl -k -X POST https://localhost:7001/example/process \
         -H "Content-Type: application/json" \
         -d '{"operationType":"slow"}'
    echo "Slow operation $i completed"
    sleep 2
done

# Generar transacciones premium
amounts=(1200 800 1500 300 2000 450 1100 600)
for i in {1..8}; do
    amount=${amounts[$((i-1))]}
    curl -k -X POST https://localhost:7001/example/transaction \
         -H "Content-Type: application/json" \
         -d "{\"amount\":$amount,\"accountType\":\"premium\"}"
    echo "Premium transaction $i completed: $amount"
    sleep 1
done

# Generar transacciones standard
for i in {1..12}; do
    amount=$((RANDOM % 1450 + 50))  # Entre 50 y 1500
    curl -k -X POST https://localhost:7001/example/transaction \
         -H "Content-Type: application/json" \
         -d "{\"amount\":$amount,\"accountType\":\"standard\"}"
    echo "Standard transaction $i completed: $amount"
    sleep 1
done

# Ver métricas finales
curl -k https://localhost:7001/metrics
```

### Windows Batch (.bat)
También puedes ejecutar el archivo `test-api.bat` incluido en el proyecto:

```batch
# Ejecutar desde la carpeta del proyecto
test-api.bat

# O desde PowerShell/CMD
.\test-api.bat
```

El archivo `.bat` incluye:
- 10 operaciones rápidas con pausa de 1 segundo
- 5 operaciones lentas con pausa de 2 segundos  
- 8 transacciones premium con montos predefinidos
- 12 transacciones standard con montos aleatorios
- Visualización final de métricas
- Manejo de errores y timeouts apropiados

## 🎯 Qué Observar en las Métricas

Después de ejecutar los requests, deberías ver en `/metrics`:

### Métricas de Operaciones Básicas
1. **example_requests_total**: Total de operaciones process (15 en el ejemplo básico)
2. **example_operations_total**: 
   - `{operation_type="fast"}`: 10
   - `{operation_type="slow"}`: 5
3. **example_processing_seconds**: Histograma con distribución de latencias

### Métricas de Transacciones (Labels Avanzados)
4. **example_transactions_total**: Total de transacciones procesadas (20 en el ejemplo completo)
5. **example_transactions_by_type_total**:
   - `{transaction_type="debito"}`: ~12-14 (según lógica de bifurcación)
   - `{transaction_type="credito"}`: ~6-8
6. **example_transactions_by_path_total** (múltiples labels):
   - `{path="premium",transaction_type="debito"}`: Montos ≥ $1000 en premium
   - `{path="premium",transaction_type="credito"}`: Montos < $1000 en premium  
   - `{path="standard",transaction_type="debito"}`: ~60% de transacciones standard
   - `{path="standard",transaction_type="credito"}`: ~40% de transacciones standard
7. **example_transaction_processing_seconds**: Histograma de latencias de transacciones

### Métricas de Errores
8. **example_errors_total**: Errores simulados (5% en operaciones, 3% en transacciones)

## 📋 Ejemplos de Respuestas

### Respuesta Exitosa - Operación
```json
{
    "success": true,
    "message": "Operación fast completada exitosamente",
    "processingTimeMs": 127,
    "timestamp": "2026-01-09T10:30:45.123Z"
}
```

### Respuesta Exitosa - Transacción Premium
```json
{
    "success": true,
    "transactionId": "A1B2C3D4",
    "amount": 1500.00,
    "transactionType": "debito",
    "path": "premium",
    "processingTimeMs": 234,
    "message": "Transacción debito de $1,500.00 procesada exitosamente en camino premium",
    "timestamp": "2026-01-09T10:30:45.456Z"
}
```

### Respuesta Exitosa - Transacción Standard
```json
{
    "success": true,
    "transactionId": "E5F6G7H8",
    "amount": 750.00,
    "transactionType": "credito",
    "path": "standard",
    "processingTimeMs": 312,
    "message": "Transacción credito de $750.00 procesada exitosamente en camino standard",
    "timestamp": "2026-01-09T10:30:45.789Z"
}
```

### Respuesta de Error - Validación
```json
{
    "error": "AccountType debe ser uno de: premium, standard"
}
```

## 🚨 Troubleshooting

### Certificado SSL en Desarrollo
Si tienes problemas con HTTPS, usa el flag `-k` en curl o `-SkipCertificateCheck` en PowerShell.

### Puerto Diferente
Si la aplicación usa un puerto diferente, revisa la consola donde ejecutaste `dotnet run` y ajusta las URLs.

### Error de Conexión
Verifica que la aplicación esté ejecutándose y que no haya conflictos de puerto.