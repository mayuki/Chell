using System;
using System.IO;
using Chell.Shell;

namespace Chell
{
    public class Run : ProcessTask
    {
        public static implicit operator Run(FormattableString commandLine)
            => new Run(commandLine);
        public static implicit operator Run(CommandLineString commandLine)
            => new Run(commandLine);

        public Run(CommandLineString commandLine) : base(commandLine) { }
        public Run(FormattableString commandLine) : base(commandLine) { }
        public Run(Stream inputStream, CommandLineString commandLine) : base(inputStream, commandLine) { }
        public Run(Stream inputStream, FormattableString commandLine) : base(inputStream, commandLine) { }
        public Run(ReadOnlyMemory<byte> inputData, CommandLineString commandLine) : base(new MemoryStream(inputData.ToArray()), commandLine) { }
        public Run(ReadOnlyMemory<byte> inputData, FormattableString commandLine) : base(new MemoryStream(inputData.ToArray()), commandLine) { }
    }
}
