using BatchProcessing.Core.Data;
using BatchProcessing.Core.Jobs;
using BatchProcessing.Core.Services;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog для логирования
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/batch-processing-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление сервисов
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Batch Processing API", 
        Version = "v1",
        Description = "API для управления пакетной обработкой данных на C#/.NET с демонстрацией retry механизмов, fallback логики и ветвления пайплайнов"
    });
    
    // Включаем XML комментарии
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Настройка Entity Framework
builder.Services.AddDbContext<BatchProcessingContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new PostgreSqlStorageOptions
    {
        QueuePollInterval = TimeSpan.FromSeconds(15),
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        PrepareSchemaIfNecessary = true,
        TransactionSynchronisationTimeout = TimeSpan.FromMinutes(5)
    }));

// Hangfire Server (временно отключено для отладки)
// builder.Services.AddHangfireServer(options =>
// {
//     options.WorkerCount = Environment.ProcessorCount * 2;
//     options.Queues = new[] { "default", "critical", "batch-processing" };
// });

// Регистрация сервисов
builder.Services.AddScoped<IBatchProcessingService, BatchProcessingService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<BatchProcessingJob>();

var app = builder.Build();

// Настройка pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Batch Processing API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();

// Настройка Hangfire Dashboard (временно отключено для отладки)
// app.UseHangfireDashboard("/hangfire", new DashboardOptions
// {
//     Authorization = new[] { new AllowAllAuthorizationFilter() } // Только для демо!
// });

app.MapControllers();

// Автоматическое создание БД и применение миграций
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BatchProcessingContext>();
    context.Database.EnsureCreated();
}

// Планирование recurring jobs (временно отключено для отладки)
// RecurringJob.AddOrUpdate<BatchProcessingJob>(
//     "daily-batch-processing",
//     job => job.ExecuteBatchProcessingAsync(),
//     Cron.Daily(2, 0)); // Каждый день в 2:00

app.Run();

// Класс для разрешения доступа к Hangfire Dashboard (только для демо!)
public class AllowAllAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
