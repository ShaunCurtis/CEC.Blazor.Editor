# Building an Editor Framework

This is the other half of a pair of articles looking at how to implement edit form control in Blazor.

The first article looked at how to control what the user could do once a form was dirty - how to stop a user unintentionally exiting.  This article looks at building a framework to detect when the dataset is dirty and or invalid.

I think many would consider that Blazor already has all the functionality you need to edit data.  Why do you need to re-invent the wheel?  If you fervently believe that then read no further.  This article is not for you.

C# 9 introduced the `Record` type, creating an immutable reference type.  The property `{get; init;}` lets us create an immutable property.  Microsoft have obviously been re-thinking data editing!

I've always been a firm believer in maintaining the integrity of records and recordsets read from databases.  What you see in your reference record or recordset is what is in the database.  If you want to edit something, make a copy, change the copy, submit the copy back to the database and refresh your reference copy from the database.

The editing framework I use, and is what this article is about, implements those principles.

## Overview

This short discussion and the example project uses the out-of-the-box Blazor WeatherForecast record as the starting point.

The `DbWeatherForecast` class represents the record read from the database.  It's declared as a `class`, not a `record` because only the properties that represent the database fields are immutable.  `DbWeatherForecast` has methods to build and read data from a `RecordCollection`.  A `RecordCollection` is a collection of `RecordFieldValue` objects.  Each represents a field/property in `DbWeatherForecast`.  A `RecordFieldValue` has an `EditedValue` field which is set by the editor and an `IsDirty` property to represent it's state. `RecordCollection` and `RecordFieldValue` provide controlled access to the underlying data values.

In the editor `WeatherForecastEditContext` exposes the editable properties from the `RecordCollection`.  It has a symbiotic relationship with the `EditContext`, tracking the edit state of the `RecordCollection` and providing validation of any properties that require data validation.


## Infrastructure classes

As always we need some supporting classes. 

#### RecordFieldValue

As already discussed, `RecordFieldValue` holds information about a single field in a Record set.  Note:

1. The properties derived from the actual record are `{get; init;}`.  They can only be set when an instance of `RecordFieldValue` is created.
2. `FieldName` is property name for the field.  We use it to make sure we using the same string value throughout the application.
2. `Value` is the database value of the field.
3. `ReadOnly` is self-evident for derived/calculated fields.
4. `DisplayName` is the string to use when displaying the name of the field.
5. `EditedValue` is the current value of the field in the edit context.  The getter ensures that on first read of the property, if it hasn't already been set, it's set the `Value`.
6. `IsDirty` does an equality check on `Value` against `EditedValue` to determine if the Field is dirty.
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

`RecordEditContext` is the base class for record edit contexts.  It contains the boilerplate code.  We'll lokk at it in more detail when we look at the `WeatherForecastEditContext`.  Key points to note:

1. It's initiliaiser requires a `RecordCollection` object.
2. It holds a reference to the valid `EditContext` and expects to be notified if it changes.
3. It handles Validation for the `EditContext` by wiring into `EditContext.OnValidationRequested`.
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
            ValidationRequested(this, ValidationRequestedEventArgs.Empty);
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
                ValidationMessageStore = new ValidationMessageStore(EditContext);
                // Wire up to the Editcontext to service Validation Requests
                EditContext.OnValidationRequested += ValidationRequested;
            }
            // Call a validation on the current data set
            Validate();
            return Task.CompletedTask;
        }

        private void ValidationRequested(object sender, ValidationRequestedEventArgs args)
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
                EditContext.NotifyValidationStateChanged();
                this.Validating = false;
            }
        }
    }
}
```

## DbWeatherForecast

The Weather forecast record.  While we only create these records on the fly, a normal application would gert these records from a database.

Note:

1. There's a static declared `RecordFieldValue` for each database property/field in the class.  In a larger application these should be declared in a central `DataDictionary`. 
2. The "Database" properties are all declared `{ get; init; }` to make them immutable.
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

I've split the data access into a data and a controller service.  We may be creating a dummy data set, but I'm mimicing the normal practice.  In a production system this would run on interfaces and boilerplated base code implementations.

## WeatherForecastDataService

The Data Service:
1. Builds the dummy data set on startup.
2. Provides a fairly standard set of `CRUD` data operations on that dataset.

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
        {
            PopulateForecasts();
        }

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

The controller service provides the interface between the data and the UI.  Most of the Properties and Methods are pretty self-evident.


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
        {
            this.DataService = weatherForecastDataService;
        }

        public async Task GetForecastsAsync()
        {
            this.Forecasts = await DataService.GetForecastsAsync();
        }

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

## UI Components

One of my gripes with most people's code is the amount of repetitive HTML markup.  Editor/Display/List forms are good exmples.  In this project I've moved most of the repetitive HTML markup into *UI Components*.  I'm a firm believer is getting all the HTML markup out of high level components.  A formatting issue such as not enough spacing, fix it in one place and it's fixed everywhere!

Let's take a look at a couple of examples here.

#### UIFormRow

It's not rocket science, far from it.  `ChildContent` is the default definition for what gets entered between the opening and closing statements.
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
    ....
</UIFormRow>
```

#### UIButton

Again, simple, but it keeps the high level stuff minimal.
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

## cc