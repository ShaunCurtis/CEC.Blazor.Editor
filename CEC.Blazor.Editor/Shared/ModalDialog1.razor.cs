using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
