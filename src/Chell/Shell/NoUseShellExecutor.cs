using System.Text;
using System.Text.RegularExpressions;
using Chell.Internal;

namespace Chell.Shell
{
    public class NoUseShellExecutor : IShellExecutor
    {
        public Encoding Encoding => Encoding.UTF8;

        public (string Command, string Arguments) GetCommandAndArguments(string commandLine)
        {
            return CommandLineHelper.Parse(commandLine);
        }

        public string Escape(string value)
            => Regex.IsMatch(value, "^[a-zA-Z0-9_.-/]+$")
                ? value
                : $"\"{value.Replace("`", "``").Replace("\"", "`\"")}\"";
    }
}