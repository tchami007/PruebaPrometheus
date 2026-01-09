using Prometheus;
using PruebaPrometheus.Application.Services;

var builder = WebApplication.CreateBuilder(args);

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

// Middleware para capturar métricas HTTP automáticas
app.UseHttpMetrics();

app.UseAuthorization();

app.MapControllers();

// Exponer endpoint /metrics para Prometheus
app.MapMetrics();

app.Run();