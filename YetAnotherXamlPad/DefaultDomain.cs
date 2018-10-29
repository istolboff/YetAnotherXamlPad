using System;
using System.Windows;
using System.Windows.Threading;

namespace YetAnotherXamlPad
{
    internal sealed class DefaultDomain : MarshalByRefObject
    {
        public DefaultDomain(Application defaultDomainApplication)
        {
            Application = defaultDomainApplication;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public Application Application { get; }

        public void RunApplication()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                Application.Run();
            }
        }

        public void InvokeOnDispatcher(Action action)
        {
            _dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        private readonly Dispatcher _dispatcher;
        private bool _isRunning;
    }
}