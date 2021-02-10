using Microsoft.AspNetCore.Components;

namespace CEC.Blazor.Editor
{
    public partial class WeatherViewer : ComponentBase
    {
        [CascadingParameter] private IModalDialog Modal { get; set; }

        private void Exit()
        {
            this.Modal?.Close(ModalResult.OK());
        }

    }
}
