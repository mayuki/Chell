using System;
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
    }
}