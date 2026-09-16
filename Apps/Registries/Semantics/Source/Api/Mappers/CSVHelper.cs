namespace Adr.Semantics.Models
{
    using System;
    using System.Collections.Generic;
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;

    public class ListStringConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(
            string? text,
            IReaderRow row,
            MemberMapData memberMapData
        )
        {
            if (text is null)
            {
                return new List<string>();
            }

            return new List<string>(text.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    public class BooleanFromYesNoConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(
            string? text,
            IReaderRow row,
            MemberMapData memberMapData
        )
        {
            if (text is null)
            {
                throw new FormatException("A Yes/No value is required.");
            }

            string lowerText = text.Trim().ToLowerInvariant();

            if (lowerText == "yes" || lowerText == "true")
            {
                return true;
            }

            if (lowerText == "no" || lowerText == "false")
            {
                return false;
            }

            throw new FormatException($"'{text}' is not a valid Yes/No value.");
        }
    }

    public class OptionalBooleanFromYesNoConverter : BooleanFromYesNoConverter
    {
        public override object ConvertFromString(
            string? text,
            IReaderRow row,
            MemberMapData memberMapData
        )
        {
            return string.IsNullOrWhiteSpace(text)
                ? false
                : base.ConvertFromString(text, row, memberMapData);
        }
    }
}
