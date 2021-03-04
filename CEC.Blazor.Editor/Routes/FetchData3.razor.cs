using CEC.Blazor.Editor.Components;
using CEC.Blazor.Editor.Data;
using CEC.Blazor.Editor.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Routes
{
    public partial class FetchData3 : ComponentBase
    {
        [Inject] EditorWeatherService ForecastService { get; set; }

        [Inject] NavigationManager NavManager { get; set; }

        private WeatherForecast[] forecasts;

        private ModalDialog1 Modal { get; set; }

        private NavigationController NavController { get; set; }

        private WeatherPageEditor _Editor { get; set; }

        private string buttoncss => (NavController != null && NavController.IsLocked) ? "btn-success" : "btn-danger";

        private string buttontext => (NavController != null && NavController.IsLocked) ? "Unlock" : "Lock";

        protected override async Task OnInitializedAsync()
        {
            forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
        }

        private void ShowEditor()
            => this._Editor?.Show();

        private void GoToEditor()
            => this.NavManager.NavigateTo("/weditor");
    }
}
