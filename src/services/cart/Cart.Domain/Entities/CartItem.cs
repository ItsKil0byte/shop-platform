namespace Cart.Domain.Entities;

public class CartItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}