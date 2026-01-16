using Prometheus;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using PruebaPrometheus.Application.Services;

// Configurar Serilog ANTES de crear el host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        uri: "http://localhost:3100",
        labels: new[]
        {
            new LokiLabel { Key = "job", Value = "prueba-prometheus" },
            new LokiLabel { Key = "env", Value = "development" },
            new LokiLabel { Key = "service", Value = "api" }
        }
    )
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Usar Serilog como logger del host
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Registrar el servicio de ejemplo
    builder.Services.AddScoped<ExampleService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Usar middleware de logging de Serilog para requests
    app.UseSerilogRequestLogging();

    // Middleware para capturar métricas HTTP automáticas
    app.UseHttpMetrics();

    app.UseAuthorization();

    app.MapControllers();

    // Exponer endpoint /metrics para Prometheus
    app.MapMetrics();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}