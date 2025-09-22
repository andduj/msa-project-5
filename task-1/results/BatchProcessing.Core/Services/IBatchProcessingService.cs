using BatchProcessing.Core.Models;

namespace BatchProcessing.Core.Services;

public interface IBatchProcessingService
{
    Task<BatchProcessingResult> ProcessDataBatchAsync();
    Task<BatchProcessingResult> ProcessLightDataAsync(int recordCount);
    Task<BatchProcessingResult> ProcessHeavyDataAsync(List<Order> orders);
    Task<List<DeliveryStatus>> ReadCsvDataAsync(string filePath);
    Task<List<Order>> ReadDatabaseDataAsync();
    Task SaveProcessingResultAsync(BatchProcessingResult result);
    Task SendEmailNotificationAsync(BatchProcessingResult result, bool isSuccess);
}

public class BatchProcessingResult
{
    public string ProcessingType { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public decimal TotalAmount { get; set; }
    public int HighValueOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int ProcessingTimeSeconds { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessingDate { get; set; }
    public string? ErrorMessage { get; set; }
}
