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
        /// Gets or sets the shell executor.
        /// </summary>
        public IShellExecutor ShellExecutor { get; set; }

        /// <summary>
        /// Gets or sets the verbosity.
        /// </summary>
        public ChellVerbosity Verbosity { get; set; }

        /// <summary>
        /// Gets or sets the working directory for the process.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        public ProcessTaskOptions(bool? redirectStandardInput = default, bool? enableAutoWireStandardInput = default, ChellVerbosity? verbosity = default, IShellExecutor? shellExecutor = default, string? workingDirectory = default)
        {
            RedirectStandardInput = redirectStandardInput ?? false;
            EnableAutoWireStandardInput = enableAutoWireStandardInput ?? true;
            ShellExecutor = shellExecutor ?? ChellEnvironment.Current.Shell.Executor;
            Verbosity = verbosity ?? ChellEnvironment.Current.Verbosity;
            WorkingDirectory = workingDirectory ?? workingDirectory;
        }

        private ProcessTaskOptions(ProcessTaskOptions orig)
        {
            RedirectStandardInput = orig.RedirectStandardInput;
            EnableAutoWireStandardInput = orig.EnableAutoWireStandardInput;
            ShellExecutor = orig.ShellExecutor;
            Verbosity = orig.Verbosity;
            WorkingDirectory = orig.WorkingDirectory;
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
    }
}
