namespace OrderSystem.Dto;

public class ProductDto : BaseResponseDto
{
    public string Name { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = default!;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}
