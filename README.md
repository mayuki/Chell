# Chell
Write scripts with the power of C# and .NET

Chell is a library and execution tool for creating a shell script-like (bash, cmd, ...) experience in C#.

```csharp
var branch = await Run($"git branch --show-current");
await Run($"git archive {branch} -o {branch}.zip");
```

 .NET applications are great for complex tasks, but executing processes can be boring. Chell brings the experience closer to shell scripting. This library and tool is heavily influenced by [google/zx](https://github.com/google/zx).

## When should I use Chell?
- **Write a better shell scripts**: Write a complex script and use the power of .NET and C#
- **Write a multi-platform shell scripts**: As an alternative to scripts that work on multiple platforms
- **Run a process easily in your app**: As .NET library for easy handling of process launch and output
- **All developers in the project are .NET developers**: 🙃

Of course, if the shell script is already working fine and you don't have any problems, then there is no need to use Chell.

## Chell at a glance
Using Chell makes the code feel more like a script by taking advantage of C# 9's top-level statements and C# 6's `using static`.

```csharp
// Chell.Exports exposes a variety of functions and properties
using Chell;
using static Chell.Exports;
```
```csharp
// Move the current directory with Cd method
Cd("/tmp");

// Dispose the return value of Cd method to return to the previous directory
using (Cd("/usr/local/bin"))
{
    // The current directory is "/usr/local/bin".
}
// The current directory is "/" again.
```
```csharp
// You can run the process by passing a string to Run method
await Run($"ls -lFa");
```
```csharp
// An interpolated string passed to Run method will be escaped and expanded if it is an array
var newDirs = new [] { "foo", "bar", "my app", "your;app" };
await Run($"mkdir {newDirs}"); // $ mkdir foo bar "my app" "your;app"
```
```csharp
// Run method returns the result object of the command (ProcessOutput class)
var result = await Run($"ls -lFa");
// You can read stdout & stderr line by line
foreach (var line in result)
{
   Echo(line);
}

// Allows to get stdout & stderr with implicit conversion to `string`
string output = result;
// You can also get stdout as bytes (ReadOnlyMemory<byte>)
var binary = result.OutputBinary;
```
```csharp
// Provides convenient extension methods for parsing JSON.
var images = await Run($"docker image ls --format {"{{json .}}"}").SuppressConsoleOutputs();
foreach (var image in images.AsJsonLines(new { Repository = "", ID = "", Tag = ""}))
{
    Echo(image);
}
// $ docker image ls --format "{{json .}}"
// { Repository = mcr.microsoft.com/dotnet/sdk, ID = b160c8f3dbd6, Tag = 5.0 }
// { Repository = <none>, ID = 3ee645b4a3bd, Tag = <none> }
```
```csharp
// Standard input/output of process tasks can be connected by pipes
await (Run($"ls -lFa") | Run($"grep dotnet"));
// The difference with `await (Run($"ls -lFa | grep dotnet"));` is that the shell can pipe or not.

// You can also specify a Stream as input or output
// Write ffmpeg output to a Stream.
await (Run($"ffmpeg ...") | destinationStream);
// Write a Stream to ffmpeg process.
await (srcStream | Run($"ffmpeg ..."));
```

Just want to make it easy for your app to handle processes? If you don't use `Chell.Exports`, you won't get any unnecessary methods or properties, and you'll get the same functions by `new Run(...)`.

```csharp
using Chell;
var result = await new Run($"ls -lF");
```

Want to run it like a scripting language? Install [Chell.Run](#chellrun) and you can run it like a script.

```bash
% dotnet tool install -g Chell.Run

% chell -e "Echo(DateTime.Now)"
9/1/2021 0:00:00 PM

% cat <<__EOF__ > MyScript.cs
var dirs = new [] { "foo bar", "baz" };
await Run($"mkdir {dirs}");
await Run($"ls -l");
__EOF__

% chell MyScript.cs
$ mkdir "foo bar" "baz"
$ ls -l
total 8
drwxr-xr-x 2 mayuki mayuki 4096 Sep  1 00:00  baz/
drwxr-xr-x 2 mayuki mayuki 4096 Sep  1 00:00 'foo bar'/
```


## Features
- Stream and Process Pipes
- Automatic shell character escaping and array expansion
- Provide utilities and shortcuts useful for scripting.
- Simple shell script-like execution tools
- Multi-platform (Windows, Linux, macOS)
- LINQPad friendly

## Install
```
dotnet package add Chell
```
### Requirements
.NET Standard 2.1, .NET 5 or higher

## Chell.Exports
Chell.Exports class exposes a variety of utilities and shortcuts to make writing feel like shell scripting. It is recommended to include this class in your scripts with `static using`.

### Methods (Functions)
#### `Run`
`Run` method starts the process execution and returns the `ProcessTask`.

```csharp
await Run($"ls -lF");

// The followings are equivalent to calling Run method
await (Run)$"ls -lF";
await new Run($"ls -lF");
```

A process will be launched asynchronously and can wait for completion by `await`. And you can `await` to get a `ProcessOutput` object with its output.

If the exit code of the process returns non-zero, it will throw an exception. To suppress this exception, see `NoThrow`.

An interpolated string passed to Run method will be escaped and expanded if it is an array.

```csharp
var newDirs = new [] { "foo", "bar", "my app", "your;app" };
await Run($"mkdir {newDirs}"); // equivalent to `mkdir foo bar "my app" "your;app"`
```

You can also pass the execution options (`ProcessTaskOptions`) to Run method.

```csharp
await Run($"ping -t localhost", new ProcessTaskOptions(
    workingDirectory: @"C:\Windows",
    timeout: TimeSpan.FromSeconds(1)
));
```

#### `Cd(string)`
```csharp
Cd("/usr/local/bin"); // equivalent to `Environment.CurrentDirectory = "/usr/local/bin";`
```

Dispose the return value of `Cd` method to return to the previous directory.

```csharp
Cd("/"); // The current directory is "/".
using (Cd("/usr/local/bin"))
{
    // The current directory is "/usr/local/bin".
}
// The current directory is "/" again.
```

#### `Dump<T>(T value)`
`Dump` method formats the object and write it to the coneole.

```csharp
Dump(new { Foo = 123, Bar = "Baz" }); // => "{ Foo = 123, Bar = "Baz" }"
```

#### `Which(string name)`, `TryWhich(string name, out string path)`
`Which` method returns a path of the specified command.

```csharp
var dotnetPath = Which("dotnet");
await Run($"{dotnetPath} run");
```

#### `Echo(object message = default)`
`Echo` method is equivalent to Console.WriteLine.

```csharp
Echo("Hello World!"); // equivalent to Console.WriteLine("Hello World!");
```

#### `Sleep(int duration)`, `Sleep(TimeSpan duration)`
`Sleep` method returns a Task that waits for the specified duration.

```csharp
await Sleep(10); // Sleep for 10 seconds.
```

#### `Exit(int exitCode)`
`Exit` method terminates the application with an exit code.

```csharp
Exit(1);
```

### Properties

#### `Env.Vars`

`Env.Vars` property exposes the environment variables as `IDictionary<string, string>`.

```csharp
Env.Vars["PATH"] = Env.Vars["PATH"] + ":/path/to/";
```

#### `Env.IsWindows`
`Env.IsWindows` property returns whether the running operating system is Windows or not. If it returns `false`, the operating system is Linux or macOS.

```csharp
if (Env.IsWindows) { /* Something to do for Windows */ }
```

#### `Env.Shell`
Specify explicitly which shell to use, or set to not use a shell.

```csharp
Env.Shell.UseBash();
Env.Shell.NoUseShell();
Env.Shell.UseCmd();
```

#### `Env.Verbosity`
`Env.Verbosity` property is output level when executing a command.

- `Verbosity.All`: Displays both the command line and the output of the command
- `Verbosity.CommandLine`: Displays the command line
- `Verbosity.Output`: Displays the output of the command
- `Verbosity.Silent`: No display

#### `Env.ProcessTimeout`

Sets the timeout for running the process. The default value is `0` (disabled).

```csharp
Env.ProcessTimeout = TimeSpan.FromSecond(1);

// OperationCanceledException will be thrown after 1s.
await Run($"ping -t localhost");
``

#### `Arguments`
Gets the arguments passed to the current application.

```csharp
// $ myapp foo bar baz => new [] { "foo", "bar", "baz" };
foreach (var arg in Arguments) { /* ... */ }
```
#### `CurrentDirectory`, `ExecutableDirectory`, `ExecutableName`, `ExecutablePath`
Get the current directory and application directory/name/path.

```csharp
// C:\> cd C:\Users\Alice
// C:\Users\Alice> Downloads\MyApp.exe

Echo(CurrentDirectory); // C:\Users\Alice
Echo(ExecutableDirectory); // C:\Users\Alice\Downloads
Echo(ExecutableName); // MyApp.exe
Echo(ExecutablePath); // C:\Users\Alice\Downloads\MyApp.exe
```
#### `StdIn`, `StdOut`, `StdErr`
Provides a wrapper with methods useful for reading and writing to the standard input/output/error streams.

```csharp
// Reads data from standard input.
await StdIn.ReadToEndAsync();

// Writes data to standard output or error.
StdOut.WriteLine("FooBar");
StdErr.WriteLine("Oops!");
```

## ProcessTask class
This class manages the execution of processes created by `Run`.

**NOTE:** `Run` method returns a instance of `Run` class that inherits from `ProcessTask` class.

### `Pipe`
Connects the standard output of the process to another `ProcessTask` or `Stream`.

```csharp
await (Run($"ls -lF") | Run($"grep .dll"));

// The followings are equivalent to using '|'.
var procTask1 = Run($"ls -lF");
var procTask2 = Run($"grep .dll");
procTask1.Pipe(procTask2);
```

A `Stream` can also be passed to Pipe. If the end of the pipe is a `Stream`, it will not be written to `ProcessOutput`.

```csharp
var memStream = new MemoryStream();
await Run($"ls -lF").Pipe(memStream);
```

### `ConnectStreamToStandardInput`
Connects the Stream to the standard input of the process. This method can be executed only once, before the process starts.

```csharp
await (myStream | Run($"grep .dll"));

// The followings are equivalent to using '|'.
var procTask = Run($"grep .dll");
procTask.ConnectStreamToStandardInput(myStream);
```

### `NoThrow`
Suppresses exception throwing when the exit code is non-zero.

```csharp
await Run($"AppReturnsExitCodeNonZero").NoThrow();
```

### `SuppressConsoleOutputs`
Suppresses the writing of command execution results to the standard output.

```csharp
// equivalent to "Env.Verbosity = Verbosity.Silent" or pipe to null.
await Run($"ls -lF").SuppressConsoleOutputs();

```
### `ExitCode`
Returns a `Task` to get the exit code of the process. This is equivalent to waiting for a `ProcessTask` with `NoThrow`.

```csharp
var proc = Run($"ls -lF");
if (await proc.ExitCode != 0)
{
    ...
}

// equivalent to `(await Run($"ls -lF").NoThrow()).ExitCode`
```

## ProcessOutput class
The class that contains the results of the process execution.

### `Combined`, `CombinedBinary`
Gets the combined standard output and error as a string or byte array.

### `Output`, `OutputBinary`
Gets the standard output as a string or byte array.

### `Error`, `ErrorBinary`
Gets the standard error as a string or byte array.

### `AsLines(bool trimEnd = false)`, `GetEnumerable()`
Gets the combined standard output and error as a per-line `IEnumerable<string>`.

```csharp
// equivalent to `foreach (var line in procOutput.AsLines())`
foreach (var line in procOutput) { ... }
```

### `ToString()`
The method equivalent to `Combined` property.

### `ExitCode`
Gets the exit code of the process.

## Utilities and shortcuts
Chell.Exports class also exposes a variety of useful utilities and shortcuts to libraries.

### `Prompt`
Prompts the user for input and gets it.

```csharp
var name = await Prompt("What's your name? ");
```

### `Chalk`: Kokuban: Terminal string styling
Provides a shortcut to [mayuki/Kokuban](https://github.com/mayuki/Kokuban). You can easily style the text on the terminal.

```csharp
// "Error: " will be colored.
Echo((Chalk.Red + "Error: ") + "Something went wrong.");
```

### `Glob`
Provides a shortcut to `Microsoft.Extensions.FileSystemGlobbing`.

- `Glob(params string[] patterns)`
- `Glob(string baseDir, string[] patterns)`

```csharp
// Glob patterns starting with '!' will be treated as excludes.
foreach (var path in Glob("**/*.cs", "!**/*.vb"))
{
  ...
}
```

### JSON serialize/deserialize (System.Text.Json)
Provides shortcuts to `System.Text.Json`.

- `ToJson<T>(T obj)`
```csharp
var obj = new { Name = "Alice", Age = 18 };
var json = ToJson(obj);
Echo(json); // {"Name":"Alice","Age":18}
```

- `FromJson<T>(string json)`
- `FromJson<T>(string json, T shape)`
```csharp
var json = "{ \"foo\": 123 }";
var obj = FromJson(json, new { Foo = 0 });
Dump(obj); // { Foo = 123 }
```

- `AsJson`
- `AsJsonLines`
```csharp
using Chell;
var output = await Run($"docker image ls --format {"{{json .}}"}");
foreach (var image in output.AsJsonLines(new { Repository = "", ID = "", Tag = ""}))
{
    // ...
}
```
```csharp
using Chell;
var output = await Run($"kubectl version --client -o json");
var obj = output.AsJson(new { clientVersion = new { major = "", minor = "", gitVersion = "" } });
Echo(obj); // { clientVersion = { major = 1, minor = 21, gitVersion = v1.21.2 } }
```

### HTTP acccess (System.Net.Http)
Provides shortcuts to `System.Net.Http.HttpClient`.

- `FetchAsync`
- `FetchByteArrayAsync`
- `FetchStreamAsync`
- `FetchStringAsync`

## Chell as a Library
Chell can also be used as a utility library to run processes.

If you don't use `Chell.Exports`, you won't get any unnecessary methods or properties, and you can use `Run` and `ChellEnvironment`, `Exports` class.

```csharp
using Chell;

var results = await new Run($"ls -lF");

// ChellEnvironment.Current is equivalent to `Env` on `Chell.Exports`.
Console.WriteLine(ChellEnvironment.Current.ExecutablePath);
Console.WriteLine(ChellEnvironment.Current.ExecutableName);
Console.WriteLine(ChellEnvironment.Current.Arguments);
Console.WriteLine(ChellEnvironment.Current.Vars["PATH"]);
```

## Chell.Run
Chell.Run executes the input source code in an environment where Chell and some libraries are available.

It does not perform any NuGet package resolution, so if you need to handle such complexities, we recommend creating a typical C# project.

```
$ dotnet tool install -g Chell.Run
```
```bash
$ chell -e "Echo(123);"
$ chell <<EOF
var result = await Run($"ls -lF");
Echo("Hello World!");
EOF
```

Chell.Run implicitly has several namespace usings and library references that can be used out-of-the-box.

Implicitly specified `using`s:

- System
- System.Collections
- System.Collections.Generic
- System.Diagnostics
- System.IO
- System.Text
- System.Text.RegularExpressions
- System.Linq
- System.Threading
- System.Threading.Tasks
- Chell
- Chell.Extensions
- Chell.Exports (using static)

Additional referenced libraries:
- Mono.Options
- Sharprompt
- Cocona.Lite

## Chell ❤ LINQPad
LINQPad is often used to do tasks in place of shell scripts. Chell can help you in this case as well.

Chell knows about LINQPad, so `Dump` method is replaced by LINQPad's `Dump`, and standard output and errors are displayed in the results without problems.

![image](https://user-images.githubusercontent.com/9012/131680759-6f138ceb-457b-489f-b17d-e36688f13346.png)

## License

```
MIT License

Copyright © Mayuki Sawatari <mayuki@misuzilla.org>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```