using OrderSystem.Commands.Orders.PlaceOrder;
using OrderSystem.Commands.Products.CreateProduct;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;
using Xunit;

namespace OrderSystem.UnitTests.Validators;

public class CommandValidatorTests
{
    [Fact]
    public async Task CreateProduct_passes_when_sku_is_unique()
    {
        var validator = new CreateProductCommandValidator(TestRepository.Build(Array.Empty<Product>()).Object);
        var command = new CreateProductCommand("Keyboard", "KB-1", 100m, "USD", 5);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateProduct_fails_on_duplicate_sku()
    {
        var existing = new Product("Keyboard", new Sku("KB-1"), new Money(100m, "USD"), 5);
        var validator = new CreateProductCommandValidator(TestRepository.Build(new[] { existing }).Object);
        var command = new CreateProductCommand("Another", "kb-1", 50m, "USD", 1);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Sku));
    }

    [Fact]
    public async Task PlaceOrder_fails_on_empty_items()
    {
        var validator = new PlaceOrderCommandValidator(TestRepository.Build(Array.Empty<Product>()).Object);
        var command = new PlaceOrderCommand(Guid.NewGuid(), "USD", Array.Empty<PlaceOrderItem>());

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }
}
