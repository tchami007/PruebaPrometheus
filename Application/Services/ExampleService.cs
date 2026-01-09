using PruebaPrometheus.Observability.Metrics;
using System.Diagnostics;

namespace PruebaPrometheus.Application.Services
{
    /// <summary>
    /// Servicio de ejemplo que implementa lógica de negocio y usa métricas
    /// </summary>
    public class ExampleService
    {
        private readonly ILogger<ExampleService> _logger;
        private static readonly Random _random = new();

        public ExampleService(ILogger<ExampleService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Procesa una operación de ejemplo y registra métricas
        /// </summary>
        /// <param name="operationType">Tipo de operación: "fast" o "slow"</param>
        /// <returns>Resultado del procesamiento</returns>
        public async Task<ProcessResult> ProcessOperationAsync(string operationType)
        {
            // Incrementar contador total de requests
            PrometheusMetrics.RequestsTotal.Inc();

            // Incrementar contador por tipo de operación
            PrometheusMetrics.OperationsTotal.WithLabels(operationType).Inc();

            // Iniciar medición de tiempo
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Procesando operación tipo: {OperationType}", operationType);

                // Simular procesamiento según el tipo
                var processingTime = operationType.ToLowerInvariant() switch
                {
                    "fast" => TimeSpan.FromMilliseconds(_random.Next(50, 200)),
                    "slow" => TimeSpan.FromMilliseconds(_random.Next(500, 1500)),
                    _ => TimeSpan.FromMilliseconds(_random.Next(100, 300))
                };

                await Task.Delay(processingTime);

                // Simular error aleatorio (5% de probabilidad)
                if (_random.NextDouble() < 0.05)
                {
                    PrometheusMetrics.ErrorsTotal.Inc();
                    throw new InvalidOperationException($"Error simulado durante procesamiento {operationType}");
                }

                var result = new ProcessResult
                {
                    Success = true,
                    Message = $"Operación {operationType} completada exitosamente",
                    ProcessingTimeMs = (int)processingTime.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Operación {OperationType} completada en {ProcessingTime}ms", 
                    operationType, processingTime.TotalMilliseconds);

                // Registrar tiempo de procesamiento en histograma
                PrometheusMetrics.ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando operación {OperationType}", operationType);
                
                // Registrar tiempo incluso en caso de error
                PrometheusMetrics.ProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
                
                return new ProcessResult
                {
                    Success = false,
                    Message = $"Error en operación {operationType}: {ex.Message}",
                    ProcessingTimeMs = 0,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Procesa una transacción financiera con bifurcación de caminos y métricas detalladas
        /// </summary>
        /// <param name="amount">Monto de la transacción</param>
        /// <param name="accountType">Tipo de cuenta: "premium" o "standard"</param>
        /// <returns>Resultado del procesamiento de la transacción</returns>
        public async Task<TransactionResult> ProcessTransactionAsync(decimal amount, string accountType)
        {
            // Incrementar contador total de transacciones
            PrometheusMetrics.TransactionsTotal.Inc();

            // Iniciar medición de tiempo
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Iniciando procesamiento de transacción: Monto={Amount}, Cuenta={AccountType}", 
                    amount, accountType);

                // Decisión de camino basada en el tipo de cuenta
                string selectedPath;
                string transactionType;
                TimeSpan processingTime;

                var normalizedAccountType = accountType.ToLowerInvariant();
                
                if (normalizedAccountType == "premium")
                {
                    selectedPath = "premium";
                    // En cuentas premium, decidir tipo basado en el monto
                    if (amount >= 1000)
                    {
                        transactionType = "debito";
                        processingTime = TimeSpan.FromMilliseconds(_random.Next(100, 300)); // Débito premium es más lento
                    }
                    else
                    {
                        transactionType = "credito";
                        processingTime = TimeSpan.FromMilliseconds(_random.Next(50, 150)); // Crédito premium es rápido
                    }
                }
                else
                {
                    selectedPath = "standard";
                    // En cuentas estándar, decidir tipo basado en probabilidad
                    if (_random.NextDouble() < 0.6) // 60% débito, 40% crédito
                    {
                        transactionType = "debito";
                        processingTime = TimeSpan.FromMilliseconds(_random.Next(200, 500)); // Débito estándar es lento
                    }
                    else
                    {
                        transactionType = "credito";
                        processingTime = TimeSpan.FromMilliseconds(_random.Next(150, 350)); // Crédito estándar es moderado
                    }
                }

                // Registrar métricas por tipo de transacción
                PrometheusMetrics.TransactionsByType.WithLabels(transactionType).Inc();

                // Registrar métricas por camino y tipo (múltiples labels)
                PrometheusMetrics.TransactionsByPath.WithLabels(selectedPath, transactionType).Inc();

                _logger.LogInformation("Procesando {TransactionType} en camino {Path}, tiempo estimado: {Time}ms", 
                    transactionType, selectedPath, processingTime.TotalMilliseconds);

                // Simular el procesamiento
                await Task.Delay(processingTime);

                // Simular error ocasional (3% de probabilidad)
                if (_random.NextDouble() < 0.03)
                {
                    PrometheusMetrics.ErrorsTotal.Inc();
                    throw new InvalidOperationException($"Error simulado en transacción {transactionType} de {amount:C}");
                }

                var result = new TransactionResult
                {
                    Success = true,
                    TransactionId = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    Amount = amount,
                    TransactionType = transactionType,
                    Path = selectedPath,
                    ProcessingTimeMs = (int)processingTime.TotalMilliseconds,
                    Message = $"Transacción {transactionType} de {amount:C} procesada exitosamente en camino {selectedPath}",
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Transacción {TransactionId} completada: {Type} en {Path} - {Time}ms", 
                    result.TransactionId, transactionType, selectedPath, processingTime.TotalMilliseconds);

                // Registrar tiempo de procesamiento en histograma
                PrometheusMetrics.TransactionProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando transacción de {Amount}", amount);
                
                // Registrar tiempo incluso en caso de error
                PrometheusMetrics.TransactionProcessingDuration.Observe(stopwatch.Elapsed.TotalSeconds);
                
                return new TransactionResult
                {
                    Success = false,
                    TransactionId = "ERROR",
                    Amount = amount,
                    TransactionType = "unknown",
                    Path = "error",
                    ProcessingTimeMs = 0,
                    Message = $"Error procesando transacción: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }

    /// <summary>
    /// Resultado del procesamiento de una operación
    /// </summary>
    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessingTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Resultado del procesamiento de una transacción financiera
    /// </summary>
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "debito" o "credito"
        public string Path { get; set; } = string.Empty; // "premium" o "standard"
        public int ProcessingTimeMs { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}