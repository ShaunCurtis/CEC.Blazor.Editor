# The Modal Editor

To set the context of this article, there have been many discussions, articles and proposals since Blazor was first released on how to handle edit forms.  Specifically how to stop, or at least warn, the user when leaving a dirty form.  The problem is not specific to Blazor: all Single Page Applications and Web Sites have the same challenges.

In a classic web form, every navigation is a get or a post back to the server. We can use the inbuilt `window.beforeunload` event to warn a user that they have unsaved data on the page.  Not great, but at least something - we'll be using it later.  This technique falls flat in SPA's.  What looks to the outsider like a navigation event isn't.  The `NavigationManager` intercepts any navigation attempt from the page, triggers it's own `LocationChanged` event and sinks the request.  The `Router`, wired into this event, does it's wizardry, and loads the new component set into the page.  No real browser navigation takes place, so there's nothing for the browser's `beforeunload` event to catch.

It's up to the programmer to write code that stops a user moving away from a dirty form.  That's easier said than done when your application depends on the URL navigation pretext.  Toolbars, navigation side bars and many buttons submit URLs to navigate around the application.  Think of the out-of-the-box Blazor template.  There's all the links in the left navigation, about in the top bar,....  

Personally I have a serious issue with the whole routing charade: an SPA is an application, not a website, but I think I must be a minority of one!  This article is for the majority.

All the NetCore 3.1 solutions I've come across were cludges in one shape or another, I've created more that one myself.  What the community hoped for were changes in NetCore 5, specifically some extra functionality in `NavigationManager` to cancel or block navigation requests.  That didn't happen: I don't think there was team concensus on the right solution, so we're back to square one.

What I cover in this article is my latest approach to the problem.  It's not perfect, but I don't think we will ever get a near perfect solution until we get some new browser standards allowing a switch to *SPA* mode and control over toolbar navigation.

## Controlling SPA Navigation

The first challenge - How do you stop the user accessing any of the button/links navigation stuff on a standard page?

1. Build a custom `NavigationManager`.  Not a bad idea, but that's a core bit of Blazor infrastructure with many knock on consequences.  I'm steering clear.
2. Build a custom `Router`.  Also not a bad idea.  I've done this, there's a released package on Nuget, a Github Repo and an article on this site.  It's still a cludge with some issues that I want to get away from.
3. Build a navigation component that you use throughout your application for **ALL** navigation.  Also not a bad idea, but needs rigorous disipline to enforce in any team environment.  One deviation and BANG!
4. Use a Modal Dialog, locking out the rest of the form.  My prefered option and the one I'll explore and develop here.


This article walks you through some **Experimental** code to demonstate what I'm doing in detail.  To follow this, rather than just pasively read this article, strike up a out-of-the-box Blazor Server application in Visual Studio.  In the code samples below my project is called `CEC.Blazor.Editor`.

> **Experimental** code in definitely not production ready. There's almost no error checking, blah, blah,...  It's miminalist to try and preserve clarity.

## Building the Dialog

First we build a basic modal dialog.  This is CSS framework agnostic. You're welcome to adopt it to fit with Bootstrap et al.

### ModalDialog1

Add a razor component to *Shared* with a code behind file called `ModalDialog1`. Add the following code.  It's a pretend editor for a weather forecast.

> Yes, It's Bootstrap before someone hangs me out to dry for the statement above!

```html
@if (this.Display)
{
    <div class="modal-background">
        <div class="modal-content">
            <div class="container-fluid">
                <div class="row">
                    <div class="col">
                        Date
                    </div>
                    <div class="col">
                        @DateTime.Now.ToLongDateString();
                    </div>
                </div>
                <div class="row">
                    <div class="col">
                        Temperature C
                    </div>
                    <div class="col">
                        0 deg C
                    </div>
                </div>
                <div class="row">
                    <div class="col">
                        Temperature C
                    </div>
                    <div class="col">
                        32 deg F
                    </div>
                </div>
                <div class="row">
                    <div class="col">
                        Summary
                    </div>
                    <div class="col">
                        Another Beast-from-the-East day
                    </div>
                </div>
                <div class="row">
                    <div class="col-12 text-right">
                        <button class="btn btn-secondary" @onclick="() => Hide()">Close</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
}
```
```c#
using Microsoft.AspNetCore.Components;

namespace CEC.Blazor.Editor.Shared
{
    public partial class ModalDialog1 : ComponentBase
    {

        public bool Display { get; private set; }

        public void Show()
        {
            this.Display = true;
            this.InvokeAsync(this.StateHasChanged);
        }

        public void Hide()
        {
            this.Display = false;
            this.InvokeAsync(this.StateHasChanged);
        }
    }
}
```

Note:
1. We're using `Show` to control the display of the modal.  No Javascript and Css required to toggle it.
2. We have two public methods to `Show` or `Hide` the dialog.
3. We invoke `StateHasChanged` to render the component.  There's no external trigger to re-render the component (no Parameters have changed and no UI event has occured), so we need to force a render.  Comment them out and see what happens!

#### FetchData1

We use the `FetchData` page component as our template, so create a new Razor Component in `Pages` and call it `FetchData1`.  Create a `FetchData1.razor.cs` code behind file.  Remember to mark the class as `partial`.

Add the following code.

```html
@page "/fetchdata1"

@using CEC.Blazor.Editor.Data

<h1>Weather forecast</h1>

<p>This component demonstrates fetching data from a service.</p>

@if (forecasts == null)
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
            @foreach (var forecast in forecasts)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.TemperatureF</td>
                    <td>@forecast.Summary</td>
                    <td><button class="btn btn-primary" @onclick="() => ShowModalDialog()">Edit</button></td>
                </tr>
            }
        </tbody>
    </table>
}

<ModalDialog1 @ref="this.Modal"></ModalDialog1>
```
```c#
using CEC.Blazor.Editor.Data;
using CEC.Blazor.Editor.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Pages
{
    public partial class FetchData1 : ComponentBase
    {
        [Inject] WeatherForecastService ForecastService { get; set; }

        private WeatherForecast[] forecasts;

        private ModalDialog1 Modal { get; set; }

        protected override async Task OnInitializedAsync()
        {
            forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
        }
        private void ShowModalDialog()
        {
            this.Modal.Show();
        }
    }
}
```

Note:
1. We've added an *Edit* button to each line to access the editor, linked to `ShowModalDialog`.
2. We've added `ModalDialog1` as a component at the bottom of the page, and `@ref` it to a property.
3. We've added a `ModalDialog1` property.
4. We've added a `ShowModalDialog` method to open the Modal Dialog. 

We need to make a couple of final changes before building.
#### NavMenu.razor

We need to add a link in the `NavMenu` to our new page.
```html
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="fetchdata1">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Fetch data 1
            </NavLink>
        </li>

```

#### Site.css

We need to add the css for the Modal Dialog to the site css file.  We can move this code to a component css file for production, but it easier to edit on-the-fly in *site.css*.   

```css
div.modal-background {
    display: block; 
    position: fixed; 
    z-index: 1; /* Sit on top */
    left: 0;
    top: 0;
    width: 100%; /* Full width */
    height: 100%; /* Full height */
    overflow: auto; /* Enable scroll if needed */
    background-color: rgb(0,0,0); /* Fallback color */
    background-color: rgba(0,0,0,0.4); /* Black w/ opacity */
}

div.modal-content {
    background-color: #fefefe;
    margin: 10% auto; 
    padding: 10px;
    border: 2px solid #888;
    width: 90%;
}
```

### Running Version 1

Run the application and go to *FetchData1*.  Click on an edit button.  All of the links on the underlying page are disabled. The three exit paths are:
1. The *Close* button.
2. Enter a new URL.
3. Close the browser tab or close the browser.

I know there's a fourth, fifth,... (kill the process,hit the power switch,...) but nothing can stop those.

![image](images/FetchData-1-0.png)

## Version 2

Let's now close these three exits.  I've created *version2* files in the repo, but you can just update your original files if you wish.

### Site.js

Add a *js* folder to *wwwroot*, and add a *site.js* file.

Add the following code.  
1. We define `window.cecblazor_showExitDialog` - an event function that pops up the browser "Are You Sure" implementation.  What appears differs between browsers, but some sort of exit challenge is always raised.
2. We define `window.cecblazor_setEditorExitCheck` which we will call though Blazor's `JsInterop` to add and remove the event handler.

```js
window.cecblazor_setEditorExitCheck = function (show) {
    if (show) {
        window.addEventListener("beforeunload", cecblazor_showExitDialog);
    }
    else {
        window.removeEventListener("beforeunload", cecblazor_showExitDialog);
    }
}

window.cecblazor_showExitDialog = function (event) {
    event.preventDefault();
    event.returnValue = "There are unsaved changes on this page.  Do you want to leave?";
}
```

We need to include this `js` file in our application.  Update `Host._cshtml`

```html
.....
    </div>

    <script src="_framework/blazor.server.js"></script>
    <script src="js/site.js"></script>
</body>
</html>
```

### Modal Dialog

We need to add more functionality to the modal dialog to simulate a dirty form.  Update the button row in `ModalDialog`.

```html
<div class="row">
    <div class="col-12 text-right">
        <button class="btn @this.DirtyButtonCss" @onclick="() => SetDirty()">@this.DirtyButtonText</button>
        @if (this.DirtyExit)
        {
            <button class="btn btn-danger" @onclick="() => DirtyHide()">Dirty Close</button>
            <button class="btn btn-dark" @onclick="() => CancelHide()">Cancel</button>
        }
        else
        {
            <button class="btn btn-secondary" @onclick="() => Hide()">Close</button>
        }
    </div>
</div>
```

Update the code behind file adding:

1. Boolean properties - `IsDirty`, `IsLocked` and `DirtyExit` - to control if the control state and button display.
2. Inject `IJSRuntime` for access to `JSInterop`.
3. CSS string properties to control the button CSS.
4. Various button event handlers to switch the states.  you should be able to work out the logic yourself.
5. `SetPageExitCheck` to interact with the page JS, and turn the browser exit challenge on and off.


```c#
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
```
### Running Version 2

Now run the application.  Click on an edit button.  All of the links on the underlying page are disabled. Click on *Set Dirty* to simulate editing a field.  Now try to close or navigate away or close the browser.  All the options should be covered

1.  Closing the browser, navigating away in the toolbar, F5 or closing the browser window hits the browser "Do you want to Leave Dialog".
2. Close gives you the *Dirty Exit* option.
3. Clicking anywhere is the browser window does nothing.
 

![image](images/FetchData-2-0.png)

## Version 3

Now lets move on to creating a ModalDialog framework we can use in production.  This will be a modal dialog wrapper that we can use to display any component.  In this case the `WeatherViewer` and `ModalEditor` components.  I've created a *Component* directory structure to organise the code, but keep it all in the base namespace for simplicity.

First we need to add some some utility classes.

#### ModalResult and ModalResultType

In *Components/ModalDialog* add a `ModalResult` class.  The code is below.

The class provides a container for return values when the Dialog is closed.  It returns an object `Data` and a `ModalResultType`.  The static constructors are for building an instance.

```c#
namespace CEC.Blazor.Editor
{
    public enum ModalResultType { NoSet, OK, Cancel, Exit }

    public class ModalResult
    {
        public ModalResultType ResultType { get; private set; } = ModalResultType.NoSet;
        public object Data { get; set; } = null;

        public static ModalResult OK() => new ModalResult() { ResultType = ModalResultType.OK };
        public static ModalResult Exit() => new ModalResult() { ResultType = ModalResultType.Exit };
        public static ModalResult Cancel() => new ModalResult() { ResultType = ModalResultType.Cancel };
        public static ModalResult OK(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.OK };
        public static ModalResult Exit(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.Exit };
        public static ModalResult Cancel(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.Cancel };
    }
}
```
#### ModalOptions

Now add a `ModalOptions` class. It's a container to pass configuration options into the modal dialog.  It's a simple `IEnumerable` collection object with getters and setters.

```c#
using System.Collections;
using System.Collections.Generic;

namespace CEC.Blazor.Editor
{
    public class ModalOptions :IEnumerable<KeyValuePair<string, object>>
    {
        // Ststic List of options - only one currently defined
        public static readonly string __Width = "Width";

        private Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var item in Parameters)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        public T Get<T>(string key)
        {
            if (this.Parameters.ContainsKey(key))
            {
                if (this.Parameters[key] is T t) return t;
            }
            return default;
        }

        public bool TryGet<T>(string key, out T value)
        {
            value = default;
            if (this.Parameters.ContainsKey(key))
            {
                if (this.Parameters[key] is T t)
                {
                    value = t;
                    return true;
                }
            }
            return false;
        }

        public bool Set(string key, object value)
        {
            if (this.Parameters.ContainsKey(key))
            {
                this.Parameters[key] = value;
                return false;
            }
            this.Parameters.Add(key, value);
            return true;
        }
    }
}
```

### IModalDialog

Add a `IModalDialog` interface. It defines the public properties and methods that any modal dialog we build need to implement. It provides an abstraction layer between our code and specific modal dialog implmentations - a clean CSS version, a BootStrap version, ....

```c#
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public interface IModalDialog
    {
        public ModalOptions Options { get; }

        Task<ModalResult> ShowAsync<TModal>(ModalOptions options) where TModal : IComponent;
        void Dismiss();
        void Close(ModalResult result);
        void Update(ModalOptions options = null);
        void Lock(bool setlock);
    }
}
```

### ModalDialog

Now add `ModalDialog`, the base CSS agnostic implementation of `IModalDialog`.

The Razor markup is pretty simple and similar to the prototype we coded:
1. `Display` turns it off and on.
2. The ModalDialog isntance is cascaded so wrapped components has access to the Modal Dialog through the `IModalDialog` interface.
3. Width can be set through `ModalOptions`.
4. The content is built in a `RenderFragment` named `_Content`.

```html
@namespace CEC.Blazor.Editor
@implements IModalDialog

@if (this.Display)
{
<CascadingValue Value="(IModalDialog)this">
    <div class="modal-background">
        <div class="modal-content" style="@this.Width">
            @this._Content
        </div>
    </div>
</CascadingValue>
}
```

In the code:

1. We have a `ModalOptions` property which get's passed in through `ShowAsync`.
2. We get the `Width` from `ModalOptions`.
3. We use a `TaskCompletionSource` object to provide async behaviour and a `Task` the caller can `await`.
4. `ShowAsync` uses generics. `TModal` can be any component that implements `IComponent` - `ComponentBase` does.
5. `ShowAsync` builds `_Content` using a `RenderTreeBuilder`: it adds a new instance of component type `TModal` to `_Content`.  It then sets `Display` to `true` and re-renders the modal dialog. If `TModal` is `WeatherEditor`,  the control effectively has `<WeatherEditor></WeatherEditor>` as it's content. 
6. `Dismiss` and `Close` close the Modal dialog by setting `Display` to false and rendering the control.  They also clear the content and set the `Task` the caller may be awaiting to completed.
7. The rest of the code is a modified version of the prototype code.


```c#
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor
{
    public partial class ModalDialog : IModalDialog
    {
        [Inject] private IJSRuntime _js { get; set; }

        public ModalOptions Options { get; protected set; } = new ModalOptions();

        public bool Display { get; protected set; }

        public bool IsLocked { get; protected set; }

        protected RenderFragment _Content { get; set; }

        protected string Width => this.Options.TryGet<string>(ModalOptions.__Width, out string value) ? $"width:{value}" : string.Empty;

        protected TaskCompletionSource<ModalResult> _ModalTask { get; set; } = new TaskCompletionSource<ModalResult>();

        public Task<ModalResult> ShowAsync<TModal>(ModalOptions options) where TModal : IComponent
        {
            this.Options = options ??= this.Options;
            this._ModalTask = new TaskCompletionSource<ModalResult>();
            this._Content = new RenderFragment(builder =>
            {
                builder.OpenComponent(1, typeof(TModal));
                builder.CloseComponent();
            });
            this.Display = true;
            InvokeAsync(StateHasChanged);
            return this._ModalTask.Task;
        }

        /// <summary>
        /// Method to update the state of the display based on UIOptions
        /// </summary>
        /// <param name="options"></param>
        public void Update(ModalOptions options = null)
        {
            this.Options = options ??= this.Options;
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Method called by the dismiss button to close the dialog
        /// sets the task to complete, show to false and renders the component (which hides it as show is false!)
        /// </summary>
        public async void Dismiss()
        {
            _ = this._ModalTask.TrySetResult(ModalResult.Cancel());
            this.Display = false;
            this._Content = null;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Method called by child components through the cascade value of this component
        /// sets the task to complete, show to false and renders the component (which hides it as show is false!)
        /// </summary>
        /// <param name="result"></param>
        public async void Close(ModalResult result)
        {
            _ = this._ModalTask.TrySetResult(result);
            this.Display = false;
            this._Content = null;
            await InvokeAsync(StateHasChanged);
        }

        private void SetPageExitCheck(bool action)
        {
            _js.InvokeAsync<bool>("cecblazor_setEditorExitCheck", action);
        }

        public void Lock(bool setlock)
        {
            if (setlock && !this.IsLocked)
            {
                this.IsLocked = true;
                this.SetPageExitCheck(true);
            }
            else if (this.IsLocked && !setlock)
            {
                this.IsLocked = false;
                this.SetPageExitCheck(false);
            }
        }
    }
}
```
## Weather Components

I've shown only `WeatherEditor` here.  `WeatherViewer` is a simpler version with no "Dirty" functionality.  See the Repo for the code.

### WeatherEditor

The Razor code is the same as we had in the prototype modal dialog.

```html
@namespace CEC.Blazor.Editor
<div class="container-fluid">
    <div class="row">
        <div class="col">
            <h2>Weather Editor</h2>
        </div>
    </div>
    <div class="row">
        <div class="col">
            Date
        </div>
        <div class="col">
            @DateTime.Now.ToLongDateString()
        </div>
    </div>
    <div class="row">
        <div class="col">
            Temperature C
        </div>
        <div class="col">
            0 deg C
        </div>
    </div>
    <div class="row">
        <div class="col">
            Temperature C
        </div>
        <div class="col">
            32 deg F
        </div>
    </div>
    <div class="row">
        <div class="col">
            Summary
        </div>
        <div class="col">
            Another Beast-from-the-East day
        </div>
    </div>
    <div class="row">
        <div class="col-10 text-right">
            <button class="btn @this.DirtyButtonCss" @onclick="() => SetDirty()">@this.DirtyButtonText</button>
        </div>
        <div class="col-2 text-right">
            @if (this.DoDirtyExit)
            {
                <button class="btn btn-danger" @onclick="() => DirtyExit()">Dirty Close</button>
                <button class="btn btn-dark" @onclick="() => CancelExit()">Cancel</button>
            }
            else
            {
                <button class="btn btn-secondary" @onclick="() => Exit()">Close</button>
            }
        </div>
    </div>
</div>
```

Much of the control code is also extracted from the prototype code.  We pick up the cascaded `IModalDialog` to use it's public interface methods to interact with the ModalDialog wrapper.  For example `Exit` checks if the control `IsDirty`.  If it's clean, it calls `this.Modal?.Close(ModalResult.OK())` which closes the dialog wrapper and destroys the instance of this class.

```c#
using Microsoft.AspNetCore.Components;

namespace CEC.Blazor.Editor
{
    public partial class WeatherEditor : ComponentBase
    {
        [CascadingParameter] private IModalDialog Modal { get; set; }

        public bool IsDirty { get; set; }

        public bool IsLocked { get; private set; }

        private bool DoDirtyExit { get; set; }

        private string DirtyButtonCss => this.IsDirty ? "btn-danger" : "btn-success";

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
```

### FetchDataModal

Finally we create a new version of `FetchData` called `FetchDataModal` in *Pages*.

The Razor markup is very similar to the prototypes.
1. We're using the `ModalDialog` version of modal dialog.
2. We now have two buttons for each row to show the Editor or Viewer.

```html
@page "/fetchdatamodal"

@using CEC.Blazor.Editor.Data

<h1>Weather forecast</h1>

<p>This component demonstrates fetching data from a service.</p>

@if (forecasts == null)
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
            @foreach (var forecast in forecasts)
            {
            <tr>
                <td>@forecast.Date.ToShortDateString()</td>
                <td>@forecast.TemperatureC</td>
                <td>@forecast.TemperatureF</td>
                <td>@forecast.Summary</td>
                <td><button class="btn btn-secondary" @onclick="() => ShowViewDialog()">View</button></td>
                <td><button class="btn btn-primary" @onclick="() => ShowEditDialog()">Edit</button></td>
            </tr>
            }
        </tbody>
    </table>
}

<ModalDialog @ref="this.Modal"></ModalDialog>

```
Very similar to the prototype with:

1. `ShowViewDialog` setting up `ModalOptions` and then calling `await this.Modal.ShowAsync<WeatherViewer>(options)`.  We're running async and waiting on the dialog to open and then close before completing, and passing type `WeatherViewer` for `Modaldialog` to display.
2. `ShowEditDialog` does as above, but loads `WeatherViewer` instead.  

```c#
using CEC.Blazor.Editor.Data;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace CEC.Blazor.Editor.Pages
{
    public partial class FetchDataModal : ComponentBase
    {
        [Inject] WeatherForecastService ForecastService { get; set; }

        private WeatherForecast[] forecasts;

        private ModalDialog Modal { get; set; }

        protected override async Task OnInitializedAsync()
        {
            forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
        }

        private async void ShowViewDialog()
        {
            var options = new ModalOptions();
            options.Set(ModalOptions.__Width, "60%");
            await this.Modal.ShowAsync<WeatherViewer>(options);
        }

        private async void ShowEditDialog()
        {
            var options = new ModalOptions();
            options.Set(ModalOptions.__Width, "80%");
            await this.Modal.ShowAsync<WeatherEditor>(options);
            // any code to execute after the editor is complete goes here
        }
    }
}
```

## Wrap Up

Hopefully I've demonstrated some techniques and strategies for dealing with edit state in Blazor and SPAs.  Some key points to take from this article:
1. You can block all the normal exit routes a user has from a dirty form.
2. You can build HTML dialogs that act in a similar manner to modal dialogs in desktop applications.
3. Modal Dialogs can be async and you can wait on their completion.

There will be second article shortly covering how to use this infrastructure in a real editor setting, and how to integrate edit state and validation state into records.

