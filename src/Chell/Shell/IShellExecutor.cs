using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chell.Shell
{
    public interface IShellExecutor
    {
        Encoding Encoding { get; }
        (string Command, string Arguments) GetCommandAndArguments(string commandLine);
        string Escape(string value);
    }
}
