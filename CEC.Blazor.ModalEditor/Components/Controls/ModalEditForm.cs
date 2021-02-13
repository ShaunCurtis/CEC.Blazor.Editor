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
            //Debug.Assert(EditContext != null);
            if (this.Loaded)
            {
                // If EditContext changes, tear down and recreate all descendants.
                // This is so we can safely use the IsFixed optimization on CascadingValue,
                // optimizing for the common case where EditContext never changes.
                builder.OpenRegion(EditContext.GetHashCode());
                builder.OpenComponent<CascadingValue<EditContext>>(1);
                builder.AddAttribute(2, "IsFixed", true);
                builder.AddAttribute(3, "Value", EditContext);
                builder.AddAttribute(4, "ChildContent", EditorContent);
                builder.CloseComponent();
                builder.CloseRegion();
            }
            else
            {
                builder.AddContent(10, LoadingContent );
            }
            builder.AddContent(20, ButtonContent);
        }

    }
}
