using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chell.Internal
{
    internal class LINQPadHelper
    {
        private static bool? _runningOnLINQPad;
        internal static bool RunningOnLINQPad => _runningOnLINQPad ??= Type.GetType("LINQPad.Util, LINQPad.Runtime") != null;

        public static void ConnectToTextWriter(StreamPipe streamPipe, TextWriter writer)
        {
            var pipe = new Pipe();
            var reader = new StreamReader(pipe.Reader.AsStream());
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line is null) return;
                    writer.WriteLine(line);
                }
            });
            streamPipe.Connect(pipe.Writer);
        }
    }
}
