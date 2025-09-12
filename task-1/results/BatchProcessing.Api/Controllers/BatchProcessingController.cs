using BatchProcessing.Core.Jobs;
using BatchProcessing.Core.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace BatchProcessing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchProcessingController : ControllerBase
{
    private readonly IBatchProcessingService _batchProcessingService;
    private readonly ILogger<BatchProcessingController> _logger;

    public BatchProcessingController(
        IBatchProcessingService batchProcessingService,
        ILogger<BatchProcessingController> logger)
    {
        _batchProcessingService = batchProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// Запуск пакетной обработки данных вручную
    /// </summary>
    /// <remarks>
    /// Выполняет полный цикл пакетной обработки данных:
    /// 1. Читает данные из CSV файла (delivery_status.csv)
    /// 2. Читает данные из PostgreSQL базы данных (таблица Orders)
    /// 3. Анализирует объем данных и выбирает стратегию обработки:
    ///    - Если записей > 20: тяжелая обработка (анализ заказов, расчет статистики)
    ///    - Если записей ≤ 20: легкая обработка (базовая обработка)
    /// 4. Сохраняет результаты в базу данных
    /// 5. Отправляет email уведомление о результатах
    /// 
    /// Демонстрирует ветвление пайплайна на основе условий.
    /// </remarks>
    /// <returns>Результат обработки с детальной информацией</returns>
    /// <response code="200">Обработка выполнена успешно</response>
    /// <response code="500">Ошибка при выполнении обработки</response>
    [HttpPost("start")]
    public async Task<IActionResult> StartBatchProcessing()
    {
        try
        {
            _logger.LogInformation("Запуск пакетной обработки данных...");
            
            // Выполняем обработку синхронно для демонстрации
            var result = await _batchProcessingService.ProcessDataBatchAsync();
            
            _logger.LogInformation($"Пакетная обработка завершена успешно. Тип: {result.ProcessingType}, Записей: {result.RecordsProcessed}");
            
            return Ok(new { 
                message = "Пакетная обработка выполнена успешно", 
                result = result,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении пакетной обработки");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Демонстрация retry механизма с ненадежной задачей
    /// </summary>
    /// <remarks>
    /// Демонстрирует работу retry политики с экспоненциальной задержкой:
    /// 1. Выполняет ненадежную задачу с 70% вероятностью сбоя
    /// 2. При сбое автоматически повторяет попытку до 5 раз
    /// 3. Использует экспоненциальную задержку между попытками (2, 4, 8, 16, 32 секунды)
    /// 4. Логирует каждую попытку и результат
    /// 
    /// Показывает надежность системы при работе с нестабильными внешними сервисами.
    /// </remarks>
    /// <returns>Результат выполнения с информацией о попытках</returns>
    /// <response code="200">Задача выполнена (успешно или после всех попыток)</response>
    /// <response code="500">Критическая ошибка системы</response>
    [HttpPost("demo-retry")]
    public async Task<IActionResult> DemoRetryMechanism()
    {
        try
        {
            _logger.LogInformation("Запуск демо задачи с retry механизмом...");
            
            // Симуляция retry логики
            var maxAttempts = 5;
            var attempt = 0;
            var success = false;
            var lastError = "";
            
            while (attempt < maxAttempts && !success)
            {
                attempt++;
                _logger.LogInformation($"Попытка {attempt} из {maxAttempts}");
                
                try
                {
                    // Симуляция ненадежной задачи (70% вероятность сбоя)
                    var random = new Random();
                    if (random.NextDouble() < 0.7)
                    {
                        throw new InvalidOperationException($"Симуляция сбоя на попытке {attempt}");
                    }
                    
                    success = true;
                    _logger.LogInformation($"Задача выполнена успешно на попытке {attempt}");
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogWarning($"Попытка {attempt} неудачна: {ex.Message}");
                    
                    if (attempt < maxAttempts)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Экспоненциальная задержка
                        _logger.LogInformation($"Ожидание {delay.TotalSeconds} секунд перед следующей попыткой...");
                        await Task.Delay(delay);
                    }
                }
            }
            
            return Ok(new { 
                message = success ? "Демо задача выполнена успешно" : "Демо задача провалилась после всех попыток",
                success = success,
                attempts = attempt,
                maxAttempts = maxAttempts,
                lastError = lastError,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении демо retry задачи");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Демонстрация workflow с fallback логикой
    /// </summary>
    /// <remarks>
    /// Демонстрирует сложный workflow с fallback логикой:
    /// 1. Выполняет основную задачу (50% вероятность сбоя)
    /// 2. При сбое основной задачи автоматически запускает fallback задачу
    /// 3. В любом случае выполняет задачу очистки ресурсов
    /// 4. Логирует каждый шаг workflow
    /// 5. Возвращает детальную информацию о выполнении всех шагов
    /// 
    /// Показывает надежность системы с резервными стратегиями обработки.
    /// </remarks>
    /// <returns>Результат выполнения workflow с деталями всех шагов</returns>
    /// <response code="200">Workflow завершен (успешно или с fallback)</response>
    /// <response code="500">Критическая ошибка системы</response>
    [HttpPost("demo-workflow")]
    public async Task<IActionResult> DemoWorkflowWithFallback()
    {
        try
        {
            _logger.LogInformation("Запуск демонстрационного workflow с fallback логикой...");
            
            var workflowSteps = new List<string>();
            var success = false;
            
            // Шаг 1: Основная задача
            workflowSteps.Add("1. Запуск основной задачи");
            _logger.LogInformation("Выполнение основной задачи...");
            
            try
            {
                // Симуляция основной задачи (50% вероятность сбоя)
                var random = new Random();
                if (random.NextDouble() < 0.5)
                {
                    throw new InvalidOperationException("Основная задача провалилась");
                }
                
                workflowSteps.Add("✅ Основная задача выполнена успешно");
                success = true;
                _logger.LogInformation("Основная задача выполнена успешно");
            }
            catch (Exception ex)
            {
                workflowSteps.Add($"❌ Основная задача провалилась: {ex.Message}");
                _logger.LogWarning($"Основная задача провалилась: {ex.Message}");
                
                // Шаг 2: Fallback задача
                workflowSteps.Add("2. Запуск fallback задачи");
                _logger.LogInformation("Выполнение fallback задачи...");
                
                try
                {
                    await Task.Delay(2000); // Симуляция fallback обработки
                    workflowSteps.Add("✅ Fallback задача выполнена успешно");
                    success = true;
                    _logger.LogInformation("Fallback задача выполнена успешно");
                }
                catch (Exception fallbackEx)
                {
                    workflowSteps.Add($"❌ Fallback задача провалилась: {fallbackEx.Message}");
                    _logger.LogError(fallbackEx, "Fallback задача провалилась");
                }
            }
            
            // Шаг 3: Задача очистки (всегда выполняется)
            workflowSteps.Add("3. Выполнение задачи очистки");
            _logger.LogInformation("Выполнение задачи очистки...");
            
            await Task.Delay(1000); // Симуляция очистки
            workflowSteps.Add("✅ Задача очистки выполнена");
            _logger.LogInformation("Задача очистки выполнена");
            
            return Ok(new { 
                message = "Демонстрационный workflow завершен",
                success = success,
                workflowSteps = workflowSteps,
                timestamp = DateTime.UtcNow,
                note = "Workflow включает основную задачу, fallback и очистку"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получение статуса системы
    /// </summary>
    /// <remarks>
    /// Возвращает текущий статус системы пакетной обработки данных.
    /// Показывает, что система работает и готова к выполнению задач.
    /// </remarks>
    /// <returns>Статус системы и ссылка на dashboard</returns>
    /// <response code="200">Система работает нормально</response>
    [HttpGet("status")]
    public IActionResult GetJobsStatus()
    {
        try
        {
            return Ok(new
            {
                message = "Система работает",
                dashboardUrl = "/hangfire",
                timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статуса задач");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получение информации о системе
    /// </summary>
    /// <remarks>
    /// Возвращает подробную информацию о системе пакетной обработки данных:
    /// - Версия системы и используемые технологии
    /// - Список доступных функций
    /// - Ссылки на интерфейсы (Swagger, Hangfire Dashboard)
    /// 
    /// Полезно для диагностики и понимания возможностей системы.
    /// </remarks>
    /// <returns>Детальная информация о системе</returns>
    /// <response code="200">Информация получена успешно</response>
    [HttpGet("info")]
    public IActionResult GetSystemInfo()
    {
        return Ok(new
        {
            system = "Batch Processing System на C#/.NET",
            version = "1.0.0",
            framework = ".NET 8.0",
            processingEngine = "Синхронная обработка с retry логикой",
            database = "PostgreSQL",
            features = new[]
            {
                "Пакетная обработка данных из CSV и PostgreSQL",
                "Ветвление пайплайна на основе объема данных", 
                "Retry механизмы с экспоненциальной задержкой", 
                "Fallback логика и workflow",
                "Email уведомления через MailHog",
                "Swagger API документация",
                "Масштабируемая архитектура"
            },
            endpoints = new
            {
                swagger = "/swagger",
                api = "/api/batchprocessing",
                mailhog = "http://localhost:8025",
                pgadmin = "http://localhost:8080"
            }
        });
    }
}
