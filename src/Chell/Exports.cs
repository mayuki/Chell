using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Chell.Internal;
using Chell.IO;
using Kokuban;
using Kokuban.AnsiEscape;

namespace Chell
{
    public static class Exports
    {
        public static class Verbosity
        {
            /// <summary>
            /// By default, no output is written to the console.
            /// </summary>
            public const ChellVerbosity Silent = ChellVerbosity.Silent;

            /// <summary>
            /// Writes a executing command line to the console.
            /// </summary>
            public const ChellVerbosity CommandLine = ChellVerbosity.CommandLine;

            /// <summary>
            /// Writes a command output to the console.
            /// </summary>
            public const ChellVerbosity ConsoleOutputs = ChellVerbosity.ConsoleOutputs;

            /// <summary>
            /// Writes all command lines and command outputs.
            /// </summary>
            public const ChellVerbosity Full = ChellVerbosity.Full;
        }

        /// <summary>
        /// Gets the current environment.
        /// </summary>
        public static ChellEnvironment Env => ChellEnvironment.Current;

        /// <summary>
        /// Gets the standard input stream.
        /// </summary>
        public static ChellReadableStream StdIn => Env.StdIn;

        /// <summary>
        /// Gets the standard output stream.
        /// </summary>
        public static ChellWritableStream StdOut => Env.StdOut;

        /// <summary>
        /// Gets the standard error stream.
        /// </summary>
        public static ChellWritableStream StdErr => Env.StdErr;

        /// <summary>
        /// Gets the command line arguments. like <c>args</c> of a entry point.
        /// </summary>
        public static string[] Arguments => Env.Arguments;

        /// <summary>
        /// Gets the path of the executing application. like <c>argv[0]</c>. (e.g. C:\\Path\To\App.exe, /path/to/app)
        /// </summary>
        /// <remarks>
        /// The path may be null when running a inline script.
        /// </remarks>
        public static string? ExecutablePath => Env.ExecutablePath;

        /// <summary>
        /// Gets the name of the executing application. like <c>argv[0]</c>. (e.g. App.exe, app)
        /// </summary>
        public static string ExecutableName => Env.ExecutableName;

        /// <summary>
        /// Gets the directory of the executing application. like <c>argv[0]</c>. (e.g. C:\\Path\To, /path/to)
        /// </summary>
        public static string ExecutableDirectory => Env.ExecutableDirectory;

        /// <summary>
        /// Gets or sets the path of the current working directory.
        /// </summary>
        public static string CurrentDirectory
        {
            get => Env.CurrentDirectory;
            set => Env.CurrentDirectory = value;
        }

        /// <summary>
        /// Gets the Kokuban ANSI style builder to decorate texts.
        /// </summary>
        public static AnsiStyle Chalk => Kokuban.Chalk.Create(KokubanOptions.Default);

        /// <summary>
        /// Starts the process task with the specified command line.
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ProcessTask Run(FormattableString commandLine, ProcessTaskOptions? options = default)
            => new ProcessTask(commandLine, options);

        /// <summary>
        /// Starts the process task with the specified command line.
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ProcessTask Run(CommandLineString commandLine, ProcessTaskOptions? options = default)
            => new ProcessTask(commandLine, options);

        /// <summary>
        /// Starts the process task with the specified command line.
        /// </summary>
        /// <param name="inputStream">The data to be passed to the standard input of the process.</param>
        /// <param name="commandLine"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ProcessTask Run(Stream inputStream, FormattableString commandLine, ProcessTaskOptions? options = default)
            => new ProcessTask(inputStream, commandLine, options);

        /// <summary>
        /// Starts the process task with the specified command line.
        /// </summary>
        /// <param name="inputStream">The data to be passed to the standard input of the process.</param>
        /// <param name="commandLine"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ProcessTask Run(Stream inputStream, CommandLineString commandLine, ProcessTaskOptions? options = default)
            => new ProcessTask(inputStream, commandLine, options);

        /// <summary>
        /// Starts the process task with the specified command line.
        /// </summary>
        /// <param name="inputData">The data to be passed to the standard input of the process.</param>
        /// <param name="commandLine"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ProcessTask Run(ReadOnlyMemory<byte> inputData, FormattableString commandLine, ProcessTaskOptions? options = default)
            => new ProcessTask(inputData, commandLine, options);

        /// <summary>
        /// Starts the process task with the specified command line.
        /// </summary>
        /// <param name="inputData">The data to be passed to the standard input of the process.</param>
        /// <param name="commandLine"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ProcessTask Run(ReadOnlyMemory<byte> inputData, CommandLineString commandLine, ProcessTaskOptions? options = default)
            => new ProcessTask(inputData, commandLine, options);

        /// <summary>
        /// Writes the message to the console.
        /// </summary>
        /// <param name="message"></param>
        public static void Echo(object? message = default)
            => Console.WriteLine(message);

        /// <summary>
        /// Writes the object details to the console.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public static void Dump<T>(T obj)
            => ObjectDumper.Dump(obj);

        /// <summary>
        /// Converts the JSON to an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T? FromJson<T>(string json, T shape)
            => FromJson<T>(json);

        /// <summary>
        /// Converts the JSON to an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T? FromJson<T>(string json)
            => Chell.Extensions.StringExtensions.AsJson<T>(json);

        /// <summary>
        /// Changes the current directory to the specified path.
        /// </summary>
        /// <remarks>
        /// Dispose the return value to return to the previous directory.
        /// </remarks>
        /// <param name="path"></param>
        public static IDisposable Cd(string path)
            => new ChangeDirectoryScope(path);

        private class ChangeDirectoryScope : IDisposable
        {
            private readonly string _previousCurrentDirectory;

            public ChangeDirectoryScope(string newCurrentDirectory)
            {
                _previousCurrentDirectory = Environment.CurrentDirectory;
                ChangeDirectory(newCurrentDirectory);
            }

            public void Dispose()
            {
                ChangeDirectory(_previousCurrentDirectory);
            }

            private void ChangeDirectory(string path)
            {
                CommandLineHelper.WriteCommandLineToConsole($"cd {path}");
                Environment.CurrentDirectory = path;
            }
        }

        /// <summary>
        /// Sleeps for the specified time.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static Task Sleep(TimeSpan timeSpan)
            => Task.Delay(timeSpan);

        /// <summary>
        /// Sleeps for the specified time.
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static Task Sleep(int seconds)
            => Task.Delay(TimeSpan.FromSeconds(seconds));

        /// <summary>
        /// Get the task to ignore the exception and return <see cref="ProcessOutput"/>.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static Task<ProcessOutput> NoThrow(ProcessTask task)
            => task.NoThrow();

        /// <summary>
        /// Terminates the current process with specified exit code.
        /// </summary>
        /// <param name="exitCode"></param>
        public static void Exit(int exitCode = 0)
            => Environment.Exit(exitCode);

        /// <summary>
        /// Fetches the content of the specified URL using GET method.
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> FetchAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            CommandLineHelper.WriteCommandLineToConsole($"{nameof(FetchAsync)} {requestUri}");
            var httpClient = new HttpClient();
            return httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
        }

        /// <summary>
        /// Fetches the content of the specified URL as string using GET method.
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> FetchStringAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            CommandLineHelper.WriteCommandLineToConsole($"{nameof(FetchStringAsync)} {requestUri}");
            var httpClient = new HttpClient();
            var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
            res.EnsureSuccessStatusCode();

#if NET5_0_OR_GREATER
            return await res.Content.ReadAsStringAsync(cancellationToken);
#else
            return await res.Content.ReadAsStringAsync();
#endif
        }

        /// <summary>
        /// Fetches the content of the specified URL as byte[] using GET method.
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<byte[]> FetchByteArrayAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            CommandLineHelper.WriteCommandLineToConsole($"{nameof(FetchByteArrayAsync)} {requestUri}");
            var httpClient = new HttpClient();
            var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
            res.EnsureSuccessStatusCode();

#if NET5_0_OR_GREATER
            return await res.Content.ReadAsByteArrayAsync(cancellationToken);
#else
            return await res.Content.ReadAsByteArrayAsync();
#endif
        }

        /// <summary>
        /// Fetches the content of the specified URL as Stream using GET method.
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<Stream> FetchStreamAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            CommandLineHelper.WriteCommandLineToConsole($"{nameof(FetchStreamAsync)} {requestUri}");
            var httpClient = new HttpClient();
            var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
            res.EnsureSuccessStatusCode();

#if NET5_0_OR_GREATER
            return await res.Content.ReadAsStreamAsync(cancellationToken);
#else
            return await res.Content.ReadAsStreamAsync();
#endif
        }

        /// <summary>
        /// Gets the full path to a command, similar to the `which` command on Unix.
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public static string Which(string commandName)
            => Internal.Which.TryGetPath(commandName, out var matchedPath)
                ? matchedPath
                : throw new FileNotFoundException($"Command '{commandName}' is not found.");

        /// <summary>
        /// Gets the full path to a command, similar to the `which` command on Unix.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="matchedPath"></param>
        /// <returns></returns>
        public static bool TryWhich(string commandName, out string matchedPath)
            => Internal.Which.TryGetPath(commandName, out matchedPath);

        /// <summary>
        /// Enumerates paths under the current directory that match the specified glob pattern.
        /// </summary>
        /// <remarks>
        /// A glob pattern accepts <c>*</c> and <c>**</c> (e.g. <c>**/*.cs</c>). If the specify a pattern is started with '!', it will be treated as an excluded pattern.
        /// </remarks>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public static IEnumerable<string> Glob(params string[] patterns)
            => Glob(Environment.CurrentDirectory, patterns);

        /// <summary>
        /// Enumerates paths under the specified directory that match the specified glob pattern.
        /// </summary>
        /// <remarks>
        /// A glob pattern accepts <c>*</c> and <c>**</c> (e.g. <c>**/*.cs</c>). If the specify a pattern is started with '!', it will be treated as an excluded pattern.
        /// </remarks>
        /// <param name="baseDir"></param>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public static IEnumerable<string> Glob(string baseDir, string[] patterns)
        {
            var matcher = new Matcher();

            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith("!"))
                {
                    matcher.AddExclude(pattern.Substring(1));
                }
                else
                {
                    matcher.AddInclude(pattern);
                }
            }

            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDir)));
            return result.Files
                .Select(x => Path.GetFullPath(Path.Combine(baseDir, x.Stem))); // NOTE: Microsoft.Extensions.FileSystemGlobbing 5.0.0 does not reflect the root directory in `Path`.
        }

        /// <summary>
        /// Displays the message and reads lines entered by the user from the console.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task<string?> Prompt(string message)
        {
            Console.Write(message);
            return await Console.In.ReadLineAsync();
        }
    }
}
