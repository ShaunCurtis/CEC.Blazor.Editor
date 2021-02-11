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

        public void AddRange(List<RecordFieldValue> list, bool clearList)
        {
            if (clearList) _items.Clear();
            list.ForEach(item => this._items.Add(item));
        }

        public void AddRange(RecordCollection recs, bool clearList)
        {
            if (clearList) _items.Clear();
            foreach(var item in recs)
                this._items.Add(item);
        }

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

        public RecordFieldValue GetRecordValue(string FieldName)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x == default)
            {
                x = new RecordFieldValue(FieldName, null);
                _items.Add(x);
            }
            return x;
        }

        public bool TryGet<T>(string FieldName, out T value)
        {
            value = default;
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x.Value is T t) value = t;
            return x.Value != default;
        }

        public bool TryGetEditValue<T>(string FieldName, out T value)
        {
            value = default;
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x.EditedValue is T t) value = t;
            return x.EditedValue != default;
        }

        public bool HasField(string FieldName)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x is null | x == default) return false;
            return true;
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

        public bool Add(RecordFieldValue record, bool overwrite = true)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(record.FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x != default)
            {
                if (overwrite)
                {
                    _items.Remove(x);
                    _items.Add(record);
                    return true;
                }
                return false;
            }
            else if (record != null)
            {
                _items.Add(record);
                return true;
            }
            return false;
        }

        public bool Add(string FieldName, object value)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x != default) _items.Remove(x);
            _items.Add(new RecordFieldValue(FieldName, value));
            return true;
        }

        public bool RemoveField(string FieldName)
        {
            var x = _items.FirstOrDefault(item => item.FieldName.Equals(FieldName, StringComparison.CurrentCultureIgnoreCase));
            if (x != null && x != default)
            {
                _items.Remove(x);
                return true;
            }
            return false;
        }
    }
}

