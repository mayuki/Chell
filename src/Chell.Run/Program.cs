using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cocona;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Chell.Run
{
    partial class Program
    {
        static Task Main(string[] args)
            => CoconaLiteApp.RunAsync<Program>(args);

        public class RunCommandParameterSet : ICommandParameterSet
        {
            [Option("ref", new[] { 'r' }, Description = "Additional reference assembly")]
            [HasDefaultValue]
            public string[]? References { get; set; } = null;

            [Option("using", new[] { 'u' }, Description = "Additional `using` namespace")]
            [HasDefaultValue]
            public string[]? Usings { get; set; } = null;

            [Option('q')]
            [HasDefaultValue]
            public bool Silent { get; set; } = false;
        }

        [IgnoreUnknownOptions]
        [Command(Description = "Chell.Run: Run C# script instantly.")]
        public async Task RunAsync(
            RunCommandParameterSet runParams, 
            [Option('e', Description = "A one-line program that can be run instantly.")] string? eval = default,
            [Argument(Description = "The path to a script file, or arguments to pass to the script")] string[]? filenameOrArgs = default
        )
        {
            var fileName = filenameOrArgs is {Length: > 0} ? filenameOrArgs[0] : null;

            // -e ".." or --eval "..."
            if (!string.IsNullOrEmpty(eval))
            {
                var args = Environment.GetCommandLineArgs();
                var index = Array.FindIndex(args, x => x == "-e" || x == "--eval");
                args = args.Skip(index + 2).ToArray();
                await RunScriptAsync("<Inline>", Environment.CurrentDirectory, eval, args, runParams);
            }
            // Read a script from stdin.
            else if (fileName == "-" || (string.IsNullOrWhiteSpace(fileName) && Console.IsInputRedirected))
            {
                // Pass the strings as arguments after '-'.
                var args = Array.Empty<string>();
                if (fileName == "-")
                {
                    args = Environment.GetCommandLineArgs();
                    var index = Array.IndexOf(args, "-");
                    args = args.Skip(index + 1).ToArray();
                }

                using var reader = new StreamReader(Console.OpenStandardInput());
                var code = await reader.ReadToEndAsync();
                await RunScriptAsync("<StdIn>", Environment.CurrentDirectory, code, args, runParams);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new CommandExitedException("Error: Specify the path or pass the script from standard input.", -1);
                }
                if (!File.Exists(fileName))
                {
                    throw new CommandExitedException("Error: No such file or directory.", -1);
                }

                var ext = Path.GetExtension(fileName);
                if (ext == ".cs")
                {
                    // Run .cs script file.
                    var fullPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, fileName));

                    await RunScriptAsync(fullPath, Path.GetDirectoryName(fullPath), await File.ReadAllTextAsync(fileName, Encoding.UTF8), filenameOrArgs.Skip(1).ToArray(), runParams);
                }
                else
                {
                    throw new CommandExitedException("Error: The specified file has unknown extension. Chell accepts a filename with `.cs` extension.", -1);
                }
            }
        }

        private async Task RunScriptAsync(string fileName, string executableDirectory, string content, string[] args, RunCommandParameterSet runParams)
        {
            _ = typeof(System.Text.Json.JsonSerializer).Assembly;
            _ = typeof(Chell.ChellEnvironment).Assembly;
            _ = typeof(Cocona.CoconaLiteApp).Assembly;

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Distinct()
                .GroupBy(x => x)
                .Select(x => x.Last())
                .Select(x => MetadataReference.CreateFromFile(x.Location));
            var usings = new[]
            {
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Diagnostics",
                "System.IO",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Linq",
                "System.Threading",
                "System.Threading.Tasks",
                "Chell",
                "Chell.Extensions",

                // using static
                "Chell.Exports"
            }.AsEnumerable();

            var scriptOptions = ScriptOptions.Default
                .AddImports(usings.Concat(runParams.Usings ?? Array.Empty<string>()))
                .AddReferences(references)
                .AddReferences(runParams.References ?? Array.Empty<string>());

            try
            {
                if (runParams.Silent)
                {
                    ChellEnvironment.Current.Verbosity = ChellVerbosity.Silent;
                }

                ChellEnvironment.Current.SetCommandLineArgs(fileName, Path.GetFileName(fileName), executableDirectory, args);
                var script = await CSharpScript.RunAsync(content, scriptOptions);
            }
            catch (CompilationErrorException e)
            {
                Console.Error.WriteLine($"{fileName}{e.Message}");
            }
            catch (ProcessTaskException e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }
    }
}
