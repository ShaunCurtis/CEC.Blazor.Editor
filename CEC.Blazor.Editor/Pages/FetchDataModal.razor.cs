﻿using CEC.Blazor.Editor.Data;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Pages
{
    public partial class FetchDataModal : ComponentBase
    {
        [Inject] WeatherForecastService ForecastService { get; set; }

        private WeatherForecast[] forecasts;

        private ModalDialog Modal { get; set; }

        protected override async Task OnInitializedAsync()
        {
            forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
        }

        private async void ShowViewDialog()
        {
            var options = new ModalOptions();
            options.Set(ModalOptions.__Width, "60%");
            await this.Modal.ShowAsync<WeatherViewer>(options); 
        }

        private async void ShowEditDialog()
        {
            var options = new ModalOptions();
            options.Set(ModalOptions.__Width, "80%");
            await this.Modal.ShowAsync<WeatherEditor>(options);
        }
    }
}
