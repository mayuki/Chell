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
