using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSystem.Commands.Products.CreateProduct;
using OrderSystem.Dto;
using OrderSystem.Queries.Products.GetProduct;
using OrderSystem.Queries.Products.ListProducts;

namespace OrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductCommand command)
    {
        var product = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id) =>
        Ok(await _mediator.Send(new GetProductQuery(id)));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List([FromQuery] bool onlyActive = false) =>
        Ok(await _mediator.Send(new ListProductsQuery(onlyActive)));
}
