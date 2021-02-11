﻿using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Pages
{
    public partial class WeatherDataModal : ComponentBase
    {
        [Inject] WeatherForecastControllerService ForecastService { get; set; }

        private ModalDialog Modal { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await ForecastService.GetForecastsAsync();
        }

        private async void ShowViewDialog(Guid id)
        {
            var options = new ModalOptions();
            {
                options.Set(ModalOptions.__Width, "80%");
                options.Set(ModalOptions.__ID, id);
            }
            await this.Modal.ShowAsync<WeatherViewer>(options);
        }

        private async void ShowEditDialog(Guid id)
        {
            var options = new ModalOptions();
            {
                options.Set(ModalOptions.__Width, "80%");
                options.Set(ModalOptions.__ID, id);
            }
            await this.Modal.ShowAsync<WeatherForecastEditor>(options);
        }
    }
}
