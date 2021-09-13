using System;
using System.IO;
using Chell.Shell;

namespace Chell
{
    /// <summary>
    /// Short cut for <see cref="ProcessTask"/> to launch a process from a <see cref="string"/> or <see cref="FormattableString"/>.
    /// </summary>
    public class Run : ProcessTask
    {
        public static implicit operator Run(FormattableString commandLine)
            => new Run(commandLine);
        public static implicit operator Run(CommandLineString commandLine)
            => new Run(commandLine);

        /// <summary>
        /// Launches a process from a <see cref="string"/>.
        /// </summary>
        /// <param name="commandLine"></param>
        public Run(CommandLineString commandLine) : base(commandLine) { }

        /// <summary>
        /// Launches a process from a <see cref="FormattableString"/>.
        /// </summary>
        /// <remarks>
        ///  The interpolated string will be escaped and the array will be expanded.
        /// </remarks>
        public Run(FormattableString commandLine) : base(commandLine) { }

        /// <summary>
        /// Launches a process from a <see cref="string"/> and connects the specified <see cref="Stream"/> to the standard input.
        /// </summary>
        public Run(Stream inputStream, CommandLineString commandLine) : base(inputStream, commandLine) { }

        /// <summary>
        /// Launches a process from a <see cref="FormattableString"/> and connects the specified <see cref="Stream"/> to the standard input.
        /// </summary>
        /// <remarks>
        /// The interpolated string will be escaped and the array will be expanded.
        /// </remarks>
        /// <param name="inputStream"></param>
        /// <param name="commandLine"></param>
        public Run(Stream inputStream, FormattableString commandLine) : base(inputStream, commandLine) { }

        /// <summary>
        /// Launches a process from a <see cref="string"/> and writes the specified binary data to the standard input.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="commandLine"></param>
        public Run(ReadOnlyMemory<byte> inputData, CommandLineString commandLine) : base(new MemoryStream(inputData.ToArray()), commandLine) { }

        /// <summary>
        /// Launches a process from a <see cref="FormattableString"/> and writes the specified binary data to the standard input.
        /// </summary>
        /// <remarks>
        /// The interpolated string will be escaped and the array will be expanded.
        /// </remarks>
        /// <param name="inputData"></param>
        /// <param name="commandLine"></param>
        public Run(ReadOnlyMemory<byte> inputData, FormattableString commandLine) : base(new MemoryStream(inputData.ToArray()), commandLine) { }
    }
}
