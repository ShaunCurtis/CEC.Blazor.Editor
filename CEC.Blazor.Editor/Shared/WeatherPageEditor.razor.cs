using CEC.Blazor.Editor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CEC.Blazor.Editor.Shared
{
    public partial class WeatherPageEditor : ComponentBase
    {

        [CascadingParameter] NavigationController NavController { get; set; }

        [Parameter] public EventCallback CloseAction { get; set; }

        public bool Display { get; private set; }

        public bool IsDirty { get; set; }

        public bool IsLocked { get; private set; }

        private bool DirtyExit { get; set; }

        private string DirtyButtonCss => this.IsDirty ? "btn-success" :"btn-danger" ;

        private string DirtyButtonText => this.IsDirty ? "Set Clean" : "Set Dirty";

        private string BorderCss => this.IsDirty ? "border-danger" : "border-secondary";

        public void Show()
        {
            this.Display = true;
            this.InvokeAsync(this.StateHasChanged);
        }

        public void Hide()
        {
            if (this.IsDirty)
                this.DirtyExit = true;
            else
            {
                NavController.Unlock();
                this.Display = false;
                this.CloseAction.InvokeAsync();
            }
            this.InvokeAsync(this.StateHasChanged);
        }

        public void DirtyHide()
        {
            this.DirtyExit = false;
            if (this.IsDirty)
            {
                NavController.Unlock();
                this.IsDirty = false;
            }
            this.Display = false;
            this.CloseAction.InvokeAsync();
            this.InvokeAsync(this.StateHasChanged);
        }

        public void CancelHide()
        {
            this.DirtyExit = false;
            this.InvokeAsync(this.StateHasChanged);
        }

        public void SetDirty()
        {
            if (this.IsDirty)
            {
                NavController?.Unlock();
                this.DirtyExit = false;
            }
            else
            {
                NavController?.Lock();
                this.DirtyExit = true;
            }
            this.IsDirty = !this.IsDirty;
            this.InvokeAsync(this.StateHasChanged);
        }

    }
}
