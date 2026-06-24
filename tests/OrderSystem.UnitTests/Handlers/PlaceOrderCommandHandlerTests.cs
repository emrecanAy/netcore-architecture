using Moq;
using OrderSystem.Commands.Orders.PlaceOrder;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.DomainEvents;
using OrderSystem.Models.ValueObjects;
using Xunit;

namespace OrderSystem.UnitTests.Handlers;

public class PlaceOrderCommandHandlerTests
{
    private const int Usd = 1;

    private static Mock<OrderSystem.Repositories.Abstract.ISQLRepository<Currency>> Currencies() =>
        TestRepository.Build(new[] { new Currency(Usd, "USD", "US Dollar") });

    [Fact]
    public async Task Handle_builds_order_snapshots_price_and_raises_placed_event()
    {
        var product = new Product("Keyboard", new Sku("KB-1"), new Money(100m, Usd), 10);
        var productsRepo = TestRepository.Build(new[] { product });
        var ordersRepo = TestRepository.Build(Array.Empty<Order>());

        Order? added = null;
        ordersRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => added = o)
            .Returns(Task.CompletedTask);

        var handler = new PlaceOrderCommandHandler(productsRepo.Object, ordersRepo.Object, Currencies().Object);
        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            "USD",
            new[] { new PlaceOrderItem(product.Id, 3) });

        var dto = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Placed", dto.Status);
        Assert.Equal(300m, dto.TotalAmount);
        Assert.Equal(Usd, dto.CurrencyId);
        Assert.Equal("USD", dto.Currency);
        Assert.Single(dto.Items);
        Assert.Equal(100m, dto.Items[0].UnitPrice);

        Assert.NotNull(added);
        Assert.Contains(added!.DomainEvents, e => e is OrderPlacedDomainEvent);
        ordersRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_when_product_missing()
    {
        var productsRepo = TestRepository.Build(Array.Empty<Product>());
        var ordersRepo = TestRepository.Build(Array.Empty<Order>());

        var handler = new PlaceOrderCommandHandler(productsRepo.Object, ordersRepo.Object, Currencies().Object);
        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            "USD",
            new[] { new PlaceOrderItem(Guid.NewGuid(), 1) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
