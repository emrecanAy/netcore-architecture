using MediatR;
using OrderSystem.Dto;

namespace OrderSystem.Queries.Products.GetProduct;

public record GetProductQuery(Guid Id) : IRequest<ProductDto>;
