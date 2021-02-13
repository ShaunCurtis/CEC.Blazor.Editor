# Building an Editor Framework

This is the second of two articles looking at how to implement edit form controls in Blazor.

The first article explores how to control what the user could do once a form was dirty; in essence how to stop a user unintentionally exiting.  This article describes how to build a framework that detects when the dataset is dirty and/or invalid.

I think many would consider that Blazor already has all the functionality needed to edit data.  Why do you need to re-invent the wheel?  It's a valid question, and you answer depends on the environment you work in.  If you fervently believe its true, read no further: this article isn't for you.

A little recent background. C# 9 introduced the `Record` type, creating an immutable reference type.  The property `{get; init;}` lets us create an immutable property.  These are recent language changes: Microsoft seem to have done a little rethinking!

I've always been a firm believer in maintaining the integrity of records and recordsets read from databases.  What you see in your reference record or recordset is what is in the database.  If you want to edit something, make a copy, change the copy, submit the copy to the database and then refresh your reference copy from the database.

The editing framework I use, described in this article, implements those principles.

## Overview

This short discussion and the example project uses the out-of-the-box Blazor WeatherForecast record as the reference point.

`DbWeatherForecast` represents the record read from the database.  It's declared as a `class`, not a `record`: only the properties that represent database fields are immutable. The editable version of   `DbWeatherForecast` is held in a `RecordCollection`.  `DbWeatherForecast` has methods to build and read data from a `RecordCollection`.  A `RecordCollection` is an `IEnumerable` object containing a list of `RecordFieldValue` objects.  Each represents a field/property in `DbWeatherForecast`.  A `RecordFieldValue` has a set of immutable fields itself, `Value` and `FieldName`, and an `EditedValue` field which can be set.  `IsDirty` is a boolean property that represents the edit state of `RecordFieldValue`. `RecordCollection` and `RecordFieldValue` provide controlled access to the underlying data values.

`WeatherForecastEditContext` is the UI editor object for `DbWeatherForecast`, exposing the editable properties of the `RecordCollection` for `DbWeatherForecast`.  It has a symbiotic relationship with the `EditContext`, tracking the edit state of the `RecordCollection` and providing validation of any properties that require data validation.


## Infrastructure Classes

As always we need some supporting classes for the main show. 

#### RecordFieldValue

As already discussed, `RecordFieldValue` holds information about a field in a Record set.  Note:

1. The properties derived from the actual record are `{get; init;}`.  They can only be set when an instance of `RecordFieldValue` is created.
2. `FieldName` is property name for the field.  We define it to ensure we use the same string value throughout the application.
2. `Value` is the database value of the field.
3. `ReadOnly` is self-evident. It's for labelling derived/calculated fields.
4. `DisplayName` is the string to use when displaying the name of the field.
5. `EditedValue` is the current value of the field in our edit context.  The getter ensures that on first get, if it hasn't already been set, it's set to `Value`.
6. `IsDirty` does the default equality check for the object type on `Value` against `EditedValue` to determine if the Field is dirty.
7. `Reset` sets `EditedValue` back to `Value`.
8. The two `Clone` methods create new copies of `RecordEditValue`.

```c#
using System;

namespace CEC.Blazor.Editor
{
    public class RecordFieldValue
    {
        public string FieldName { get; init; }
        public object Value { get; init; }
        public bool ReadOnly { get; init; }
        public string DisplayName { get; set; }
        public object EditedValue
        {
            get
            {
                if (this._EditedValue is null && this.Value != null) this._EditedValue = this.Value;
                return this._EditedValue;
            }
            set => this._EditedValue = value;
        }
        private object _EditedValue { get; set; }

        public bool IsDirty
        {
            get
            {
                if (Value != null && EditedValue != null) return !Value.Equals(EditedValue);
                if (Value is null && EditedValue is null) return false;
                return true;
            }
        }

        public RecordFieldValue() { }

        public RecordFieldValue(string field, object value)
        {
            this.FieldName = field;
            this.Value = value;
            this.EditedValue = value;
            this.GUID = Guid.NewGuid();
        }

        public void Reset()
            => this.EditedValue = this.Value;

        public RecordFieldValue Clone()
        {
            return new RecordFieldValue()
            {
                DisplayName = this.DisplayName,
                FieldName = this.FieldName,
                Value = this.Value,
                ReadOnly = this.ReadOnly
            };
        }

        public RecordFieldValue Clone(object value)
        {
            return new RecordFieldValue()
            {
                DisplayName = this.DisplayName,
                FieldName = this.FieldName,
                Value = value,
                ReadOnly = this.ReadOnly
            };
        }
    }
}
```

### RecordCollection

`RecordCollection` is a managed `IEnumerable` collection of `RecordFieldValue` objects.  Note:

1. There are lots of getters, setters, etc for getting to the individual `RecordFieldValue` objects.
2. `IsDirty` checks for any dirty items in the collection.
3. `FieldValueChanged` provides a event triggered whenever an individual `RecordFieldValue` is set.  You can see it being invoked when `SetField` is called.

```c#
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CEC.Blazor.Editor
{
    public class RecordCollection :IEnumerable<RecordFieldValue>
    {
        private List<RecordFieldValue> _items = new List<RecordFieldValue>();
        public int Count => _items.Count;
        public Action<bool> FieldValueChanged;
        public bool IsDirty => _items.Any(item => item.IsDirty);

        public IEnumerator<RecordFieldValue> GetEnumerator()
        {
            foreach (var item in _items)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        public void ResetValues()
            => _items.ForEach(item => item.Reset());

        public void Clear()
            => _items.Clear();

        // .......  lots of getters, setters, deleters, adders.  A few examples show.
        public T Get<T>(string FieldName)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x.Value is T t) return t;
            return default;
        }

        public T GetEditValue<T>(string FieldName)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x.EditedValue is T t) return t;
            return default;
        }
        public bool SetField(string FieldName, object value)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x != default)
            {
                x.EditedValue = value;
                this.FieldValueChanged?.Invoke(this.IsDirty);
            }
            else _items.Add(new RecordFieldValue(FieldName, value));
            return true;
        }
}
```

## RecordEditContext

`RecordEditContext` is the base class for the record edit context.  It contains the boilerplate code.  We'll look at it in more detail when we look at the `WeatherForecastEditContext`.  Key points to note:

1. It's initiliaiser requires a `RecordCollection` object.  In the app;lication this is the ControllerService `RecordCollection` associated with the current record.  It gets loaded whenever the record getys changed.
2. It holds a reference to the valid `EditContext` and expects to be notified of changes.
3. It handles Validation for the `EditContext` and is wired into `EditContext.OnValidationRequested`.
4. It holds a List of `ValidationActions` which get run whenever validation is triggered.

```c#
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{

    public abstract class RecordEditContext : IRecordEditContext
    {
        public EditContext EditContext { get; private set; }
        public bool IsValid => !Trip;
        public bool IsDirty => this.RecordValues?.IsDirty ?? false;
        public bool IsClean => !this.IsDirty;
        public bool IsLoaded => this.EditContext != null && this.RecordValues != null;

        protected RecordCollection RecordValues { get; private set; } = new RecordCollection();
        protected bool Trip = false;
        protected List<Func<bool>> ValidationActions { get; } = new List<Func<bool>>();
        protected virtual void LoadValidationActions() { }
        protected ValidationMessageStore ValidationMessageStore;

        private bool Validating;

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

        public bool Validate()
        {
            // using Validating to stop being called multiple times
            if (ValidationMessageStore != null && !this.Validating)
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

        public Task NotifyEditContextChangedAsync(EditContext context)
        {
            var oldcontext = this.EditContext;
            if (context is null)
                throw new InvalidOperationException($"{nameof(RecordEditContext)} - NotifyEditContextChangedAsync requires a valid {nameof(EditContext)} object");
            // if we already have an edit context, we will have registered with OnValidationRequested, so we need to drop it before losing our reference to the editcontext object.
            if (this.EditContext != null)
            {
                EditContext.OnValidationRequested -= ValidationRequested;
            }
            // assign the Edit Context internally
            this.EditContext = context;
            if (this.IsLoaded)
            {
                // Get the Validation Message Store from the EditContext
                this.ValidationMessageStore = new ValidationMessageStore(EditContext);
                // Wire up to the Editcontext to service Validation Requests
                this.EditContext.OnValidationRequested += this.ValidationRequested;
            }
            // Call a validation on the current data set
            this.Validate();
            return Task.CompletedTask;
        }

        private void ValidationRequested(object sender, ValidationRequestedEventArgs args)
        {
            this.Validate();
         }
    }
}
```

## DbWeatherForecast

The new Weather forecast record.  While we only create these records on the fly, a normal application would get them from a database.

Note:

1. There's a static declared `RecordFieldValue` for each database property/field in the class.  In a larger application these should be declared in a central `DataDictionary`. 
2. The "Database" properties are all declared `{ get; init; }`: they're immutable.
3. `AsRecordCollection` builds a `RecordCollection` object from the record.
4. `FromRecordCollection` is static, it builds a new record from the supplied `RecordCollection` using the edited values.

```c#
using System;

namespace CEC.Blazor.Editor
{
    public class DbWeatherForecast
    {
        public static RecordFieldValue __ID = new RecordFieldValue() { FieldName = "ID", DisplayName = "ID" };
        public static RecordFieldValue __Date = new RecordFieldValue() { FieldName = "Date", DisplayName = "Forecast Date" };
        public static RecordFieldValue __TemperatureC = new RecordFieldValue() { FieldName = "TemperatureC", DisplayName = "Temperature C" };
        public static RecordFieldValue __TemperatureF = new RecordFieldValue() { FieldName = "TemperatureF", DisplayName = "Temperature F", ReadOnly = true };
        public static RecordFieldValue __Summary = new RecordFieldValue() { FieldName = "Summary", DisplayName = "Summary" };

        public Guid ID { get; init; } = Guid.Empty;
        public DateTime Date { get; init; } = DateTime.Now;
        public int TemperatureC { get; init; } = 25;
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string Summary { get; init; }
        public RecordCollection AsRecordCollection
        {
            get
            {
                var coll = new RecordCollection();
                {
                    coll.Add(__ID.Clone(this.ID));
                    coll.Add(__Date.Clone(this.Date));
                    coll.Add(__TemperatureC.Clone(this.TemperatureC));
                    coll.Add(__TemperatureF.Clone(this.TemperatureF));
                    coll.Add(__Summary.Clone(this.Summary));
                }
                return coll;
            }
        }

        public static DbWeatherForecast FromRecordCollection(RecordCollection coll)
            => new DbWeatherForecast()
            {
                ID = coll.GetEditValue<Guid>(__ID.FieldName),
                Date = coll.GetEditValue<DateTime>(__Date.FieldName),
                TemperatureC = coll.GetEditValue<int>(__TemperatureC.FieldName),
                Summary = coll.GetEditValue<string>(__Summary.FieldName)
            };
    }
}
```
## Data Services

I've split up the data access into a data and a controller service: makes it more realistic.  We may be creating a dummy data set, but I'm mimicing normal practice.  In a production system this would run on interfaces and boilerplated base code implementations.

## WeatherForecastDataService

The Data Service:
1. Builds the dummy data set on startup.
2. Provides *CRUD* data operations on that dataset.
3. We're using Guids for Ids.

```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public class WeatherForecastDataService
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private List<DbWeatherForecast> Forecasts { get; set; } = new List<DbWeatherForecast>();

        public WeatherForecastDataService()
            => PopulateForecasts();

        public void PopulateForecasts()
        {
            var rng = new Random();
            for (int x = 0; x < 5; x++)
            {
                Forecasts.Add(new DbWeatherForecast
                {
                    ID = Guid.NewGuid(),
                    Date = DateTime.Now.AddDays((double)x),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                }); 
            }
        }

        public Task<List<DbWeatherForecast>> GetForecastsAsync()
            => Task.FromResult(this.Forecasts);

        public Task<DbWeatherForecast> GetForecastAsync(Guid id)
            => Task.FromResult(this.Forecasts.FirstOrDefault(item => item.ID.Equals(id)));

        public Task<Guid> UpdateForecastAsync(DbWeatherForecast record)
        {
            var rec = this.Forecasts.FirstOrDefault(item => item.ID.Equals(record.ID));
            if (rec != default) this.Forecasts.Remove(rec);
            this.Forecasts.Add(record);
            return Task.FromResult(record.ID);
        }

        public Task<Guid> AddForecastAsync(DbWeatherForecast record)
        {
            var id = Guid.NewGuid();
            if (record.ID.Equals(Guid.Empty))
            {
                var recdata = record.AsRecordCollection;
                recdata.SetField(DbWeatherForecast.__ID.FieldName, id);
                record = DbWeatherForecast.FromRecordCollection(recdata);
            }
            else
            {
                var rec = this.Forecasts.FirstOrDefault(item => item.ID.Equals(record.ID));
                if (rec != default) return Task.FromResult(Guid.Empty);
            }
            this.Forecasts.Add(record);
            return Task.FromResult(id);
        }
    }
}
```

#### Controller Data 

The controller service is the interface between the data and the UI, providing a high level business logic interface into the data.  Most of the Properties and Methods are self-evident.

```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public class WeatherForecastControllerService
    {
        public WeatherForecastDataService DataService { get; set; }
        public event EventHandler RecordChanged;
        public event EventHandler ListChanged;
        public RecordCollection RecordData { get; } = new RecordCollection();

        public List<DbWeatherForecast> Forecasts { 
            get => _Forecasts;
            private set
            {
                _Forecasts = value;
                ListChanged?.Invoke(value, EventArgs.Empty);
            }
        }
        private List<DbWeatherForecast> _Forecasts;

        public DbWeatherForecast Forecast
        {
            get => _Forecast;
            private set
            {
                _Forecast = value;
                RecordData.AddRange(_Forecast.AsRecordCollection, true);
                RecordChanged?.Invoke(_Forecast, EventArgs.Empty);
            }
        }
        private DbWeatherForecast _Forecast;

        public WeatherForecastControllerService(WeatherForecastDataService weatherForecastDataService )
            => this.DataService = weatherForecastDataService;

        public async Task GetForecastsAsync()
            => this.Forecasts = await DataService.GetForecastsAsync();

        public async Task GetForecastAsync(Guid id)
        {
            this.Forecast = await DataService.GetForecastAsync(id);
            this.RecordChanged?.Invoke(RecordChanged, EventArgs.Empty);
        }

        public async Task<bool> SaveForecastAsync()
        {
            Guid id = Guid.Empty;
            var record = DbWeatherForecast.FromRecordCollection(this.RecordData);
            if (this.Forecast.ID.Equals(Guid.Empty))
                 id = await this.DataService.AddForecastAsync(record);
            else
              id =  await this.DataService.UpdateForecastAsync(record);
            if (!id.Equals(Guid.Empty))
                await GetForecastAsync(id);
            return !id.Equals(Guid.Empty);

        }
    }
}
```
## Building the UI

Moving on to the UI and digressing a little.

### UI Components

One of my gripes with much of the UI code I see is HTML markup repetition.  What coders do in Razor Markup they would never dream of doing in C# code.  Editor/Display/List forms are good examples.  In this project I've moved most of the repetitive HTML markup into *UI Components*.  I like to get all my HTML markup out of high level components.  A formatting issue such as not enough spacing. Fix it in one place and it's fixed everywhere!

Let's take a look at a couple of examples. All the UI components are in the *UIComponents* directory.

#### UIFormRow

Not rocket science.  `ChildContent` is the default definition of what gets entered between the opening and closing statements.
```html
@namespace CEC.Blazor.Editor

<div class="row form-group">
    @this.ChildContent
</div>
```
```c#
@code {
    [Parameter] public RenderFragment ChildContent { get; set; }
}
```

With this you can now declare each row as:

```html
<UIFormRow>
    ....(ChildContent)
</UIFormRow>
```

#### UIButton

Again simple, but it keeps the high level declaration minimal.
```html
@if (this.Show)
{
    <button class="btn mr-1 @this.CssColor" @onclick="ButtonClick">
        @this.ChildContent
    </button>
}
```
```c#
@code {

    [Parameter] public bool Show { get; set; } = true;
    [Parameter] public EventCallback<MouseEventArgs> ClickEvent { get; set; }
    [Parameter] public string CssColor { get; set; } = "btn-primary";
    [Parameter] public RenderFragment ChildContent { get; set; }
    protected void ButtonClick(MouseEventArgs e) => this.ClickEvent.InvokeAsync(e);
}
```

```html
<UIButton CssColor="btn-success" Show="this.CanSave" ClickEvent="this.Save">@this.SaveButtonText</UIButton>
```

#### ModalEditForm

We replace `EditForm` with `ModalEditForm`.  It:

1. Has three RenderFragments.  
2. `LoadingContent` is only shown whilst the form is loading. 
2. `EditorContent` is shown once loading is complete.  It cascades `EditContext`.
3. `ButtonContent` is always shown at the bottom of the control.
5. `Loaded` controls what gets rendered.
6. We build the control with `BuildRenderTree`.

```c#
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace CEC.Blazor.ModalEditor
{
    public class ModalEditForm : ComponentBase
    {
        [Parameter] public RenderFragment EditorContent { get; set; }
        [Parameter] public RenderFragment ButtonContent { get; set; }
        [Parameter] public RenderFragment LoadingContent { get; set; }
        [Parameter] public bool Loaded { get; set; }
        [Parameter] public EditContext EditContext {get; set;}

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (this.Loaded)
            {
                builder.OpenRegion(EditContext.GetHashCode());
                builder.OpenComponent<CascadingValue<EditContext>>(1);
                builder.AddAttribute(2, "IsFixed", true);
                builder.AddAttribute(3, "Value", EditContext);
                builder.AddAttribute(4, "ChildContent", EditorContent);
                builder.CloseComponent();
                builder.CloseRegion();
            }
            else
                builder.AddContent(10, LoadingContent );
            builder.AddContent(20, ButtonContent);
        }
    }
}

```


### WeatherDataModal

Moving on to the real UI stuff.

This replaces `FetchData`.  It's similar.  The `Edit` and `View` buttons now pass the `ID` of the record.
```html
@page "/weatherdatamodal"
@using CEC.Blazor.Editor.Data
@namespace CEC.Blazor.Editor.Pages

<ModalDialog @ref="this.Modal"></ModalDialog>

<h1>Weather forecast</h1>

<p>This component demonstrates fetching data from a service.</p>

@if (this.ForecastService.Forecasts == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Temp. (F)</th>
                <th>Summary</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var forecast in this.ForecastService.Forecasts)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.TemperatureF</td>
                    <td>@forecast.Summary</td>
                    <td class="text-right">
                        <button class="btn btn-sm btn-secondary" @onclick="() => ShowViewDialog(forecast.ID)">View</button>
                        <button class="btn btn-sm btn-primary" @onclick="() => ShowEditDialog(forecast.ID)">Edit</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}
```

In code:

1. We now use the new `WeatherForecastControllerService`, and the Razor markup uses the service `Forecasts` list.
2. We load the `Forecasts` list in `WeatherForecastControllerService` as part of the form `OnInitializedAsync()`.
3. The two button handlers create a `ModalOptions` object and add the `ID` to pass into the controls.

```c#
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Pages
{
    public partial class WeatherDataModal : ComponentBase
    {
        [Inject] WeatherForecastControllerService ForecastService { get; set; }

        private ModalDialog Modal { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await ForecastService.GetForecastsAsync();
        }

        private async void ShowViewDialog(Guid id)
        {
            var options = new ModalOptions();
            {
                options.Set(ModalOptions.__Width, "80%");
                options.Set(ModalOptions.__ID, id);
            }
            await this.Modal.ShowAsync<WeatherViewer>(options);
        }

        private async void ShowEditDialog(Guid id)
        {
            var options = new ModalOptions();
            {
                options.Set(ModalOptions.__Width, "80%");
                options.Set(ModalOptions.__ID, id);
            }
            await this.Modal.ShowAsync<WeatherForecastEditor>(options);
        }
    }
}
```

### WeatherForecastEditor

The Editor is the container that sets up all the edit components and then updates the buttons in the UI as things change in the underlying EditContext and RecordEditorContext.  It contains a set of Boolean Properties for controlling the UI and button state.

On Initialization it:
1. Gets the record ID from `ModalOptions`.
2. Loads the `DbWeatherForecast` record in the controller - which loads `RecordData`, the `RecordCollection` object in the service.
3. Creates a new `RecordEditorContext`, passing in the `RecordCollection`.
4. Creates a `EditContext` with `RecordEditorContext` as the modal.
5. Notifies `RecordEditorContext` that the `EditContext` has changed.
6. Wires up the `EditContext.OnFieldChanged` event to a local `OnFieldChanged` event handler.

Key points to note:
1. The editor wires up a local property to the cascaded ModalDialog so it can lock and unlock the form and exit.
2. Contains the save and various exit methods.
3. `OnFieldChanged` sorts out the UI buttons, form locking and rendering.  It doesn't interact with the data - that's all done by the `RecordEditorContext`.

```c#
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public partial class WeatherForecastEditor : ComponentBase
    {
        public EditContext EditContext => _EditContext;
        private EditContext _EditContext = null;
        protected WeatherForecastEditContext RecordEditorContext { get; set; }
        [Inject] protected WeatherForecastControllerService ControllerService { get; set; }
        [CascadingParameter] private IModalDialog Modal { get; set; }
        private bool IsModal => this.Modal != null;
        private bool HasServices => this.IsModal && this.ControllerService != null;
        private bool IsDirtyExit;
        private bool IsDirty => RecordEditorContext.IsDirty;
        private bool IsValid => RecordEditorContext.IsValid;
        private bool IsLoaded => RecordEditorContext?.IsLoaded ?? false;
        private bool CanSave => this.IsDirty && this.IsValid;
        private bool CanExit => !this.IsDirtyExit;
        private string SaveButtonText => this.ControllerService.Forecast.ID.Equals(Guid.Empty) ? "Save" : "Update";

        protected async override Task OnInitializedAsync()
        {
            //await Task.Yield();
            if (this.HasServices && Modal.Options.TryGet<Guid>(ModalOptions.__ID, out Guid modalid))
            {
                await this.ControllerService.GetForecastAsync(modalid);
                this.RecordEditorContext = new WeatherForecastEditContext(this.ControllerService.RecordData);
                this._EditContext = new EditContext(RecordEditorContext);
                await this.RecordEditorContext.NotifyEditContextChangedAsync(this.EditContext);
                this.EditContext.OnFieldChanged += OnFieldChanged;
            }
            await base.OnInitializedAsync();
        }

        protected void OnFieldChanged(object sender, EventArgs e)
            => this.SetLock();

        private void SetLock()
        {
            this.IsDirtyExit = false;
            if (this.RecordEditorContext.IsDirty)
                this.Modal.Lock(true);
            else
                this.Modal.Lock(false);
            InvokeAsync(StateHasChanged);
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
                }
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
            => SetLock();
    }
}
```

The markup code is fairly standard editor fare.

1. You can see the use of the UIComponents to standardize the HTML.
2. `EditForm` is replaved by `ModalEditForm`.  It ensures stuff isn't rendered until it's loaded and cascades the `EditContext`.
3. The buttons use the various boolean Properties to control their display state.

```html
@namespace CEC.Blazor.ModalEditor
    <UIContainer>

        <UIFormRow>
            <UIColumn>
                <h2>Weather Forecast Editor</h2>
            </UIColumn>
        </UIFormRow>
    
    </UIContainer>

    <ModalEditForm EditContext="this.EditContext" Loaded="this.IsLoaded">
        <LoadingContent>
            ... loading
        </LoadingContent>
        <EditorContent>

            <UIContainer>
                <UIFormRow>
                    <UILabelColumn>
                        Date
                    </UILabelColumn>
                    <UIInputColumn Cols="3">
                        <InputDate class="form-control" @bind-Value="this.RecordEditorContext.Date"></InputDate>
                    </UIInputColumn>
                    <UIColumn Cols="3"></UIColumn>
                    <UIValidationColumn>
                        <ValidationMessage For=@(() => this.RecordEditorContext.Date) />
                    </UIValidationColumn>
                </UIFormRow>

                <UIFormRow>
                    <UILabelColumn>
                        Temperature &deg;C
                    </UILabelColumn>
                    <UIInputColumn Cols="2">
                        <InputNumber class="form-control" @bind-Value="this.RecordEditorContext.TemperatureC"></InputNumber>
                    </UIInputColumn>
                    <UIColumn Cols="4"></UIColumn>
                    <UIValidationColumn>
                        <ValidationMessage For=@(() => this.RecordEditorContext.TemperatureC) />
                    </UIValidationColumn>
                </UIFormRow>

                <UIFormRow>
                    <UILabelColumn>
                        Summary
                    </UILabelColumn>
                    <UIInputColumn>
                        <InputText class="form-control" @bind-Value="this.RecordEditorContext.Summary"></InputText>
                    </UIInputColumn>
                    <UIValidationColumn>
                        <ValidationMessage For=@(() => this.RecordEditorContext.Summary) />
                    </UIValidationColumn>
                </UIFormRow>

            </UIContainer>

        </EditorContent>

        <ButtonContent>
            <UIContainer>

                <UIFormRow>
                    <UIButtonColumn>
                        <UIButton CssColor="btn-success" Show="this.CanSave" ClickEvent="this.Save">@this.SaveButtonText</UIButton>
                        <UIButton CssColor="btn-danger" Show="this.IsDirtyExit" ClickEvent="this.DirtyExit">Exit Without Saving</UIButton>
                        <UIButton CssColor="btn-warning" Show="this.IsDirtyExit" ClickEvent="this.CancelExit">Cancel Exit</UIButton>
                        <UIButton CssColor="btn-secondary" Show="this.CanExit" ClickEvent="this.Exit">Exit</UIButton>
                    </UIButtonColumn>
                </UIFormRow>

            </UIContainer>

        </ButtonContent>
    </ModalEditForm>
```

### WeatherForecastEditorContext

Note:
1. The Properties exposing the underlying fields in the `RecordValues`, referenced all the way back to the `RecordCollection` object in the ControllerService.
2. The Property setters setting the `EditedValue` on the `RecordFieldValue`.
3. The Property setting calling `Validate` and precipitating the validation process throughout the edit components in the form - turning any control red and displaying any validation messages.
4. The definition of `Validators` for properties requiring validation.
5. The validators being loaded through `LoadValidationActions`.

```c#
using System;

namespace CEC.Blazor.Editor
{
    public class WeatherForecastEditContext : RecordEditContext, IRecordEditContext
    {
        public DateTime Date
        {
            get => this.RecordValues.GetEditValue<DateTime>(DbWeatherForecast.__Date.FieldName);
            set
            {
                this.RecordValues.SetField(DbWeatherForecast.__Date.FieldName, value);
                this.Validate();
            }
        }

        public string Summary
        {
            get => this.RecordValues.GetEditValue<string>(DbWeatherForecast.__Summary.FieldName);
            set
            {
                this.RecordValues.SetField(DbWeatherForecast.__Summary.FieldName, value);
                this.Validate();
            }
        }

        public int TemperatureC
        {
            get => this.RecordValues.GetEditValue<int>(DbWeatherForecast.__TemperatureC.FieldName);
            set
            {
                this.RecordValues.SetField(DbWeatherForecast.__TemperatureC.FieldName, value);
                this.Validate();
            }
        }

        public Guid WeatherForecastID
            => this.RecordValues.GetEditValue<Guid>(DbWeatherForecast.__ID.FieldName);

        public WeatherForecastEditContext(RecordCollection collection) : base(collection) { }

        protected override void LoadValidationActions()
        {
            this.ValidationActions.Add(ValidateSummary);
            this.ValidationActions.Add(ValidateTemperatureC);
            this.ValidationActions.Add(ValidateDate);
        }

        private bool ValidateSummary()
        {
            return this.Summary.Validation(DbWeatherForecast.__Summary.FieldName, this, ValidationMessageStore)
                .LongerThan(2, "Your description needs to be a little longer! 3 letters minimum")
                .Validate();
        }
        private bool ValidateDate()
        {
            return this.Date.Validation(DbWeatherForecast.__Date.FieldName, this, ValidationMessageStore)
                .NotDefault("You must select a date")
                .LessThan(DateTime.Now.AddMonths(1), true, "Date can only be up to 1 month ahead")
                .Validate();
        }

        private bool ValidateTemperatureC()
        {
            return this.TemperatureC.Validation(DbWeatherForecast.__TemperatureC.FieldName, this, ValidationMessageStore)
                .LessThan(70, "The temperature must be less than 70C")
                .GreaterThan(-60, "The temperature must be greater than -60C")
                .Validate();
        }
    }
}
```

### Validators

We build a custom validation process.  It's not rocket science and once you understand the principles it very flexible.

Skip down to the next section to see an implementation first before coming back to the the abstract `Validator` class.  It will make more sense.

```c#
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;

namespace CEC.Blazor.Editor
{
    public abstract class Validator<T>
    {
        public bool IsValid => !Trip;
        public bool Trip = false;
        public List<string> Messages { get; } = new List<string>();

        protected string FieldName { get; set; }
        protected T Value { get; set; }
        protected string DefaultMessage { get; set; } = "The value failed validation";
        protected ValidationMessageStore ValidationMessageStore { get; set; }
        protected object Model { get; set; }

        public Validator(T value, string fieldName, object model, ValidationMessageStore validationMessageStore, string message)
        {
            this.FieldName = fieldName;
            this.Value = value;
            this.Model = model;
            this.ValidationMessageStore = validationMessageStore;
            this.DefaultMessage = string.IsNullOrWhiteSpace(message) ? this.DefaultMessage : message;
        }

        public virtual bool Validate(string message = null)
        {
            if (!this.IsValid)
            {
                message ??= this.DefaultMessage;
                // Check if we've logged specific messages.  If not add the default message
                if (this.Messages.Count == 0) Messages.Add(message);
                //set up a FieldIdentifier and add the message to the Edit Context ValidationMessageStore
                var fi = new FieldIdentifier(this.Model, this.FieldName);
                this.ValidationMessageStore.Add(fi, this.Messages);
            }
            return this.IsValid;
        }

        protected void LogMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) Messages.Add(message);
        }
    }
}
```

#### StringValidator

This is a `Validator` implementation for strings.

The key to how validators work is in the static class.  `Validation` is an extension method for `string`.  When you call it on a string it creates a `StringValidator` object and returns it.  You now have a `StringValidator` that you can call a validation method on.  Each validation method returns a reference to the validation object.  You can chain as many as you like together with their specific messages.  You call the base `Validate` method to complete the process.  It logs any validation messages into the `ValidationMessageStore`, and returns true or false.  The `ValidationMessageStore` is linked back to the `EditContext`. 

```c#
using Microsoft.AspNetCore.Components.Forms;
using System.Text.RegularExpressions;

namespace CEC.Blazor.Editor
{
    public static class StringValidatorExtensions
    {
        public static StringValidator Validation(this string value, string fieldName, object model, ValidationMessageStore validationMessageStore, string message = null)
        {
            var validation = new StringValidator(value, fieldName, model, validationMessageStore, message);
            return validation;
        }
    }

    public class StringValidator : Validator<string>
    {
        public StringValidator(string value, string fieldName, object model, ValidationMessageStore validationMessageStore, string message) : base(value, fieldName, model, validationMessageStore, message) { }

        public StringValidator LongerThan(int test, string message = null)
        {
            if (string.IsNullOrEmpty(this.Value) || !(this.Value.Length > test))
            {
                Trip = true;
                LogMessage(message);
            }
            return this;
        }

        public StringValidator ShorterThan(int test, string message = null)
        {
            
            if (string.IsNullOrEmpty(this.Value) || !(this.Value.Length < test))
            {
                Trip = true;
                LogMessage(message);
            }
            return this;
        }

        public StringValidator Matches(string pattern, string message = null)
        {
            if (!string.IsNullOrWhiteSpace(this.Value))
            {
                var match = Regex.Match(this.Value, pattern);
                if (match.Success && match.Value.Equals(this.Value)) return this;
            }
            this.Trip = true;
            LogMessage(message);
            return this;
        }
    }
}
```

### What Makes all this Work?

If you havn't dug through the AspNetCore code on Github how all the edit stuff hangs together can be a little baffling.
 
There's an intrincate set of relationships and links within the edit form components that make all this work.  The net result is a lot of co-ordinated re-rendering of components to display validation problems and the right buttons displayed based on the edit state.

We've covered the intial load process for the form.  `<UILoader Loaded="this.IsLoaded">` controls when the form gets rendered. Once we have a live `EditContext` and interlinked `RecordEditorContext` `IsLoaded` is true.  All the Input controls get rendered and linked into the cascaded `EditContext`.  An initial validation has been run on `RecordEditorContext` as part of `NotifyEditContextChangedAsync`, so the form will display validation messages.

Lets suppose we change the Summary.  Here's the important code snippet from `InputBase`.

```c#
// Code snippet from InputBase.cs
protected TValue? CurrentValue
{
    get => Value;
    set
    {
        var hasChanged = !EqualityComparer<TValue>.Default.Equals(value, Value);
        if (hasChanged)
        {
            Value = value;
            _ = ValueChanged.InvokeAsync(Value);
            EditContext.NotifyFieldChanged(FieldIdentifier);
        }
    }
}
```

On exiting the Summary edit control, the `InputText` control sets `Value = value` (`Value` is the property value in `RecordEditorContext`), invokes it's own `ValueChanged` event then calls `NotifyFieldChanged` on `EditContext`.  This precipitates two processes.

#### RecordEditorContext Property Set

The property set in `RecordEditorContext` sets the `EditedValue` of the `RecordFieldValue` to the new value and then kicks off `Validate` which performs a validation.  The set, `Validate` and subsiquent validations are all synchronous operations.  They complete before `NotifyFieldChanged` is called on `EditContext`.  This is important, the validation process is complete before `EditContext` runs code or kicks off any events.  The last action of `Validate` is to notify the `EditContext` that the Validation State has changed - `this.EditContext.NotifyValidationStateChanged()`. 

```c#
// Code snippet from EditContext.cs
public void NotifyValidationStateChanged()
{
    OnValidationStateChanged?.Invoke(this, ValidationStateChangedEventArgs.Empty);
}
```
`EditContext` kicks off it's own `OnValidationStateChanged` event.  All the Input controls and `ValidationMessage` instances wire into this event as shown below.  The input controls change their color and render if there's a validation message for their property.  `ValidationMessage` instances look up their relevant message and display it if there is one.    

```c#
public override Task SetParametersAsync(ParameterView parameters)
{
    ....
    EditContext.OnValidationStateChanged += _validationStateChangedHandler;
    ...
}
```

All the affected fields get notifications of changes and can individually update and re-render themselves.

#### EditContext.FieldChanged
The Input control passes `NotifyFieldChanged` a `FieldIdentifier` object - the property name it's linked to and the `Model` object - in our case `RecordEditorContext`.  `EditContext` updates it's internal FieldStates collection, logging the `FieldIdentifier` as `IsModified` in the `FieldState` object associated with the `FieldIdentifier`.  Finally it triggers the `OnFieldChanged` event.  The code snippet below shows `NotifyFieldChanged` in `EditContext`. 

```c#
// Code snippet from EditContext.cs
public void NotifyFieldChanged(in FieldIdentifier fieldIdentifier)
{
    GetOrAddFieldState(fieldIdentifier).IsModified = true;
    OnFieldChanged?.Invoke(this, new FieldChangedEventArgs(fieldIdentifier));
}
```

The last action is in the edit form.  We wired up a local method `OnFieldChanged` to `EditContext.OnFieldChanged`.

```c#
// Code snippet from WeatherForecastEditor.razor.cs
protected void OnFieldChanged(object sender, EventArgs e)
    => this.SetLock();

private void SetLock()
{
    this.IsDirtyExit = false;
    if (this.RecordEditorContext.IsDirty)
        this.Modal.Lock(true);
    else
        this.Modal.Lock(false);
    InvokeAsync(StateHasChanged);
}
```

`SetLock` clears `IsDirtyExit` - if we've tried to exit a dirt form and then subsiquently changed a field value then w've cancelled the exit.  We then check if `RecordEditorContext` is dirty.  In our case it is so we lock the browser window.  We finally render the control which sorts out the buttons.  Note that if we had already edited the value once and then changed it back to the original, `RecordEditorContext` would be clean.  You could exit and the *Update* button would dissappear.

### WeatherForecast Viewer

I won't go into detail.  You can review the code in the Repo to see how it's put together.  It's a simple version of the editor, accessing the controller service record directly for it's data, and using a custom `InputReadOnlyText` component to display values.

## Wrap Up

Much of the infrastructure I've put together here is simplistic.  The services and data records would make greater use of interfaces and core abstract classes to provide abstraction and implement boilerplate code.  A set of articles - [Building a Database Application](https://www.codeproject.com/Articles/5279560/Building-a-Database-Application-in-Blazor-Part-1-P)-  covers such a framework in more detail.  Note the current article set is based on my NetCore 3.1 4 month old Framework and will be revised over the next week.


What I've covered here is a methodology for editing records.  It's not for everybody.  It depends on your mindset on data, and the environment you have to work in.  If nothing more, I hope it provokes you to question how you view and deal with data.
