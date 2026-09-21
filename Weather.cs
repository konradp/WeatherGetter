namespace WeatherGetter
{
    public class Weather
    {
        public int Id { get; set; }
        public Location? Location { get; set; }
        public DateOnly DateOnly { get; set; }
        public TimeOnly TimeOnly { get; set; }
        public double TemperatureC { get; set; }
    }
}