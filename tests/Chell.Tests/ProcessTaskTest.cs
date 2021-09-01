using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;

namespace Chell.Tests
{
    public class ProcessTaskTestFixture : IDisposable
    {
        public TemporaryAppBuilder.Compilation EchoArg = TemporaryAppBuilder.Create(nameof(EchoArg))
            .WriteSourceFile("Program.cs", @"
                using System;
                Console.WriteLine(""["" + Environment.GetCommandLineArgs()[1] + ""]"");
            ")
            .Build();
        public TemporaryAppBuilder.Compilation EchoOutAndErrorArgs = TemporaryAppBuilder.Create(nameof(EchoOutAndErrorArgs))
            .WriteSourceFile("Program.cs", @"
                using System;
                using System.Threading.Tasks;
                Console.Out.WriteLine(""["" + Environment.GetCommandLineArgs()[1] + ""]"");
                await Task.Delay(100);
                Console.Error.WriteLine(""["" + Environment.GetCommandLineArgs()[2] + ""]"");
                await Task.Delay(100);
                Console.Out.WriteLine(""["" + Environment.GetCommandLineArgs()[3] + ""]"");
                await Task.Delay(100);
                Console.Error.WriteLine(""["" + Environment.GetCommandLineArgs()[4] + ""]"");
            ")
            .Build();
        public TemporaryAppBuilder.Compilation HelloWorld = TemporaryAppBuilder.Create(nameof(HelloWorld))
            .WriteSourceFile("Program.cs", @"
                using System;
                Console.WriteLine(""Hello World!"");
            ")
            .Build();
        public TemporaryAppBuilder.Compilation ExitCodeNonZero = TemporaryAppBuilder.Create(nameof(ExitCodeNonZero))
            .WriteSourceFile("Program.cs", @"
                using System;
                Environment.ExitCode = 456;
            ")
            .Build();
        public TemporaryAppBuilder.Compilation WriteCommandLineArgs = TemporaryAppBuilder.Create(nameof(WriteCommandLineArgs))
            .WriteSourceFile("Program.cs", @"
                using System;
                foreach (var line in Environment.GetCommandLineArgs())
                {
                    Console.WriteLine(line);
                }
            ")
            .Build();
        public TemporaryAppBuilder.Compilation StandardInputPassThroughText = TemporaryAppBuilder.Create(nameof(StandardInputPassThroughText))
            .WriteSourceFile("Program.cs", @"
                using System;
                Console.InputEncoding = System.Text.Encoding.UTF8;
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.Write(Console.In.ReadToEnd());
            ")
            .Build();
        public TemporaryAppBuilder.Compilation StandardInputPassThroughBinary = TemporaryAppBuilder.Create(nameof(StandardInputPassThroughBinary))
            .WriteSourceFile("Program.cs", @"
                using System;
                Console.OpenStandardInput().CopyTo(Console.OpenStandardOutput());
            ")
            .Build();
        public TemporaryAppBuilder.Compilation WriteSleepWriteExit = TemporaryAppBuilder.Create(nameof(WriteSleepWriteExit))
            .WriteSourceFile("Program.cs", @"
                using System;
                using System.Threading;
                Console.WriteLine(""Hello"");
                Thread.Sleep(1000);
                Console.WriteLine(""Hello"");
            ")
            .Build();
        public TemporaryAppBuilder.Compilation ReadOnce = TemporaryAppBuilder.Create(nameof(ReadOnce))
            .WriteSourceFile("Program.cs", @"
                using System;
                Console.WriteLine(Console.ReadLine());
            ")
            .Build();
        public TemporaryAppBuilder.Compilation ReadAllLines = TemporaryAppBuilder.Create(nameof(ReadAllLines))
            .WriteSourceFile("Program.cs", @"
                using System;
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line == null) return;
                    Console.WriteLine(line);
                }
            ")
            .Build();
        public TemporaryAppBuilder.Compilation WriteCurrentDirectory = TemporaryAppBuilder.Create(nameof(WriteCurrentDirectory))
            .WriteSourceFile("Program.cs", @"
                using System;
                Console.WriteLine(Environment.CurrentDirectory);
            ")
            .Build();

        public void Dispose()
        {
            EchoArg.Dispose();
            HelloWorld.Dispose();
            ExitCodeNonZero.Dispose();
            WriteCommandLineArgs.Dispose();
            StandardInputPassThroughText.Dispose();
            StandardInputPassThroughBinary.Dispose();
        }
    }

    public class ProcessTaskTest : IClassFixture<ProcessTaskTestFixture>
    {
        private readonly ProcessTaskTestFixture _fixture;

        public ProcessTaskTest(ProcessTaskTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CommandNotFound_UseShell()
        {
            var dummyCommandName = Guid.NewGuid().ToString();
            var procTask = new ProcessTask($"{dummyCommandName} --help");
            (await procTask.ExitCode).Should().Be(1); // A shell (bash, cmd, ...) will return 1.
        }

        [Fact]
        public async Task Execute()
        {
            var procTask = new ProcessTask($"{_fixture.HelloWorld.ExecutablePath}");
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
            var procTask1 = new ProcessTask($"{_fixture.HelloWorld.ExecutablePath}");
            var result1 = await procTask1;
            var procTask2 = new ProcessTask($"{_fixture.EchoArg.ExecutablePath} {result1}");
            var result2 = await procTask2;

            result1.Combined.Should().Be("Hello World!" + Environment.NewLine);
            result2.Combined.Should().Be("[Hello World!]" + Environment.NewLine);
        }

        [Fact]
        public async Task ExpandArguments()
        {
            var args = new[] { "Alice", "Karen", "Program Files", @"C:\Program Files (x86)\Microsoft Visual Studio" };
            var procTask = new ProcessTask($"{_fixture.WriteCommandLineArgs.ExecutablePath} {args}");
            var result = await procTask;

            // NOTE: We need skip the first line which is path of the command.
            result.AsLines(trimEnd: true).Skip(1).Should().BeEquivalentTo(args);
        }

        [Fact]
        public async Task ExpandArguments_Escape()
        {
            var args = new[] { "Alice", "Karen", "Program Files", @"C:\Program Files (x86)\Microsoft Visual Studio", "\"\\'|<>" };
            var procTask = new ProcessTask($"{_fixture.WriteCommandLineArgs.ExecutablePath} {args}");
            var result = await procTask;

            // NOTE: We need skip the first line which is path of the command.
            result.AsLines(trimEnd: true).Skip(1).Should().BeEquivalentTo(args);
        }

        [Fact]
        public async Task ExitCode()
        {
            var procTask = new ProcessTask($"{_fixture.ExitCodeNonZero.ExecutablePath}");
            (await procTask.ExitCode).Should().Be(456);
        }

        [Fact]
        public async Task ExitCode_ThrowIfNonZero()
        {
            var procTask = new ProcessTask($"{_fixture.ExitCodeNonZero.ExecutablePath}");
            await Assert.ThrowsAsync<ProcessTaskException>(async () => await procTask);
        }

        [Fact]
        public async Task ProcessOutput_StandardInputPassThroughText()
        {
            var memStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, コンニチハ!\nABCDEFG"));
            var procTask = new ProcessTask(memStream, $"{_fixture.StandardInputPassThroughText.ExecutablePath}");
            var result = await procTask;

            result.Output.TrimEnd().Should().Be("Hello, コンニチハ!\nABCDEFG");
        }

        [Fact]
        public async Task ProcessOutput_StandardInputOutputCombined()
        {
            var procTask = new ProcessTask($"{_fixture.EchoOutAndErrorArgs.ExecutablePath} Arg0 Arg1 Arg2 Arg3");
            var result = await procTask;

            result.Output.TrimEnd().Should().Be(string.Join(Environment.NewLine, "[Arg0]", "[Arg2]"));
            result.Error.TrimEnd().Should().Be(string.Join(Environment.NewLine, "[Arg1]", "[Arg3]"));
            result.Combined.TrimEnd().Should().Be(string.Join(Environment.NewLine, "[Arg0]", "[Arg1]", "[Arg2]", "[Arg3]"));
        }


        [Fact]
        public async Task ProcessOutput_StandardInputPassThroughBinary()
        {
            var memStream = new MemoryStream(Encoding.Unicode.GetBytes("Hello, コンニチハ!\nABCDEFG"));
            var procTask = new ProcessTask(memStream, $"{_fixture.StandardInputPassThroughBinary.ExecutablePath}");
            var result = await procTask;

            result.OutputBinary.ToArray().Should().BeEquivalentTo(memStream.ToArray());
        }

        [Fact]
        public async Task Pipe_StandardInputPassThroughBinary()
        {
            var data = Encoding.Unicode.GetBytes("Hello, コンニチハ!\nABCDEFG");
            var memStream = new MemoryStream(data);
            var procTask = new ProcessTask(memStream, $"{_fixture.StandardInputPassThroughBinary.ExecutablePath}");
            var destStream = new MemoryStream();
            var result = await procTask.Pipe(destStream);

            result.ExitCode.Should().Be(0);
            result.OutputBinary.Length.Should().Be(0);
            destStream.ToArray().Should().BeEquivalentTo(data);
        }

        [Fact]
        public async Task Pipe_CloseDestFirst()
        {
            var srcTask = new ProcessTask($"{_fixture.WriteSleepWriteExit.ExecutablePath}");
            var destTask = new ProcessTask($"{_fixture.ReadOnce.ExecutablePath}");

            await (srcTask | destTask);
        }

        [Fact]
        public async Task Pipe_CloseSrcFirst()
        {
            var srcTask = new ProcessTask($"{_fixture.HelloWorld.ExecutablePath}");
            var destTask = new ProcessTask($"{_fixture.ReadAllLines.ExecutablePath}");

            await (srcTask | destTask);
        }

        [Fact]
        public async Task Pipe_ExitCode_NonZero()
        {
            var srcTask = new ProcessTask($"{_fixture.ExitCodeNonZero.ExecutablePath}");
            var destTask = new ProcessTask($"{_fixture.ReadAllLines.ExecutablePath}");

            await Assert.ThrowsAsync<ProcessTaskException>(async () => await (srcTask | destTask));
        }

        [Fact]
        public async Task Pipe_ExitCode_NonZero_NoThrow()
        {
            var srcTask = new ProcessTask($"{_fixture.ExitCodeNonZero.ExecutablePath}");
            var destTask = new ProcessTask($"{_fixture.ReadAllLines.ExecutablePath}");

            await (srcTask.NoThrow() | destTask);
        }

        [Fact]
        public async Task WorkingDirectory()
        {
            {
                var currentDirectory = Environment.CurrentDirectory;
                var output = await new ProcessTask($"{_fixture.WriteCurrentDirectory.ExecutablePath}");
                output.ToString().Trim().Should().Be(currentDirectory);
            }
            {
                var currentDirectory = Environment.CurrentDirectory;
                var workingDirectory = Path.GetFullPath(Path.Combine(currentDirectory, ".."));
                var output = await new ProcessTask($"{_fixture.WriteCurrentDirectory.ExecutablePath}", ProcessTaskOptions.Default.WithWorkingDirectory(workingDirectory));
                output.ToString().Trim().Should().Be(workingDirectory);
            }
        }
    }
}
