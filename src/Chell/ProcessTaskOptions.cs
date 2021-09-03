using System;
using Chell.IO;
using Chell.Shell;

namespace Chell
{
    public class ProcessTaskOptions
    {
        /// <summary>
        /// Gets or sets whether to enable automatic wiring of standard input to the process. The default value is <value>true</value>.
        /// </summary>
        public bool EnableAutoWireStandardInput { get; set; }

        /// <summary>
        /// Gets or sets to enable standard input redirection. The default value is <value>false</value>.
        /// </summary>
        public bool RedirectStandardInput { get; set; }

        /// <summary>
        /// Gets or sets the shell executor. The default value is <c>ChellEnvironment.Current.Shell.Executor</c>.
        /// </summary>
        public IShellExecutor ShellExecutor { get; set; }

        /// <summary>
        /// Gets or sets the console provider. The default value is <c>ChellEnvironment.Current.Console</c>.
        /// </summary>
        public IConsoleProvider Console { get; set; }

        /// <summary>
        /// Gets or sets the verbosity. The default value is <c>ChellEnvironment.Current.Verbosity</c>.
        /// </summary>
        public ChellVerbosity Verbosity { get; set; }

        /// <summary>
        /// Gets or sets the working directory for the process.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the duration to timeout the process. The default value is <c>ChellEnvironment.Current.ProcessTimeout</c>.
        /// </summary>
        /// <remarks>
        /// If the value is <see cref="TimeSpan.Zero"/> or <see cref="TimeSpan.MaxValue"/>, the process will not be timed out.
        /// </remarks>
        public TimeSpan Timeout { get; set; }

        public ProcessTaskOptions(
            bool? redirectStandardInput = default,
            bool? enableAutoWireStandardInput = default,
            ChellVerbosity? verbosity = default,
            IShellExecutor? shellExecutor = default,
            IConsoleProvider? console = default,
            string? workingDirectory = default,
            TimeSpan? timeout = default
        )
        {
            RedirectStandardInput = redirectStandardInput ?? false;
            EnableAutoWireStandardInput = enableAutoWireStandardInput ?? true;
            ShellExecutor = shellExecutor ?? ChellEnvironment.Current.Shell.Executor;
            Console = console ?? ChellEnvironment.Current.Console;
            Verbosity = verbosity ?? ChellEnvironment.Current.Verbosity;
            WorkingDirectory = workingDirectory ?? workingDirectory;
            Timeout = timeout ?? ChellEnvironment.Current.ProcessTimeout;
        }

        private ProcessTaskOptions(ProcessTaskOptions orig)
        {
            RedirectStandardInput = orig.RedirectStandardInput;
            EnableAutoWireStandardInput = orig.EnableAutoWireStandardInput;
            ShellExecutor = orig.ShellExecutor;
            Console = orig.Console;
            Verbosity = orig.Verbosity;
            WorkingDirectory = orig.WorkingDirectory;
            Timeout = orig.Timeout;
        }

        public ProcessTaskOptions WithRedirectStandardInput(bool redirectStandardInput)
            => new ProcessTaskOptions(this) { RedirectStandardInput = redirectStandardInput };
        public ProcessTaskOptions WithEnableAutoWireStandardInput(bool enableAutoWireStandardInput)
            => new ProcessTaskOptions(this) { EnableAutoWireStandardInput = enableAutoWireStandardInput };
        public ProcessTaskOptions WithShellExecutor(IShellExecutor shellExecutor)
            => new ProcessTaskOptions(this) { ShellExecutor = shellExecutor };
        public ProcessTaskOptions WithVerbosity(ChellVerbosity verbosity)
            => new ProcessTaskOptions(this) { Verbosity = verbosity };
        public ProcessTaskOptions WithWorkingDirectory(string? workingDirectory)
            => new ProcessTaskOptions(this) { WorkingDirectory = workingDirectory };
        public ProcessTaskOptions WithTimeout(TimeSpan timeout)
            => new ProcessTaskOptions(this) { Timeout = timeout };
    }
}
