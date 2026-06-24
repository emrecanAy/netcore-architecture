namespace OrderSystem.Dto;

public class OrderDto : BaseResponseDto
{
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = default!;
    public int CurrencyId { get; set; }
    public string Currency { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
