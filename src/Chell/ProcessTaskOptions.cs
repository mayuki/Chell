using Chell.Shell;

namespace Chell
{
    public class ProcessTaskOptions
    {
        public bool RedirectStandardInput { get; set; }
        public IShellExecutor ShellExecutor { get; set; }
        public ChellVerbosity Verbosity { get; set; }

        public ProcessTaskOptions(bool? redirectStandardInput = default, ChellVerbosity? verbosity = default, IShellExecutor? shellExecutor = default)
        {
            RedirectStandardInput = redirectStandardInput ?? false;
            ShellExecutor = shellExecutor ?? ChellEnvironment.Current.Shell.Executor;
            Verbosity = verbosity ?? ChellEnvironment.Current.Verbosity;
        }
    }
}