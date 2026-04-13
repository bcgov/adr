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
                return false;
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

            return false;
        }
    }
}
