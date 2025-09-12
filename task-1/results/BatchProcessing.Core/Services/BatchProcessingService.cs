using System.Globalization;
using BatchProcessing.Core.Data;
using BatchProcessing.Core.Models;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;

namespace BatchProcessing.Core.Services;

public class BatchProcessingService : IBatchProcessingService
{
    private readonly BatchProcessingContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<BatchProcessingService> _logger;

    public BatchProcessingService(
        BatchProcessingContext context, 
        IEmailService emailService,
        ILogger<BatchProcessingService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<BatchProcessingResult> ProcessDataBatchAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Начало пакетной обработки данных");

        try
        {
            // 1. Чтение CSV данных
            var csvData = await ReadCsvDataAsync("Data/delivery_status.csv");
            _logger.LogInformation($"Загружено {csvData.Count} записей из CSV");

            // 2. Чтение данных из БД
            var dbData = await ReadDatabaseDataAsync();
            _logger.LogInformation($"Загружено {dbData.Count} записей из базы данных");

            var totalRecords = csvData.Count + dbData.Count;
            
            // 3. Анализ объема данных и ветвление
            BatchProcessingResult result;
            if (totalRecords > 20)
            {
                _logger.LogInformation("Выбрана тяжелая обработка данных");
                result = await ProcessHeavyDataAsync(dbData);
            }
            else
            {
                _logger.LogInformation("Выбрана легкая обработка данных");
                result = await ProcessLightDataAsync(totalRecords);
            }

            stopwatch.Stop();
            result.ProcessingTimeSeconds = (int)stopwatch.Elapsed.TotalSeconds;
            result.ProcessingDate = DateTime.UtcNow;

            // 4. Сохранение результатов
            await SaveProcessingResultAsync(result);

            // 5. Отправка уведомления об успехе
            await SendEmailNotificationAsync(result, true);

            _logger.LogInformation($"Пакетная обработка завершена успешно за {result.ProcessingTimeSeconds} секунд");
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Ошибка при пакетной обработке данных");
            
            var errorResult = new BatchProcessingResult
            {
                Status = "failed",
                ErrorMessage = ex.Message,
                ProcessingTimeSeconds = (int)stopwatch.Elapsed.TotalSeconds,
                ProcessingDate = DateTime.UtcNow
            };

            await SendEmailNotificationAsync(errorResult, false);
            throw;
        }
    }

    public async Task<List<DeliveryStatus>> ReadCsvDataAsync(string filePath)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    _logger.LogWarning($"Попытка {retryCount} чтения CSV файла через {delay.TotalSeconds} секунд");
                });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"CSV файл не найден: {filePath}. Создаю тестовые данные.");
                await CreateTestCsvFileAsync(filePath);
            }

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            var records = csv.GetRecords<DeliveryStatus>().ToList();
            _logger.LogInformation($"Успешно прочитано {records.Count} записей из CSV");
            return records;
        });
    }

    public async Task<List<Order>> ReadDatabaseDataAsync()
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    _logger.LogWarning($"Попытка {retryCount} подключения к БД через {delay.TotalSeconds} секунд");
                });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            _logger.LogInformation($"Успешно загружено {orders.Count} заказов из базы данных");
            return orders;
        });
    }

    public async Task<BatchProcessingResult> ProcessLightDataAsync(int recordCount)
    {
        _logger.LogInformation($"Выполняется легкая обработка для {recordCount} записей");
        
        // Симуляция легкой обработки
        await Task.Delay(2000);

        return new BatchProcessingResult
        {
            ProcessingType = "light",
            RecordsProcessed = recordCount,
            Status = "completed",
            TotalAmount = 0,
            HighValueOrders = 0,
            AverageOrderValue = 0
        };
    }

    public async Task<BatchProcessingResult> ProcessHeavyDataAsync(List<Order> orders)
    {
        _logger.LogInformation($"Выполняется тяжелая обработка для {orders.Count} заказов");
        
        // Симуляция сложной обработки
        await Task.Delay(5000);

        var totalAmount = orders.Sum(o => o.TotalAmount);
        var highValueOrders = orders.Count(o => o.TotalAmount > 10000);
        var avgOrderValue = orders.Count > 0 ? totalAmount / orders.Count : 0;

        _logger.LogInformation($"Обработано заказов на сумму: {totalAmount:C}");
        _logger.LogInformation($"Высокоценных заказов: {highValueOrders}");
        _logger.LogInformation($"Средняя стоимость заказа: {avgOrderValue:C}");

        return new BatchProcessingResult
        {
            ProcessingType = "heavy",
            RecordsProcessed = orders.Count,
            Status = "completed",
            TotalAmount = totalAmount,
            HighValueOrders = highValueOrders,
            AverageOrderValue = avgOrderValue
        };
    }

    public async Task SaveProcessingResultAsync(BatchProcessingResult result)
    {
        var processingResult = new ProcessingResult
        {
            BatchDate = result.ProcessingDate.Date,
            TotalOrders = result.RecordsProcessed,
            TotalAmount = result.TotalAmount,
            HighValueOrders = result.HighValueOrders,
            ProcessingStatus = result.Status,
            ProcessingType = result.ProcessingType,
            ProcessingTimeSeconds = result.ProcessingTimeSeconds,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProcessingResults.Add(processingResult);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Результаты обработки сохранены в базу данных");
    }

    public async Task SendEmailNotificationAsync(BatchProcessingResult result, bool isSuccess)
    {
        try
        {
            var subject = isSuccess 
                ? "✅ Пакетная обработка данных завершена успешно"
                : "❌ Ошибка при пакетной обработке данных";

            var body = isSuccess
                ? $@"
                <h2>Отчет о выполнении пакетной обработки</h2>
                <p><strong>Дата выполнения:</strong> {result.ProcessingDate:yyyy-MM-dd HH:mm:ss}</p>
                <p><strong>Тип обработки:</strong> {result.ProcessingType}</p>
                <p><strong>Количество обработанных записей:</strong> {result.RecordsProcessed}</p>
                <p><strong>Время обработки:</strong> {result.ProcessingTimeSeconds} секунд</p>
                <p><strong>Общая сумма заказов:</strong> {result.TotalAmount:C}</p>
                <p><strong>Высокоценных заказов:</strong> {result.HighValueOrders}</p>
                <p><strong>Средняя стоимость заказа:</strong> {result.AverageOrderValue:C}</p>
                <p><strong>Статус:</strong> <span style='color: green;'>{result.Status}</span></p>
                <hr>
                <p><em>Это автоматически сгенерированное сообщение от системы пакетной обработки C#.</em></p>
                "
                : $@"
                <h2>Ошибка при выполнении пакетной обработки</h2>
                <p><strong>Дата:</strong> {result.ProcessingDate:yyyy-MM-dd HH:mm:ss}</p>
                <p><strong>Статус:</strong> <span style='color: red;'>{result.Status}</span></p>
                <p><strong>Ошибка:</strong> {result.ErrorMessage}</p>
                <hr>
                <p><em>Проверьте логи для получения подробной информации.</em></p>
                ";

            await _emailService.SendEmailAsync("admin@company.com", subject, body);
            _logger.LogInformation("Email уведомление отправлено");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке email уведомления");
        }
    }

    private async Task CreateTestCsvFileAsync(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var testData = new List<DeliveryStatus>
        {
            new() { OrderId = 1, Status = "delivered", DeliveryDate = DateTime.Parse("2024-01-15"), DeliveryAddress = "г. Москва ул. Ленина д. 10", CourierRating = 5 },
            new() { OrderId = 2, Status = "delivered", DeliveryDate = DateTime.Parse("2024-01-16"), DeliveryAddress = "г. Санкт-Петербург пр. Невский д. 25", CourierRating = 4 },
            new() { OrderId = 3, Status = "in_transit", DeliveryAddress = "г. Екатеринбург ул. Мира д. 5" },
            new() { OrderId = 4, Status = "pending", DeliveryAddress = "г. Новосибирск ул. Советская д. 12" }
        };

        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(testData);
        
        _logger.LogInformation($"Создан тестовый CSV файл: {filePath}");
    }
}
