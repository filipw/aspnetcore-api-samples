using System;

namespace MvcCoreApi
{
    public static class StringExtensions
    {
        public static string Between(this string value, string start, string end)
        {
            var startPosition = value.IndexOf(start, StringComparison.Ordinal);
            var endPosition = value.LastIndexOf(end, StringComparison.Ordinal);
            if (startPosition == -1 || endPosition == -1) return string.Empty;

            var adjustedStart = startPosition + start.Length;
            return adjustedStart >= endPosition ? string.Empty : value.Substring(adjustedStart, endPosition - adjustedStart);
        }
    }
}