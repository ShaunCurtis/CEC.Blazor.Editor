using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public partial class WeatherForecastEditor : ComponentBase
    {
        public EditContext EditContext
        {
            get => _EditContext;
        }
        private EditContext _EditContext = null;

        protected WeatherForecastEditContext RecordEditorContext { get; set; }

        [Inject] protected WeatherForecastControllerService ControllerService { get; set; }

        [CascadingParameter]
        private IModalDialog Modal { get; set; }
        
        private bool IsModal => this.Modal != null;

        private bool IsDirtyExit;

        private bool IsDirty => RecordEditorContext.IsDirty;

        private bool IsValid => RecordEditorContext.IsValid;

        private bool IsLoaded => RecordEditorContext?.IsLoaded ?? false;

        private bool CanSave => this.IsDirty && this.IsValid;

        private bool CanExit => !this.IsDirtyExit;

        private string SaveButtonText => this.ControllerService.Forecast.ID.Equals(Guid.Empty) ? "Save" : "Update";

        protected async override Task OnInitializedAsync()
        {
            await Task.Yield();
            if (this.IsModal && Modal.Options.TryGet<Guid>(ModalOptions.__ID, out Guid modalid))
            {
                await this.ControllerService.GetForecastAsync(modalid);
                this.RecordEditorContext = new WeatherForecastEditContext(this.ControllerService.RecordData);
                this._EditContext = new EditContext(RecordEditorContext);
                await this.RecordEditorContext.NotifyEditContextChangedAsync(this.EditContext);
                this.EditContext.OnFieldChanged += OnFieldChanged;
            }
            await base.OnInitializedAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        protected void OnFieldChanged(object sender, EventArgs e)
        {
            this.SetLock();
            this.IsDirtyExit = false;
            InvokeAsync(StateHasChanged);
        }

        private void SetLock()
        {
            if (this.RecordEditorContext.IsDirty)
                this.Modal.Lock(true);
            else
                this.Modal.Lock(false);
        }

        protected override Task OnParametersSetAsync()
        {
            return base.OnParametersSetAsync();
        }

        protected async Task<bool> Save()
        {
            var ok = false;
            // Validate the EditContext
            if (this.RecordEditorContext.EditContext.Validate())
            {
                // Save the Record
                ok = await this.ControllerService.SaveForecastAsync();
                if (ok)
                {
                    // Set the EditContext State
                    this.RecordEditorContext.EditContext.MarkAsUnmodified();
                    // Set the View Lock i.e. unlock it
                    this.SetLock();
                    this.IsDirtyExit = false;
                }
                await this.InvokeAsync(StateHasChanged);
            }
            return ok;
        }

        protected void Exit()
        {
            if (RecordEditorContext.IsDirty)
            {
                this.IsDirtyExit = true;
                this.InvokeAsync(StateHasChanged);
            } 
            else 
                this.Modal.Close(ModalResult.OK());
        }

        protected void DirtyExit()
        {
            this.Modal.Lock(false);
            this.Modal.Close(ModalResult.OK());
        }

        protected void CancelExit()
        {
                this.IsDirtyExit = false;
                this.InvokeAsync(StateHasChanged);
        }

    }
}
