using CEC.Blazor.Editor.Data;
using CEC.Blazor.Editor.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Routes
{
    public partial class FetchData2 : ComponentBase
    {
        [Inject] EditorWeatherService ForecastService { get; set; }

        private WeatherForecast[] forecasts;

        private ModalDialog2 Modal { get; set; }

        protected override async Task OnInitializedAsync()
        {
            forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
        }
        private void ShowModalDialog()
        {
            this.Modal.Show();
        }
    }
}
