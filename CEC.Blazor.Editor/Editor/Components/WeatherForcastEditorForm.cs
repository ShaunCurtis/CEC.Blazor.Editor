using CEC.Blazor.Editor.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public class WeatherForcastEditorForm : ComponentBase
    {
        public EditContext EditContext
        {
            get => _EditContext;
        }
        private EditContext _EditContext = null;

        protected IRecordEditContext RecordEditorContext { get; set; }

        [Inject] protected WeatherForecastControllerService ControllerService { get; set; }

        private IModalDialog Modal { get; set; }

        private Guid ID { get; set; } = Guid.Empty;

        private bool IsModal => this.Modal is null;
        protected async override Task OnInitializedAsync()
        {
            await Task.Yield();
            if (this.IsModal && Modal.Options.TryGet<Guid>(ModalOptions.__ID, out Guid modalid))
            {
                this.ID = modalid;
                this._EditContext = new EditContext(RecordEditorContext);
                await this.RecordEditorContext.NotifyEditContextChangedAsync(this.EditContext);
                this.EditContext.OnFieldChanged += onFieldChanged;
            }
            await base.OnInitializedAsync();
        }

    protected void onFieldChanged(object sender, EventArgs e)
    {
        this.SetLock();
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

        protected virtual async Task<bool> Save()
        {
            var ok = false;
            // Validate the EditContext
            if (this.RecordEditorContext.EditContext.Validate())
            {
                // Save the Record
                ok = await this.ControllerService.SaveRecordAsync();
                if (ok)
                {
                    // Set the EditContext State
                    this.RecordEditorContext.EditContext.MarkAsUnmodified();
                    // Set the View Lock i.e. unlock it
                    this.SetLock();
                }
                // Set the alert message to the return result
                this.AlertMessage.SetAlert(this.Service.TaskResult);
                // Trigger a component State update - buttons and alert need to be sorted
                await RenderAsync();
            }
            else this.AlertMessage.SetAlert("A validation error occurred.  Check individual fields for the relevant error.", MessageType.Danger);
            return ok;
        }

    }
}
