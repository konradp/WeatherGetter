using Microsoft.EntityFrameworkCore;
namespace WeatherGetter
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }
        public DbSet<Weather> Weathers { get; set; }
        public DbSet<Location> Locations { get; set; }
    }
}