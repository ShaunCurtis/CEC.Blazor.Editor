using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CEC.Blazor.Editor.Shared
{
    public partial class ModalDialog2 : ComponentBase
    {

        [Inject] private IJSRuntime _js { get; set; }

        public bool Display { get; private set; }

        public bool IsDirty { get; set; }

        public bool IsLocked { get; private set; }

        private bool DirtyExit { get; set; }

        private string DirtyButtonCss => this.IsDirty ? "btn-danger" : "btn-success";

        private string DirtyButtonText => this.IsDirty ? "Set Clean" : "Set Dirty";

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
                this.Display = false;
            this.InvokeAsync(this.StateHasChanged);
        }

        public void DirtyHide()
        {
            this.Display = false;
            this.DirtyExit = false;
            if (this.IsDirty)
            {
                this.IsDirty = false;
                CheckLock();
            }
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
                this.DirtyExit = false;
            this.IsDirty = !this.IsDirty;
            this.CheckLock();
            this.InvokeAsync(this.StateHasChanged);
        }

        public void SetPageExitCheck(bool action)
        {
            _js.InvokeAsync<bool>("cecblazor_setEditorExitCheck", action);
        }

        public void CheckLock()
        {
            if (this.IsDirty && !this.IsLocked)
            {
                this.IsLocked = true;
                this.SetPageExitCheck(true);
            }
            else if (this.IsLocked && !this.IsDirty)
            {
                this.IsLocked = false;
                this.SetPageExitCheck(false);
            }
        }

    }
}
