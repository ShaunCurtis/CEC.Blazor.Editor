using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Shared
{
    public partial class ModalDialog : ComponentBase
    {

        [Inject] private IJSRuntime _js { get; set; }

        public bool Display { get; private set; }

        public bool IsDirty { get; set; }

        public bool IsLocked { get; private set; }

        private bool DirtyExit { get; set; }


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
        /// <summary>
        /// Method to lock the View
        /// </summary>
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
