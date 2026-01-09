using Prometheus;

namespace PruebaPrometheus.Observability.Metrics
{
    /// <summary>
    /// Definición centralizada de métricas personalizadas para Prometheus
    /// </summary>
    public static class PrometheusMetrics
    {
        /// <summary>
        /// Contador total de requests procesados
        /// </summary>
        public static readonly Counter RequestsTotal = Prometheus.Metrics
            .CreateCounter(
                "example_requests_total", 
                "Cantidad total de requests procesados"
            );

        /// <summary>
        /// Contador de operaciones por tipo (fast/slow)
        /// </summary>
        public static readonly Counter OperationsTotal = Prometheus.Metrics
            .CreateCounter(
                "example_operations_total", 
                "Cantidad total de operaciones por tipo",
                new[] { "operation_type" }
            );

        /// <summary>
        /// Histograma para medir tiempo de procesamiento
        /// </summary>
        public static readonly Histogram ProcessingDuration = Prometheus.Metrics
            .CreateHistogram(
                "example_processing_seconds", 
                "Tiempo de procesamiento de cada request en segundos",
                new HistogramConfiguration
                {
                    // Buckets personalizados para latencia (en segundos)
                    Buckets = Histogram.LinearBuckets(0.1, 0.1, 10) // 0.1s a 1.0s
                }
            );

        /// <summary>
        /// Contador de errores
        /// </summary>
        public static readonly Counter ErrorsTotal = Prometheus.Metrics
            .CreateCounter(
                "example_errors_total", 
                "Cantidad total de errores ocurridos"
            );

        /// <summary>
        /// Contador total de transacciones procesadas
        /// </summary>
        public static readonly Counter TransactionsTotal = Prometheus.Metrics
            .CreateCounter(
                "example_transactions_total", 
                "Cantidad total de transacciones procesadas"
            );

        /// <summary>
        /// Contador de transacciones por tipo (debito/credito)
        /// </summary>
        public static readonly Counter TransactionsByType = Prometheus.Metrics
            .CreateCounter(
                "example_transactions_by_type_total", 
                "Cantidad total de transacciones por tipo",
                new[] { "transaction_type" }
            );

        /// <summary>
        /// Contador de transacciones por camino de decisión
        /// </summary>
        public static readonly Counter TransactionsByPath = Prometheus.Metrics
            .CreateCounter(
                "example_transactions_by_path_total", 
                "Cantidad total de transacciones por camino de decisión",
                new[] { "path", "transaction_type" }
            );

        /// <summary>
        /// Histograma para medir tiempo de procesamiento de transacciones
        /// </summary>
        public static readonly Histogram TransactionProcessingDuration = Prometheus.Metrics
            .CreateHistogram(
                "example_transaction_processing_seconds", 
                "Tiempo de procesamiento de transacciones en segundos",
                new HistogramConfiguration
                {
                    // Buckets para transacciones (pueden ser más lentas que operaciones simples)
                    Buckets = Histogram.LinearBuckets(0.1, 0.2, 15) // 0.1s a 3.0s
                }
            );
    }
}