using Microsoft.AspNetCore.Mvc;
using PruebaPrometheus.Application.Services;

namespace PruebaPrometheus.Controllers
{
    /// <summary>
    /// Controlador de ejemplo para demostrar métricas Prometheus
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ExampleController : ControllerBase
    {
        private readonly ExampleService _exampleService;
        private readonly ILogger<ExampleController> _logger;

        public ExampleController(ExampleService exampleService, ILogger<ExampleController> logger)
        {
            _exampleService = exampleService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para procesar operaciones y generar métricas
        /// </summary>
        /// <param name="request">Parámetros de la operación</param>
        /// <returns>Resultado del procesamiento</returns>
        [HttpPost("process")]
        public async Task<ActionResult<ProcessResult>> ProcessOperation([FromBody] ProcessRequest request)
        {
            try
            {
                // Validar tipo de operación
                if (string.IsNullOrWhiteSpace(request.OperationType))
                {
                    return BadRequest(new { error = "OperationType es requerido" });
                }

                var validTypes = new[] { "fast", "slow" };
                if (!validTypes.Contains(request.OperationType.ToLowerInvariant()))
                {
                    return BadRequest(new { 
                        error = $"OperationType debe ser uno de: {string.Join(", ", validTypes)}" 
                    });
                }

                _logger.LogInformation("Recibido request para operación: {OperationType}", request.OperationType);

                // Procesar operación usando el servicio
                var result = await _exampleService.ProcessOperationAsync(request.OperationType);

                // Retornar respuesta apropiada
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode(500, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en ProcessOperation");
                return StatusCode(500, new ProcessResult
                {
                    Success = false,
                    Message = "Error interno del servidor",
                    ProcessingTimeMs = 0,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Endpoint para procesar transacciones financieras con bifurcación de caminos
        /// </summary>
        /// <param name="request">Parámetros de la transacción</param>
        /// <returns>Resultado del procesamiento de la transacción</returns>
        [HttpPost("transaction")]
        public async Task<ActionResult<TransactionResult>> ProcessTransaction([FromBody] TransactionRequest request)
        {
            try
            {
                // Validar monto
                if (request.Amount <= 0)
                {
                    return BadRequest(new { error = "Amount debe ser mayor que 0" });
                }

                // Validar tipo de cuenta
                if (string.IsNullOrWhiteSpace(request.AccountType))
                {
                    return BadRequest(new { error = "AccountType es requerido" });
                }

                var validAccountTypes = new[] { "premium", "standard" };
                if (!validAccountTypes.Contains(request.AccountType.ToLowerInvariant()))
                {
                    return BadRequest(new { 
                        error = $"AccountType debe ser uno de: {string.Join(", ", validAccountTypes)}" 
                    });
                }

                _logger.LogInformation("Recibido request para transacción: Monto={Amount}, Cuenta={AccountType}", 
                    request.Amount, request.AccountType);

                // Procesar transacción usando el servicio
                var result = await _exampleService.ProcessTransactionAsync(request.Amount, request.AccountType);

                // Retornar respuesta apropiada
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode(500, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en ProcessTransaction");
                return StatusCode(500, new TransactionResult
                {
                    Success = false,
                    TransactionId = "ERROR",
                    Amount = request?.Amount ?? 0,
                    TransactionType = "unknown",
                    Path = "error",
                    ProcessingTimeMs = 0,
                    Message = "Error interno del servidor",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Endpoint de información básica
        /// </summary>
        /// <returns>Información sobre la API</returns>
        [HttpGet("info")]
        public ActionResult<object> GetInfo()
        {
            return Ok(new
            {
                service = "PruebaPrometheus",
                version = "1.0.0",
                description = "API de demostración para métricas Prometheus",
                endpoints = new
                {
                    metrics = "/metrics",
                    process = "/example/process",
                    transaction = "/example/transaction",
                    swagger = "/swagger"
                },
                supportedOperationTypes = new[] { "fast", "slow" },
                supportedAccountTypes = new[] { "premium", "standard" }
            });
        }
    }

    /// <summary>
    /// Request para el endpoint de procesamiento
    /// </summary>
    public class ProcessRequest
    {
        /// <summary>
        /// Tipo de operación: "fast" o "slow"
        /// </summary>
        public string OperationType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para el endpoint de transacciones
    /// </summary>
    public class TransactionRequest
    {
        /// <summary>
        /// Monto de la transacción
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Tipo de cuenta: "premium" o "standard"
        /// </summary>
        public string AccountType { get; set; } = string.Empty;
    }
}