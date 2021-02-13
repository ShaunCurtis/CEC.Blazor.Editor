/// =================================
/// Author: Shaun Curtis, Cold Elm
/// License: MIT
/// ==================================

using System;

namespace CEC.Blazor.ModalEditor
{
    public class RecordFieldValue
    {
        public string FieldName { get; init; }

        public object Value { get; init; }

        public bool ReadOnly { get; init; }

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

        public RecordFieldValue() { }

        public RecordFieldValue(string field, object value)
        {
            this.FieldName = field;
            this.Value = value;
            this.EditedValue = value;
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
