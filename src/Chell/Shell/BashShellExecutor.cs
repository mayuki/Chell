using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Chell.Internal;

namespace Chell.Shell
{
    public class BashShellExecutor : IShellExecutor
    {
        public static string? AutoDetectedPath { get; set; }

        public string? Path { get; set; } = AutoDetectedPath;

        public Encoding Encoding => Encoding.UTF8;

        public string Prefix { get; set; }

        public (string Command, string Arguments) GetCommandAndArguments(string commandLine)
            => (Path ?? throw new FileNotFoundException("Bash is not found in the PATH."), $"-c \"{Prefix}{commandLine}\"");

        // https://unix.stackexchange.com/questions/187651/how-to-echo-single-quote-when-using-single-quote-to-wrap-special-characters-in
        public string Escape(string value)
            => Regex.IsMatch(value, "^[a-zA-Z0-9_.-/]+$")
                ? value
                : $"$'{value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"")}'";

        public BashShellExecutor(string? prefix = null)
        {
            Prefix = prefix ?? "set -euo pipefail;";
        }

        static BashShellExecutor()
        {
            if (Which.TryGetPath("bash", out var bashPath))
            {
                AutoDetectedPath = bashPath;
            }
            else
            {
                AutoDetectedPath = null;
            }
        }
    }
}