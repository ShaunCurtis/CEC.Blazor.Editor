using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.ModalEditor
{
    public partial class WeatherViewer : ComponentBase
    {
        private bool HasServices => this.Modal != null && this.ControllerService != null;

        [Inject] protected ModalEditorWeatherForecastControllerService ControllerService { get; set; }

        private bool IsLoaded => this.ControllerService != null && this.ControllerService.Forecast != null;
        
        [CascadingParameter] private IModalDialog Modal { get; set; }

        protected async override Task OnInitializedAsync()
        {
            if (this.HasServices && Modal.Options.TryGet<Guid>(ModalOptions.__ID, out Guid modalid))
                await this.ControllerService.GetForecastAsync(modalid);
        }

        private void Exit()
        {
            this.Modal?.Close(ModalResult.OK());
        }

    }
}
