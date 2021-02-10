/// =================================
/// Author: Shaun Curtis, Cold Elm
/// License: MIT
/// ==================================

using Microsoft.AspNetCore.Components.Forms;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{

    /// <summary>
    /// Interface for RecordEditData objects
    /// </summary>
    public interface IRecordEditContext
    {
        /// <summary>
        /// Edit context associated with the Context
        /// </summary>
        public EditContext EditContext { get; }

        /// <summary>
        /// Property to indicate whether the dataset was valid the last time validation took place
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Property to indicate whether the dataset is Dirty - i.e. edited
        /// </summary>
        public bool IsDirty { get; }

        /// <summary>
        /// Property to indicate whether the dataset is Clean - i.e. unedited
        /// </summary>
        public bool IsClean { get; }

        /// <summary>
        /// Property to indicate if the object has been loaded and is read to work
        /// </summary>
        public bool IsLoaded { get; }

        /// <summary>
        /// Method to call to notify the class that the Edit Context has changed
        /// </summary>
        /// <param name="context"></param>
        /// <param name="recordValues"></param>
        public Task NotifyEditContextChangedAsync(EditContext context);

        /// <summary>
        /// Method to call to validate the Data Set
        /// </summary>
        /// <returns></returns>
        public bool Validate();

    }
}
