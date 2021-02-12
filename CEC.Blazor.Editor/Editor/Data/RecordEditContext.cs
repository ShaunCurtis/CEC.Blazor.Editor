/// =================================
/// Author: Shaun Curtis, Cold Elm
/// License: MIT
/// ==================================

using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{

    /// <summary>
    /// A class to surface data stored in the underlying RecordCollection
    /// Ufor theprovides an object with the data that the Editor uses and a Validator can be applied to
    /// The properties point to the data stored in the underlying RecordCollection
    /// </summary>
    public abstract class RecordEditContext : IRecordEditContext
    {
        #region Public

        /// <summary>
        /// Edit context associated with the Context
        /// </summary>
        public EditContext EditContext { get; private set; }

        /// <summary>
        /// Public bool to expose Validation state of dataset
        /// </summary>
        public bool IsValid => !Trip;

        /// <summary>
        /// Public bool to expose Edit State of dataset
        /// </summary>
        public bool IsDirty => this.RecordValues?.IsDirty ?? false;

        /// <summary>
        /// Public bool to expose Edit State of dataset
        /// </summary>
        public bool IsClean => !this.IsDirty;

        /// <summary>
        /// Public bool to expose Load state of the class instance
        /// </summary>
        public bool IsLoaded => this.EditContext != null && this.RecordValues != null;

        /// <summary>
        /// New Method
        /// </summary>
        /// <param name="collection">Record Collection for the current record</param>
        public RecordEditContext(RecordCollection collection)
        {
            Debug.Assert(collection != null);

            if (collection is null)
                throw new InvalidOperationException($"{nameof(RecordEditContext)} requires a valid {nameof(RecordCollection)} object");
            else
            {
                this.RecordValues = collection;
                this.LoadValidationActions();
            }
        }

        /// <summary>
        /// Method called to validate the Dataset
        /// </summary>
        /// <returns>True if valid</returns>
        public bool Validate()
        {
            // using Validating to stop being called multiple times
            if (this.ValidationMessageStore != null && !this.Validating)
            {
                this.Validating = true;
                // clear the message store and trip wire and check we have Validators to run
                this.ValidationMessageStore.Clear();
                this.Trip = false;
                foreach (var validator in this.ValidationActions)
                {
                    // invoke the action - defined as a func<bool> and trip if validation failed (false)
                    if (!validator.Invoke()) this.Trip = true;
                }
                this.EditContext.NotifyValidationStateChanged();
                this.Validating = false;
            }
            return IsValid;
        }

        /// <summary>
        /// Notification method to inform the class that the edit context has changed
        /// Called from the RecordEditform
        /// </summary>
        /// <param name="context">New Edit Context</param>
        /// <returns></returns>
        public Task NotifyEditContextChangedAsync(EditContext context)
        {
            var oldcontext = this.EditContext;
            if (context is null)
                throw new InvalidOperationException($"{nameof(RecordEditContext)} - NotifyEditContextChangedAsync requires a valid {nameof(EditContext)} object");
            // if we already have an edit context, we will have registered with OnValidationRequested, so we need to drop it before losing our reference to the editcontext object.
            if (this.EditContext != null)
            {
                this.EditContext.OnValidationRequested -= ValidationRequested;
            }
            // assign the Edit Context internally
            this.EditContext = context;
            if (this.IsLoaded)
            {
                // Get the Validation Message Store from the EditContext
                this.ValidationMessageStore = new ValidationMessageStore(EditContext);
                // Wire up to the Editcontext to service Validation Requests
                this.EditContext.OnValidationRequested += ValidationRequested;
            }
            // Call a validation on the current data set
            this.Validate();
            return Task.CompletedTask;
        }

        #endregion

        #region Protected


        protected RecordCollection RecordValues { get; private set; } = new RecordCollection();

        protected bool Trip = false;

        protected List<Func<bool>> ValidationActions { get; } = new List<Func<bool>>();

        protected virtual void LoadValidationActions() { }

        protected ValidationMessageStore ValidationMessageStore;

        #endregion

        #region Private

        private bool Validating;

        private void ValidationRequested(object sender, ValidationRequestedEventArgs args)
        {
            this.Validate();
        }

        #endregion
    }
}
