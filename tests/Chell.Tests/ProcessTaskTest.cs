using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chell.IO;
using Chell.Shell;
using FluentAssertions;
using Kokuban;
using Xunit;

namespace Chell.Tests
{
    internal static class ProcessTaskTestFixtureExtensions
    {
        public static TemporaryAppBuilder.Compilation AddTo(this TemporaryAppBuilder.Compilation compilation,
            IList<IDisposable> disposables)
        {
            disposables.Add(compilation);
            return compilation;
        }
    }

    public class ProcessTaskTestFixture : IDisposable
    {
        private readonly TemporaryAppSolutionBuilder _slnBuilder = new TemporaryAppSolutionBuilder();
        public string EchoArg { get; }
        public string EchoOutAndErrorArgs { get; }
        public string HelloWorld { get; }
        public string ExitCodeNonZero { get; }
        public string ExitCodeNonZeroWaited { get; }
        public string WriteCommandLineArgs { get; }
        public string StandardInputPassThroughText { get; }
        public string StandardInputPassThroughBinary { get; }
        public string WriteSleepWriteExit { get; }
        public string ReadOnce { get; }
        public string ReadAllLines { get; }
        public string WriteCurrentDirectory { get; }
        public string Never { get; }

        public ProcessTaskTestFixture()
        {
            EchoArg = _slnBuilder.CreateProject(nameof(EchoArg), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    Console.WriteLine(""["" + Environment.GetCommandLineArgs()[1] + ""]"");
                "));
            EchoOutAndErrorArgs = _slnBuilder.CreateProject(nameof(EchoOutAndErrorArgs), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    using System.Threading.Tasks;
                    Console.Out.WriteLine(""["" + Environment.GetCommandLineArgs()[1] + ""]"");
                    await Task.Delay(100);
                    Console.Error.WriteLine(""["" + Environment.GetCommandLineArgs()[2] + ""]"");
                    await Task.Delay(100);
                    Console.Out.WriteLine(""["" + Environment.GetCommandLineArgs()[3] + ""]"");
                    await Task.Delay(100);
                    Console.Error.WriteLine(""["" + Environment.GetCommandLineArgs()[4] + ""]"");
                "));
            HelloWorld = _slnBuilder.CreateProject(nameof(HelloWorld), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    Console.WriteLine(""Hello World!"");
                "));
            ExitCodeNonZero = _slnBuilder.CreateProject(nameof(ExitCodeNonZero), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    Environment.ExitCode = 192;
                "));
            ExitCodeNonZeroWaited = _slnBuilder.CreateProject(nameof(ExitCodeNonZeroWaited), builder => 
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    using System.Threading.Tasks;
                    await Task.Delay(100);
                    Environment.ExitCode = 192;
                "));
            WriteCommandLineArgs = _slnBuilder.CreateProject(nameof(WriteCommandLineArgs), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    foreach (var line in Environment.GetCommandLineArgs())
                    {
                        Console.WriteLine(line);
                    }
                "));
            StandardInputPassThroughText = _slnBuilder.CreateProject(nameof(StandardInputPassThroughText), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    Console.InputEncoding = System.Text.Encoding.UTF8;
                    Console.OutputEncoding = System.Text.Encoding.UTF8;
                    Console.Write(Console.In.ReadToEnd());
                "));
            StandardInputPassThroughBinary = _slnBuilder.CreateProject(nameof(StandardInputPassThroughBinary), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    Console.OpenStandardInput().CopyTo(Console.OpenStandardOutput());
                "));
            WriteSleepWriteExit = _slnBuilder.CreateProject(nameof(WriteSleepWriteExit), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    using System.Threading;
                    Console.WriteLine(""Hello"");
                    Thread.Sleep(1000);
                    Console.WriteLine(""Hello"");
                "));
            ReadOnce = _slnBuilder.CreateProject(nameof(ReadOnce), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    Console.WriteLine(Console.ReadLine());
                "));
            ReadAllLines = _slnBuilder.CreateProject(nameof(ReadAllLines), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    while (true)
                    {
                        var line = Console.ReadLine();
                        if (line == null) return;
                        Console.WriteLine(line);
                    }
                "));
            WriteCurrentDirectory = _slnBuilder.CreateProject(nameof(WriteCurrentDirectory), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    Console.WriteLine(Environment.CurrentDirectory);
                "));
            Never = _slnBuilder.CreateProject(nameof(Never), builder =>
                builder.WriteSourceFile("Program.cs", @"
                    using System;
                    using System.Threading;
                    Console.WriteLine(""Hello"");
                    while (true) { Thread.Sleep(1000); }
                "));
            _slnBuilder.Build();
        }

        public void Dispose()
        {
            _slnBuilder.Dispose();
        }
    }

    [Collection("ProcessTaskTest")] // NOTE: Test cases use `Console` and does not run in parallel.
    public class ProcessTaskTest : IClassFixture<ProcessTaskTestFixture>
    {
        private readonly ProcessTaskTestFixture _fixture;

        public ProcessTaskTest(ProcessTaskTestFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<(string StandardOut, string StandardError)> RunAsync(Func<IConsoleProvider, Task> func)
        {
            var fakeConsole = new FakeConsoleProvider();

            await func(fakeConsole);

            return (fakeConsole.GetStandardOutputAsString(), fakeConsole.GetStandardErrorAsString());
        }

        [Fact]
        public async Task CommandNotFound_UseShell()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var dummyCommandName = Guid.NewGuid().ToString();
            var procTask = new ProcessTask($"{dummyCommandName} --help");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                (await procTask.ExitCode).Should().Be(1); // A shell (cmd) will return 1.
            }
            else
            {
                (await procTask.ExitCode).Should().Be(127); // A shell (bash) will return 127.
            }
        }

        [Fact]
        public async Task CommandNotFound_NoUseShell()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var dummyCommandName = Guid.NewGuid().ToString();
            var procTask = new ProcessTask($"{dummyCommandName} --help", new ProcessTaskOptions(shellExecutor: new NoUseShellExecutor()));
            (await procTask.ExitCode).Should().Be(127); // System.Diagnostics.Process will return 127.
        }

        [Fact]
        public async Task Execute()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var procTask = new ProcessTask($"{_fixture.HelloWorld}");
            (await procTask.ExitCode).Should().Be(0);

            var result = await procTask;
            result.ExitCode.Should().Be(0);
            result.Combined.Should().Be("Hello World!"  + Environment.NewLine);
            result.Output.Should().Be("Hello World!" + Environment.NewLine);
            result.Error.Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessOutputInArgumentShouldBeTrimmed()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var procTask1 = new ProcessTask($"{_fixture.HelloWorld}");
            var result1 = await procTask1;
            var procTask2 = new ProcessTask($"{_fixture.EchoArg} {result1}");
            var result2 = await procTask2;

            result1.Combined.Should().Be("Hello World!" + Environment.NewLine);
            result2.Combined.Should().Be("[Hello World!]" + Environment.NewLine);
        }

        [Fact]
        public async Task ExpandArguments()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var args = new[] { "Alice", "Karen", "Program Files", @"C:\Program Files (x86)\Microsoft Visual Studio" };
            var procTask = new ProcessTask($"{_fixture.WriteCommandLineArgs} {args}");
            var result = await procTask;

            // NOTE: We need skip the first line which is path of the command.
            result.AsLines(trimEnd: true).Skip(1).Should().BeEquivalentTo(args);
        }

        [Fact]
        public async Task ExpandArguments_Escape()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var args = new[] { "Alice", "Karen", "Program Files", @"C:\Program Files (x86)\Microsoft Visual Studio", "\"\\'|<>" };
            var procTask = new ProcessTask($"{_fixture.WriteCommandLineArgs} {args}");
            var result = await procTask;

            // NOTE: We need skip the first line which is path of the command.
            result.AsLines(trimEnd: true).Skip(1).Should().BeEquivalentTo(args);
        }

        [Fact]
        public async Task ExitCode()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var procTask = new ProcessTask($"{_fixture.ExitCodeNonZero}");
            (await procTask.ExitCode).Should().Be(192);
        }

        [Fact]
        public async Task ExitCode_ThrowIfNonZero()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var procTask = new ProcessTask($"{_fixture.ExitCodeNonZero}");
            await Assert.ThrowsAsync<ProcessTaskException>(async () => await procTask);
        }

        [Fact]
        public async Task ProcessOutput_StandardInputPassThroughText()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var memStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, コンニチハ!\nABCDEFG"));
            var procTask = new ProcessTask(memStream, $"{_fixture.StandardInputPassThroughText}");
            var result = await procTask;

            result.Output.TrimEnd().Should().Be("Hello, コンニチハ!\nABCDEFG");
        }

        [Fact]
        public async Task ProcessOutput_StandardInputOutputCombined()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var procTask = new ProcessTask($"{_fixture.EchoOutAndErrorArgs} Arg0 Arg1 Arg2 Arg3");
            var result = await procTask;

            result.Output.TrimEnd().Should().Be(string.Join(Environment.NewLine, "[Arg0]", "[Arg2]"));
            result.Error.TrimEnd().Should().Be(string.Join(Environment.NewLine, "[Arg1]", "[Arg3]"));
            result.Combined.TrimEnd().Should().Be(string.Join(Environment.NewLine, "[Arg0]", "[Arg1]", "[Arg2]", "[Arg3]"));
        }


        [Fact]
        public async Task ProcessOutput_StandardInputPassThroughBinary()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var memStream = new MemoryStream(Encoding.Unicode.GetBytes("Hello, コンニチハ!\nABCDEFG"));
            var procTask = new ProcessTask(memStream, $"{_fixture.StandardInputPassThroughBinary}");
            var result = await procTask;

            result.OutputBinary.ToArray().Should().BeEquivalentTo(memStream.ToArray());
        }

        [Fact]
        public async Task Pipe_StandardInputPassThroughBinary()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var data = Encoding.Unicode.GetBytes("Hello, コンニチハ!\nABCDEFG");
            var memStream = new MemoryStream(data);
            var procTask = new ProcessTask(memStream, $"{_fixture.StandardInputPassThroughBinary}");
            var destStream = new MemoryStream();
            var result = await procTask.Pipe(destStream);

            result.ExitCode.Should().Be(0);
            result.OutputBinary.Length.Should().Be(0);
            destStream.ToArray().Should().BeEquivalentTo(data);
        }

        [Fact]
        public async Task Pipe_CloseDestFirst()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var srcTask = new ProcessTask($"{_fixture.WriteSleepWriteExit}");
            var destTask = new ProcessTask($"{_fixture.ReadOnce}");

            await (srcTask | destTask);
        }

        [Fact]
        public async Task Pipe_CloseSrcFirst()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var srcTask = new ProcessTask($"{_fixture.HelloWorld}");
            var destTask = new ProcessTask($"{_fixture.ReadAllLines}");

            await (srcTask | destTask);
        }

        [Fact]
        public async Task Pipe_ExitCode_NonZero()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var srcTask = new ProcessTask($"{_fixture.ExitCodeNonZero}");
            var destTask = new ProcessTask($"{_fixture.ReadAllLines}");

            await Assert.ThrowsAsync<ProcessTaskException>(async () => await (srcTask | destTask));
        }
        
        [Fact]
        public async Task Pipe_ExitCode_NonZero_ExitTailFirst()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var srcTask = new ProcessTask($"{_fixture.ExitCodeNonZeroWaited}");
            var destTask = new ProcessTask($"{_fixture.ReadAllLines}");

            await Assert.ThrowsAsync<ProcessTaskException>(async () => await (srcTask | destTask));
        }

        [Fact]
        public async Task Pipe_ExitCode_NonZero_NoThrow()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var srcTask = new ProcessTask($"{_fixture.ExitCodeNonZero}");
            var destTask = new ProcessTask($"{_fixture.ReadAllLines}");

            await (srcTask.NoThrow() | destTask);
        }

        [Fact]
        public async Task WorkingDirectory()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            {
                var currentDirectory = Environment.CurrentDirectory;
                var output = await new ProcessTask($"{_fixture.WriteCurrentDirectory}");
                output.ToString().Trim().Should().Be(currentDirectory);
            }
            {
                var currentDirectory = Environment.CurrentDirectory;
                var workingDirectory = Path.GetFullPath(Path.Combine(currentDirectory, ".."));
                var output = await new ProcessTask($"{_fixture.WriteCurrentDirectory}", new ProcessTaskOptions().WithWorkingDirectory(workingDirectory));
                output.ToString().Trim().Should().Be(workingDirectory);
            }
        }

        [Fact]
        public async Task ProcessTimeout()
        {
            Func<Task> execute = async () =>
            {
                using var fakeConsoleScope = new FakeConsoleProviderScope();
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await new ProcessTask($"{_fixture.WriteSleepWriteExit}",
                        new ProcessTaskOptions().WithTimeout(TimeSpan.FromMilliseconds(300)));
                });
            };
            await execute.Should().CompleteWithinAsync(TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task ProcessTimeout_Never()
        {
            Func<Task> execute = async () =>
            {
                using var fakeConsoleScope = new FakeConsoleProviderScope();
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await new ProcessTask($"{_fixture.Never}",
                        new ProcessTaskOptions().WithTimeout(TimeSpan.FromMilliseconds(300)));
                });
            };
            await execute.Should().CompleteWithinAsync(TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task Verbosity_Silent()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var (stdOut, stdErr) = await RunAsync(async (console) =>
            {
                await new ProcessTask($"{_fixture.HelloWorld}", new ProcessTaskOptions(console: console).WithVerbosity(ChellVerbosity.Silent));
            });

            stdOut.Should().BeEmpty();
        }

        [Fact]
        public async Task Verbosity_CommandLine()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var (stdOut, stdErr) = await RunAsync(async (console) =>
            {
                await new ProcessTask($"{_fixture.HelloWorld}", new ProcessTaskOptions(console: console).WithVerbosity(ChellVerbosity.CommandLine));
            });

            stdOut.Should().StartWith("$ ");
            stdOut.Should().NotContain("Hello World!");
        }

        [Fact]
        public async Task Verbosity_ConsoleOutputs()
        {
            using var fakeConsoleScope = new FakeConsoleProviderScope();
            var (stdOut, stdErr) = await RunAsync(async (console) =>
            {
                await new ProcessTask($"{_fixture.HelloWorld}", new ProcessTaskOptions(console: console).WithVerbosity(ChellVerbosity.ConsoleOutputs));
            });

            stdOut.Should().Be("Hello World!" + Environment.NewLine);
        }

        private class FakeConsoleProviderScope : IDisposable
        {
            private readonly KokubanColorMode _origKokubanColorMode;
            private readonly IConsoleProvider _origConsoleProvider;
            private readonly FakeConsoleProvider _fakeConsoleProvider;

            public string StdOut => _fakeConsoleProvider.GetStandardOutputAsString();
            public string StdErr => _fakeConsoleProvider.GetStandardErrorAsString();

            public FakeConsoleProviderScope()
            {
                _origKokubanColorMode = KokubanOptions.Default.Mode;
                _origConsoleProvider = ChellEnvironment.Current.Console;
                _fakeConsoleProvider = new FakeConsoleProvider();

                KokubanOptions.Default.Mode = KokubanColorMode.None;
                ChellEnvironment.Current.Console = _fakeConsoleProvider;
            }

            public void Dispose()
            {
                ChellEnvironment.Current.Console = _origConsoleProvider;
                _fakeConsoleProvider.Dispose();
                KokubanOptions.Default.Mode = _origKokubanColorMode;
            }
        }
    }

    public class FakeConsoleProvider : IConsoleProvider, IDisposable
    {
        private readonly Pipe _outputPipe;
        private readonly Pipe _errorPipe;
        private readonly CancellationTokenSource _cts;

        private readonly MemoryStream _input = new MemoryStream();
        private readonly MemoryStream _output = new MemoryStream();
        private readonly MemoryStream _error = new MemoryStream();

        public FakeConsoleProvider()
        {
            _cts = new CancellationTokenSource();
            _outputPipe = new Pipe(new PipeOptions(readerScheduler:PipeScheduler.Inline, writerScheduler:PipeScheduler.Inline));
            _outputPipe.Reader.CopyToAsync(_output, _cts.Token);
            _errorPipe = new Pipe(new PipeOptions(readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline));
            _errorPipe.Reader.CopyToAsync(_error, _cts.Token);
        }

        public string GetStandardOutputAsString() => Encoding.UTF8.GetString(_output.ToArray());
        public string GetStandardErrorAsString() => Encoding.UTF8.GetString(_error.ToArray());

        public Stream OpenStandardInput()
            => _input;

        public Stream OpenStandardOutput()
            => _outputPipe.Writer.AsStream(leaveOpen: true);

        public Stream OpenStandardError()
            => _errorPipe.Writer.AsStream(leaveOpen: true);

        public Encoding InputEncoding => new UTF8Encoding(false);
        public Encoding OutputEncoding => new UTF8Encoding(false);
        public Encoding ErrorEncoding => new UTF8Encoding(false);
        public bool IsInputRedirected => false;
        public bool IsOutputRedirected => false;
        public bool IsErrorRedirected => false;
        public TextWriter Out => new StreamWriter(_outputPipe.Writer.AsStream(leaveOpen: true), OutputEncoding, leaveOpen: true) { AutoFlush = true };
        public TextWriter Error => new StreamWriter(_errorPipe.Writer.AsStream(leaveOpen: true), ErrorEncoding, leaveOpen: true) { AutoFlush = true };
        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
