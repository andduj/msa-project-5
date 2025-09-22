namespace BatchProcessing.Core.Models;

public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public Customer? Customer { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public string LoyaltyLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class DeliveryStatus
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public int? CourierRating { get; set; }
}

public class ProcessingResult
{
    public int Id { get; set; }
    public DateTime BatchDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public int HighValueOrders { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ProcessingType { get; set; } = string.Empty;
    public int ProcessingTimeSeconds { get; set; }
}
