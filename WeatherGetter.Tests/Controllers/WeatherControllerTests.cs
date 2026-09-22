using System.Net;
using Microsoft.AspNetCore.Mvc;
using WeatherGetter.Controllers;
using WeatherGetter.Tests.Helpers;

namespace WeatherGetter.Tests.Controllers;

public class WeatherControllerTests
{
    [Fact]
    public async Task PostCityToDB_AddsNewLocation_WhenCityDoesNotExist()
    {
        using var db = TestDbFactory.CreateContext();
        var controller = CreateController(db);

        var result = await controller.PostCityToDB("Warsaw");

        var location = Assert.IsType<Location>(result.Value);
        Assert.Equal("Warsaw", location.City);
        Assert.Single(db.Locations);
    }

    [Fact]
    public async Task PostCityToDB_ReturnsExistingLocation_WhenCityExists()
    {
        using var db = TestDbFactory.CreateContext();
        var existing = new Location { City = "Warsaw", Latitude = 10.0, Longitude = 10.0 };
        await db.Locations.AddAsync(existing);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.PostCityToDB("Warsaw");

        var location = Assert.IsType<Location>(result.Value);
        Assert.Equal(existing.Id, location.Id);
        Assert.Single(db.Locations);
    }

    [Fact]
    public async Task GetAllLocations_ReturnsAllSavedLocations()
    {
        using var db = TestDbFactory.CreateContext();
        await db.Locations.AddRangeAsync(
            new Location { City = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 },
            new Location { City = "Krakow", Latitude = 50.0647, Longitude = 19.9450 }
        );
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetAllLocations();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetWeathersByDate_ReturnsOnlyMatchingDate()
    {
        using var db = TestDbFactory.CreateContext();
        var location = new Location { City = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };
        await db.Locations.AddAsync(location);
        await db.SaveChangesAsync();

        await db.Weathers.AddRangeAsync(
            new Weather { Location = location, DateOnly = new DateOnly(2026, 9, 22), TimeOnly = new TimeOnly(10, 0), TemperatureC = 12.0 },
            new Weather { Location = location, DateOnly = new DateOnly(2026, 9, 23), TimeOnly = new TimeOnly(10, 0), TemperatureC = 13.0 }
        );
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.GetWeathersByDate(new DateOnly(2026, 9, 22));

        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(new DateOnly(2026, 9, 22), list[0].DateOnly);
    }

    [Fact]
    public async Task DeleteCityFromDB_RemovesCityAndRelatedWeather()
    {
        using var db = TestDbFactory.CreateContext();
        var location = new Location { City = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };
        await db.Locations.AddAsync(location);
        await db.SaveChangesAsync();

        await db.Weathers.AddAsync(new Weather
        {
            Location = location,
            DateOnly = new DateOnly(2026, 9, 22),
            TimeOnly = new TimeOnly(14, 30),
            TemperatureC = 17.6
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.DeleteCityFromDB("Warsaw");

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(db.Locations);
        Assert.Empty(db.Weathers);
    }

    [Fact]
    public async Task DeleteCityFromDB_ReturnsBadRequest_WhenCityDoesNotExist()
    {
        using var db = TestDbFactory.CreateContext();
        var controller = CreateController(db);

        var result = await controller.DeleteCityFromDB("MissingCity");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static WeatherController CreateController(WeatherDbContext db)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri is not null && request.RequestUri.AbsoluteUri.Contains("geocoding-api.open-meteo.com", StringComparison.OrdinalIgnoreCase))
            {
                return FakeHttpMessageHandler.JsonResponse("""
                {
                  "results": [
                    { "name": "Warsaw", "latitude": 52.2297, "longitude": 21.0122 }
                  ]
                }
                """, HttpStatusCode.OK);
            }

            return FakeHttpMessageHandler.JsonResponse("""
            {
              "current": {
                "time": "2026-09-22T14:30",
                "temperature_2m": 17.6
              }
            }
            """, HttpStatusCode.OK);
        });

        var client = new HttpClient(handler);
        var service = new WeatherService(client, db);
        return new WeatherController(service, db);
    }
}
