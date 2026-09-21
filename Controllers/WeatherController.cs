using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace WeatherGetter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;
        private readonly WeatherDbContext _dbContext;

        public WeatherController(WeatherService weatherService, WeatherDbContext dbContext)
        {
            _weatherService = weatherService;
            _dbContext = dbContext;
        }

        [HttpPost("Post-CityToDB", Name = "PostCityIntoDB")]
        public async Task<ActionResult<Location>> PostCityToDB(string cityName)
        {
            var cityFinderUrl = string.Format("https://geocoding-api.open-meteo.com/v1/search?name={0}&count=1&language=en&format=json", cityName);
            var cityData = await _weatherService.GetCityAsync(cityFinderUrl);
            var location = _weatherService.JsonLocationConverter(JsonDocument.Parse(cityData));

            if (location == null)
            {
                return NotFound();
            }

            //check if location exists
            if (await _dbContext.Locations.AnyAsync(x => x.City == location.City))
            {
                return await _dbContext.Locations.FirstAsync(x => x.City == location.City);
            }

            await _dbContext.Locations.AddAsync(location);
            await _dbContext.SaveChangesAsync();

            return location;
        }

        [HttpGet("Get-AllCities", Name = "GetAllCities")]
        public async Task<IEnumerable<Location>> GetAllLocations()
        {
            return await _dbContext.Locations.ToListAsync();
        }

        [HttpGet("Get-ByCity", Name = "GetWeatherByCity")]
        public async Task<IEnumerable<Weather>> GetWeathersByCity(string cityName)
        {
            return await _dbContext.Weathers.Where(x => x.Location.City == cityName).ToListAsync();
        }

        [HttpGet("Get-ByDate", Name = "GetWeatherAtDate")]
        public async Task<IEnumerable<Weather>> GetWeathersByDate(DateOnly date)
        {
            return await _dbContext.Weathers.Where(x => x.DateOnly == date).ToListAsync();
        }

        [HttpGet("Get-All-DB", Name = "GetWeatherFromDB")]
        public async Task<IEnumerable<Weather>> GetAllSavedWeathers()
        {
            return await _dbContext.Weathers.ToListAsync();
        }

        [HttpGet("Get-One-Test", Name = "GetWeatherFromAPI")]
        public async Task<Weather> Get(string cityName)
        {
            var cityFinderUrl = string.Format("https://geocoding-api.open-meteo.com/v1/search?name={0}&count=1&language=en&format=json", cityName);
            var cityData = await _weatherService.GetCityAsync(cityFinderUrl);
            var location = _weatherService.JsonLocationConverter(JsonDocument.Parse(cityData));

            //check if location exists
            if (await _dbContext.Locations.AnyAsync(x => x.City == location.City))
            {
                location = await _dbContext.Locations.FirstOrDefaultAsync(x => x.City == location.City);
            }

            var apiUrl = _weatherService.ApiWeatherStringBuilder(location);
            var data = await _weatherService.GetWeatherAsync(apiUrl);
            var doc = JsonDocument.Parse(data);
            var weather = _weatherService.JsonWeatherConverter(doc, location);

            _dbContext.Weathers.Add(weather);
            await _dbContext.SaveChangesAsync();
            return weather;
        }

        [HttpDelete("Delete-City", Name = "DeleteCityFromDB")]
        public async Task<IActionResult> DeleteCityFromDB(string cityName)
        {
            if (await _dbContext.Locations.AnyAsync(x => x.City == cityName))
            {
                var loc = await _dbContext.Locations.FirstAsync(x => x.City == cityName);
                var assignedTemperatures = _dbContext.Weathers.Where(x => x.Location == loc).ToList();

                _dbContext.Locations.Remove(loc);
                _dbContext.Weathers.RemoveRange(assignedTemperatures);
                await _dbContext.SaveChangesAsync();
                return Ok(new { message = $"City '{cityName}' and its weather data removed." });
            }
            else
            {
                return BadRequest(new { message = $"City '{cityName}' not found in database." });
            }
        }

    }
}
