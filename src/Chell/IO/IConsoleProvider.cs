using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chell.IO
{
    public interface IConsoleProvider
    {
        Stream OpenStandardInput();
        Stream OpenStandardOutput();
        Stream OpenStandardError();
        Encoding InputEncoding { get; }
        Encoding OutputEncoding { get; }
        bool IsInputRedirected { get; }
        bool IsOutputRedirected { get; }
        bool IsErrorRedirected { get; }

        TextWriter Out { get; }
        TextWriter Error { get; }
    }
}
