using System;
using System.IO;
using System.Text;

namespace Chell.IO
{
    /// <summary>
    /// Encapsulates console intrinsic members as object.
    /// </summary>
    public sealed class SystemConsoleProvider : IConsoleProvider
    {
        public static IConsoleProvider Instance { get; } = new SystemConsoleProvider();

        private SystemConsoleProvider()
        {}

        public Stream OpenStandardInput()
            => Console.OpenStandardInput();

        public Stream OpenStandardOutput()
            => Console.OpenStandardOutput();

        public Stream OpenStandardError()
            => Console.OpenStandardError();

        public Encoding InputEncoding => Console.InputEncoding;
        public Encoding OutputEncoding => Console.OutputEncoding;

        public bool IsInputRedirected => Console.IsInputRedirected;
        public bool IsOutputRedirected => Console.IsOutputRedirected;
        public bool IsErrorRedirected => Console.IsErrorRedirected;

        public TextWriter Out => Console.Out;
        public TextWriter Error => Console.Error;
    }
}
