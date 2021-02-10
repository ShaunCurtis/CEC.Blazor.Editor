/// =================================
/// Author: Shaun Curtis, Cold Elm
/// License: MIT
/// ==================================

using System;

namespace CEC.Blazor.Editor
{
    public class RecordValue
    {
        public string FieldName { get; init; }

        public Guid GUID { get; init; } = Guid.NewGuid();

        public object Value { get; init; }

        public bool ReadOnly { get; init; }

        public object EditedValue { get; set; }

        public string DisplayName { get; set; }

        public bool IsDirty
        {
            get
            {
                if (Value != null && EditedValue != null) return !Value.Equals(EditedValue);
                if (Value is null && EditedValue is null) return false;
                return true;
            }
        }

        public RecordValue() { }

        public RecordValue(string field, object value)
        {
            this.FieldName = field;
            this.Value = value;
            this.EditedValue = value;
            this.GUID = Guid.NewGuid();
        }

        public void Reset()
            => this.EditedValue = this.Value;

        public RecordValue Clone()
        {
            return new RecordValue()
            {
                DisplayName = this.DisplayName,
                FieldName = this.FieldName,
                Value = this.Value,
                ReadOnly = this.ReadOnly
            };
        }

        public RecordValue Clone(object value)
        {
            return new RecordValue()
            {
                DisplayName = this.DisplayName,
                FieldName = this.FieldName,
                Value = value,
                ReadOnly = this.ReadOnly
            };
        }
    }
}
