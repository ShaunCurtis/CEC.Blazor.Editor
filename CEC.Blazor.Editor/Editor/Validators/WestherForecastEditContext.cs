/// =================================
/// Author: Shaun Curtis, Cold Elm
/// License: MIT
/// ==================================

using System;

namespace CEC.Blazor.Editor
{

    /// <summary>
    /// A class to surface data stored in the underlying RecordCollection
    /// provides Validation and Edit State management for the Record Collection 
    /// The properties point to the data stored in the underlying RecordCollection
    /// The purposes of the Properties and validation Methods are self-evident 
    /// </summary>
    public class WeatherForecastEditContext : RecordEditContext, IRecordEditContext
    {
        #region Public

        public DateTime Date
        {
            get => this.RecordValues.GetEditValue<DateTime>(DbWeatherForecast.__Date.FieldName);
            set
            {
                this.RecordValues.SetField(DbWeatherForecast.__Date.FieldName, value);
                this.Validate();
            }
        }


        public string Summary
        {
            get => this.RecordValues.GetEditValue<string>(DbWeatherForecast.__Summary.FieldName);
            set
            {
                this.RecordValues.SetField(DbWeatherForecast.__Summary.FieldName, value);
                this.Validate();
            }
        }

        public decimal TemperatureC
        {
            get => this.RecordValues.GetEditValue<int>(DbWeatherForecast.__TemperatureC.FieldName);
            set
            {
                this.RecordValues.SetField(DbWeatherForecast.__TemperatureC.FieldName, value);
                this.Validate();
            }
        }

        public Guid WeatherForecastID
            => this.RecordValues.GetEditValue<Guid>(DbWeatherForecast.__ID.FieldName);

        /// <summary>
        /// New Method to load base object
        /// </summary>
        /// <param name="collection"></param>
        public WeatherForecastEditContext(RecordCollection collection) : base(collection) { }

        #endregion

        #region Protected

        protected override void LoadValidationActions()
        {
            this.ValidationActions.Add(ValidateSummary);
            this.ValidationActions.Add(ValidateTemperatureC);
            this.ValidationActions.Add(ValidateDate);
        }

        #endregion

        #region Private

        private bool ValidateSummary()
        {
            return this.Summary.Validation(DbWeatherForecast.__Summary.FieldName, this, ValidationMessageStore)
                .LongerThan(3, "Your description needs to be a little longer! 4 letters minimum")
                .Validate();
        }
        private bool ValidateDate()
        {
            return this.Date.Validation(DbWeatherForecast.__Date.FieldName, this, ValidationMessageStore)
                .NotDefault("You must select a date")
                .LessThan(DateTime.Now.AddMonths(1), true, "Date can only be up to 1 month ahead")
                .Validate();
        }

        private bool ValidateTemperatureC()
        {
            return this.TemperatureC.Validation(DbWeatherForecast.__TemperatureC.FieldName, this, ValidationMessageStore)
                .LessThan(70, "The temperature must be less than 70C")
                .GreaterThan(-60, "The temperature must be greater than -60C")
                .Validate();
        }

        #endregion
    }
}
