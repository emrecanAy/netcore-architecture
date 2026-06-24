using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSystem.Commands.Orders.PayOrder;
using OrderSystem.Commands.Orders.PlaceOrder;
using OrderSystem.Dto;
using OrderSystem.Queries.Orders.GetOrder;
using OrderSystem.Queries.Orders.ListOrders;

namespace OrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Place(PlaceOrderCommand command)
    {
        var order = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<OrderDto>> Pay(Guid id) =>
        Ok(await _mediator.Send(new PayOrderCommand(id)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id) =>
        Ok(await _mediator.Send(new GetOrderQuery(id)));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> List([FromQuery] string? status = null) =>
        Ok(await _mediator.Send(new ListOrdersQuery(status)));
}
