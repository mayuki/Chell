using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chell.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Enumerates lines by converting them to objects as JSON.
        /// </summary>
        /// <remarks>
        /// Converts to the anonymous type specified in <see cref="shape"/> argument.
        /// </remarks>
        public static IEnumerable<T?> AsJsonLines<T>(this IEnumerable<string> lines, T shape, bool skipEmptyLine = true, JsonSerializerOptions? options = null)
        {
            return AsJsonLines<T>(lines, skipEmptyLine, options);
        }

        /// <summary>
        /// Enumerates lines by converting them to objects as JSON.
        /// </summary>
        public static IEnumerable<T?> AsJsonLines<T>(this IEnumerable<string> lines, bool skipEmptyLine = true, JsonSerializerOptions? options = null)
        {
            return lines.Where(x => !skipEmptyLine || !string.IsNullOrWhiteSpace(x)).Select(x => JsonSerializer.Deserialize<T>(x, options));
        }

        /// <summary>
        /// Converts the output string to an object as JSON.
        /// </summary>
        /// <remarks>
        /// Converts to the anonymous type specified in <see cref="shape"/> argument.
        /// </remarks>
        public static T? AsJson<T>(this string json, T shape, JsonSerializerOptions? options = null)
            => AsJson<T>(json, options);

        /// <summary>
        /// Converts the output string to an object as JSON.
        /// </summary>
        public static T? AsJson<T>(this string json, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize<T>(json.ToString(), options);
    }
}
