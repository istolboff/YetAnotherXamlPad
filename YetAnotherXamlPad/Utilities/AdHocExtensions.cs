using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace YetAnotherXamlPad.Utilities
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

        public static void ReplaceAllWithRange<T>(this ICollection<T> @this, IEnumerable<T> range)
        {
            @this.Clear();
            @this.AddRange(range);
        }

        private static void AddRange<T>(this ICollection<T> @this, IEnumerable<T> range)
        { 
            if (range == null)
            {
                return;
            }

            if (@this is List<T> list)
            {
                list.AddRange(range);
            }
            else
            {
                foreach (var value in range)
                {
                    @this.Add(value);
                }
            }
        }
    }
}
