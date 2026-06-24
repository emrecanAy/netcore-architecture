using MediatR;
using OrderSystem.Dto;

namespace OrderSystem.Commands.Products.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    int InitialStock) : IRequest<ProductDto>;
