namespace Order.Application.DTOs
{
    public class CartItemDto
    {
        public required string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}