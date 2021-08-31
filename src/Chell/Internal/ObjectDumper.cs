using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Chell.Internal
{
    internal static class ObjectDumper
    {
        public static T Dump<T>(T obj)
            => DumpMethodCache<T>.Method(obj);

        private static class DumpMethodCache
        {
            public static MethodInfo? LINQPadDumpMethod { get; }

            static DumpMethodCache()
            {
                var linqPadExtensionsType = Type.GetType("LINQPad.Extensions, LINQPad.Runtime");
                if (linqPadExtensionsType != null)
                {
                    LINQPadDumpMethod = linqPadExtensionsType.GetMethods()
                        .FirstOrDefault(x => x.Name == "Dump" && x.GetParameters().Length == 1);
                }
            }
        }

        private static class DumpMethodCache<T>
        {
            public static Func<T, T> Method { get; }

            static DumpMethodCache()
            {
                if (DumpMethodCache.LINQPadDumpMethod != null)
                {
                    var closedDumpMethod = DumpMethodCache.LINQPadDumpMethod.MakeGenericMethod(typeof(T));
                    Method = (Func<T, T>)closedDumpMethod.CreateDelegate(typeof(Func<T, T>));
                }
                else
                {
                    Method = x =>
                    {
                        Console.WriteLine(JsonSerializer.Serialize(x, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true }));
                        return x;
                    };
                }
            }
        }
    }
}
