using System;

namespace Chell
{
    /// <summary>
    /// Represents an error that occurs during process execution.
    /// </summary>
    public class ProcessTaskException : Exception
    {
        public ProcessTask ProcessTask { get; }
        public ProcessOutput Output { get; }

        public ProcessTaskException(string processName, int processId, ProcessTask processTask, ProcessOutput output, Exception? innerException = default)
            : base($"Process '{processName}' ({processId}) has exited with exit code {output.ExitCode}. (Executed command: {processTask.Command} {processTask.Arguments})", innerException)
        {
            ProcessTask = processTask;
            Output = output;
        }

        public ProcessTaskException(ProcessTask processTask, ProcessOutput output, Exception? innerException = default)
            : base($"Failed to start the process. (Executed command: {processTask.Command} {processTask.Arguments})", innerException)
        {
            ProcessTask = processTask;
            Output = output;
        }
    }
}