using System;

namespace CEC.Blazor.Editor
{
    public class DbWeatherForecast
    {
        public static RecordValue __ID = new RecordValue() { FieldName = "ID", DisplayName = "ID" };
        public static RecordValue __Date = new RecordValue() { FieldName = "Date", DisplayName = "Forecast Date" };
        public static RecordValue __TemperatureC = new RecordValue() { FieldName = "TemperatureC", DisplayName = "Temperature C" };
        public static RecordValue __TemperatureF = new RecordValue() { FieldName = "TemperatureF", DisplayName = "Temperature F", ReadOnly = true };
        public static RecordValue __Summary = new RecordValue() { FieldName = "Summary", DisplayName = "Summary" };

        public Guid ID { get; init; } = Guid.NewGuid();

        public DateTime Date { get; init; }

        public int TemperatureC { get; init; }

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
