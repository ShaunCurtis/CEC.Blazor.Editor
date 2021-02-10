using Microsoft.AspNetCore.Components;

namespace CEC.Blazor.Editor
{
    public partial class WeatherEditor : ComponentBase
    {
        [CascadingParameter] private IModalDialog Modal { get; set; }

        public bool IsDirty { get; set; }

        public bool IsLocked { get; private set; }

        private bool DoDirtyExit { get; set; }

        private string DirtyButtonCss => this.IsDirty ? "btn-warning" : "btn-info";

        private string DirtyButtonText => this.IsDirty ? "Set Clean" : "Set Dirty";

        private void Exit()
        {
            if (this.IsDirty)
            {
                this.DoDirtyExit = true;
                this.InvokeAsync(this.StateHasChanged);
            }
            else
                this.Modal?.Close(ModalResult.OK());
        }

        public void DirtyExit()
        {
            if (this.DoDirtyExit)
            {
                this.IsDirty = false;
                this.Modal?.Lock(false);
                this.Modal?.Close(ModalResult.Cancel());
            }
        }

        public void CancelExit()
        {
            this.DoDirtyExit = false;
            this.InvokeAsync(this.StateHasChanged);
        }

        public void SetDirty()
        {
            if (this.IsDirty)
                this.DoDirtyExit = false;
            this.IsDirty = !this.IsDirty;
            this.Modal?.Lock(this.IsDirty);
            this.InvokeAsync(this.StateHasChanged);
        }

    }
}
