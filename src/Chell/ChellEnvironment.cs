using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Chell.Internal;
using Chell.IO;
using Chell.Shell;

namespace Chell
{
    [Flags]
    public enum ChellVerbosity
    {
        /// <summary>
        /// By default, no output is written to the console.
        /// </summary>
        Silent = 0,

        /// <summary>
        /// Writes a executing command line to the console.
        /// </summary>
        CommandLine = 1 << 0,

        /// <summary>
        /// Writes a command output to the console.
        /// </summary>
        ConsoleOutputs = 1 << 1,

        /// <summary>
        /// Writes all command lines and command outputs.
        /// </summary>
        Full = CommandLine | ConsoleOutputs,
    }

    public class ChellEnvironment
    {
        public static ChellEnvironment Current { get; set; } = new ChellEnvironment();
        
        private string[] _arguments;
        private string? _executablePath;
        private string _executableName;
        private string _executableDirectory;

        public ChellEnvironment()
        {
            var args = Environment.GetCommandLineArgs();
            var path = args[0];
            _arguments = args.Skip(1).ToArray();
            _executablePath = path;
            _executableName = Path.GetFileName(path);
            _executableDirectory = Path.GetDirectoryName(path)!;
        }

        /// <summary>
        /// Gets or sets the verbosity.
        /// </summary>
        public ChellVerbosity Verbosity { get; set; } = ChellVerbosity.Full;

        public ShellExecutorProvider Shell { get; } = new ShellExecutorProvider();

        /// <summary>
        /// Gets whether the current application is running on Windows.
        /// </summary>
        public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Gets the command line arguments. like <c>args</c> of a entry point.
        /// </summary>
        public string[] Arguments => _arguments;

        /// <summary>
        /// Gets the path of the executing application. like <c>argv[0]</c>. (e.g. C:\\Path\To\App.exe, /path/to/app)
        /// </summary>
        /// <remarks>
        /// The path may be null when running a inline script.
        /// </remarks>
        public string? ExecutablePath => _executablePath;

        /// <summary>
        /// Gets the name of the executing application. like <c>argv[0]</c>. (e.g. App.exe, app)
        /// </summary>
        public string ExecutableName => _executableName;

        /// <summary>
        /// Gets the directory of the executing application. like <c>argv[0]</c>. (e.g. C:\\Path\To, /path/to)
        /// </summary>
        public string ExecutableDirectory => _executableDirectory;

        /// <summary>
        /// Gets or sets the path of the current working directory.
        /// </summary>
        public string CurrentDirectory
        {
            get => Environment.CurrentDirectory;
            set => Environment.CurrentDirectory = value;
        }

        /// <summary>
        /// Gets the environment variables as <see cref="IDictionary{String, String}"/> representation.
        /// </summary>
        public IDictionary<string, string> Vars { get; } = new EnvironmentVariables();

        /// <summary>
        /// Gets the standard input stream.
        /// </summary>
        public ChellReadableStream StdIn => new ChellReadableStream(Console.OpenStandardInput(), Console.InputEncoding);

        /// <summary>
        /// Gets the standard output stream.
        /// </summary>
        public ChellWritableStream StdOut => new ChellWritableStream(Console.OpenStandardOutput(), Console.OutputEncoding);

        /// <summary>
        /// Gets the standard output stream.
        /// </summary>
        public ChellWritableStream StdErr => new ChellWritableStream(Console.OpenStandardError(), Console.OutputEncoding);


        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetCommandLineArgs(string? executablePath, string executableName, string executableDirectory, string[] args)
        {
            _arguments = args.ToArray();
            _executableName = executableName;
            _executablePath = executablePath;
            _executableDirectory = executableDirectory;
        }
    }
}