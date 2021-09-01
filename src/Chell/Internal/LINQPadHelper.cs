using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
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
            Debug.Assert(RunningOnLINQPad);

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

        public static void WriteRawHtml(string html)
        {
            Debug.Assert(RunningOnLINQPad);

            var t = Type.GetType("LINQPad.Util, LINQPad.Runtime");
            if (t != null)
            {
                var obj = t.InvokeMember("RawHtml", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new[] {html});
                ObjectDumper.Dump(obj);
            }
        }
    }
}
