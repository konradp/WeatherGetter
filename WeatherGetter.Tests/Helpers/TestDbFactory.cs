using Microsoft.EntityFrameworkCore;

namespace WeatherGetter.Tests.Helpers;

internal static class TestDbFactory
{
    public static WeatherDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new WeatherDbContext(options);
    }
}
