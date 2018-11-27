using System.Collections.Generic;

namespace YetAnotherXamlPad.Utilities
{
    internal static class Make
    {
        public static KeyValuePair<TKey, TValue> Pair<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }
}
