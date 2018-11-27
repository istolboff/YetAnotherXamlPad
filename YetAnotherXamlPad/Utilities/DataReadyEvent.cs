using System.Threading;

namespace YetAnotherXamlPad.Utilities
{
    internal sealed class DataReadyEvent<T> 
    {
        public void Set(T data)
        {
            _data = data;
            _event.Set();
        }

        public T Wait()
        {
            _event.WaitOne();
            var result = _data;
            _data = default;
            return result;
        }

        private T _data;

        private readonly EventWaitHandle _event = new AutoResetEvent(initialState: false);
    }
}
