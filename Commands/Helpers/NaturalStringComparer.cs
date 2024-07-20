using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Commands.Helpers
{
    public class NaturalStringComparer : IComparer<string>
    {
        private static readonly Regex numericPattern = new Regex(@"\d+", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            if (x == y)
                return 0;

            x = Utilities.GetFileNameWithoutExtension(x);
            y = Utilities.GetFileNameWithoutExtension(y);

            var match = Regex.Match(x, @"\d+$");
            if (match.Success)
            {
                x = match.Value;
            }

            match = Regex.Match(y, @"\d+$");
            if (match.Success)
            {
                y = match.Value;
            }

            if (x.Contains("_"))
            {
                x = x.Substring(x.LastIndexOf('_') + 1);
                y = y.Substring(y.LastIndexOf('_') + 1);
            }

            // If both parts are numeric, compare numerically
            if (int.TryParse(x, out int xInt) && int.TryParse(y, out int yInt))
            {
                return xInt.CompareTo(yInt);
            }

            var xParts = numericPattern.Split(x);
            var yParts = numericPattern.Split(y);

            int minLength = Math.Min(xParts.Length, yParts.Length);

            for (int i = 0; i < minLength; i++)
            {
                // If parts are the same, continue to the next part
                if (xParts[i] == yParts[i])
                    continue;

                // If both parts are numeric, compare numerically
                if (int.TryParse(xParts[i], out int xInt2) && int.TryParse(yParts[i], out int yInt2))
                {
                    return xInt2.CompareTo(yInt2);
                }

                // If parts are not the same and not both numeric, compare alphabetically
                int result = xParts[i].CompareTo(yParts[i]);
                if (result != 0)
                    return result;
            }

            // If all parts are the same but the lengths are different, the shorter string should come first
            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}
