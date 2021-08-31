using System;
using System.Threading;

namespace Chell.Internal
{
    internal class StandardInput
    {
        private static readonly Lazy<StreamPipe> _pipe;
        internal static StreamPipe Pipe => _pipe.Value;

        static StandardInput()
        {
            _pipe = new Lazy<StreamPipe>(() => new StreamPipe(Console.OpenStandardInput()), LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}