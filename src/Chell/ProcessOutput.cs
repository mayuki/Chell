using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Chell.Internal;

namespace Chell
{
    /// <summary>
    /// Provides the outputs and results from the process.
    /// </summary>
    /// <remarks>
    /// If the output is redirected or piped, it will not be captured.
    /// </remarks>
    public class ProcessOutput : IEnumerable<string>
    {
        /// <summary>
        /// Gets the standard outputs as <see cref="String"/>.
        /// </summary>
        public string Output => Sink.Output;

        /// <summary>
        /// Gets the standard errors as <see cref="String"/>.
        /// </summary>
        public string Error => Sink.Error;

        /// <summary>
        /// Gets the standard outputs and standard errors as <see cref="String"/>.
        /// </summary>
        public string Combined => Sink.Combined;

        /// <summary>
        /// Gets the standard outputs as <see cref="byte"/> sequence.
        /// </summary>
        public ReadOnlyMemory<byte> OutputBinary => Sink.OutputBinary;

        /// <summary>
        /// Gets the standard errors as <see cref="byte"/> sequence.
        /// </summary>
        public ReadOnlyMemory<byte> ErrorBinary => Sink.ErrorBinary;

        /// <summary>
        /// Gets the standard outputs and standard errors as <see cref="byte"/> sequence.
        /// </summary>
        public ReadOnlyMemory<byte> CombinedBinary => Sink.CombinedBinary;

        /// <summary>
        /// Get the exit code when the process terminated.
        /// </summary>
        public int ExitCode { get; internal set; }

        internal OutputSink Sink { get; }

        public ProcessOutput(Encoding encoding)
        {
            Sink = new OutputSink(encoding);
        }

        public static implicit operator string(ProcessOutput processOutput)
            => processOutput.ToString();

        /// <summary>
        /// Gets the standard outputs and standard errors as <see cref="String"/>.
        /// </summary>
        public override string ToString() => Combined;

        public IEnumerator<string> GetEnumerator()
            => AsLines().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        /// Gets the standard outputs and standard errors as lines.
        /// </summary>
        public IEnumerable<string> AsLines(bool trimEnd = false)
            => (trimEnd ? Combined.TrimEnd('\n', '\r') : Combined).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }
}
