using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace YetAnotherXamlPad
{
    internal static class AdHocExtensions
    {
        public static void Post(this Dispatcher @this, Action action)
        {
            @this.BeginInvoke(action);
        }

        public static IReadOnlyCollection<T> AsImmutable<T>(this IEnumerable<T> @this)
        {
            return (@this as IReadOnlyCollection<T>) ?? @this.ToArray();
        }
    }
}
