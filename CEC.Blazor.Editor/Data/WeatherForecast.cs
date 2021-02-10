using System;

namespace CEC.Blazor.Editor.Data
{
    public class WeatherForecast
    {
        public Guid ID { get; init; } = Guid.NewGuid();

        public DateTime Date { get; init; }

        public int TemperatureC { get; init; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; init; }
    }
}
