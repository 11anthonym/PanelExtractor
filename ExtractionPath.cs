namespace PanelExtractor
{
    internal static class ExtractionPath
    {
        private const string UnsafeNameMessage =
            "The panel returned an unsafe file or directory name. Extraction was stopped.";

        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        public static string Combine(string directory, string name)
        {
            if (string.IsNullOrEmpty(name) ||
                name is "." or ".." ||
                name.EndsWith(" ", StringComparison.Ordinal) ||
                name.EndsWith(".", StringComparison.Ordinal) ||
                Path.IsPathRooted(name) ||
                name.IndexOfAny(InvalidFileNameChars) >= 0 ||
                IsReservedDeviceName(name))
            {
                throw new InvalidDataException(UnsafeNameMessage);
            }

            string root = Path.GetFullPath(directory);
            string path = Path.GetFullPath(Path.Combine(root, name));
            string rootPrefix = root.EndsWith(
                Path.DirectorySeparatorChar.ToString(),
                StringComparison.Ordinal)
                ? root
                : root + Path.DirectorySeparatorChar;

            if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(UnsafeNameMessage);
            }

            return path;
        }

        private static bool IsReservedDeviceName(string name)
        {
            int dotIndex = name.IndexOf('.');
            string stem = name.Substring(0, dotIndex < 0 ? name.Length : dotIndex)
                .TrimEnd(' ')
                .ToUpperInvariant();

            return stem is "CON" or "PRN" or "AUX" or "NUL" or "CLOCK$" ||
                stem.Length == 4 &&
                (stem.StartsWith("COM", StringComparison.Ordinal) ||
                 stem.StartsWith("LPT", StringComparison.Ordinal)) &&
                stem[3] is >= '1' and <= '9';
        }
    }
}
