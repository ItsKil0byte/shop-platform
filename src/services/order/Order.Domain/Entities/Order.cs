using Order.Domain.Enums;

namespace Order.Domain.Entities;

public class OrderEntity
{
    public Guid Id {get; private set;}
    public string UserId {get; private set;} = string.Empty;
    public OrderStatus Status {get; private set;}
    public decimal TotalPrice {get; private set;}
    public DateTime CreatedAt {get; private set;}

    private OrderEntity() {} // Для EF Core

    public OrderEntity(string userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Status = OrderStatus.Pending;
        TotalPrice = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateTotalPrice(decimal newTotalPrice)
    {
        TotalPrice = newTotalPrice;
    }
}