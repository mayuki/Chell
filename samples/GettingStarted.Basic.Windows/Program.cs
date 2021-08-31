using System;
using System.Linq;
using static Chell.Exports;

// Starts a process.
// The array will be expanded and the elements will be escaped
var dirs = new[] { @"C:\Windows\Microsoft.NET", @"C:\Program Files" };
var results = await Run($"dir {dirs}"); // 

// Enumerates the results to retrieve the standard output line by line.
foreach (var line in results.Where(x => x.Contains("Windows")))
{
    Echo($"Result> {line}");
}
Echo();

// Built-in Variables
Echo((Chalk.Green + "ExecutableName: ") + string.Join(' ', ExecutableName));
Echo((Chalk.Green + "ExecutableDirectory: ") + string.Join(' ', ExecutableDirectory));
Echo((Chalk.Green + "Arguments: ") + string.Join(' ', Arguments));
Echo((Chalk.Green + "CurrentDirectory: ") + string.Join(' ', CurrentDirectory));
Echo();

// Environment Variables
Echo((Chalk.Green + "Env.Vars[\"PATH\"]: ") + Env.Vars["PATH"]);
Echo();

// Standard Input/Error as Stream + Utility methods.
StdOut.WriteLine("Hello World!");
StdErr.WriteLine("Hello World! (Error)");
Echo();

// Get the data from network and pipe it to the process
await (await FetchByteArrayAsync("http://www.example.com/") | Run("findstr title"));

Exit(1);
