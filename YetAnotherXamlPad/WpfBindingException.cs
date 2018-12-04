using System;

namespace YetAnotherXamlPad
{
    internal sealed class WpfBindingException : InvalidOperationException 
    {
        public WpfBindingException(string message)
            : base(message)
        {
        }
    }
}
