using System.Diagnostics;
using System.Text;

namespace YetAnotherXamlPad
{
    internal static class BindingErrorsReporting
    {
        public static void Setup()
        {
            PresentationTraceSources.DataBindingSource.Listeners.Add(BindingErrorTraceListener.Instance);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        }

        public static void Toggle(bool switchOn)
        {
            if (!switchOn)
            {
                BindingErrorTraceListener.Instance.Flush();
            }

            BindingErrorTraceListener.Instance.SwitchedOn = switchOn;
        }

        private sealed class BindingErrorTraceListener : DefaultTraceListener
        {
            public bool SwitchedOn { get; set; }

            public override void Write(string message)
            {
                if (!SwitchedOn)
                {
                    return;
                }

                _messageBuilder.Append(message);
            }

            public override void WriteLine(string message)
            {
                if (!SwitchedOn)
                {
                    return;
                }

                _messageBuilder.Append(message);
                var bindingErrorMessage = _messageBuilder.ToString();
                _messageBuilder.Clear();
                throw new WpfBindingException(bindingErrorMessage);
            }

            private readonly StringBuilder _messageBuilder = new StringBuilder();

            public static readonly BindingErrorTraceListener Instance = new BindingErrorTraceListener { TraceOutputOptions = TraceOptions.None };
        }
    }
}
