
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
