using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public class WeatherForecastDataService
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private List<DbWeatherForecast> Forecasts { get; set; } = new List<DbWeatherForecast>();

        public WeatherForecastDataService()
        {
            PopulateForecasts();
        }

        public void PopulateForecasts()
        {
            var rng = new Random();
            for (int x = 0; x < 5; x++)
            {
                Forecasts.Add(new DbWeatherForecast
                {
                    Date = DateTime.Now.AddDays((double)x),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                });
            }
        }

        public Task<List<DbWeatherForecast>> GetForecastsAsync()
        {
            return Task.FromResult(this.Forecasts);
        }

        public Task<DbWeatherForecast> GetForecastAsync(Guid id)
        {
            return Task.FromResult(this.Forecasts.FirstOrDefault(item => item.ID.Equals(id)));
        }

        public Task<bool> UpdateForecastAsync(DbWeatherForecast record)
        {
            var rec = this.Forecasts.FirstOrDefault(item => item.ID.Equals(record.ID));
            if (rec != default) this.Forecasts.Remove(rec);
            this.Forecasts.Add(rec);
            return Task.FromResult(true);
        }

        public Task<bool> AddForecastAsync(DbWeatherForecast record)
        {
            var rec = this.Forecasts.FirstOrDefault(item => item.ID.Equals(record.ID));
            if (rec != default) return Task.FromResult(false);
            this.Forecasts.Add(rec);
            return Task.FromResult(true);
        }

    }
}
