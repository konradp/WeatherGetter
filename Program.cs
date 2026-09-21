using Microsoft.EntityFrameworkCore;
using WeatherGetter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<WeatherService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var dbContext = sp.GetRequiredService<WeatherDbContext>();
    return new WeatherService(httpClient, dbContext);
});

builder.Services.AddDbContext<WeatherDbContext>
(
    options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AlternativeConnection"))
);

builder.Services.AddHostedService<WeatherBackgroundService>();
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
