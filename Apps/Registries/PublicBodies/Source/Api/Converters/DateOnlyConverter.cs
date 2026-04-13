namespace Adr.PublicBodies.Converters
{
    using System;
    using System.Globalization;
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;

    /// <summary>
    /// Converts CSV datetime strings to DateOnly by extracting just the date component.
    /// Handles ISO 8601 formats (e.g. "2017-01-01T15:00:00-07:00") and simple date
    /// formats (e.g. "2026-01-01 15:00").
    /// </summary>
    public class DateOnlyConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            {
                return DateOnly.FromDateTime(dto.DateTime);
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return DateOnly.FromDateTime(dt);
            }

            return null;
        }
    }
}
