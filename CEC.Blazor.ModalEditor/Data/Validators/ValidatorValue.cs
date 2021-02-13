/// =================================
/// Author: Shaun Curtis, Cold Elm
/// License: MIT
/// ==================================

namespace CEC.Blazor.ModalEditor
{
    public class ValidatorValue
    {
        public string Field { get; init; }

        public bool IsValid { get; set; }

        public ValidatorValue(string field, bool valid)
        {
            this.Field = field;
            this.IsValid = valid;
        }

    }
}
