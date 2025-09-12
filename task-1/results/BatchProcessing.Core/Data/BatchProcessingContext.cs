using Microsoft.EntityFrameworkCore;
using BatchProcessing.Core.Models;

namespace BatchProcessing.Core.Data;

public class BatchProcessingContext : DbContext
{
    public BatchProcessingContext(DbContextOptions<BatchProcessingContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<ProcessingResult> ProcessingResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(e => e.CustomerId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.LoyaltyLevel).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<ProcessingResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasPrecision(12, 2);
            entity.Property(e => e.ProcessingStatus).HasMaxLength(50);
            entity.Property(e => e.ProcessingType).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Seed data
        modelBuilder.Entity<Customer>().HasData(
            new Customer { CustomerId = 1, FirstName = "Иван", LastName = "Петров", Email = "ivan.petrov@example.com", RegistrationDate = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc), LoyaltyLevel = "gold", CreatedAt = DateTime.UtcNow },
            new Customer { CustomerId = 2, FirstName = "Мария", LastName = "Сидорова", Email = "maria.sidorova@example.com", RegistrationDate = new DateTime(2023, 2, 20, 0, 0, 0, DateTimeKind.Utc), LoyaltyLevel = "silver", CreatedAt = DateTime.UtcNow },
            new Customer { CustomerId = 3, FirstName = "Алексей", LastName = "Козлов", Email = "alexey.kozlov@example.com", RegistrationDate = new DateTime(2023, 3, 10, 0, 0, 0, DateTimeKind.Utc), LoyaltyLevel = "bronze", CreatedAt = DateTime.UtcNow },
            new Customer { CustomerId = 4, FirstName = "Елена", LastName = "Васильева", Email = "elena.vasileva@example.com", RegistrationDate = new DateTime(2023, 4, 5, 0, 0, 0, DateTimeKind.Utc), LoyaltyLevel = "platinum", CreatedAt = DateTime.UtcNow },
            new Customer { CustomerId = 5, FirstName = "Дмитрий", LastName = "Смирнов", Email = "dmitry.smirnov@example.com", RegistrationDate = new DateTime(2023, 5, 12, 0, 0, 0, DateTimeKind.Utc), LoyaltyLevel = "bronze", CreatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<Order>().HasData(
            new Order { OrderId = 1, CustomerId = 1, OrderDate = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 15000.00m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 2, CustomerId = 2, OrderDate = new DateTime(2024, 1, 11, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 8500.50m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 3, CustomerId = 3, OrderDate = new DateTime(2024, 1, 12, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 3200.75m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 4, CustomerId = 4, OrderDate = new DateTime(2024, 1, 13, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 25000.00m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 5, CustomerId = 5, OrderDate = new DateTime(2024, 1, 14, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 1500.25m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 6, CustomerId = 1, OrderDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 12000.00m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 7, CustomerId = 2, OrderDate = new DateTime(2024, 1, 16, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 6750.30m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 8, CustomerId = 3, OrderDate = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 2800.90m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 9, CustomerId = 4, OrderDate = new DateTime(2024, 1, 18, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 35000.00m, Status = "completed", CreatedAt = DateTime.UtcNow },
            new Order { OrderId = 10, CustomerId = 5, OrderDate = new DateTime(2024, 1, 19, 0, 0, 0, DateTimeKind.Utc), TotalAmount = 9200.45m, Status = "completed", CreatedAt = DateTime.UtcNow }
        );
    }
}
