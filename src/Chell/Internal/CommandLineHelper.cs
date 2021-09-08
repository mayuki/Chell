using System;
using System.Collections;
using System.Linq;
using Chell.IO;
using Chell.Shell;
using Kokuban;

// ReSharper disable CoVariantArrayConversion

namespace Chell.Internal
{
    internal class CommandLineHelper
    {
        public static void WriteCommandLineToConsole(IConsoleProvider console, string commandLine, ChellVerbosity? verbosity = default)
        {
            verbosity ??= ChellEnvironment.Current.Verbosity;
            if (verbosity.Value.HasFlag(ChellVerbosity.CommandLine))
            {
                var parts = commandLine.Split(' ', 2);
                if (LINQPadHelper.RunningOnLINQPad)
                {
                    LINQPadHelper.WriteRawHtml(
                        $"<span style=\"font-weight:bold;\">$ <span style=\"color:green\">{EscapeHtml(parts[0])}</span>{EscapeHtml((parts.Length > 1 ? " " + parts[1] : string.Empty))}</span>");
                }
                else
                {
                    console.Out.WriteLine("$ " + (Chalk.BrightGreen + parts[0]) + (parts.Length > 1 ? " " + parts[1] : string.Empty));
                }
            }

            static string EscapeHtml(string s)
                => s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&gt;");
        }

        public static string Expand(FormattableString commandLine, IShellExecutor shellExecutor)
        {
            return string.Format(commandLine.Format.Trim(), commandLine.GetArguments().Select(x =>
            {
                return x switch
                {
                    ProcessOutput procOutput => shellExecutor.Escape(procOutput.Output.TrimEnd('\n')),
                    string s => shellExecutor.Escape(s),
                    IEnumerable enumerable => string.Join(" ", enumerable.OfType<object>().Select(y => shellExecutor.Escape(y.ToString() ?? string.Empty))),
                    null => string.Empty,
                    _ => shellExecutor.Escape(x.ToString() ?? string.Empty),
                };
            }).ToArray());
        }

        public static (string Command, string Arguments) Parse(FormattableString commandLine)
        {
            var (command, argumentsFormat) = Parse(commandLine.Format.Trim());
            return (command, string.Format(argumentsFormat, commandLine.GetArguments().Select(x =>
            {
                if (x is IEnumerable enumerable)
                {
                    return string.Join(" ", enumerable.OfType<object>().Select(y => Escape(y.ToString() ?? string.Empty)));
                }
                return Escape(x?.ToString() ?? string.Empty);
            }).ToArray()));
        }

        public static (string Command, string Arguments) Parse(string commandLine)
        {
            if (commandLine.StartsWith("\""))
            {
                var pos = commandLine.IndexOf('"', 1);
                if (pos == -1)
                {
                    throw new InvalidOperationException("Invalid Command");
                }
                return (Command: commandLine.Substring(1, pos), Arguments: commandLine.Substring(pos));
            }
            else
            {
                var parts = commandLine.Split(' ', 2);
                return (Command: parts[0], Arguments: parts.Length == 1 ? string.Empty : parts[1]);
            }
        }

        public static string Escape(string v)
            => $"\"{v.Replace("`", "``").Replace("\"", "`\"")}\"";
    }
}
