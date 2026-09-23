namespace Adr.Semantics.Providers
{
    using System;
    using System.Globalization;

    internal readonly record struct GlossarySemanticVersion(int Major, int Minor, int Patch)
    {
        public static GlossarySemanticVersion Initial => new(0, 1, 0);

        public GlossarySemanticVersion Increment(GlossaryChangeKind change)
        {
            return change switch
            {
                GlossaryChangeKind.Major => new GlossarySemanticVersion(Major + 1, 0, 0),
                GlossaryChangeKind.Minor => new GlossarySemanticVersion(Major, Minor + 1, 0),
                GlossaryChangeKind.Patch => new GlossarySemanticVersion(Major, Minor, Patch + 1),
                _ => this,
            };
        }

        public static GlossarySemanticVersion Parse(string value)
        {
            var parts = value.Split('.');
            if (
                parts.Length != 3
                || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
                || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var patch)
            )
            {
                throw new FormatException($"Glossary version '{value}' is not a semantic version.");
            }

            return new GlossarySemanticVersion(major, minor, patch);
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"{Major}.{Minor}.{Patch}");
        }
    }

    internal enum GlossaryChangeKind
    {
        None = 0,
        Patch = 1,
        Minor = 2,
        Major = 3,
    }
}
