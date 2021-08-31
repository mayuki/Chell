using Chell.Shell;

namespace Chell
{
    public class ProcessTaskOptions
    {
        public static ProcessTaskOptions Default { get; } = new ProcessTaskOptions();

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

        public ProcessTaskOptions(bool? redirectStandardInput = default, bool? enableAutoWireStandardInput = default, ChellVerbosity? verbosity = default, IShellExecutor? shellExecutor = default)
        {
            RedirectStandardInput = redirectStandardInput ?? false;
            EnableAutoWireStandardInput = enableAutoWireStandardInput ?? true;
            ShellExecutor = shellExecutor ?? ChellEnvironment.Current.Shell.Executor;
            Verbosity = verbosity ?? ChellEnvironment.Current.Verbosity;
        }
    }
}
