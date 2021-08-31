using System.Collections.Generic;
using System.Text.Json;

namespace Chell
{
    public static class ProcessOutputExtensions
    {
        /// <summary>
        /// Enumerates lines by converting them to objects as JSON.
        /// </summary>
        /// <remarks>
        /// Converts to the anonymous type specified in <see cref="shape"/> argument.
        /// </remarks>
        public static IEnumerable<T?> AsJsonLines<T>(this ProcessOutput processOutput, T shape, bool skipEmptyLine = true, JsonSerializerOptions? options = null)
            => Chell.Extensions.StringExtensions.AsJsonLines<T>(processOutput.AsLines(), skipEmptyLine, options);

        /// <summary>
        /// Enumerates lines by converting them to objects as JSON.
        /// </summary>
        public static IEnumerable<T?> AsJsonLines<T>(this ProcessOutput processOutput, bool skipEmptyLine = true, JsonSerializerOptions? options = null)
            => Chell.Extensions.StringExtensions.AsJsonLines<T>(processOutput.AsLines(), skipEmptyLine, options);

        /// <summary>
        /// Converts the output string to an object as JSON.
        /// </summary>
        /// <remarks>
        /// Converts to the anonymous type specified in <see cref="shape"/> argument.
        /// </remarks>
        public static T? AsJson<T>(this ProcessOutput processOutput, T shape, JsonSerializerOptions? options = null)
            => AsJson<T>(processOutput, options);

        /// <summary>
        /// Converts the output string to an object as JSON.
        /// </summary>
        public static T? AsJson<T>(this ProcessOutput processOutput, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize<T>(processOutput.ToString(), options);
    }
}