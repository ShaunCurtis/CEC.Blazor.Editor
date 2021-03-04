using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public class EditorWeatherForecastControllerService
    {

        public EditorWeatherForecastDataService DataService { get; set; }

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

        public RecordCollection RecordData { get; } = new RecordCollection();

        public EditorWeatherForecastControllerService(EditorWeatherForecastDataService weatherForecastDataService )
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
            this.RecordChanged?.Invoke(RecordChanged, EventArgs.Empty);
        }

        public async Task<bool> SaveForecastAsync()
        {
            Guid id = Guid.Empty;
            var record = DbWeatherForecast.FromRecordCollection(this.RecordData);
            if (this.Forecast.ID.Equals(Guid.Empty))
                 id = await this.DataService.AddForecastAsync(record);
            else
              id =  await this.DataService.UpdateForecastAsync(record);
            if (!id.Equals(Guid.Empty))
                await GetForecastAsync(id);
            return !id.Equals(Guid.Empty);

        }
    }
}
