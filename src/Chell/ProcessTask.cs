using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Chell.Internal;

namespace Chell
{
    /// <summary>
    /// Represents the execution task of a process.
    /// </summary>
    public class ProcessTask 
    {
        private static readonly TimeSpan ProcessStartScheduledDelay = TimeSpan.FromMilliseconds(250);
        private static int _idSequence = 0;

        private readonly int _id;
        private readonly DateTimeOffset _startedAt;
        private readonly Lazy<Task<ProcessOutput>> _taskLazy;
        private readonly ProcessOutput _output;
        private readonly ProcessTaskOptions _options;
        private readonly CancellationTokenSource _processCancellation;
        private readonly CancellationTokenRegistration _processCancellationRegistration;

        private readonly object _syncLock = new object();
        private string? _processName;
        private Process? _process;
        private ExceptionDispatchInfo? _processException;

        private Stream? _stdInStream;
        private StreamPipe? _stdOutPipe;
        private StreamPipe? _stdErrorPipe;
        private bool _piped;
        private bool _hasStandardIn;
        private bool _suppressPipeToConsole;
        private bool _suppressExceptionExitCodeNonZero;

        /// <summary>
        /// Gets the process of the task. If the process is not started yet, the property returns <value>null</value>.
        /// </summary>
        public Process? Process => _process;

        /// <summary>
        /// Gets the command line string of the task.
        /// </summary>
        public string CommandLine { get; }

        /// <summary>
        /// Gets the command name of the task.
        /// </summary>
        /// <remarks>
        /// This value is passed to <see cref="ProcessStartInfo.FileName"/>.
        /// </remarks>
        public string Command { get; }

        /// <summary>
        /// Gets the arguments of the task.
        /// </summary>
        /// <remarks>
        /// The value is passed to <see cref="ProcessStartInfo.Arguments"/>. It may be escaped or quoted.
        /// </remarks>
        public string Arguments { get; }

        /// <summary>
        /// Gets the previous task of the task if piped.
        /// </summary>
        public ProcessTask? PreviousTask { get; private set; }

        public ProcessTask(FormattableString commandLine, ProcessTaskOptions? options = default)
            : this(default(Stream?), commandLine, options)
        { }
        public ProcessTask(Stream? inputStream, FormattableString commandLine, ProcessTaskOptions? options = default)
            : this(inputStream, CommandLineHelper.Expand(commandLine, options?.ShellExecutor ?? ChellEnvironment.Current.Shell.Executor), options)
        { }
        public ProcessTask(ReadOnlyMemory<byte> inputData, FormattableString commandLine, ProcessTaskOptions? options = default)
            : this(new MemoryStream(inputData.ToArray()), CommandLineHelper.Expand(commandLine, options?.ShellExecutor ?? ChellEnvironment.Current.Shell.Executor), options)
        { }

        // NOTE: The overload of `string commandLine` cannot be made public due to restrictions.
        public ProcessTask(CommandLineString commandLine, ProcessTaskOptions? options = default)
            : this(default(Stream?), commandLine, options)
        { }
        public ProcessTask(Stream? inputStream, CommandLineString commandLine, ProcessTaskOptions? options = default)
            : this(inputStream, commandLine.StringValue ?? CommandLineHelper.Expand(commandLine.FormattableStringValue ?? throw new InvalidOperationException("The command line string cannot be null."), options?.ShellExecutor ?? ChellEnvironment.Current.Shell.Executor), options)
        { }
        public ProcessTask(ReadOnlyMemory<byte> inputData, CommandLineString commandLine, ProcessTaskOptions? options = default)
            : this(new MemoryStream(inputData.ToArray()), commandLine.StringValue ?? CommandLineHelper.Expand(commandLine.FormattableStringValue ?? throw new InvalidOperationException("The command line string cannot be null."), options?.ShellExecutor ?? ChellEnvironment.Current.Shell.Executor), options)
        { }
        private ProcessTask(Stream? inputStream, string commandLine, ProcessTaskOptions? options)
            : this(inputStream, commandLine, (options?.ShellExecutor ?? ChellEnvironment.Current.Shell.Executor).GetCommandAndArguments(commandLine), options)
        { }

        public ProcessTask(string command, string arguments, ProcessTaskOptions? options = default)
            : this(default(Stream?), command, arguments, options)
        { }
        public ProcessTask(Stream? inputStream, string command, string arguments, ProcessTaskOptions? options = default)
            : this(inputStream, $"{command} {arguments}", (command, arguments), options)
        { }
        public ProcessTask(ReadOnlyMemory<byte> inputData, string command, string arguments, ProcessTaskOptions? options = default)
            : this(new MemoryStream(inputData.ToArray()), $"{command} {arguments}", (command, arguments), options)
        { }

        private ProcessTask(Stream? inputStream, string commandLine, (string Command, string Arguments) commandAndArguments, ProcessTaskOptions? options = default)
        {
            _startedAt = DateTimeOffset.Now;
            _options = options ?? new ProcessTaskOptions();
            _id = Interlocked.Increment(ref _idSequence);
            _processCancellation = new CancellationTokenSource();
            _processCancellationRegistration = _processCancellation.Token.Register(() =>
            {
                WriteDebugTrace($"ProcessTimedOut: Pid={_process?.Id}; StartedAt={_startedAt}; Elapsed={DateTimeOffset.Now - _startedAt}");
                _process?.Kill();
            });

            _output = new ProcessOutput(_options.ShellExecutor.Encoding);
            _taskLazy = new Lazy<Task<ProcessOutput>>(AsTaskCore, LazyThreadSafetyMode.ExecutionAndPublication);
            _suppressPipeToConsole = !_options.Verbosity.HasFlag(ChellVerbosity.ConsoleOutputs);

            CommandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
            (Command, Arguments) = commandAndArguments;

            if (inputStream != null)
            {
                // Set the stdin stream and start a process immediately.
                ConnectStreamToStandardInput(inputStream);
            }
            else if (_options.RedirectStandardInput)
            {
                // Enable stdin redirection and start a process immediately.
                RedirectStandardInput();
            }
            else
            {
                // Delay startup to allow time to configure the stdin stream.
                // If a Task is requested (e.g. `await`, `AsTask`, `Pipe` ...), it will be started immediately.
                _ = ScheduleStartProcessAsync();
            }

            WriteDebugTrace($"Created: {commandLine}");

            async Task ScheduleStartProcessAsync()
            {
                await Task.Delay(ProcessStartScheduledDelay).ConfigureAwait(false);
                EnsureProcess();
            }
        }
        
        public static ProcessTask operator |(ProcessTask a, FormattableString b)
            => a.Pipe(new ProcessTask(b));
        public static ProcessTask operator |(ProcessTask a, Stream b)
            => a.Pipe(b);
        public static ProcessTask operator |(ProcessTask a, ProcessTask b)
            => a.Pipe(b);

        public static ProcessTask operator |(Stream a, ProcessTask b)
        {
            b.ConnectStreamToStandardInput(a);
            return b;
        }
        public static ProcessTask operator |(ReadOnlyMemory<byte> a, ProcessTask b)
        {
            b.ConnectStreamToStandardInput(new MemoryStream(a.ToArray()));
            return b;
        }

        public static implicit operator Task(ProcessTask task)
            => task.AsTask();
        public static implicit operator Task<ProcessOutput>(ProcessTask task)
            => task.AsTask();

        /// <summary>
        /// Gets the output of the process as Task.
        /// </summary>
        /// <returns></returns>
        public async Task<ProcessOutput> AsTask()
        {
            try
            {
                return await _taskLazy.Value.ConfigureAwait(false);
            }
            catch when (_suppressExceptionExitCodeNonZero)
            {
                return _output;
            }
        }

        public System.Runtime.CompilerServices.TaskAwaiter<ProcessOutput> GetAwaiter()
        {
            return AsTask().GetAwaiter();
        }

        /// <summary>
        /// Gets the exit code of the process as Task.
        /// </summary>
        public Task<int> ExitCode
        {
            get
            {
                if (_taskLazy.Value.IsCompleted)
                {
                    return Task.FromResult(_output.ExitCode);
                }

                return _taskLazy.Value.ContinueWith(x => _output.ExitCode);
            }
        }

        /// <summary>
        /// Configures the task to ignore the exception when the process returns exit code with non-zero.
        /// </summary>
        /// <returns></returns>
        public ProcessTask NoThrow()
        {
            _suppressExceptionExitCodeNonZero = true;
            return this;
        }

        public override string ToString()
        {
            return PreviousTask != null ? $"{PreviousTask} | {CommandLine}" : $"{CommandLine}";
        }

        /// <summary>
        /// Enables standard output redirection of the process.
        /// </summary>
        /// <remarks>
        /// Redirecting standard input must be done before the process has started. You can also use a <see cref="ProcessTaskOptions"/> that is guaranteed to enable redirection while creating a <see cref="ProcessTask"/>.
        /// </remarks>
        public void RedirectStandardInput(bool immediateLaunchProcess = true)
        {
            WriteDebugTrace($"RedirectStandardInput: immediateLaunchProcess={immediateLaunchProcess}");
            lock (_syncLock)
            {
                if (_process != null)
                {
                    throw new InvalidOperationException("The process has already been started. Redirecting standard input must be done before the process has started.");
                }

                _hasStandardIn = true;
            }

            if (immediateLaunchProcess)
            {
                EnsureProcess();
            }
        }

        /// <summary>
        /// Connects a stream to the standard input of the process.
        /// </summary>
        /// <remarks>
        /// Connecting standard input must be done before the process has started. You can also use a constructor argument that is guaranteed to receive a stream.
        /// </remarks>
        /// <param name="stream"></param>
        public void ConnectStreamToStandardInput(Stream stream)
        {
            lock (_syncLock)
            {
                if (_stdInStream != null) throw new InvalidOperationException("The standard input has already connected to the process.");
                _stdInStream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            RedirectStandardInput();
        }

        /// <summary>
        /// Suppresses output to the console if the standard output of the process is not configured.
        /// </summary>
        /// <returns></returns>
        public ProcessTask SuppressConsoleOutputs()
        {
            _suppressPipeToConsole = true;
            return this;
        }

        /// <summary>
        /// Pipes the standard output to the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public ProcessTask Pipe(Stream stream)
        {
            EnsureProcess();

            if (_stdOutPipe != null && _stdErrorPipe != null)
            {
                _stdOutPipe.Connect(stream);
            }

            _piped = true;

            ReadyPipe();
            return this;
        }

        /// <summary>
        /// Pipes the standard output to the another process.
        /// </summary>
        /// <param name="nextProcess"></param>
        /// <returns></returns>
        public ProcessTask Pipe(ProcessTask nextProcess)
        {
            // First, enable standard input redirection before starting a process for the next ProcessTask.
            nextProcess.RedirectStandardInput(immediateLaunchProcess: false);

            // Second, start a process.
            EnsureProcess();

            // Third, start a process for the next ProcessTask.
            nextProcess.EnsureProcess();

            if (_stdOutPipe != null && _stdErrorPipe != null)
            {
                if (nextProcess.Process != null)
                {
                    WriteDebugTrace($"Pipe: {Process!.Id} -> {nextProcess.Process.Id}");
                    _stdOutPipe.Connect(nextProcess.Process.StandardInput.BaseStream ?? Stream.Null);
                }
            }

            nextProcess.PreviousTask = this;
            _piped = true;

            ReadyPipe();
            return nextProcess;
        }

        private void EnsureProcess()
        {
            lock (_syncLock)
            {
                if (_process is null && _processException is null)
                {
                    StartProcess();
                }
            }
        }

        private void StartProcess()
        {
            Debug.Assert(_process is null);
            Debug.Assert(_processException is null);

            // Enable only when stdin is redirected or has input stream.
            // If RedirectStandardInput or CreateNoWindow is set to 'true', a process will not be interactive.
            var procStartInfo = new ProcessStartInfo
            {
                FileName = Command,
                Arguments = Arguments,
                UseShellExecute = false,
                CreateNoWindow = _options.Console.IsInputRedirected,
                RedirectStandardOutput = true,
                RedirectStandardInput = _options.Console.IsInputRedirected || _hasStandardIn,
                RedirectStandardError = true,
                WorkingDirectory = _options.WorkingDirectory ?? string.Empty,
            };

            if (_options.Verbosity.HasFlag(ChellVerbosity.CommandLine))
            {
                CommandLineHelper.WriteCommandLineToConsole(CommandLine, _options.Verbosity);
            }

            try
            {
                _processName = procStartInfo.FileName;
                _process = Process.Start(procStartInfo)!;
                WriteDebugTrace($"Process.Start: Pid={_process.Id}; HasStandardIn={_hasStandardIn}; StandardIn={_stdInStream}; IsInputRedirected={Console.IsInputRedirected}");

                // Set a timeout if the option has non-zero or max-value.
                if (_options.Timeout != TimeSpan.MaxValue && _options.Timeout != TimeSpan.Zero)
                {
                    _processCancellation.CancelAfter(_options.Timeout);
                }

                // Connect the stream to the standard input.
                if (_stdInStream != null)
                {
                    _ = CopyCoreAsync(_stdInStream, _process.StandardInput.BaseStream);
                }

                _stdOutPipe = new StreamPipe(Process?.StandardOutput.BaseStream ?? Stream.Null);
                _stdErrorPipe = new StreamPipe(Process?.StandardError.BaseStream ?? Stream.Null);
            }
            catch (Exception e)
            {
                _processException = ExceptionDispatchInfo.Capture(e);
            }

            static async Task CopyCoreAsync(Stream src, Stream dest)
            {
                await UnbufferedCopyToAsync(src, dest).ConfigureAwait(false);
                dest.Close();
            }
        }

        private void ReadyPipe()
        {
            WriteDebugTrace($"ReadyPipe: Pid={Process?.Id}; Piped={_piped}; _stdOutPipe={_stdOutPipe}; _stdErrorPipe={_stdErrorPipe}");

            if (!_piped)
            {
                if (!_suppressPipeToConsole)
                {
                    _stdOutPipe?.Connect(_options.Console.OpenStandardOutput());
                    _stdErrorPipe?.Connect(_options.Console.OpenStandardError());

                    // NOTE: LINQPad has no standard output support. Use TextWriter instead.
                    if (LINQPadHelper.RunningOnLINQPad)
                    {
                        if (_stdOutPipe != null)
                        {
                            LINQPadHelper.ConnectToTextWriter(_stdOutPipe, _options.Console.Out);
                        }
                        if (_stdErrorPipe != null)
                        {
                            LINQPadHelper.ConnectToTextWriter(_stdErrorPipe, _options.Console.Error);
                        }
                    }
                }
                _stdOutPipe?.Connect(_output.Sink.OutputWriter);
                _stdErrorPipe?.Connect(_output.Sink.ErrorWriter);
            }

            _stdOutPipe?.Ready();
            _stdErrorPipe?.Ready();
        }

        private static async Task UnbufferedCopyToAsync(Stream src, Stream dest, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[80 * 1024];
            while (true)
            {
                var read = await src.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }

                await dest.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                await dest.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ThrowIfParentTaskHasThrownProcessException()
        {
            if (PreviousTask != null)
            {
                // First, throw an exception for the parent task.
                await PreviousTask.ThrowIfParentTaskHasThrownProcessException().ConfigureAwait(false);

                // Second, Start the process.
                PreviousTask.EnsureProcess();

                // Third, If the process is failed to start, the task will be Faulted state immediately. We should throw an exception of prev task here.
                var t = PreviousTask.AsTask();
                if (t.IsFaulted)
                {
                    await t.ConfigureAwait(false);
                }
            }
        }

        private async Task<ProcessOutput> AsTaskCore()
        {
            await ThrowIfParentTaskHasThrownProcessException().ConfigureAwait(false);

            EnsureProcess();

            if (Process is {} proc)
            {
                if (_stdOutPipe is null || _stdErrorPipe is null) throw new InvalidOperationException();

                // If we have no stdin stream and Console's StandardInput is redirected, connects them automatically.
                var connectStdInToPipe = _options.EnableAutoWireStandardInput && !_hasStandardIn && _options.Console.IsInputRedirected /*Non-Interactive*/;
                if (connectStdInToPipe)
                {
                    StandardInput.Pipe.Connect(proc.StandardInput.BaseStream);
                    StandardInput.Pipe.Ready();
                }

                ReadyPipe();


                try
                {
#if NET5_0_OR_GREATER
                    await proc.WaitForExitAsync().ConfigureAwait(false);
#else
                    await Task.Run(() => proc.WaitForExit()).ConfigureAwait(false);
#endif
                }
                finally
                {
                    _processCancellationRegistration.Dispose();
                }

                _output.ExitCode = proc.ExitCode;

                WriteDebugTrace($"ProcessExited: Pid={proc.Id}; ExitCode={proc.ExitCode}");

                if (connectStdInToPipe)
                {
                    StandardInput.Pipe.Disconnect(proc.StandardInput.BaseStream);
                }

                // Flush output streams/pipes
                WriteDebugTrace($"Pipe/Sink.CompleteAsync: Pid={proc.Id}");
                await _stdOutPipe.CompleteAsync().ConfigureAwait(false);
                await _stdErrorPipe.CompleteAsync().ConfigureAwait(false);
                await _output.Sink.CompleteAsync().ConfigureAwait(false);
                WriteDebugTrace($"Pipe/Sink.CompleteAsync:Done: Pid={proc.Id}");

                await ThrowIfParentTaskHasThrownProcessException().ConfigureAwait(false);

                if (_output.ExitCode != 0)
                {
                    var ex = new ProcessTaskException(_processName ?? "Unknown", proc.Id, this, _output);
                    if (_processCancellation.IsCancellationRequested)
                    {
                        throw new OperationCanceledException($"The process has reached the timeout. (Timeout: {_options.Timeout})", ex);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
            else
            {
                _output.ExitCode = 127;
                _processCancellationRegistration.Dispose();
                if (_processException != null)
                {
                    throw new ProcessTaskException(this, _output, _processException.SourceException);
                }
                else
                {
                    throw new ProcessTaskException(this, _output);
                }
            }
            return _output;
        }

        [Conditional("DEBUG")]
        private void WriteDebugTrace(string s)
        {
            if (_options.Verbosity.HasFlag(ChellVerbosity.Debug))
            {
                _options.Console.Out.WriteLine($"[DEBUG][{_id}] {s}");
            }
        }
    }
}
