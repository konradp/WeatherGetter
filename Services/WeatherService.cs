using System.Text.Json;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace WeatherGetter
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly WeatherDbContext _weatherDb;

        public WeatherService(HttpClient httpClient, WeatherDbContext weatherDb)
        {
            _httpClient = httpClient;
            _weatherDb = weatherDb;
        }

        public async Task<string> GetWeatherAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetCityAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task PostWeather(Weather weather)
        {
            //check for duplicates
            if (await _weatherDb.Weathers.AnyAsync(x => x.Location.City == weather.Location.City
            && x.DateOnly == weather.DateOnly
            && x.TimeOnly == weather.TimeOnly))
            {
                return;
            }

            await _weatherDb.Weathers.AddAsync(weather);
            await _weatherDb.SaveChangesAsync();
        }

        public string ApiWeatherStringBuilder(Location loc)
        {
            return string.Format(
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m&timezone=auto&forecast_days=1",
                loc.Latitude.ToString("0.###", CultureInfo.InvariantCulture),
                loc.Longitude.ToString("0.###", CultureInfo.InvariantCulture)
            );
        }

        public Location JsonLocationConverter(JsonDocument doc)
        {
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                throw new KeyNotFoundException("No results found for the given city.");
            }

            var firstResult = results[0];
            return new Location
            {
                City = firstResult.GetProperty("name").GetString(),
                Latitude = firstResult.GetProperty("latitude").GetDouble(),
                Longitude = firstResult.GetProperty("longitude").GetDouble()
            };
        }

        public Weather JsonWeatherConverter(JsonDocument doc, Location location)
        {
            Weather ret = new Weather();
            ret.Location = location;
            var dateTime = doc.RootElement.GetProperty("current").GetProperty("time").GetDateTime();
            ret.DateOnly = DateOnly.FromDateTime(dateTime);
            ret.TimeOnly = TimeOnly.FromDateTime(dateTime);
            ret.TemperatureC = doc.RootElement.GetProperty("current").GetProperty("temperature_2m").GetDouble();
            return ret;
        }

        public Weather JsonWeatherConverter(JsonDocument doc, int locationID)
        {
            Weather ret = new Weather();
            ret.Location = _weatherDb.Locations.First(x => x.Id == locationID);
            var dateTime = doc.RootElement.GetProperty("current").GetProperty("time").GetDateTime();
            ret.DateOnly = DateOnly.FromDateTime(dateTime);
            ret.TimeOnly = TimeOnly.FromDateTime(dateTime);
            ret.TemperatureC = doc.RootElement.GetProperty("current").GetProperty("temperature_2m").GetDouble();
            return ret;
        }
    }
}