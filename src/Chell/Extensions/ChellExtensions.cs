using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Chell.Internal;

namespace Chell.Extensions
{
    public static class ChellExtensions
    {
        /// <summary>
        /// Writes a object details to the output.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Dump<T>(this T value)
        {
            return ObjectDumper.Dump(value);
        }

        /// <summary>
        /// Writes a object details to the output.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task<T> Dump<T>(this Task<T> task)
        {
            var result = await task.ConfigureAwait(false);
            ObjectDumper.Dump(result);
            return result;
        }
    }
}
