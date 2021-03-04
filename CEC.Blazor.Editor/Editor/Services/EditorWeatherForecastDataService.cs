using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public class EditorWeatherForecastDataService
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private List<DbWeatherForecast> Forecasts { get; set; } = new List<DbWeatherForecast>();

        public EditorWeatherForecastDataService()
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
                    ID = Guid.NewGuid(),
                    Date = DateTime.Now.AddDays((double)x),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                }); 
            }
        }

        public Task<List<DbWeatherForecast>> GetForecastsAsync()
            => Task.FromResult(this.Forecasts);

        public Task<DbWeatherForecast> GetForecastAsync(Guid id)
            => Task.FromResult(this.Forecasts.FirstOrDefault(item => item.ID.Equals(id)));

        public Task<Guid> UpdateForecastAsync(DbWeatherForecast record)
        {
            var rec = this.Forecasts.FirstOrDefault(item => item.ID.Equals(record.ID));
            if (rec != default) this.Forecasts.Remove(rec);
            this.Forecasts.Add(record);
            return Task.FromResult(record.ID);
        }

        public Task<Guid> AddForecastAsync(DbWeatherForecast record)
        {
            var id = Guid.NewGuid();
            if (record.ID.Equals(Guid.Empty))
            {
                var recdata = record.AsRecordCollection;
                recdata.SetField(DbWeatherForecast.__ID.FieldName, id);
                record = DbWeatherForecast.FromRecordCollection(recdata);
            }
            else
            {
                var rec = this.Forecasts.FirstOrDefault(item => item.ID.Equals(record.ID));
                if (rec != default) return Task.FromResult(Guid.Empty);
            }
            this.Forecasts.Add(record);
            return Task.FromResult(id);
        }
    }
}
