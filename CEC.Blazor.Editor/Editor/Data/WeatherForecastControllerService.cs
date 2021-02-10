using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public class WeatherForecastControllerService
    {

        public WeatherForecastDataService DataService { get; set; }

        public List<DbWeatherForecast> Forecasts { 
            get => _Forecasts;
            private set
            {
                _Forecasts = value;
                ListChanged?.Invoke(value, EventArgs.Empty);
            }
        }
        private List<DbWeatherForecast> _Forecasts;

        public DbWeatherForecast Forecast
        {
            get => _Forecast;
            private set
            {
                _Forecast = value;
                RecordData.AddRange(_Forecast.AsRecordCollection, true);
                RecordChanged?.Invoke(_Forecast, EventArgs.Empty);
            }
        }
        private DbWeatherForecast _Forecast;

        public event EventHandler RecordChanged;

        public event EventHandler ListChanged;

        public RecordCollection RecordData { get; }

        public WeatherForecastControllerService(WeatherForecastDataService weatherForecastDataService )
        {
            this.DataService = weatherForecastDataService;
        }

        public async Task GetForecastsAsync()
        {
            this.Forecasts = await DataService.GetForecastsAsync();
        }

        public async Task GetForecastAsync(Guid id)
        {
            this.Forecast = await DataService.GetForecastAsync(id);
        }
        public async Task SaveForecastAsync(DbWeatherForecast record)
        {
            await DataService.UpdateForecastAsync(record);
        }
    }
}
