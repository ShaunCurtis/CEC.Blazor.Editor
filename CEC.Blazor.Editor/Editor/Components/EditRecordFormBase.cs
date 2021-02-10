/// =================================
/// Author: Shaun Curtis, Cold Elm
/// License: MIT
/// ==================================

using CEC.Blazor.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.SPA.Components.Forms
{
    public abstract class EditRecordFormBase<TRecord, TDbContext> :
        RecordFormBase<TRecord, TDbContext>
        where TRecord : class, IDbRecord<TRecord>, new()
       where TDbContext : DbContext
    {
        #region Public Properties

        /// <summary>
        /// Read only Property to get the Edit Context
        /// Only this component can change it
        /// It's created from the RecordEditorContext which implements <see cref="IRecordEditContext"/>
        /// </summary>
        public EditContext EditContext
        {
            get => _EditContext;
        }

        /// <summary>
        /// Property to concatinate the Page Title
        /// </summary>
        public override string PageTitle
        {
            get
            {
                if (this.IsNewRecord) return $"New {this.Service?.RecordInfo?.RecordDescription ?? "Record"}";
                else return $"{this.Service?.RecordInfo?.RecordDescription ?? "Record"} Editor";
            }
        }

        /// <summary>
        /// Boolean Property exposing the Service Clean state
        /// </summary>
        public bool IsClean => (this.RecordEditorContext?.IsClean ?? true);

        /// <summary>
        /// The current state of the record
        /// </summary>
        public bool IsDirty => this.RecordEditorContext?.IsDirty ?? false;

        /// <summary>
        /// Boolean Validation checker for exposing last Validation check
        /// </summary>
        protected bool IsValidated => this.RecordEditorContext?.IsValid ?? true;

        /// <summary>
        /// property used by the UIErrorHandler component
        /// </summary>
        protected override bool IsError => !(this.IsRecord);


        /// <summary>
        /// Boolean Property to determine if the record is new or an edit
        /// </summary>
        public bool IsNewRecord => this.Service?.RecordID == 0 ? true : false;

        #endregion

        #region Protected Properties

        /// <summary>
        /// RecordEditData object used for edit control and Validation
        /// Should be assigned in defived classes at first render
        /// </summary>
        protected IRecordEditContext RecordEditorContext { get; set; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Inherited - Always call the base method first
        /// </summary>
        protected async override Task LoadRecordAsync(bool firstLoad = false, bool setLoading = true)
        {
            await Task.Yield();
            this.Loading = true;
            await base.LoadRecordAsync(firstLoad, false);
            if (firstLoad)
            {
                this._EditContext = new EditContext(RecordEditorContext);
                await this.RecordEditorContext.NotifyEditContextChangedAsync(this.EditContext);
                this.EditContext.OnFieldChanged += onFieldChanged;
            }
            this.Loading = false;
        }

        /// <summary>
        /// Event handler for EditContext OnFieldChanged
        /// Checks the state of the RecordCollection and locks or unlocks the View
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void onFieldChanged(object sender, EventArgs e)
        {
            this.SetViewManagerLock();
            this.RenderAsync();
        }

        /// <summary>
        /// Save Method called from the Button
        /// </summary>
        protected virtual async Task<bool> Save()
        {
            var ok = false;
            // Validate the EditContext
            if (this.RecordEditorContext.EditContext.Validate())
            {
                // Save the Record
                ok = await this.Service.SaveRecordAsync();
                if (ok)
                {
                    // Set the EditContext State
                    this.RecordEditorContext.EditContext.MarkAsUnmodified();
                    // Set the View Lock i.e. unlock it
                    this.SetViewManagerLock();
                }
                // Set the alert message to the return result
                this.AlertMessage.SetAlert(this.Service.TaskResult);
                // Trigger a component State update - buttons and alert need to be sorted
                await RenderAsync();
            }
            else this.AlertMessage.SetAlert("A validation error occurred.  Check individual fields for the relevant error.", MessageType.Danger);
            return ok;
        }

        /// <summary>
        /// Save and Exit Method called from the Button
        /// </summary>
        protected virtual async void SaveAndExit()
        {
            if (await this.Save()) this.ConfirmExit();
        }

        /// <summary>
        /// Confirm Exit Method called from the Button
        /// </summary>
        protected virtual void TryExit()
        {
            // Check if we are free to exit ot need confirmation
            if (this.IsClean) ConfirmExit();
        }

        /// <summary>
        /// Confirm Exit Method called from the Button
        /// </summary>
        protected virtual void ConfirmExit()
        {
            // To escape a dirty component reset the Record Collection and unlock the View
            this.Service.RecordValueCollection.ResetValues();
            this.SetViewManagerLock();
            this.Exit();
        }

        #endregion

        #region Private Properties

        /// <summary>
        /// Internal field for EditContext property
        /// </summary>
        private EditContext _EditContext = null;

        #endregion

        #region Private Methods

        private void OnValidationStateChanged(object sender, ValidationStateChangedEventArgs e) 
            => this.RenderAsync();

        private void SetViewManagerLock()
        {
            if (this.RecordEditorContext.IsDirty)
            {
                this.ViewManager.LockView();
                this.AlertMessage.SetAlert("The Record isn't Saved", MessageType.Warning);
            }
            else
            {
                this.ViewManager.UnLockView();
                this.AlertMessage.ClearAlert();
            }
        }

        #endregion

        #region Static

        #endregion
    }
}
