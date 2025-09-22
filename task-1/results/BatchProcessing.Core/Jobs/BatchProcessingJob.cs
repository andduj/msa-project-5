using BatchProcessing.Core.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace BatchProcessing.Core.Jobs;

public class BatchProcessingJob
{
    private readonly IBatchProcessingService _batchProcessingService;
    private readonly ILogger<BatchProcessingJob> _logger;

    public BatchProcessingJob(IBatchProcessingService batchProcessingService, ILogger<BatchProcessingJob> logger)
    {
        _batchProcessingService = batchProcessingService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteBatchProcessingAsync()
    {
        _logger.LogInformation("=== Запуск задачи пакетной обработки данных ===");
        
        try
        {
            var result = await _batchProcessingService.ProcessDataBatchAsync();
            _logger.LogInformation($"Пакетная обработка завершена успешно. Тип: {result.ProcessingType}, Записей: {result.RecordsProcessed}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при выполнении пакетной обработки");
            throw; // Перебрасываем исключение для активации retry механизма Hangfire
        }
    }

    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 10, 30, 60, 120, 300 })]
    public async Task ExecuteUnreliableTaskAsync()
    {
        _logger.LogInformation("=== Запуск ненадежной задачи для демонстрации retry ===");
        
        // Симуляция случайных сбоев для демонстрации retry механизма
        var random = new Random();
        if (random.NextDouble() < 0.7) // 70% вероятность сбоя
        {
            _logger.LogError("Симуляция сбоя ненадежной задачи");
            throw new InvalidOperationException("Симуляция случайного сбоя для демонстрации retry механизма");
        }

        await Task.Delay(1000); // Добавляем await для устранения предупреждения
        _logger.LogInformation("Ненадежная задача выполнена успешно");
    }

    public async Task ExecuteFallbackTaskAsync()
    {
        _logger.LogInformation("=== Выполнение fallback задачи ===");
        
        try
        {
            // Симуляция альтернативной обработки данных
            await Task.Delay(2000);
            
            var fallbackResult = new BatchProcessingResult
            {
                ProcessingType = "fallback",
                RecordsProcessed = 0,
                Status = "fallback_executed",
                ProcessingDate = DateTime.Now,
                ErrorMessage = "Основная задача не выполнена, использована резервная логика"
            };

            await _batchProcessingService.SaveProcessingResultAsync(fallbackResult);
            await _batchProcessingService.SendEmailNotificationAsync(fallbackResult, false);
            
            _logger.LogInformation("Fallback задача выполнена успешно");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении fallback задачи");
            throw;
        }
    }

    public async Task ExecuteCleanupTaskAsync()
    {
        _logger.LogInformation("=== Выполнение задачи очистки ===");
        
        try
        {
            // Симуляция очистки временных файлов, кэша и т.д.
            await Task.Delay(1000);
            
            _logger.LogInformation("Задача очистки выполнена успешно");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении задачи очистки");
            // Не перебрасываем исключение, так как очистка не критична
        }
    }

    /// <summary>
    /// Демонстрационный workflow с retry и fallback логикой
    /// </summary>
    public void ScheduleRetryDemoWorkflow()
    {
        _logger.LogInformation("Запуск демонстрационного workflow с retry и fallback");

        // Планируем основную задачу
        var mainJobId = BackgroundJob.Enqueue(() => ExecuteUnreliableTaskAsync());

        // Планируем fallback задачу, которая выполнится только если основная провалится
        var fallbackJobId = BackgroundJob.ContinueJobWith(mainJobId, () => ExecuteFallbackTaskAsync());

        // Планируем задачу очистки, которая выполнится в любом случае
        BackgroundJob.ContinueJobWith(fallbackJobId, () => ExecuteCleanupTaskAsync());

        _logger.LogInformation($"Workflow запланирован. Основная задача ID: {mainJobId}");
    }
}
