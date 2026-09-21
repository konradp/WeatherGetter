using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace WeatherGetter
{
    public class WeatherBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(14);

        public WeatherBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
                    var weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();

                    var locations = await dbContext.Locations.AsNoTracking().ToListAsync();

                    foreach (var location in locations)
                    {
                        string apiUrl = weatherService.ApiWeatherStringBuilder(location);
                        var data = await weatherService.GetWeatherAsync(apiUrl);
                        var doc = JsonDocument.Parse(data);
                        var weather = weatherService.JsonWeatherConverter(doc, location.Id);

                        await weatherService.PostWeather(weather);
                    }
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}