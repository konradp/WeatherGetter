# WeatherGetter

WeatherGetter is an ASP.NET Core Web API for retrieving, storing, and managing weather data for cities using the Open-Meteo API. It supports city geocoding, weather fetching, and database operations.


## Features

- Add cities to the database via geocoding API
- Fetch current weather for a city and store it
- Retrieve weather data by city or date
- List all cities and weather records
- Delete cities and their weather data
- Background weather collection with hosted service
- Async API and database operations (EF Core async queries)
- Swagger UI for API documentation
- Automated unit tests with xUnit and EF Core InMemory

## Technologies

- ASP.NET Core (.NET 8)
- Entity Framework Core (SQL Server)
- Swashbuckle (Swagger)
- Hosted background service for weather tasks
- xUnit (unit testing)
- EF Core InMemory provider (test database)
- coverlet.collector (test coverage data collector)

## Getting Started

1. Clone the repository.
2. Configure your SQL Server connection in `appsettings.json`.
3. Run database migrations if needed.
4. Start the API:
   ```pwsh
   dotnet run
   ```
5. Access Swagger UI at `http://localhost:5000/swagger`.

## Tests

Unit tests are located in the `WeatherGetter.Tests` project.

- Run all tests:
   ```pwsh
   dotnet test WeatherGetter.sln
   ```
- Run tests with detailed output:
   ```pwsh
   dotnet test WeatherGetter.sln -v normal
   ```

Current tests cover:

- `WeatherService` parsing and URL-building logic
- Duplicate weather record prevention logic
- `WeatherController` core flows (add city, list/filter data, delete city)

## API Endpoints

- `POST /Weather/Post-CityToDB?cityName={name}`: Add city to DB
- `GET /Weather/Get-AllCities`: List all cities
- `GET /Weather/Get-ByCity?cityName={name}`: Get weather by city
- `GET /Weather/Get-ByDate?date={date}`: Get weather by date
- `GET /Weather/Get-All-DB`: List all weather records
- `GET /Weather/Get-One-Test?cityName={name}`: Fetch and store weather for city
- `DELETE /Weather/Delete-City?cityName={name}`: Remove city and its weather

## License

BSD 3-Clause
