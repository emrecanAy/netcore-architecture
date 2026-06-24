using MockQueryable;
using Moq;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.UnitTests;

/// <summary>
/// Builds an <see cref="ISQLRepository{T}"/> mock whose IQueryable members are
/// backed by an async-enabled in-memory queryable (MockQueryable), so handler
/// and validator EF LINQ (FirstOrDefaultAsync, AnyAsync, ...) work in tests.
/// </summary>
public static class TestRepository
{
    public static Mock<ISQLRepository<T>> Build<T>(IEnumerable<T> data) where T : class
    {
        var queryable = data.AsQueryable().BuildMock();

        var mock = new Mock<ISQLRepository<T>>();
        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        return mock;
    }
}
