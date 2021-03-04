# The Editor Controller

This article describes a methodology for disabling navigation though links, buttons the URL bar,... everywhere except within a component.  The functionality is implemented in a standard Blazor Component.  All navigation is disabled except from content within the component.

I've written articles on how to do this with Modal Dialogs, View Managers and a custom router.  While all of these work, this is the simplest to implement in a normal site, and can be used on any page.  The same basic methodology can be applied to any SPA framework.

![Dirty Page](https://raw.githubusercontent.com/ShaunCurtis/CEC.Blazor.Editor/master/images/Dirty-Dialog.png?token=AF6NT3LJWA6SOY4PXWNGWHLAEPYC4)

## Code Repository

The code is available here at Gitub - [CEC.Blazor.Editor](https://github.com/ShaunCurtis/CEC.Blazor.Editor)cand demo [here on my Hydra Site](https://cec-blazor-examples.azurewebsites.net/editor/fetchdata3)

## Taster

If you want to see the component in action got to [the page on my Demo Site](https://cec-blazor-examples.azurewebsites.net/editor/fetchdata3).  It's a mockup.  There are two choices for editing a record - *In Page* opens an editor card in the page, *Editor* routes to an edit page.  The *Set Dirty* button simulates making edits to data in the page and *Set Clean* saving them.  Set the editor to Dirty and try to click on the links in the page, or navigate elsewhere.

The Editor page looks like this:

```html
@page "/weditor"
@page "/modal/weditor"

<NavigationController Cascade="true">
    <WeatherPageEditor @ref="this._Editor" CloseAction="Exit"></WeatherPageEditor>
</NavigationController>

@code {
    [Inject] NavigationManager NavManager { get; set; }
    private WeatherPageEditor _Editor { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            this._Editor.Show();
        base.OnAfterRender(firstRender);
    }

    private void Exit()
        => NavManager.NavigateTo("/editor/fetchdata3");
}
```
Pretty simple.  `WeatherPageEditor` contains all the "Editor" code, so I'll skip most of that.  Here's some code snippets showing what going on.
1. We pick up the Cascaded `NavigationController` so we can `Lock` and `Unlock` it.
2. `CloseAction` gives us a callback into the Editor page to navigate somewhere when "Exit" is clicked.  When opened *In Page* it just closes.

We'll look at this in more detail later.

```c#
[CascadingParameter] NavigationController NavController { get; set; }
[Parameter] public EventCallback CloseAction { get; set; }

public void SetDirty()
{
    if (this.IsDirty)
    {
        NavController?.Unlock();
        this.DirtyExit = false;
    }
    else
    {
        NavController?.Lock();
        this.DirtyExit = true;
    }
    this.IsDirty = !this.IsDirty;
    this.InvokeAsync(this.StateHasChanged);
}

public void Hide()
{
    if (this.IsDirty)
        this.DirtyExit = true;
    else
    {
        NavController.Unlock();
        this.Display = false;
        this.CloseAction.InvokeAsync();
    }
    this.InvokeAsync(this.StateHasChanged);
}

```

## Building the Component

Lets look at the Parameters and public Properties first.
1. We capture any added Attributes.  We're only going to use `class`, but it's the easiest way to do that.
2. `Cascade` turns on/off cascading `this` i.e. the instance of `NavigationController`. Default is `true`.
3. `Transparent` sets the background to either transparent or translucent.  You can see the difference in the demo.
4. `ChildContent` is what's between `<NavigationController>` and `</NavigationController>`.
5. `IsLocked` is a read only Property for checking the conponent state.

```c#
[Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();
[Parameter] public bool Cascade { get; set; } = true;
[Parameter] public bool Transparent { get; set; } = true;
[Parameter] public RenderFragment ChildContent { get; set; }
public bool IsLocked => this._isLocked;
```
The private properties:
1. Inject `IJSRuntime` so we can access the Javascript Interop and set/unset the browser `BeforeUnload` event.
2. `CssClass` builds the Html  `class` attribute for the component, combining any entered classes with the ones configured by the component.
3. The Css properties define the various options for the `class`.
4. `_isLocked` in the private field for controlling lock state.


```c#
[Inject] private IJSRuntime _js { get; set; }

private string CssClass => (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("class", out var obj))
    ? $"{this.frontcss} { Convert.ToString(obj, CultureInfo.InvariantCulture)}"
    : this.frontcss;

private string backcss = string.Empty;
private string frontcss = string.Empty;
private string _backcss => this.Transparent ? "back-block-transparent" : "back-block";
private string _frontcss => this.Transparent ? "fore-block-transparent" : "fore-block";
private string __backcss => string.Empty;
private string __frontcss => string.Empty;
private bool _isLocked;
```

The two public methods are used to `Lock` or `Unlock` the control.  `SetPageExitCheck` interfaces with the Javascript functions loaded into the page. to add or remove the `beforeunload` event on `Window`.  The code is show below.

```c#
public void Lock()
{
    this._isLocked = true;
    this.backcss = this._backcss;
    this.frontcss = this._frontcss;
    this.SetPageExitCheck(true);
    this.InvokeAsync(StateHasChanged);
}

public void Unlock()
{
    this._isLocked = false;
    this.backcss = this.__backcss;
    this.frontcss = this.__frontcss;
    this.SetPageExitCheck(false);
    this.InvokeAsync(StateHasChanged);
}

private void SetPageExitCheck(bool action)
    => _js.InvokeAsync<bool>("cecblazor_setEditorExitCheck", action);
```

The Javascript in *site.js*:

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

Moving on to the Razor for the component:
1. We add a `div` with a class of `backcss`. this is either *back-block-transparent* or *back-block* when `Locked` or empty when `Unlocked`.
2. We add a `div` with a class of `CssClass`. this is either *fore-block-transparent* or *fore-block* when `Locked` or empty when `Unlocked` combined with any `class` attribute value we have added to the component.
2. We cascade `this` if `Cascade` is true.

```html
<div class="@this.backcss"></div>

<div class="@this.CssClass">
    @if (this.Cascade)
    {
        <CascadingValue Value="this">
            @this.ChildContent
        </CascadingValue>
    }
    else
    {
        @this.ChildContent
    }
</div>
```

Finally we move on to the component Css, which is where the magic happens.  We're using a similar technique to that used in modal dialogs, adding a transparent or translucent layer over the page content to *lock* that content, and placing the contents of `NavigationController` in front of that layer.  You may need to tweak the Z-index depending on your specific application.

```css
div.back-block {
    display: block;
    position: fixed;
    z-index: 1; /* Sit on top */
    left: 0;
    top: 0;
    width: 100%; /* Full width */
    height: 100%; /* Full height */
    overflow: auto; /* Enable scroll if needed */
    background-color: RGBA(224, 224, 224, 0.4);
}

div.back-block-transparent {
    display: block;
    position: fixed;
    z-index: 1; /* Sit on top */
    left: 0;
    top: 0;
    width: 100%; /* Full width */
    height: 100%; /* Full height */
    overflow: auto; /* Enable scroll if needed */
    background-color: transparent; 
}

div.fore-block-transparent {
    display: block;
    position: relative;
    z-index: 2; /* Sit on top */
}

div.fore-block {
    display: block;
    position: relative;
    z-index: 2; /* Sit on top */
    background-color: RGB(255, 255, 255);
}
```

### FetchData3

This is the display "Page" or route.

1. We define the `NavigationController` component and set the `WeatherPageEditor` within it.
2. `ShowEditor` calls `Show` on the editor which makes it visible.  Under normal circumstances we would pass an ID to ser which record we are displaying.
3. `GottoEditor` routes to the *weditor* route. Again we would noramlly pass an ID */weditor/nn*.

```html
....
<NavigationController @ref="this.NavController" Cascade="true" Transparent="false">
    <WeatherPageEditor @ref="this._Editor"></WeatherPageEditor>
</NavigationController>
....
```
```c#
....
private void ShowEditor()
    => this._Editor?.Show();

private void GoToEditor()
    => this.NavManager.NavigateTo("/weditor");
...
```

### WeatherPageEditor

Standard Editor stuff.  This is just a pretend editor so display some fixed information.

```html
@if (this.Display)
{
    <div class="container-fluid border border-2 rounded-1 @this.BorderCss p-3">
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
                <button class="btn @this.DirtyButtonCss mr-1" @onclick="() => SetDirty()">@this.DirtyButtonText</button>
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
    </div>
}
```

Code again is pretty standard fare.
1. We have access to the `NavigationController` thoughj the cascaded value.
2. `CloseAction` provides a callback into the "Page/Route".  We use it to navigate back to the list.
3. The various button event handlers call into the `NavigationController` to `Lock` or `Unlock` it.

```c#
    public partial class WeatherPageEditor : ComponentBase
    {

        [CascadingParameter] NavigationController NavController { get; set; }
        [Parameter] public EventCallback CloseAction { get; set; }
        public bool Display { get; private set; }
        public bool IsDirty { get; set; }
        public bool IsLocked { get; private set; }
        private bool DirtyExit { get; set; }
        private string DirtyButtonCss => this.IsDirty ? "btn-success" :"btn-danger" ;
        private string DirtyButtonText => this.IsDirty ? "Set Clean" : "Set Dirty";
        private string BorderCss => this.IsDirty ? "border-danger" : "border-secondary";

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
            {
                NavController.Unlock();
                this.Display = false;
                this.CloseAction.InvokeAsync();
            }
            this.InvokeAsync(this.StateHasChanged);
        }

        public void DirtyHide()
        {
            this.DirtyExit = false;
            if (this.IsDirty)
            {
                NavController.Unlock();
                this.IsDirty = false;
            }
            this.Display = false;
            this.CloseAction.InvokeAsync();
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
            {
                NavController?.Unlock();
                this.DirtyExit = false;
            }
            else
            {
                NavController?.Lock();
                this.DirtyExit = true;
            }
            this.IsDirty = !this.IsDirty;
            this.InvokeAsync(this.StateHasChanged);
        }
    }
```

## Wrap Up

This solution useds the basic methodology used by modal dialogs in placing a barrier between the controls on the page and the contents on the control.  `Lock` inserts than barrier and `Unlock` removes it.  We add the Javascript Interop to turn on add off the `beforeunload` event on the browser.

To sum up. The best solutions are often the simplest!  
