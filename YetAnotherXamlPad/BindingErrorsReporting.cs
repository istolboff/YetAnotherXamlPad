using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;
using YetAnotherXamlPad.Utilities;

namespace YetAnotherXamlPad
{
    internal static class BindingErrorsReporting
    {
        public static void Setup()
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(BindingErrorTraceListener.Instance);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical | SourceLevels.Error | SourceLevels.Warning;
        }

        public static event Action<Exception> ErrorOccured
        {
            add => BindingErrorTraceListener.Instance.ErrorOccuredCore += value; 
            remove => BindingErrorTraceListener.Instance.ErrorOccuredCore -= value; 
        }

        private sealed class BindingErrorTraceListener : DefaultTraceListener
        {
            public override void Write(string message)
            {
                _messageBuilder.Append(message);
            }

            public override void WriteLine(string message)
            {
                _messageBuilder.Append(message);
                var bindingErrorMessage = _messageBuilder.ToString();
                _messageBuilder.Clear();
                _dispatcher.Post(() => ErrorOccuredCore(new WpfBindingException(bindingErrorMessage)));
            }

            public event Action<Exception> ErrorOccuredCore = Do.Nothing;

            private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
            private readonly StringBuilder _messageBuilder = new StringBuilder();

            public static readonly BindingErrorTraceListener Instance = new BindingErrorTraceListener { TraceOutputOptions = TraceOptions.None,  };
        }
    }
}
