namespace OrderSystem.Dto;

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int CurrencyId { get; set; }
    public string Currency { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
