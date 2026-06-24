using MediatR;
using OrderSystem.Dto;

namespace OrderSystem.Queries.Products.ListProducts;

public record ListProductsQuery(bool OnlyActive = false) : IRequest<IReadOnlyList<ProductDto>>;
