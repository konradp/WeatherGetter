using System.Globalization;
using System.Text.Json;
using WeatherGetter.Tests.Helpers;

namespace WeatherGetter.Tests.Services;

public class WeatherServiceTests
{
    [Fact]
    public void ApiWeatherStringBuilder_UsesInvariantCultureForCoordinates()
    {
        using var db = TestDbFactory.CreateContext();
        var client = new HttpClient(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("{}")));
        var service = new WeatherService(client, db);
        var location = new Location { City = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
        CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");

        try
        {
            var url = service.ApiWeatherStringBuilder(location);

            Assert.Contains("latitude=52.23", url);
            Assert.Contains("longitude=21.012", url);
            Assert.DoesNotContain(",", url);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void JsonLocationConverter_ReturnsFirstResult()
    {
        using var db = TestDbFactory.CreateContext();
        var client = new HttpClient(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("{}")));
        var service = new WeatherService(client, db);
        using var doc = JsonDocument.Parse("""
        {
          "results": [
            { "name": "Warsaw", "latitude": 52.2297, "longitude": 21.0122 },
            { "name": "Krakow", "latitude": 50.0647, "longitude": 19.9450 }
          ]
        }
        """);

        var result = service.JsonLocationConverter(doc);

        Assert.Equal("Warsaw", result.City);
        Assert.Equal(52.2297, result.Latitude, 4);
        Assert.Equal(21.0122, result.Longitude, 4);
    }

    [Fact]
    public void JsonLocationConverter_ThrowsWhenResultsMissing()
    {
        using var db = TestDbFactory.CreateContext();
        var client = new HttpClient(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("{}")));
        var service = new WeatherService(client, db);
        using var doc = JsonDocument.Parse("{\"results\": []}");

        Assert.Throws<KeyNotFoundException>(() => service.JsonLocationConverter(doc));
    }

    [Fact]
    public void JsonWeatherConverter_WithLocation_MapsFields()
    {
        using var db = TestDbFactory.CreateContext();
        var client = new HttpClient(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("{}")));
        var service = new WeatherService(client, db);
        var location = new Location { City = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };
        using var doc = JsonDocument.Parse("""
        {
          "current": {
            "time": "2026-09-22T14:30",
            "temperature_2m": 17.6
          }
        }
        """);

        var weather = service.JsonWeatherConverter(doc, location);

        Assert.Equal(location, weather.Location);
        Assert.Equal(new DateOnly(2026, 9, 22), weather.DateOnly);
        Assert.Equal(new TimeOnly(14, 30), weather.TimeOnly);
        Assert.Equal(17.6, weather.TemperatureC, 3);
    }

    [Fact]
    public async Task PostWeather_AddsRecordWhenNoDuplicateExists()
    {
        using var db = TestDbFactory.CreateContext();
        var client = new HttpClient(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("{}")));
        var service = new WeatherService(client, db);

        var location = new Location { City = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };
        await db.Locations.AddAsync(location);
        await db.SaveChangesAsync();

        var weather = new Weather
        {
            Location = location,
            DateOnly = new DateOnly(2026, 9, 22),
            TimeOnly = new TimeOnly(14, 30),
            TemperatureC = 17.6
        };

        await service.PostWeather(weather);

        Assert.Single(db.Weathers);
    }

    [Fact]
    public async Task PostWeather_DoesNotAddDuplicateRecord()
    {
        using var db = TestDbFactory.CreateContext();
        var client = new HttpClient(new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse("{}")));
        var service = new WeatherService(client, db);

        var location = new Location { City = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };
        await db.Locations.AddAsync(location);

        await db.Weathers.AddAsync(new Weather
        {
            Location = location,
            DateOnly = new DateOnly(2026, 9, 22),
            TimeOnly = new TimeOnly(14, 30),
            TemperatureC = 17.0
        });
        await db.SaveChangesAsync();

        var duplicate = new Weather
        {
            Location = location,
            DateOnly = new DateOnly(2026, 9, 22),
            TimeOnly = new TimeOnly(14, 30),
            TemperatureC = 19.0
        };

        await service.PostWeather(duplicate);

        Assert.Single(db.Weathers);
    }
}
