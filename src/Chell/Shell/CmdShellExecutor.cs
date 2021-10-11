using System.Text;
using System.Text.RegularExpressions;

namespace Chell.Shell
{
    public class CmdShellExecutor : IShellExecutor
    {
        public Encoding Encoding => Encoding.UTF8;

        public (string Command, string Arguments) GetCommandAndArguments(string commandLine)
            => ("cmd", $"/c \"{commandLine}\"");

        public string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            if (Regex.IsMatch(value, "^[a-zA-Z0-9_.-/\\\\]+$"))
            {
                return value;
            }

            value = Regex.Replace(value, "([<>|&^])", "^$1");
            value = Regex.Replace(value, "(\\\\)?\"", x => x.Groups[1].Success ? "\\\\\\\"" : "\\\"");

            return $"\"{value}\"";
        }
    }
}
